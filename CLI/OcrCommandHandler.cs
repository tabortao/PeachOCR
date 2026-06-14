using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OCR;
using PDF;

namespace PeachOCR.CLI
{
    public class OcrCommandHandler
    {
        public async Task<int> ExecuteAsync(OcrOptions options)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(options.Input))
                {
                    Console.Error.WriteLine("错误：必须指定输入文件或目录（-i 或 --input）");
                    return 1;
                }

                if (!File.Exists(options.Input) && !Directory.Exists(options.Input))
                {
                    Console.Error.WriteLine($"错误：输入路径不存在：{options.Input}");
                    return 1;
                }

                if (options.Verbose)
                {
                    Console.WriteLine("PeachOCR CLI");
                    Console.WriteLine($"输入路径：{options.Input}");
                    Console.WriteLine($"输出路径：{options.Output}");
                    Console.WriteLine($"OCR模型：{options.Model}");
                    Console.WriteLine($"输出格式：{options.Format}");
                    Console.WriteLine($"GPU加速：{options.Gpu}");
                    Console.WriteLine($"并发数：{options.Concurrency}");
                    Console.WriteLine($"合并结果：{options.Merge}");
                    Console.WriteLine();
                }

                var inputFiles = CollectInputFiles(options.Input);
                if (inputFiles.Count == 0)
                {
                    Console.Error.WriteLine("错误：未找到支持的文件（JPG, PNG, BMP, TIFF, WEBP, PDF）");
                    return 1;
                }

                if (options.Verbose)
                {
                    Console.WriteLine($"找到 {inputFiles.Count} 个文件待处理");
                }

                var pdfFiles = inputFiles.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
                var imageFiles = inputFiles.Where(f => !f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();

                var allImageFiles = new List<string>(imageFiles);

                if (pdfFiles.Count > 0)
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine($"正在转换 {pdfFiles.Count} 个PDF文件...");
                    }

