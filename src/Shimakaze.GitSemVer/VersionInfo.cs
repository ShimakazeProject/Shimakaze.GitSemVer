using System.Globalization;

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
    public required string? VersionSourceSha { get; init; }
    public required int Height { get; init; }
    public required DateTimeOffset CommitDate { get; init; }

    public string ShortSha => Sha[..Math.Min(7, Sha.Length)];

    public string MajorMinorPatch => $"{Major}.{Minor}.{Patch}";

    public string? PreReleaseTag { get; private set; }
    public string? SemVer { get; private set; }
    public string? FullSemVer { get; private set; }
    public string? AssemblySemVer { get; private set; }
    public string? AssemblyFileSemVer { get; private set; }
    public string? InformationalVersion { get; private set; }

    public void SetTemplates(ITemplateProvider templateProvider)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Major)] = Major.ToString(CultureInfo.InvariantCulture),
            [nameof(Minor)] = Minor.ToString(CultureInfo.InvariantCulture),
            [nameof(Patch)] = Patch.ToString(CultureInfo.InvariantCulture),
            [nameof(Label)] = Label,
            [nameof(PreReleaseNumber)] = PreReleaseNumber?.ToString(CultureInfo.InvariantCulture),
            [nameof(Sha)] = Sha,
            [nameof(Branch)] = Branch,
            [nameof(VersionSourceSha)] = VersionSourceSha,
            [nameof(Height)] = Height.ToString(CultureInfo.InvariantCulture),
            [nameof(CommitDate)] = CommitDate.ToString("o", CultureInfo.InvariantCulture),
            [nameof(ShortSha)] = ShortSha,
            [nameof(MajorMinorPatch)] = MajorMinorPatch,
        };
        Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(PreReleaseTag)] = templateProvider.PreReleaseTag,
            [nameof(SemVer)] = templateProvider.Semver,
            [nameof(FullSemVer)] = templateProvider.FullSemver,
            [nameof(AssemblySemVer)] = templateProvider.AssemblySemver,
            [nameof(AssemblyFileSemVer)] = templateProvider.AssemblyFileSemver,
            [nameof(InformationalVersion)] = templateProvider.InformationalVersion,
        };
        TemplateEngine engine = new (values, templates);

        PreReleaseTag = NullIfEmpty(engine.Resolve(nameof(PreReleaseTag)));
        SemVer = engine.Resolve(nameof(SemVer));
        FullSemVer = engine.Resolve(nameof(FullSemVer));
        AssemblySemVer = engine.Resolve(nameof(AssemblySemVer));
        AssemblyFileSemVer = engine.Resolve(nameof(AssemblyFileSemVer));
        InformationalVersion = engine.Resolve(nameof(InformationalVersion));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}