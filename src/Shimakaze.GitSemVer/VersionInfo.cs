using System.Globalization;
using System.Text;

namespace Shimakaze.GitSemVer;

public sealed class VersionInfo
{
    public required int Major { get; init; }
    public required int Minor { get; init; }
    public required int Patch { get; init; }
    public required string? Label { get; init; }
    public required int? PreReleaseNumber { get; init; }
    public required string Sha { get; init; }
    public required string? Branch { get; init; }
    public required string VersionSourceSha { get; init; }
    public required int Height { get; init; }
    public required DateTimeOffset CommitDate { get; init; }

    public string ShortSha => Sha[..Math.Min(7, Sha.Length)];

    public string? PreReleaseTag => Label is null
        ? null
        : PreReleaseNumber is { } number
            ? $"{Label}.{number}"
            : Label;

    public string MajorMinorPatch => $"{Major}.{Minor}.{Patch}";

    public string SemVer => PreReleaseTag is { } tag
        ? $"{MajorMinorPatch}-{tag}"
        : MajorMinorPatch;

    public string FullSemVer => $"{SemVer}+{ShortSha}";

    public string? AssemblySemVer { get; private set; }
    public string? AssemblyFileSemVer { get; private set; }
    public string? InformationalVersion { get; private set; }

    public void SetTemplates(string assemblySemVer, string assemblyFileSemVer, string informationalVersion)
    {
        AssemblySemVer = ApplyTemplate(assemblySemVer);
        AssemblyFileSemVer = ApplyTemplate(assemblyFileSemVer);
        InformationalVersion = ApplyTemplate(informationalVersion);
    }

    private string ApplyTemplate(string template) => new StringBuilder(template)
        .Replace("{Major}", Major.ToString(CultureInfo.InvariantCulture))
        .Replace("{Minor}", Minor.ToString(CultureInfo.InvariantCulture))
        .Replace("{Patch}", Patch.ToString(CultureInfo.InvariantCulture))
        .Replace("{Label}", Label ?? "")
        .Replace("{PreReleaseNumber}", PreReleaseNumber?.ToString(CultureInfo.InvariantCulture) ?? "")
        .Replace("{Sha}", Sha)
        .Replace("{Branch}", Branch ?? "")
        .Replace("{VersionSourceSha}", VersionSourceSha)
        .Replace("{Height}", Height.ToString(CultureInfo.InvariantCulture))
        .Replace("{CommitDate}", CommitDate.ToString("o", CultureInfo.InvariantCulture))
        .Replace("{ShortSha}", ShortSha)
        .Replace("{PreReleaseTag}", PreReleaseTag ?? "")
        .Replace("{MajorMinorPatch}", MajorMinorPatch)
        .Replace("{SemVer}", SemVer)
        .Replace("{FullSemVer}", FullSemVer)
        .ToString();
}