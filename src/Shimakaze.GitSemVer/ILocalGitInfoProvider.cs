using System.Diagnostics.CodeAnalysis;

namespace Shimakaze.GitSemVer;

public interface IGitInfoProvider
{
    IDictionary<string, string> Tags { get; }
    bool TryGetBranchSha(string branch, [NotNullWhen(true)] out string? sha);
    IEnumerable<string> EnumerateFirstParentHistory(string startSha);
    string? GetBranchName(string sha);
    DateTimeOffset GetCommitDate(string sha);
}
