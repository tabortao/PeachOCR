using Sdcb.OpenVINO.PaddleOCR.Models.Online;
using System.IO;

// ============================================================
// PeachOCR 模型预下载工具
// 用法:
//   dotnet run --project tools/download_models -- [options]
//   dotnet run --project tools/download_models -- --all
//   dotnet run --project tools/download_models -- --model v4
//   dotnet run --project tools/download_models -- --model v6
// ============================================================

var targetDir = args.Length > 0 && args[0] != "--all" && args[0] != "--model"
    ? args[0]
    : @"..\..\..\..\models";

var modelsToDownload = args.Contains("--all")
    ? new[] { "v4", "v6" }
    : args.Contains("--model")
        ? args.SkipWhile(a => a != "--model").Skip(1).Take(1).ToArray()
        : new[] { "v4", "v6" };

Console.WriteLine($"Target directory: {Path.GetFullPath(targetDir)}");
Console.WriteLine();

foreach (var model in modelsToDownload)
{
    switch (model.ToLower())
    {
        case "v4":
            await DownloadV4Async(targetDir);
            break;
        case "v6":
            await DownloadV6Async(targetDir);
            break;
        default:
            Console.WriteLine($"Unknown model: {model}. Supported: v4, v6");
            break;
    }
}

Console.WriteLine("\n=== All downloads complete ===");
PrintDirectorySummary(targetDir);

static async Task DownloadV4Async(string targetDir)
{
    Settings.GlobalModelDirectory = targetDir;
    Console.WriteLine("Downloading PP-OCRv4 (Paddle format)...");
    var model = await OnlineFullModels.ChineseV4.DownloadAsync();
    Console.WriteLine($"  Done: {model.GetType().Name}");
}

static async Task DownloadV6Async(string targetDir)
{
    var v6Dir = Path.Combine(targetDir, "ch_PP-OCRv6");
    Settings.GlobalModelDirectory = v6Dir;
    Console.WriteLine("Downloading PP-OCRv6 Small (ONNX format)...");
    var model = await OnlineFullModels.ChineseV6Small.DownloadAsync();
    Console.WriteLine($"  Done: {model.GetType().Name}");
}

static void PrintDirectorySummary(string targetDir)
{
    Console.WriteLine($"\n=== {Path.GetFullPath(targetDir)}/ ===");
    foreach (var dir in new DirectoryInfo(targetDir).GetDirectories())
    {
        var files = dir.GetFiles("*", SearchOption.AllDirectories)
            .Where(f => !f.Name.EndsWith(".lock")
                && !f.Name.EndsWith(".metadata")
                && f.Name != ".gitattributes"
                && f.Name != ".gitignore"
                && !f.Name.StartsWith("._"))
            .ToList();
        if (files.Count > 0)
        {
            var totalSize = files.Sum(f => f.Length);
            var sizeStr = totalSize >= 1024 * 1024
                ? $"{totalSize / 1024.0 / 1024.0:F1}MB"
                : $"{totalSize / 1024.0:F1}KB";
            Console.WriteLine($"  {dir.Name}/ ({sizeStr}, {files.Count} files)");
        }
    }
}