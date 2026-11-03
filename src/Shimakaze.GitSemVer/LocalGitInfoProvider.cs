using System.Diagnostics.CodeAnalysis;

using LibGit2Sharp;

namespace Shimakaze.GitSemVer;

public sealed class LocalGitInfoProvider(string path) : IGitInfoProvider, IDisposable
{
    private readonly Repository _repo = new(path);
    private bool _disposedValue;

    public IDictionary<string, string> Tags => _repo.Tags.ToDictionary(
        i => i.FriendlyName,
        i => i.Target.Sha);

    public LocalGitInfoProvider() : this(Environment.CurrentDirectory)
    {
    }

    public bool TryGetBranchSha(string branch, [NotNullWhen(true)] out string? sha)
    {
        if (_repo.Branches[branch] is not { Tip: { } commit })
        {
            sha = null;
            return false;
        }

        sha = commit.Sha;
        return true;
    }

    public IEnumerable<string> EnumerateFirstParentHistory(string startSha)
    {
        for (var commit = _repo.Lookup<Commit>(startSha); commit is { }; commit = commit.Parents.FirstOrDefault())
            yield return commit.Sha;
    }

    public string? GetBranchName(string sha)
    {
        if (_repo.Lookup<Commit>(sha) is not { } commit)
            return null;

        return _repo.Branches
            .Where(b => b.Tip?.Sha == commit.Sha)
            .OrderBy(b => b.FriendlyName.Split('/').Length)
            .ThenBy(b => b.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Select(b => b.FriendlyName)
            .FirstOrDefault();
    }

    public DateTimeOffset GetCommitDate(string sha)
    {
        var commit = _repo.Lookup<Commit>(sha);
        return commit?.Committer?.When ?? default;
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            _repo.Dispose();
        }

        _disposedValue = true;
    }

    // ~LocalGitHistoryProvider()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}