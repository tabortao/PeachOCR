using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading; // For SemaphoreSlim
using System.Threading.Tasks;
using PDFtoImage; // Import the PDFtoImage library
using SkiaSharp; // Import SkiaSharp for RenderOptions and SKBitmap

namespace PDF
{
    /// <summary>
    /// 提供 PDF 文件转换的静态工具方法。
    /// </summary>
    public static class Convert
    {
        // --- Helper method to convert string format to SkiaSharp enum ---
        // This method is kept for clarity and centralized format handling.
        private static SKEncodedImageFormat GetSkiaSharpEncodeFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentNullException(nameof(format), "Image format cannot be null or empty.");

            switch (format.ToLowerInvariant())
            {
                case "png": return SKEncodedImageFormat.Png;
                case "jpeg":
                case "jpg": return SKEncodedImageFormat.Jpeg;
                case "webp": return SKEncodedImageFormat.Webp;
                case "bmp": return SKEncodedImageFormat.Bmp;
                case "gif": return SKEncodedImageFormat.Gif;
                default:
                    throw new ArgumentException($"Unsupported image format: '{format}'. Supported formats are png, jpeg, jpg, webp, bmp, gif.", nameof(format));
            }
        }

        // --- Internal helper to convert a single PDF file to images ---
        // This method encapsulates the core logic for processing one PDF file.
        // It's private because the public API is the static ToImages and ToImage.
        private static async Task<List<string>> ProcessSinglePdfFileAsync(
            string pdfFilePath,
            string outputDirectory,
            string imageFormat,
            int dpi,
            int quality)
        {
            // --- Parameter Validation ---
            if (string.IsNullOrWhiteSpace(pdfFilePath))
                throw new ArgumentNullException(nameof(pdfFilePath), "PDF file path cannot be null or empty.");
            if (!File.Exists(pdfFilePath))
                throw new FileNotFoundException($"PDF file not found: {pdfFilePath}");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentNullException(nameof(outputDirectory), "Output directory cannot be null or empty.");
            if (dpi <= 0)
                throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be positive.");
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Get SkiaSharp format using the helper method
            SKEncodedImageFormat skFormat = GetSkiaSharpEncodeFormat(imageFormat);

            var generatedImagePaths = new List<string>();
            string baseName = Path.GetFileNameWithoutExtension(pdfFilePath);

            // --- Core Conversion Logic using PDFtoImage library ---
            // The PDFtoImage library's ToImages method is synchronous,
            // but we wrap it in Task.Run to make our wrapper async.
            // This allows the calling thread to be freed while the conversion happens.
            var conversionTask = Task.Run(() =>
            {
                var conversionOptions = new RenderOptions(Dpi: dpi);
                List<string> currentFileImagePaths = new List<string>();

                using (var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
                {
                    // The PDFtoImage library's ToImages method returns SKBitmap objects.
                    // We need to iterate, encode, save, and dispose them.
                    #pragma warning disable CA1416 // 我们知道此工具在受支持的桌面OS上运行
                    IEnumerable<SKBitmap> images = Conversion.ToImages(fileStream, options: conversionOptions);
                    #pragma warning restore CA1416

                    int pageNumber = 1;
                    foreach (SKBitmap image in images)
                    {
                        string outputFileName = $"{baseName}_page_{pageNumber}.{imageFormat.ToLowerInvariant()}";
                        string outputFilePath = Path.Combine(outputDirectory, outputFileName);

                        using (var stream = new FileStream(outputFilePath, FileMode.Create))
                        {
                            // Use the determined SkiaSharp format and quality
                            image.Encode(stream, skFormat, quality);
                        }
                        currentFileImagePaths.Add(outputFilePath);
                        pageNumber++;
                        image.Dispose(); // Dispose the SKBitmap after encoding and saving
                    }
                }
                return currentFileImagePaths;
            });

            // Await the task that performs the synchronous conversion
            generatedImagePaths = await conversionTask;

            return generatedImagePaths;
        }


        // --- Public static function to convert a single PDF file to images ---
        /// <summary>
        /// 将单个 PDF 文档转换为图像文件。
        /// </summary>
        /// <param name="pdfFilePath">要转换的 PDF 文件路径。</param>
        /// <param name="outputDirectory">保存图像文件的目录。</param>
        /// <param name="dpi">输出图像的分辨率 (DPI)。默认为 150。</param>
        /// <param name="imageFormat">输出图像的格式 (例如 "png", "jpg", "webp")。默认为 "jpg"。</param>
        /// <param name="quality">对于 JPEG 和 WebP 格式，指定图像质量 (0-100)。默认为 80。</param>
        /// <returns>一个任务，表示异步操作。任务完成后，返回一个包含所有生成图像文件路径的列表。</returns>
        /// <exception cref="ArgumentNullException">如果 pdfFilePath 或 outputDirectory 为 null 或空。</exception>
        /// <exception cref="FileNotFoundException">如果指定的 pdfFilePath 不存在。</exception>
        /// <exception cref="ArgumentException">如果 imageFormat 不支持。</exception>
        /// <exception cref="DirectoryNotFoundException">如果 outputDirectory 路径无效且无法创建。</exception>
        /// <exception cref="Exception">在转换过程中发生其他错误。</exception>
        public static Task<List<string>> PDFToImageAsync(
            string pdfFilePath,
            string outputDirectory,
            int dpi = 150,
            string imageFormat = "jpg",
            int quality = 80)
        {
            // Call the internal processing method.
            return ProcessSinglePdfFileAsync(pdfFilePath, outputDirectory, imageFormat, dpi, quality);
        }


        // --- Public static function to convert multiple PDF files to images ---
        /// <summary>
        /// 将多个 PDF 文档转换为图像文件。
        /// </summary>
        /// <param name="pdfFilePaths">要转换的 PDF 文件路径列表。</param>
        /// <param name="outputDirectory">保存图像文件的目录。</param>
        /// <param name="dpi">输出图像的分辨率 (DPI)。默认为 150。</param>
        /// <param name="imageFormat">输出图像的格式 (例如 "png", "jpg", "webp")。默认为 "jpg"。</param>
        /// <param name="quality">对于 JPEG 和 WebP 格式，指定图像质量 (0-100)。默认为 80。</param>
        /// <param name="maxConcurrentTasks">同时执行的最大转换任务数。0 表示不限制（使用 Environment.ProcessorCount）。</param>
        /// <returns>一个任务，表示异步操作。任务完成后，返回一个包含所有生成图像文件路径的列表。</returns>
        /// <exception cref="ArgumentNullException">如果 pdfFilePaths 或 outputDirectory 为 null 或空。</exception>
        /// <exception cref="FileNotFoundException">如果列表中的任何一个 pdfFilePath 不存在。</exception>
        /// <exception cref="ArgumentException">如果 imageFormat 不支持。</exception>
        /// <exception cref="DirectoryNotFoundException">如果 outputDirectory 路径无效且无法创建。</exception>
        /// <exception cref="Exception">在转换过程中发生其他错误。</exception>
        public static async Task<List<string>> PDFToImagesAsync(
            IEnumerable<string> pdfFilePaths,
            string outputDirectory,
            int dpi = 150,
            string imageFormat = "jpg",
            int quality = 80,
            int maxConcurrentTasks = 0)
        {
            // --- Parameter Validation ---
            if (pdfFilePaths == null)
                throw new ArgumentNullException(nameof(pdfFilePaths), "PDF file paths list cannot be null.");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentNullException(nameof(outputDirectory), "Output directory cannot be null or empty.");

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            var files = pdfFilePaths.ToList();
            if (!files.Any())
            {
                return new List<string>(); // Return empty list if no files provided
            }

            // Check if all input files exist before starting parallel processing
            foreach (var filePath in files)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    throw new FileNotFoundException($"PDF file not found or invalid path: {filePath}");
                }
            }

