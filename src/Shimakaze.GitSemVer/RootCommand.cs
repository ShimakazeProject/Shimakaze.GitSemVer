using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using DotMake.CommandLine;

using NuGet.Versioning;

namespace Shimakaze.GitSemVer;

[CliCommand]
public sealed partial class RootCommand
{
    [CliOption]
    public string Path { get; set; } = Environment.CurrentDirectory;

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

    private VersionInfo? GetInfo()
    {
        var gitInfo = Source switch
        {
            GitSource.Local => new LocalGitInfoProvider(Path),
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

            var tag = commitTags[0].Version;

            var labels = tag.ReleaseLabels;

            int? preReleaseNumber = null;
            if (int.TryParse(labels.LastOrDefault(), CultureInfo.InvariantCulture, out var number))
            {
                labels = labels.SkipLast(1);
                preReleaseNumber = number;
            }

            return new()
            {
                Major = tag.Major,
                Minor = tag.Minor,
                Patch = tag.Patch,
                Label = tag.IsPrerelease ? string.Join(".", labels) : null,
                PreReleaseNumber = preReleaseNumber,
                Sha = latestCommit,
                CommitDate = gitInfo.GetCommitDate(latestCommit),
                Branch = gitInfo.GetBranchName(latestCommit),
                VersionSourceSha = commit,
                Height = counter,
            };
        }

        return null;
    }

    public void Run()
    {
        var info = GetInfo()
            ?? throw new InvalidOperationException("No version information could be determined.");

        info.SetTemplates(AssemblySemVer, AssemblyFileSemVer, InformationalVersion);

        Console.WriteLine(JsonSerializer.Serialize(info, GitSemVerJsonSerializerContext.Default.VersionInfo));
    }


    private string TrimVersionPrefix(string tag) => TagPrefix.Replace(tag, string.Empty);

}
