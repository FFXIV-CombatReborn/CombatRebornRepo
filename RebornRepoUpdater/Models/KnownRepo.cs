using Newtonsoft.Json;
using Octokit;
using RebornRepoUpdater.Models.Dalamud;

namespace RebornRepoUpdater.Models;

public class KnownRepo(string projectName, string internalName, string organizationName = "FFXIV-CombatReborn",
	string manifestFilePath = "")
{
	public string ProjectName { get; set; } = projectName;
	public string OrganizationName { get; set; } = organizationName;
	public string ManifestFilePath { get; set; } = string.IsNullOrWhiteSpace(manifestFilePath) ? $"manifest.json" : manifestFilePath;
	public string InternalName { get; set; } = internalName;
	public PluginManifest? Manifest { get; private set; } = null;

    public async Task Update(GitHubClient client)
    {
        Console.WriteLine($"Getting current manifest for {ProjectName}...");

        await EnsureRateLimitAsync(client);

        var jsonContent =
            await client.Repository.Content.GetAllContents(OrganizationName, ProjectName, ManifestFilePath);
        var json = jsonContent[0].Content;
        var manifest = JsonConvert.DeserializeObject<PluginManifest>(json);
        Manifest = manifest;

        if (Manifest != null)
            await GetReleaseInfo(client);
    }

	private async Task GetReleaseInfo(GitHubClient client)
	{
		if (Manifest == null)
			return;

		Console.WriteLine($"Getting releases for {ProjectName}...");

		await EnsureRateLimitAsync(client);

		var releases = await client.Repository.Release.GetAll(OrganizationName, ProjectName, new ApiOptions { PageSize = 100, PageCount = 1 });
		var releaseList = new List<Release>(releases);

		if (releaseList.Count == 0)
		{
			// Fall back to GetLatest() to attempt to retrieve at least the latest stable release
			Console.WriteLine($"GetAll returned empty for {ProjectName}, attempting GetLatest fallback...");
			try
			{
				await EnsureRateLimitAsync(client);
				var latest = await client.Repository.Release.GetLatest(OrganizationName, ProjectName);
				if (latest != null)
					releaseList.Add(latest);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetLatest also failed for {ProjectName}: {ex.Message}");
			}
		}

		if (releaseList.Count == 0)
			throw new Exception($"GitHub returned an empty release list for {ProjectName}. Skipping update to avoid overwriting existing data.");

		releaseList.Sort((a, b) => DateTimeOffset.Compare(
			b.PublishedAt ?? DateTimeOffset.MinValue,
			a.PublishedAt ?? DateTimeOffset.MinValue));

		Release? latestRelease = null;
		foreach (var r in releaseList)
		{
			if (!r.Prerelease) { latestRelease = r; break; }
		}

		Release? latestTestingRelease = null;
		foreach (var r in releaseList)
		{
			if (r.Prerelease) { latestTestingRelease = r; break; }
		}

		try
		{
			if (latestRelease != null)
			{
				Console.WriteLine($"Latest stable release: {latestRelease.Name}");

				ReleaseAsset? stableZip = null;
				foreach (var a in latestRelease.Assets)
				{
					if (a.Name.EndsWith(".zip")) { stableZip = a; break; }
				}

				if (stableZip == null)
				{
					Console.WriteLine($"WARNING: No zip asset found for stable release {latestRelease.Name}. Skipping stable release update to preserve existing data.");
				}
				else
				{
					Manifest.AssemblyVersion = latestRelease.TagName;
					var downloadLink = stableZip.BrowserDownloadUrl;
					Manifest.DownloadLinkInstall = downloadLink;
					Manifest.DownloadLinkUpdate = downloadLink;
					Manifest.Changelog = latestRelease.Body;
				}
			}
			else
			{
				Console.WriteLine("No stable release found.");
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get releases for {ProjectName}", ex);
		}

		try
		{
			if (latestTestingRelease != null)
			{
				Console.WriteLine($"Latest testing release: {latestTestingRelease.Name}");

				ReleaseAsset? testingZip = null;
				foreach (var a in latestTestingRelease.Assets)
				{
					if (a.Name.EndsWith(".zip")) { testingZip = a; break; }
				}

				if (testingZip == null)
				{
					Console.WriteLine($"WARNING: No zip asset found for testing release {latestTestingRelease.Name}. Skipping testing release update to preserve existing data.");
				}
				else
				{
					Manifest.TestingAssemblyVersion = latestTestingRelease.TagName;
					var downloadLinkTesting = testingZip.BrowserDownloadUrl;
					Manifest.DownloadLinkTesting = downloadLinkTesting;
				}
			}
			else
			{
				Console.WriteLine("No testing release found.");
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get testing releases for {ProjectName}", ex);
		}

		Manifest.SetDownloadCount(releaseList);
        Console.WriteLine($"Download count: {Manifest.DownloadCount}");
    }

    private static async Task EnsureRateLimitAsync(GitHubClient client)
    {
        var apiInfo = await client.RateLimit.GetRateLimits();
        var rateLimit = apiInfo?.Rate;

        if (rateLimit == null)
        {
            Console.WriteLine("No rate limit information available. Proceeding with API call.");
            return;
        }

        var remaining = rateLimit.Remaining;
        var resetTime = rateLimit.Reset;

        if (remaining > 0)
        {
            Console.WriteLine($"Rate Limit OK: {remaining} requests remaining.");
            return;
        }

        var waitTime = resetTime - DateTimeOffset.UtcNow;
        if (waitTime.TotalSeconds > 0)
        {
            Console.WriteLine($"Rate limit reached. Waiting for {waitTime.TotalSeconds:N0} seconds...");
            await Task.Delay(waitTime);
        }
    }
}