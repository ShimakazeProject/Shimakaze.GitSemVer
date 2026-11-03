using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using DotMake.CommandLine;

using NuGet.Versioning;

namespace Shimakaze.GitSemVer;

[CliCommand]
public sealed partial class RootCommand
{
    [CliOption(Required = false)]
    public string Path { get; set; } = Environment.CurrentDirectory;

    [CliOption(Required = false)]
    public string? GithubOwner { get; set; } = Environment.GetEnvironmentVariable("GITHUB_OWNER");
    [CliOption(Required = false)]
    public string? GithubRepo { get; set; } = Environment.GetEnvironmentVariable("GITHUB_REPO");
    [CliOption(Required = false)]
    public string? GithubToken { get; set; } = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    [CliOption(Required = false)]
    public Uri GithubHost { get; set; } = new("https://api.github.com");

    [CliOption]
    public string Branch { get; set; } = "HEAD";

    [CliOption]
    public GitSource Source { get; set; } = GitSource.Local;

    [CliOption]
    public Regex TagPrefix { get; set; } = new("^[vV]?", RegexOptions.Compiled);

    [CliOption]
    public SemanticVersion DefaultVersion { get; set; } = SemanticVersion.Parse("0.0.0");

    [CliOption]
    public string AssemblySemVer { get; set; } = "{Major}.{Minor}.0.0";

    [CliOption]
    public string AssemblyFileSemVer { get; set; } = "{Major}.{Minor}.{Patch}.0";

    [CliOption]
    public string InformationalVersion { get; set; } = "{FullSemVer}";

    private VersionInfo GetInfo()
    {
        IGitInfoProvider gitInfo = Source switch
        {
            GitSource.Local => new LocalGitInfoProvider(Path),
            GitSource.GitHub => new GitHubGitInfoProvider(
                GithubOwner ?? throw new ArgumentException("GITHUB_OWNER is not set."),
                GithubRepo ?? throw new ArgumentException("GITHUB_REPO is not set."),
                GithubToken ?? throw new ArgumentException("GITHUB_TOKEN is not set."),
                GithubHost),
            _ => throw new NotSupportedException($"Git source '{Source}' is not supported.")
        };
        using var dispose = gitInfo as IDisposable;

        if (!gitInfo.TryGetBranchSha(Branch, out var sha))
            throw new ArgumentException($"Branch '{Branch}' not found.", nameof(Branch));

        var latestCommit = sha;

        var tags = gitInfo.Tags
            .Select(i =>
            {
                _ = SemanticVersion.TryParse(TrimVersionPrefix(i.Key), out var version);
                return (Tag: i.Key, Commit: i.Value, Version: version!);
            })
            .Where(static i => i is { Version: not null })
            .GroupBy(static i => i.Commit)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(i => i.Version)
                    .ToArray());

        int counter = 0;
        foreach (var commit in gitInfo.EnumerateFirstParentHistory(sha))
        {
            if (!tags.TryGetValue(commit, out var commitTags))
            {
                counter++;
                continue;
            }

            return CreateVersionInfo(
                commitTags[0].Version,
                latestCommit,
                gitInfo.GetCommitDate(latestCommit),
                gitInfo.GetBranchName(latestCommit),
                commit,
                counter);
        }

        return CreateVersionInfo(
            DefaultVersion,
            latestCommit,
            gitInfo.GetCommitDate(latestCommit),
            gitInfo.GetBranchName(latestCommit),
            null,
            counter);
    }

    private static VersionInfo CreateVersionInfo(SemanticVersion semver, string sha, DateTimeOffset commitDate, string? branch, string? versionSourceSha, int height)
    {
        var labels = semver.ReleaseLabels;

        int? preReleaseNumber = null;
        if (int.TryParse(labels.LastOrDefault(), CultureInfo.InvariantCulture, out var number))
        {
            labels = labels.SkipLast(1);
            preReleaseNumber = number;
        }

        return new()
        {
            Major = semver.Major,
            Minor = semver.Minor,
            Patch = semver.Patch,
            Label = semver.IsPrerelease ? string.Join(".", labels) : null,
            PreReleaseNumber = preReleaseNumber,
            Sha = sha,
            CommitDate = commitDate,
            Branch = branch,
            VersionSourceSha = versionSourceSha,
            Height = height,
        };
    }

    public void Run()
    {
        var info = GetInfo();

        info.SetTemplates(AssemblySemVer, AssemblyFileSemVer, InformationalVersion);

        Console.WriteLine(JsonSerializer.Serialize(info, GitSemVerJsonSerializerContext.Default.VersionInfo));
    }


    private string TrimVersionPrefix(string tag) => TagPrefix.Replace(tag, string.Empty);

}