                    foreach (var pdfFile in pdfFiles)
                    {
                        var pdfImages = await ConvertPdfToImagesAsync(pdfFile, options.Verbose);
                        allImageFiles.AddRange(pdfImages);
                    }
                }

                if (allImageFiles.Count == 0)
                {
                    Console.Error.WriteLine("错误：没有可处理的文件");
                    return 1;
                }

                var processor = CreateProcessor(options);
                processor.AddImages(allImageFiles);

                if (options.Verbose)
                {
                    Console.WriteLine($"开始OCR识别...");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var (results, totalMs) = await processor.RunBatchOcrAsync(
                    options.Concurrency,
                    (finished, total) =>
                    {
                        if (options.Verbose)
                        {
                            Console.WriteLine($"\r处理进度：{finished}/{total} ({finished * 100 / total}%)");
                        }
                    });

                stopwatch.Stop();

                if (options.Verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine($"OCR识别完成，总耗时：{totalMs}ms");
                }

                var outputDir = DetermineOutputDirectory(options);
                var outputFiles = SaveResults(results, outputDir, options);

                if (options.Verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine($"结果已保存到：{outputDir}");
                    foreach (var file in outputFiles)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
                else
                {
                    foreach (var file in outputFiles)
                    {
                        Console.WriteLine(file);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误：{ex.Message}");
                if (options.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private List<string> CollectInputFiles(string inputPath)
        {
            var files = new List<string>();

            if (File.Exists(inputPath))
            {
                files.Add(inputPath);
            }
            else if (Directory.Exists(inputPath))
            {
                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp", ".pdf" };
                files.AddRange(Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())));
            }

            return files;
        }

        private async Task<List<string>> ConvertPdfToImagesAsync(string pdfFile, bool verbose)
        {
            var imageFiles = new List<string>();
            var tempDir = Path.Combine(Path.GetTempPath(), "PeachOCR", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                if (verbose)
                {
                    Console.WriteLine($"  转换PDF: {Path.GetFileName(pdfFile)}");
                }

                var images = await PDF.Convert.PDFToImageAsync(pdfFile, tempDir, 150, "jpg", 80);
                imageFiles.AddRange(images);
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.Error.WriteLine($"  PDF转换失败 {pdfFile}: {ex.Message}");
                }
            }

            return imageFiles;
        }

        private OcrBatchProcessor CreateProcessor(OcrOptions options)
        {
            var processor = new OcrBatchProcessor();

            switch (options.Model.ToLower())
            {
                case "v4":
                    processor.SetModel(OcrBatchProcessor.ModelType.PP_OCRv4);
                    break;
                case "v5":
                    processor.SetModel(OcrBatchProcessor.ModelType.PP_OCRv5);
                    break;
                case "v6":
                    processor.SetModel(OcrBatchProcessor.ModelType.PP_OCRv6);
                    break;
                default:
                    processor.SetModel(OcrBatchProcessor.ModelType.PP_OCRv6);
                    break;
            }

            processor.SetUseGpu(options.Gpu, options.Gpu);

            var format = options.Format.ToLower() switch
            {
                "md" => "md文件",
                "json" => "json格式",
                _ => "txt标准格式"
            };
            processor.SetOutputFileFormat(format);

            return processor;
        }

        private string DetermineOutputDirectory(OcrOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.Output))
            {
                if (File.Exists(options.Output))
                {
                    return Path.GetDirectoryName(options.Output) ?? Directory.GetCurrentDirectory();
                }
                if (Directory.Exists(options.Output))
                {
                    return options.Output;
                }
                var parentDir = Path.GetDirectoryName(options.Output);
                if (!string.IsNullOrWhiteSpace(parentDir) && Directory.Exists(parentDir))
                {
                    return parentDir;
                }
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "OCR_Result");
        }

        private List<string> SaveResults(List<OcrBatchProcessor.OcrResultDetail> results, string outputDir, OcrOptions options)
        {
            Directory.CreateDirectory(outputDir);
            var outputFiles = new List<string>();

            if (options.Format.ToLower() == "json")
            {
                var jsonFile = SaveJsonResults(results, outputDir, options);
                outputFiles.Add(jsonFile);
            }
            else if (options.Merge)
            {
                var mergedFile = SaveMergedResults(results, outputDir, options);
                outputFiles.Add(mergedFile);
            }
            else
            {
                foreach (var result in results)
                {
                    var resultFile = SaveIndividualResult(result, outputDir, options);
                    outputFiles.Add(resultFile);
                }
            }

            return outputFiles;
        }

        private string SaveJsonResults(List<OcrBatchProcessor.OcrResultDetail> results, string outputDir, OcrOptions options)
        {
            var jsonOutput = new List<object>();

            foreach (var result in results)
            {
                var textItems = new List<Dictionary<string, object?>>();

                if (result.Result != null)
                {
                    foreach (var r in result.Result)
                    {
                        textItems.Add(new Dictionary<string, object?>
                        {
                            ["text"] = r?.Text ?? string.Empty,
                            ["confidence"] = r?.Score ?? 0.0,
                            ["boundingBox"] = r?.Box
                        });
                    }
                }

                var fileInfo = new Dictionary<string, object?>
                {
                    ["filePath"] = result.ImgPath,
                    ["fileName"] = Path.GetFileName(result.ImgPath),
                    ["processingTimeMs"] = result.OcrMs,
                    ["textCount"] = result.Result?.Count ?? 0,
                    ["texts"] = textItems
                };

                jsonOutput.Add(fileInfo);
            }

            var jsonFileName = !string.IsNullOrWhiteSpace(options.Output) && options.Merge
                ? options.Output
                : Path.Combine(outputDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_OCR_Result.json");

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(jsonOutput, jsonOptions);
            File.WriteAllText(jsonFileName, json, Encoding.UTF8);

            return jsonFileName;
        }

        private string SaveMergedResults(List<OcrBatchProcessor.OcrResultDetail> results, string outputDir, OcrOptions options)
        {
            var isMarkdown = options.Format.ToLower() == "md";

            var extension = isMarkdown ? ".md" : ".txt";
            var fileName = !string.IsNullOrWhiteSpace(options.Output) && Path.GetExtension(options.Output) == extension
                ? options.Output
                : Path.Combine(outputDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_CombinedOCRResult{extension}");

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                if (isMarkdown)
                {
                    writer.WriteLine("# PeachOCR 批量识别结果");
                    writer.WriteLine();
                    writer.WriteLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"文件数量：{results.Count}");
                    writer.WriteLine();
                    writer.WriteLine("---");
                    writer.WriteLine();
                }

                foreach (var result in results)
                {
                    if (isMarkdown)
                    {
                        writer.WriteLine($"## {Path.GetFileName(result.ImgPath)}");
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine($"=== {Path.GetFileName(result.ImgPath)} ===");
                        writer.WriteLine();
                    }

                    if (result.Result != null)
                    {
                        foreach (var item in result.Result)
                        {
                            if (!string.IsNullOrWhiteSpace(item?.Text))
                            {
                                writer.WriteLine(item.Text);
                            }
                        }
                    }

                    writer.WriteLine();
                    if (isMarkdown)
                    {
                        writer.WriteLine("---");
                        writer.WriteLine();
                    }
                }
            }

            return fileName;
        }

        private string SaveIndividualResult(OcrBatchProcessor.OcrResultDetail result, string outputDir, OcrOptions options)
        {
            var isMarkdown = options.Format.ToLower() == "md";
            var extension = isMarkdown ? ".md" : ".txt";
            var baseName = Path.GetFileNameWithoutExtension(result.ImgPath);
            var fileName = Path.Combine(outputDir, baseName + extension);

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                if (isMarkdown)
                {
                    writer.WriteLine($"# {baseName}");
                    writer.WriteLine();
                    writer.WriteLine("## OCR 识别结果");
                    writer.WriteLine();
                }

                if (result.Result != null)
                {
                    foreach (var item in result.Result)
                    {
                        if (!string.IsNullOrWhiteSpace(item?.Text))
                        {
                            writer.WriteLine(item.Text);
                            writer.WriteLine();
                        }
                    }
                }
            }

            return fileName;
        }
    }
}
