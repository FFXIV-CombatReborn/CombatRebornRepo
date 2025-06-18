using Newtonsoft.Json;
using Octokit;

namespace RebornRepoUpdater.Models.Dalamud;

public class PluginManifest
{
    [JsonProperty("Author")] public string Author { get; set; }

    [JsonProperty("Name")] public string Name { get; set; }

    [JsonProperty("InternalName")] public string InternalName { get; set; }

    [JsonProperty("RepoUrl")] public string RepoUrl { get; set; }

    [JsonProperty("Description")] public string Description { get; set; }

    [JsonProperty("ApplicableVersion")] public string ApplicableVersion { get; set; }

    [JsonProperty("DalamudApiLevel")] public int DalamudApiLevel { get; set; }

    [JsonProperty("TestingDalamudApiLevel", NullValueHandling = NullValueHandling.Ignore)]
    public int TestingDalamudApiLevel { get; set; }

    [JsonProperty("Tags", NullValueHandling = NullValueHandling.Ignore)] public List<string> Tags { get; set; }

    [JsonProperty("CategoryTags", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> CategoryTags { get; set; }

    [JsonProperty("LoadRequiredState")] public int LoadRequiredState { get; set; }

    [JsonProperty("LoadSync")] public bool LoadSync { get; set; }

    [JsonProperty("CanUnloadAsync")] public bool CanUnloadAsync { get; set; }

    [JsonProperty("LoadPriority")] public int LoadPriority { get; set; }

    [JsonProperty("IsTestingExclusive")] public bool IsTestingExclusive { get; set; }

    [JsonProperty("IconUrl")] public string IconUrl { get; set; }

    [JsonProperty("Punchline")] public string Punchline { get; set; }

    [JsonProperty("AcceptsFeedback")] public bool AcceptsFeedback { get; set; }

    [JsonProperty("DownloadLinkInstall", NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkInstall { get; set; }

    [JsonProperty("AssemblyVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssemblyVersion { get; set; }

    [JsonProperty("DownloadLinkUpdate", NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkUpdate { get; set; }

    [JsonProperty("DownloadLinkTesting", NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkTesting { get; set; }

    [JsonProperty("TestingAssemblyVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? TestingAssemblyVersion { get; set; }

    [JsonProperty("Changelog", NullValueHandling = NullValueHandling.Ignore)]
    public string? Changelog { get; set; }

    [JsonProperty("DownloadCount")] public int DownloadCount { get; private set; }

    public void SetDownloadCount(IEnumerable<Release> releases)
    {
        DownloadCount = releases.Sum(r => r.Assets.Sum(a => a.DownloadCount));
    }
}
