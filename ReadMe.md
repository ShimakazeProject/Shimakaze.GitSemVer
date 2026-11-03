# Shimakaze.GitSemVer

**Shimakaze.GitSemVer** 从 Git 仓库历史中计算语义化版本，并以 JSON 输出版本、提交和程序集版本信息。它既可以读取本地仓库，也可以通过 GitHub API 查询仓库。

## 功能

- 沿指定分支的 first-parent 历史查找最近的有效 SemVer 标签。
- 支持自定义标签前缀、无版本标签时的回退版本，以及多种版本模板。
- 输出 `SemVer`、`FullSemVer`、程序集版本、提交 SHA、分支、提交日期和提交高度等信息。
- CLI 帮助信息支持简体中文和英文。

## 安装

需要安装 .NET 11 Runtime。作为全局工具安装：

```shell
dotnet tool install --global Shimakaze.GitSemVer
```

安装后可通过 `gitver` 调用：

```shell
gitver --help
gitver
```

也可以在仓库中安装为本地工具：

```shell
dotnet new tool-manifest
dotnet tool install Shimakaze.GitSemVer
dotnet tool run gitver
```

## 快速开始

在 Git 仓库目录中运行：

```shell
gitver
```

指定仓库路径和分支：

```shell
gitver --repo C:\src\my-project --branch main
```

工具将 JSON 写入标准输出。例如：

```json
{
  "Major": 1,
  "Minor": 2,
  "Patch": 3,
  "Label": "beta",
  "PreReleaseNumber": 5,
  "Sha": "0123456789abcdef0123456789abcdef01234567",
  "Branch": "main",
  "VersionSourceSha": "89abcdef0123456789abcdef0123456789abcdef",
  "Height": 4,
  "CommitDate": "2026-01-02T03:04:05+00:00",
  "ShortSha": "0123456",
  "MajorMinorPatch": "1.2.3",
  "PreReleaseTag": "beta.5",
  "SemVer": "1.2.3-beta.5",
  "FullSemVer": "1.2.3-beta.5+0123456",
  "AssemblySemVer": "1.2.3.4",
  "AssemblyFileSemVer": "1.2.3.4",
  "InformationalVersion": "1.2.3-beta.5+0123456"
}
```

## 版本计算

工具从所选分支的最新提交开始，沿 first-parent 历史向前查找可解析为 SemVer 的 Git 标签。若同一提交存在多个有效版本标签，则采用版本最高的标签。`Height` 表示分支头与版本标签之间经过的提交数；找到的标签提交本身不计入高度。

默认会移除标签开头可选的 `v` 或 `V`，因此 `v1.2.3` 会按 `1.2.3` 解析。若历史中没有有效版本标签，则使用 `--fallback-version` 指定的版本（默认为 `0.0.0`），并将 `VersionSourceSha` 设为 `null`。

## 命令行选项

运行 `gitver --help` 可查看当前完整帮助。常用选项如下：

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--type` | `Local` | Git 信息来源：`Local` 或 `GitHub`。 |
| `--repo` | 当前目录 | 本地 Git 仓库路径。 |
| `--branch` | `HEAD` | 用于计算版本的分支或引用；GitHub 来源下 `HEAD` 表示仓库默认分支。 |
| `--tag-prefix` | `^[vV]?` | 从标签开头移除的正则表达式。 |
| `--fallback-version` | `0.0.0` | 找不到有效版本标签时使用的 SemVer 版本。 |
| `--github-owner` | `GITHUB_OWNER` 环境变量 | GitHub 仓库所有者。 |
| `--github-repo` | `GITHUB_REPO` 环境变量 | GitHub 仓库名称。 |
| `--github-token` | `GITHUB_TOKEN` 环境变量 | 用于 GitHub API 身份验证的访问令牌。 |
| `--github-host` | `https://api.github.com` | GitHub API 基础 URL；用于 GitHub Enterprise 时可指定相应 API URL。 |
| `--pre-release-tag` | 见下方 | 预发布标签模板。 |
| `--semver` | 见下方 | SemVer 模板。 |
| `--full-semver` | 见下方 | FullSemVer 模板。 |
| `--assembly-semver` | `{Major}.{Minor}.{Patch}.{Height}` | 程序集版本模板。 |
| `--assembly-file-semver` | `{Major}.{Minor}.{Patch}.{Height}` | 程序集文件版本模板。 |
| `--informational-version` | `{FullSemVer}` | 信息版本模板。 |

GitHub 来源示例：

```shell
GITHUB_OWNER=owner GITHUB_REPO=repository GITHUB_TOKEN=token \
  gitver --type GitHub --branch main
```

PowerShell 示例：

```powershell
$env:GITHUB_OWNER = "owner"
$env:GITHUB_REPO = "repository"
$env:GITHUB_TOKEN = "token"
gitver --type GitHub --branch main
```

