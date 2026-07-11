using System.Diagnostics;

namespace ACSync.Core;

public static class SyncPatch
{
    public const string PatchFileName = "ACSync.Core_patch.7z";
    public const string DeleteListFileName = "ACSync.Core_delete.txt";

    /// <summary>
    /// 创建补丁：比对旧清单与新目录，将变更文件 + 清单 + 删除列表打包为 7z。
    /// </summary>
    public static void CreatePatch(string oldPath, string newPath, string? outputPath = null, string? sevenZipPath = null)
    {
        var oldManifest = ManifestService.LoadFromDirectory(oldPath);

        SyncManifest? newManifest;
        var newManifestPath = Path.Combine(newPath, ManifestService.ManifestFileName);
        if (File.Exists(newManifestPath))
        {
            Console.WriteLine("Loading existing manifest from new version directory.");
            newManifest = ManifestService.LoadFromFile(newManifestPath);
        }
        else
        {
            Console.WriteLine("Scanning new version directory...");
            newManifest = ManifestService.ScanDirectory(newPath);
        }

        // 比对新旧差异
        var (changed, deleted) = ManifestService.Diff(oldManifest, newManifest);

        if (changed.Count == 0 && deleted.Count == 0)
        {
            Console.WriteLine("No differences found. Patch not created.");
            return;
        }

        var patchDir = Path.Combine(Path.GetTempPath(), $"ACSync.Core_patch_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(patchDir);

            // 复制变更文件到临时目录（保持目录结构）
            foreach (var entry in changed)
            {
                var srcFile = Path.Combine(newPath, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var dstFile = Path.Combine(patchDir, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var dstDir = Path.GetDirectoryName(dstFile)!;
                Directory.CreateDirectory(dstDir);
                File.Copy(srcFile, dstFile, overwrite: true);
            }

            // 写入新清单
            var manifestPath = Path.Combine(patchDir, ManifestService.ManifestFileName);
            ManifestService.SaveToFile(newManifest, manifestPath);

            // 写入删除列表
            if (deleted.Count > 0)
            {
                var deleteListPath = Path.Combine(patchDir, DeleteListFileName);
                var lines = deleted.Select(f => f.RelativePath);
                File.WriteAllLines(deleteListPath, lines);
            }

            // 用 7za 打包
            var patchFile = outputPath ?? Path.Combine(Environment.CurrentDirectory, PatchFileName);
            Run7Zip($"a -t7z -mx=9 -m0=LZMA2 \"{patchFile}\" \"{patchDir}{Path.DirectorySeparatorChar}*\" -r", sevenZipPath);
            Console.WriteLine($"Patch created: {patchFile}");
            Console.WriteLine($"  Changed/New: {changed.Count} files");
            Console.WriteLine($"  To delete:   {deleted.Count} files");
        }
        finally
        {
            if (Directory.Exists(patchDir))
                Directory.Delete(patchDir, recursive: true);
        }
    }

    /// <summary>
    /// 应用补丁：解压 7z 到目标目录，覆盖文件、删除过期文件、排除指定类型。
    /// </summary>
    public static void ApplyPatch(string targetPath, string? patchFile = null, string[]? excludeExtensions = null, string? sevenZipPath = null)
    {
        var patch = patchFile ?? Path.Combine(Environment.CurrentDirectory, PatchFileName);
        if (!File.Exists(patch))
            throw new FileNotFoundException($"Patch file not found: {patch}");

        if (!Directory.Exists(targetPath))
            throw new DirectoryNotFoundException($"Target directory not found: {targetPath}");

        targetPath = Path.GetFullPath(targetPath);
        var excludeSet = NormalizeExcludes(excludeExtensions);

        var extractDir = Path.Combine(Path.GetTempPath(), $"ACSync.Core_extract_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(extractDir);

            // 解压补丁
            Run7Zip($"x \"{patch}\" -o\"{extractDir}\" -y", sevenZipPath);

            // 应用删除列表
            var deleteListPath = Path.Combine(extractDir, DeleteListFileName);
            if (File.Exists(deleteListPath))
            {
                var deleteLines = File.ReadAllLines(deleteListPath);
                foreach (var line in deleteLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var targetFile = Path.Combine(targetPath, line.Trim().Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                        Console.WriteLine($"  Deleted: {line.Trim()}");
                    }
                }
            }

            // 复制文件到目标目录
            var extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
            foreach (var file in extractedFiles)
            {
                var relativePath = Path.GetRelativePath(extractDir, file);

                // 跳过清单和删除列表
                if (relativePath == ManifestService.ManifestFileName ||
                    relativePath == DeleteListFileName)
                    continue;

                // 检查排除规则
                var ext = Path.GetExtension(relativePath).ToLowerInvariant();
                if (excludeSet.Contains(ext))
                {
                    Console.WriteLine($"  Skipped (excluded): {relativePath}");
                    continue;
                }

                var targetFile = Path.Combine(targetPath, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile)!;
                Directory.CreateDirectory(targetDir);
                File.Copy(file, targetFile, overwrite: true);
            }

            // 将新清单写入目标目录
            var manifestPath = Path.Combine(extractDir, ManifestService.ManifestFileName);
            if (File.Exists(manifestPath))
            {
                var targetManifest = Path.Combine(targetPath, ManifestService.ManifestFileName);
                File.Copy(manifestPath, targetManifest, overwrite: true);
            }

            Console.WriteLine($"Patch applied successfully to: {targetPath}");

            // 补丁应用成功后自动删除补丁文件
            if (File.Exists(patch))
            {
                File.Delete(patch);
                Console.WriteLine($"Patch success,clear patch file: {patch}");
            }
        }
        finally
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
    }

    private static HashSet<string> NormalizeExcludes(string[]? extensions)
    {
        var set = new HashSet<string>();
        if (extensions == null) return set;

        foreach (var ext in extensions)
        {
            var trimmed = ext.Trim().ToLowerInvariant();
            if (!trimmed.StartsWith('.'))
                trimmed = "." + trimmed;
            set.Add(trimmed);
        }
        return set;
    }

    private static void Run7Zip(string arguments, string? sevenZipPath = null)
    {
        var exePath = sevenZipPath ?? Path.Combine(AppContext.BaseDirectory, "7za.exe");
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"7za.exe not found at: {exePath}");

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"7za.exe exited with code {process.ExitCode}.\nArguments: {arguments}\n{stderr}");
        }

        if (!string.IsNullOrWhiteSpace(stdout))
            Console.WriteLine(stdout);
    }
}
