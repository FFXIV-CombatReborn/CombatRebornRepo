using System.Text;
using Newtonsoft.Json;
using Octokit;
using RebornRepoUpdater.Models.Dalamud;

namespace RebornRepoUpdater.Models;

public class KnownRepo
{
    public string ProjectName { get; set; }
    public string OrganizationName { get; set; }
    public string ManifestFilePath { get; set; }
    public string InternalName { get; set; }
    public PluginManifest? Manifest { get; private set; } = null;

    public async Task Update(GitHubClient client)
    {
        Console.WriteLine($"Getting current manifest for {ProjectName}...");

        await EnsureRateLimitAsync(client);

        var jsonContent =
            await client.Repository.Content.GetAllContents(ProjectName, OrganizationName, ManifestFilePath);
        var json = jsonContent.First().Content;
        var manifest = JsonConvert.DeserializeObject<PluginManifest>(json);
        Manifest = manifest;

        if (Manifest != null)
            await GetReleaseInfo(client);
    }

    private async Task GetReleaseInfo(GitHubClient client)
    {
        Console.WriteLine($"Getting releases for {ProjectName}...");

        await EnsureRateLimitAsync(client);

        var releases = await client.Repository.Release.GetAll(OrganizationName, ProjectName);
        var releasesOrderedByDate = releases.OrderByDescending(r => r.PublishedAt);

        var latestRelease = releasesOrderedByDate.FirstOrDefault(r => !r.Prerelease); // Latest stable release
        var latestTestingRelease = releasesOrderedByDate.FirstOrDefault(r => r.Prerelease); // Latest prerelease

        try
        {
            if (latestRelease != null)
            {
                Console.WriteLine($"Latest stable release: {latestRelease.Name}");
                Manifest.AssemblyVersion = latestRelease.TagName;
                Manifest.DownloadLinkInstall =
                    latestRelease.Assets.First(a => a.Name.EndsWith(".zip")).BrowserDownloadUrl;
                Manifest.Changelog = latestRelease.Body; // Release notes or changelog
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
                Manifest.TestingAssemblyVersion = latestTestingRelease.TagName;
                Manifest.DownloadLinkTesting =
                    latestTestingRelease.Assets.First(a => a.Name.EndsWith(".zip")).BrowserDownloadUrl;
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

        Manifest.SetDownloadCount(releasesOrderedByDate);
        Console.WriteLine($"Download count: {Manifest.DownloadCount}");
    }

    private async Task EnsureRateLimitAsync(GitHubClient client)
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



    public KnownRepo(string projectName, string internalName, string organizationName = "FFXIV-CombatReborn",
        string manifestFilePath = "")
    {
        ProjectName = projectName;
        OrganizationName = organizationName;
        InternalName = internalName;
        ManifestFilePath = string.IsNullOrWhiteSpace(manifestFilePath) ? $"manifest.json" : manifestFilePath;
    }
}