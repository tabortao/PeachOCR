
using System;
using System.Collections.Generic;
using System.IO;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Bmp;

/// <summary>
/// PDF 批量转图片工具类，支持自定义分辨率、格式、输出目录，基于 Docnet.Core + ImageSharp 跨平台实现
/// </summary>
public class PDFToImage
{
    /// <summary>
    /// 转换参数配置
    /// </summary>
    public class ConvertOptions
    {
        /// <summary>输出图片分辨率（DPI）</summary>
        public int Dpi { get; set; } = 300;
        /// <summary>输出图片格式（png/jpg/bmp）</summary>
        public string ImageFormat { get; set; } = "png";
        /// <summary>JPEG 图片质量（1-100，仅对jpg有效）</summary>
        public int JpegQuality { get; set; } = 90;
        /// <summary>输出目录</summary>
        public string OutputDir { get; set; } = "Output";
    }

    /// <summary>
    /// 批量转换多个 PDF 文件为图片
    /// </summary>
    /// <param name="pdfPaths">PDF 文件路径集合</param>
    /// <param name="options">转换参数</param>
    public static void ConvertBatch(IEnumerable<string> pdfPaths, ConvertOptions? options = null)
    {
        options ??= new ConvertOptions();
        int total = 0;
        foreach (var pdfPath in pdfPaths)
        {
            total++;
            Console.WriteLine($"[{total}] 开始转换: {pdfPath}");
            Convert(pdfPath, options);
        }
        Console.WriteLine($"全部转换完成，共 {total} 个 PDF 文件。");
    }

    /// <summary>
    /// 单个 PDF 转图片，自动分文件夹输出
    /// </summary>
    /// <param name="pdfPath">PDF 文件路径</param>
    /// <param name="options">转换参数</param>
    public static void Convert(string pdfPath, ConvertOptions options)
    {
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"未找到文件: {pdfPath}");
            return;
        }
        var fileName = Path.GetFileNameWithoutExtension(pdfPath);
        var outDir = Path.Combine(options.OutputDir, fileName);
        Directory.CreateDirectory(outDir);

        using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(options.Dpi, options.Dpi));
        int pageCount = docReader.GetPageCount();
        Console.WriteLine($"  共 {pageCount} 页，输出目录: {outDir}");
        for (int i = 0; i < pageCount; i++)
        {
            Console.Write($"    正在转换第 {i + 1}/{pageCount} 页 ... ");
            using var pageReader = docReader.GetPageReader(i);
            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            // 用 ImageSharp 直接将 BGRA 字节流转为图片对象
            using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            string ext = options.ImageFormat.ToLower();
            string outPath = Path.Combine(outDir, $"page_{i + 1}.{ext}");
            switch (ext)
            {
                case "jpg":
                case "jpeg":
                    var jpegEncoder = new JpegEncoder { Quality = options.JpegQuality };
                    image.Save(outPath, jpegEncoder);
                    break;
                case "bmp":
                    image.Save(outPath, new BmpEncoder());
                    break;
                default:
                    image.Save(outPath, new PngEncoder());
                    break;
            }
            Console.WriteLine($"已保存: {outPath}");
        }
        Console.WriteLine($"  {pdfPath} 转换完成！\n");
    }

    // ImageSharp 不需要 AddBytes 和 GetEncoder
}
