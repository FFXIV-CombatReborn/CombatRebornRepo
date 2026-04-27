using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using RebornRepoUpdater.Models;
using RebornRepoUpdater.Models.Dalamud;

namespace RebornRepoUpdater;

class Program
{
	public static GitHubClient Client = new(new ProductHeaderValue("CombatRebornRepoUpdater"));

	public static List<KnownRepo> KnownRepos =
	[
		new KnownRepo("RotationSolverReborn", "RotationSolver"),
		new KnownRepo("BossModReborn", "BossModReborn"),
		new KnownRepo("ActionTimelineReborn", "ActionTimelineEx"),
		new KnownRepo("EasyZoomReborn", "EasyZoomReborn"),
		new KnownRepo("GatherBuddyReborn", "GatherbuddyReborn"),
		new KnownRepo("RebornToolbox", "RebornToolbox"),
		new KnownRepo("ZodiacBuddyReborn", "ZodiacBuddyReborn"),
		new KnownRepo("PassportCheckerReborn", "PassportCheckerReborn"),
		new KnownRepo("NoSillyReborn", "NoSillyReborn"),
        //new KnownRepo("party-finder-plugin", "PartyFinderReborn", "Party-Finder-Reborn"),
        new KnownRepo("Understudy", "Understudy", "aventurescence")
	];

	static async Task Main(string[] args)
	{
		string? filePath = null;
		string? githubToken = null;

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--path":
					filePath = args[i + 1];
					i++;
					break;
				case "--token":
					githubToken = args[i + 1];
					i++;
					break;
			}
		}

		if (filePath == null)
		{
			Console.WriteLine("File path is required.");
			return;
		}

		if (githubToken == null)
		{
			Client.Credentials = Credentials.Anonymous;
		}
		else
		{
			Client.Credentials = new Credentials(githubToken);
		}

		var manifestJson = File.ReadAllText(filePath);
		var manifests = JsonConvert.DeserializeObject<List<PluginManifest>>(manifestJson) ?? [];
		var updatedManifests = new List<PluginManifest>();
		bool fatalError = false;

		foreach (var repo in KnownRepos)
		{
			Console.WriteLine($"Processing {repo.ProjectName}");
			PluginManifest? localManifest = null;
			foreach (var m in manifests)
			{
				if (m.InternalName == repo.InternalName)
				{
					localManifest = m;
					break;
				}
			}

			try
			{
				await repo.Update(Client);

				if (repo.Manifest == null)
					throw new Exception("Manifest was still null after updating.");
				else
					updatedManifests.Add(repo.Manifest);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to update manifest for {repo.ProjectName}: {ex.Message}");
				if (localManifest != null)
				{
					Console.WriteLine($"Falling back to local manifest for {repo.ProjectName}");
					updatedManifests.Add(localManifest);
				}
				else
				{
					Console.WriteLine($"ERROR: Remote update failed and no local manifest exists for {repo.ProjectName}. Aborting to prevent data loss.");
					fatalError = true;
				}
			}
		}

		if (fatalError)
		{
			Console.WriteLine("One or more plugins had no fallback manifest. The manifest file will NOT be updated to prevent removing plugins.");
			return;
		}

		Console.WriteLine($"Found {updatedManifests.Count} manifests from both remote and local sources");

		// Convert to JSON token so we can sanitize version strings without needing to change the manifest model types.
		var token = JToken.FromObject(updatedManifests);

		// Helper to trim leading 'v'/'V' from string properties if present.
		foreach (var child in token.Children())
			if (child is JObject obj)
			{
				void TrimProp(string prop)
				{
					if (obj.TryGetValue(prop, out var val) && val.Type == JTokenType.String)
					{
						var s = val.Value<string>()!;
						if (!string.IsNullOrEmpty(s))
							obj[prop] = s.TrimStart('v', 'V');
					}
				}

				// Common property names that may contain versions — add more if your manifest uses other names.
				TrimProp("AssemblyVersion");
				TrimProp("Version");
			}

		var manifestsJson = token.ToString(Formatting.Indented);
		await File.WriteAllTextAsync(filePath, manifestsJson);
	}
}
