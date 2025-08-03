using System.Net;
using Newtonsoft.Json;
using Octokit;
using RebornRepoUpdater.Models;
using RebornRepoUpdater.Models.Dalamud;

namespace RebornRepoUpdater;

class Program
{
    public static GitHubClient Client = new GitHubClient(new ProductHeaderValue("CombatRebornRepoUpdater"));

    public static List<KnownRepo> KnownRepos =
    [
        new KnownRepo("RotationSolverReborn", "RotationSolver"),
        new KnownRepo("BossModReborn", "BossModReborn"),
        new KnownRepo("ActionTimelineReborn", "ActionTimelineEx"),
        new KnownRepo("EasyZoomReborn", "EasyZoomReborn"),
        new KnownRepo("GatherBuddyReborn", "GatherbuddyReborn"),
        new KnownRepo("RebornToolbox", "RebornToolbox"),
        new KnownRepo("ZodiacBuddyReborn", "ZodiacBuddyReborn"),
        new KnownRepo("party-finder-plugin", "PartyFinderReborn", "Party-Finder-Reborn")
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
        var manifests = JsonConvert.DeserializeObject<List<PluginManifest>>(manifestJson) ?? new List<PluginManifest>();
        var updatedManifests = new List<PluginManifest>();

        foreach (var repo in KnownRepos)
        {
            Console.WriteLine($"Processing {repo.ProjectName}");
            var localManifest = manifests.FirstOrDefault(m => m.InternalName == repo.InternalName);

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
                    Console.WriteLine($"Local manifest for {repo.ProjectName} does not exist, PROJECT WILL NOT BE PART OF PLUGINMASTER DATA.");
                }
            }
        }

        Console.WriteLine($"Found {updatedManifests.Count} manifests from both remote and local sources");
        var manifestsJson = JsonConvert.SerializeObject(updatedManifests, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, manifestsJson);
    }
}
