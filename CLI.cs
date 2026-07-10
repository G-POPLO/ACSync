namespace ACSync;

public class CLI
{
    public static void Main(string[] args)
    {
        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        var mode = args[^1]; // 最后一个参数为模式标志
        var positional = args[..^1];

        switch (mode)
        {
            case "-l" when positional.Length == 1:
                // acsync <path> -l
                CmdCreateManifest(positional[0]);
                break;

            case "-m" when positional.Length == 2:
                // acsync <oldpath> <newpath> -m
                CmdCreatePatch(positional[0], positional[1]);
                break;

            case "-u" when positional.Length >= 1:
                // acsync <path> -u [-e ext1,ext2]
                var targetPath = positional[0];
                string[]? excludes = null;

                // 解析可选参数
                var remaining = positional[1..];
                for (int i = 0; i < remaining.Length; i++)
                {
                    if ((remaining[i] == "-e" || remaining[i] == "--exclude") && i + 1 < remaining.Length)
                    {
                        excludes = remaining[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        i++;
                    }
                }

                CmdApplyPatch(targetPath, excludes);
                break;

            default:
                PrintUsage();
                break;
        }
    }

    private static void CmdCreateManifest(string path)
    {
        Console.WriteLine($"Scanning: {path}");
        var manifest = ManifestService.ScanDirectory(path);
        var manifestPath = Path.Combine(path, ManifestService.ManifestFileName);
        ManifestService.SaveToFile(manifest, manifestPath);
        Console.WriteLine($"Manifest created: {manifestPath}");
        Console.WriteLine($"  Total files: {manifest.Files.Count}");
    }

    private static void CmdCreatePatch(string oldPath, string newPath)
    {
        Console.WriteLine($"Old version: {oldPath}");
        Console.WriteLine($"New version: {newPath}");
        SyncPatch.CreatePatch(oldPath, newPath);
    }

    private static void CmdApplyPatch(string targetPath, string[]? excludes)
    {
        Console.WriteLine($"Target: {targetPath}");
        SyncPatch.ApplyPatch(targetPath, excludeExtensions: excludes);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("ACSync - Incremental Update System");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  acsync <path> -l                  Create manifest for directory");
        Console.WriteLine("  acsync <path> -u [-e ext1,ext2]   Apply patch to directory");
        Console.WriteLine("  acsync <oldpath> <newpath> -m     Create patch from old to new");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -e, --exclude   Comma-separated extensions to skip during update");
        Console.WriteLine("                  Example: -e .json,.ini,.yaml");
    }
}
