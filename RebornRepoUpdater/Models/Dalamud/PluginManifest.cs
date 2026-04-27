using Newtonsoft.Json;
using Octokit;

namespace RebornRepoUpdater.Models.Dalamud;

public class PluginManifest
{
    [JsonProperty(nameof(Author))] public string? Author { get; set; }

    [JsonProperty(nameof(Name))] public string? Name { get; set; }

    [JsonProperty(nameof(InternalName))] public string? InternalName { get; set; }

    [JsonProperty(nameof(Description))] public string? Description { get; set; }

    [JsonProperty(nameof(ApplicableVersion))] public string? ApplicableVersion { get; set; }

    [JsonProperty(nameof(DalamudApiLevel))] public int DalamudApiLevel { get; set; }

    [JsonProperty(nameof(TestingDalamudApiLevel), NullValueHandling = NullValueHandling.Ignore)]
    public int TestingDalamudApiLevel { get; set; }

    [JsonProperty(nameof(Tags), NullValueHandling = NullValueHandling.Ignore)] public List<string>? Tags { get; set; }

    [JsonProperty(nameof(CategoryTags), NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? CategoryTags { get; set; }

    [JsonProperty(nameof(LoadRequiredState))] public int LoadRequiredState { get; set; }

    [JsonProperty(nameof(LoadSync))] public bool LoadSync { get; set; }

    [JsonProperty(nameof(CanUnloadAsync))] public bool CanUnloadAsync { get; set; }

    [JsonProperty(nameof(LoadPriority))] public int LoadPriority { get; set; }

    [JsonProperty(nameof(IsTestingExclusive))] public bool IsTestingExclusive { get; set; }

    [JsonProperty(nameof(IconUrl))] public string? IconUrl { get; set; }

    [JsonProperty(nameof(Punchline))] public string? Punchline { get; set; }

	[JsonProperty(nameof(RepoUrl))] public string? RepoUrl { get; set; }

	[JsonProperty(nameof(AcceptsFeedback))] public bool AcceptsFeedback { get; set; }

    [JsonProperty(nameof(DownloadLinkInstall), NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkInstall { get; set; }

    [JsonProperty(nameof(AssemblyVersion), NullValueHandling = NullValueHandling.Ignore)]
    public string? AssemblyVersion { get; set; }

    [JsonProperty(nameof(DownloadLinkUpdate), NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkUpdate { get; set; }

    [JsonProperty(nameof(DownloadLinkTesting), NullValueHandling = NullValueHandling.Ignore)]
    public string? DownloadLinkTesting { get; set; }

    [JsonProperty(nameof(TestingAssemblyVersion), NullValueHandling = NullValueHandling.Ignore)]
    public string? TestingAssemblyVersion { get; set; }

    [JsonProperty(nameof(Changelog), NullValueHandling = NullValueHandling.Ignore)]
    public string? Changelog { get; set; }

    [JsonProperty(nameof(DownloadCount))] public int DownloadCount { get; private set; }

    public void SetDownloadCount(IEnumerable<Release> releases)
    {
        var newCount = releases.Sum(r => r.Assets.Sum(a => a.DownloadCount));
        if (newCount > DownloadCount)
            DownloadCount = newCount;
    }
}