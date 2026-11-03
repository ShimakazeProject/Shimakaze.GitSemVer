namespace Shimakaze.GitSemVer;

public interface ITemplateProvider
{
    string PreReleaseTag { get; }
    string Semver { get; }
    string FullSemver { get; }
    string AssemblySemver { get; }
    string AssemblyFileSemver { get; }
    string InformationalVersion { get; }
}