GitHub 来源需要提供仓库所有者、仓库名称和可读取该仓库的访问令牌。令牌也可直接从调用环境注入，不要将令牌写入脚本或提交到版本控制。

## 模板

模板以花括号引用变量，可在模板选项中引用其他模板。变量名不区分大小写。默认模板为：

```text
--pre-release-tag      {PreReleaseNumber ? {Label}.{PreReleaseNumber} : {Label}}
--semver               {PreReleaseTag ? {MajorMinorPatch}-{PreReleaseTag} : {MajorMinorPatch}}
--full-semver          {SemVer}+{ShortSha}
--assembly-semver      {Major}.{Minor}.{Patch}.{Height}
--assembly-file-semver {Major}.{Minor}.{Patch}.{Height}
--informational-version {FullSemVer}
```

条件表达式格式为 `{变量 ? 非空时模板 : 空时模板}`。当变量值为 `null` 或空字符串时选择空分支；否则选择非空分支。条件分支可以包含嵌套表达式：

```text
{PreReleaseTag ? {MajorMinorPatch}-{PreReleaseTag} : {MajorMinorPatch}}
```

可以自定义预发布标签、SemVer 和 FullSemVer，并在后续模板中引用计算结果：

```shell
gitver \
  --pre-release-tag "{Label}-{PreReleaseNumber}" \
  --semver "{MajorMinorPatch}-{PreReleaseTag}" \
  --full-semver "{SemVer}+{ShortSha}.build.{Height}" \
  --informational-version "{FullSemVer}"
```

上例会生成类似 `1.2.3-beta-5+0123456.build.4` 的 FullSemVer。模板中引用了不存在的变量、表达式格式错误或模板之间出现循环引用时，工具会报错。

### 可用变量

| 变量 | 含义 |
| --- | --- |
| `{Major}`、`{Minor}`、`{Patch}` | 版本号的主版本、次版本和修订号。 |
| `{Label}` | SemVer 预发布标签；稳定版本为空。 |
| `{PreReleaseNumber}` | 可选的数字预发布标识；不存在时为空。 |
| `{MajorMinorPatch}` | `Major.Minor.Patch`。 |
| `{Sha}`、`{ShortSha}` | 当前分支头提交的完整 SHA 和前 7 位 SHA。 |
| `{Branch}` | 解析出的分支名称；无法确定时为空。 |
| `{VersionSourceSha}` | 找到的版本标签所指向的提交 SHA；未找到标签时为空。 |
| `{Height}` | 距版本标签的 first-parent 提交数；无有效标签时为遍历到的提交数。 |
| `{CommitDate}` | 当前分支头提交的提交者日期，使用 ISO 8601 格式。 |
| `{PreReleaseTag}` | 预发布标签模板的结果。 |
| `{SemVer}` | SemVer 模板的结果。 |
| `{FullSemVer}` | FullSemVer 模板的结果。 |
| `{AssemblySemVer}` | 程序集版本模板的结果。 |
| `{AssemblyFileSemVer}` | 程序集文件版本模板的结果。 |
| `{InformationalVersion}` | 信息版本模板的结果。 |

空值在普通变量引用中会渲染为空字符串；在条件表达式中会选择空分支。

## CI / MSBuild 集成

版本信息可直接转换为 MSBuild 属性文件。以下 PowerShell 示例将输出写入 `Directory.Version.props`：

```powershell
$Version = gitver | ConvertFrom-Json
@"
<Project>
  <PropertyGroup>
    <Version>$($Version.SemVer)</Version>
    <VersionPrefix>$($Version.MajorMinorPatch)</VersionPrefix>
    <VersionSuffix>$($Version.PreReleaseTag)</VersionSuffix>
    <PackageVersion>$($Version.FullSemVer)</PackageVersion>
    <InformationalVersion>$($Version.InformationalVersion)</InformationalVersion>
    <AssemblyVersion>$($Version.AssemblySemVer)</AssemblyVersion>
    <FileVersion>$($Version.AssemblyFileSemVer)</FileVersion>
    <RepositoryBranch>$($Version.Branch)</RepositoryBranch>
    <RepositoryCommit>$($Version.Sha)</RepositoryCommit>
  </PropertyGroup>
</Project>
"@ | Set-Content Directory.Version.props -Encoding utf8
```

在生成版本文件之后，MSBuild 会在后续构建中读取这些属性。

## 本地化

CLI 描述包含简体中文和英文资源。帮助文本的语言由进程的 .NET `CurrentUICulture` 决定；未设置时使用简体中文资源。

## 开发

仓库使用 `global.json` 指定 .NET SDK。构建项目：

```shell
dotnet build src/Shimakaze.GitSemVer/Shimakaze.GitSemVer.csproj
```

显示 CLI 帮助：

```shell
dotnet run --project src/Shimakaze.GitSemVer/Shimakaze.GitSemVer.csproj -- --help
```

## License

[MIT](LICENSE)