            // --- Parallel Asynchronous Processing ---
            var allGeneratedImagePaths = new List<string>();
            // Each task will process one PDF file and return a list of image paths generated from it.
            var tasks = new List<Task<List<string>>>();

            // Determine concurrency level
            int concurrency = maxConcurrentTasks <= 0 ? Environment.ProcessorCount : maxConcurrentTasks;
            concurrency = Math.Min(concurrency, files.Count); // Don't exceed the number of files

            // Use SemaphoreSlim to limit the number of concurrent tasks
            using (var semaphore = new SemaphoreSlim(concurrency, concurrency))
            {
                foreach (var filePath in files)
                {
                    // Wait for a slot to become available before starting a new task
                    await semaphore.WaitAsync();

                    // Create and start the task for converting this PDF
                    var pdfProcessingTask = Task.Run(async () =>
                    {
                        try
                        {
                            // Call the single PDF conversion method
                            return await PDFToImageAsync( // Now calling the re-added ToImageAsync
                                filePath,
                                outputDirectory,
                                dpi,
                                imageFormat,
                                quality);
                        }
                        finally
                        {
                            // Release the semaphore slot, allowing the next waiting task to start
                            semaphore.Release();
                        }
                    });
                    tasks.Add(pdfProcessingTask);
                }

                // Wait for all tasks to complete and collect their results
                var results = await Task.WhenAll(tasks);

                // Flatten the list of lists into a single list of all generated image paths
                foreach (var resultList in results)
                {
                    allGeneratedImagePaths.AddRange(resultList);
                }
            }

            return allGeneratedImagePaths;
        }
    }
}