using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ACSync;

public sealed class SyncManifest
{
    public List<FileEntry> Files { get; set; } = [];
}

public sealed class FileEntry
{
    public string RelativePath { get; set; } = "";
    public DateTime LastWriteTimeUtc { get; set; }
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";

    public override bool Equals(object? obj) =>
        obj is FileEntry other && Sha256 == other.Sha256 && RelativePath == other.RelativePath;

    public override int GetHashCode() => HashCode.Combine(RelativePath, Sha256);
}

// AOT-compatible JSON source generation context
[JsonSerializable(typeof(SyncManifest))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ManifestJsonContext : JsonSerializerContext
{
}

public static class ManifestService
{
    public const string ManifestFileName = "acsync_manifest.json";

    public static SyncManifest ScanDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var manifest = new SyncManifest();
        var basePath = Path.GetFullPath(directoryPath);

        var files = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(basePath, file);

            // 跳过清单文件自身
            if (relativePath == ManifestFileName)
                continue;

            var fileInfo = new FileInfo(file);

            manifest.Files.Add(new FileEntry
            {
                RelativePath = relativePath.Replace('\\', '/'),
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                Length = fileInfo.Length,
                Sha256 = ComputeSha256(file)
            });
        }

        return manifest;
    }

    public static void SaveToFile(SyncManifest manifest, string filePath)
    {
        var json = JsonSerializer.Serialize(manifest, ManifestJsonContext.Default.SyncManifest);
        File.WriteAllText(filePath, json);
    }

    public static SyncManifest LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Manifest file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, ManifestJsonContext.Default.SyncManifest)
               ?? throw new InvalidOperationException("Failed to deserialize manifest.");
    }

    public static SyncManifest LoadFromDirectory(string directoryPath)
    {
        var manifestPath = Path.Combine(directoryPath, ManifestFileName);
        return LoadFromFile(manifestPath);
    }

    /// <summary>
    /// 比对新旧清单，返回 (新增/变更文件, 待删除文件)。
    /// 通过 SHA256 哈希判定文件是否变更。
    /// </summary>
    public static (List<FileEntry> Changed, List<FileEntry> Deleted) Diff(
        SyncManifest oldManifest, SyncManifest newManifest)
    {
        var oldMap = oldManifest.Files.ToDictionary(f => f.RelativePath);
        var newMap = newManifest.Files.ToDictionary(f => f.RelativePath);

        var changed = new List<FileEntry>();
        var deleted = new List<FileEntry>();

        foreach (var (path, newEntry) in newMap)
        {
            if (!oldMap.TryGetValue(path, out var oldEntry))
            {
                // 新文件
                changed.Add(newEntry);
            }
            else if (newEntry.Sha256 != oldEntry.Sha256)
            {
                // 文件已变更
                changed.Add(newEntry);
            }
        }

        foreach (var (path, oldEntry) in oldMap)
        {
            if (!newMap.ContainsKey(path))
            {
                // 旧文件已删除
                deleted.Add(oldEntry);
            }
        }

        return (changed, deleted);
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}
