using System.Text.Json.Serialization;

namespace Shimakaze.GitSemVer;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(VersionInfo))]
public sealed partial class GitSemVerJsonSerializerContext : JsonSerializerContext
{
}