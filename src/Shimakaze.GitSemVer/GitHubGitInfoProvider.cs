using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Octokit;

namespace Shimakaze.GitSemVer;

public sealed class GitHubGitInfoProvider : IGitInfoProvider
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repo;

    private readonly Lazy<IDictionary<string, string>> _tags;

    private readonly ConcurrentDictionary<string, GitHubCommit> _commits =
        new(StringComparer.OrdinalIgnoreCase);

    public GitHubGitInfoProvider(
        string owner,
        string repo,
        string token,
        Uri host)
    {
        _owner = owner;
        _repo = repo;

        _client = new GitHubClient(
            new ProductHeaderValue("Shimakaze.GitSemVer"),
            host)
        {
            Credentials = new Credentials(token)
        };

        _tags = new Lazy<IDictionary<string, string>>(
            LoadTags,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IDictionary<string, string> Tags => _tags.Value;

    public bool TryGetBranchSha(string branch, [NotNullWhen(true)] out string? sha)
    {
        try
        {
            if (string.Equals(branch, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                branch = _client.Repository
                    .Get(_owner, _repo)
                    .Result
                    .DefaultBranch;
            }

            sha = _client.Repository.Branch
                .Get(_owner, _repo, branch)
                .Result
                .Commit.Sha;

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error getting branch SHA for branch '{branch}': {ex.Message}");

            sha = null;
            return false;
        }
    }

    public IEnumerable<string> EnumerateFirstParentHistory(string startSha)
    {
        var sha = startSha;

        while (!string.IsNullOrEmpty(sha))
        {
            var commit = GetCommit(sha);

            yield return commit.Sha;

            if (commit.Parents.Count is 0)
                yield break;

            sha = commit.Parents[0].Sha;
        }
    }

    public string? GetBranchName(string sha)
    {
        try
        {
            return _client.Repository.Branch
                .GetAll(_owner, _repo)
                .Result
                .Where(branch =>
                    string.Equals(branch.Commit.Sha, sha, StringComparison.OrdinalIgnoreCase))
                .OrderBy(branch => branch.Name.ToLowerInvariant() switch
                {
                    "master" => 0,
                    "main" => 1,
                    _ => 2
                })
                .ThenBy(branch => branch.Name, StringComparer.OrdinalIgnoreCase)
                .Select(branch => branch.Name)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting branch name for SHA '{sha}': {ex.Message}");
            return null;
        }
    }

    public DateTimeOffset GetCommitDate(string sha) => GetCommit(sha).Commit.Committer.Date;

    private Dictionary<string, string> LoadTags() => _client.Repository
        .GetAllTags(_owner, _repo)
        .Result
        .ToDictionary(
            tag => tag.Name,
            tag => tag.Commit.Sha,
            StringComparer.Ordinal);

    private GitHubCommit GetCommit(string sha) => _commits.GetOrAdd(
        sha,
        key => _client.Repository.Commit
            .Get(_owner, _repo, key)
            .Result);
}
