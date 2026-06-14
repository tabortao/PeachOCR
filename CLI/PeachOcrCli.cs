using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PeachOCR.CLI
{
    public class PeachOcrCli
    {
        public async Task<int> Run(string[] args)
        {
            if (args.Length == 0)
            {
                ShowGeneralHelp();
                return 0;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "ocr":
                    return await RunOcrCommandAsync(args.Skip(1).ToArray());

                case "version":
                    ShowVersion();
                    return 0;

                case "help":
                    var topic = args.Length > 1 ? args[1] : null;
                    ShowHelp(topic);
                    return 0;

                case "--help":
                case "-h":
                case "/?":
                    ShowGeneralHelp();
                    return 0;

                case "--version":
                case "-v":
                    ShowVersion();
                    return 0;

                default:
                    Console.Error.WriteLine($"未知命令：{command}");
                    Console.Error.WriteLine("使用 'PeachOCR help' 查看帮助");
                    return 1;
            }
        }

        private async Task<int> RunOcrCommandAsync(string[] args)
        {
            var options = ParseOcrOptions(args);

            if (options == null)
            {
                ShowOcrHelp();
                return 1;
            }

            var handler = new OcrCommandHandler();
            return await handler.ExecuteAsync(options);
        }

        private OcrOptions? ParseOcrOptions(string[] args)
        {
            var options = new OcrOptions();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                switch (arg.ToLower())
                {
                    case "-i":
                    case "--input":
                        if (i + 1 < args.Length)
                        {
                            options.Input = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("错误：-i/--input 需要一个参数");
                            return null;
                        }
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            options.Output = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("错误：-o/--output 需要一个参数");
                            return null;
                        }
                        break;

                    case "-m":
                    case "--model":
                        if (i + 1 < args.Length)
                        {
                            options.Model = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("错误：-m/--model 需要一个参数");
                            return null;
                        }
                        break;

                    case "-f":
                    case "--format":
                        if (i + 1 < args.Length)
                        {
                            options.Format = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("错误：-f/--format 需要一个参数");
                            return null;
                        }
                        break;

                    case "-c":
                    case "--concurrency":
                        if (i + 1 < args.Length)
                        {
                            if (int.TryParse(args[++i], out int concurrency))
                            {
                                options.Concurrency = concurrency;
                            }
                            else
                            {
                                Console.Error.WriteLine("错误：-c/--concurrency 需要一个数字参数");
                                return null;
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("错误：-c/--concurrency 需要一个参数");
                            return null;
                        }
                        break;

                    case "--gpu":
                        options.Gpu = true;
                        break;

                    case "-v":
                    case "--verbose":
                        options.Verbose = true;
                        break;

                    case "--merge":
                        options.Merge = true;
                        break;

                    case "-h":
                    case "--help":
                        ShowOcrHelp();
                        return null;

                    default:
                        if (arg.StartsWith("-"))
                        {
                            Console.Error.WriteLine($"警告：未知选项 {arg}");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(options.Input))
                            {
                                options.Input = arg;
                            }
                            else
                            {
                                Console.Error.WriteLine($"警告：未知参数 {arg}");
                            }
                        }
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(options.Input))
            {
                Console.Error.WriteLine("错误：必须指定输入文件或目录（使用 -i 或 --input）");
                return null;
            }

            return options;
        }

        private void ShowVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"PeachOCR CLI v{version?.ToString(3) ?? "1.0.0"}");
            Console.WriteLine("基于 .NET 10 的批量图片/PDF文字识别工具");
        }

        private void ShowHelp(string? topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                ShowGeneralHelp();
            }
            else
            {
                switch (topic.ToLower())
                {
                    case "ocr":
                        ShowOcrHelp();
                        break;
                    case "models":
                        ShowModelsHelp();
                        break;
                    case "format":
                        ShowFormatHelp();
                        break;
                    default:
                        Console.WriteLine($"未知主题：{topic}");
                        Console.WriteLine("可用主题：ocr, models, format");
                        break;
                }
            }
        }

        private void ShowGeneralHelp()
        {
            Console.WriteLine(@"
PeachOCR - 批量图片/PDF文字识别工具

使用方法：
  PeachOCR <command> [options]

可用命令：
  ocr          识别图片或PDF文件中的文字
  version      显示版本信息
  help         显示帮助信息

OCR命令选项：
  -i, --input <path>           输入文件或目录路径（必需）
  -o, --output <path>          输出目录或文件路径（默认：与输入文件同目录）
  -m, --model <name>          OCR模型：v6, v5, v4, wechat, online（默认：v6）
  -f, --format <type>         输出格式：txt, md, json（默认：txt）
  --gpu                         启用GPU加速（默认：禁用）
  -c, --concurrency <num>     并发处理数（默认：2）
  -v, --verbose                显示详细输出
  --merge                      合并所有结果到单个文件

示例：
  PeachOCR ocr -i screenshot.png
  PeachOCR ocr -i ./images -o ./results -m v5 --gpu
  PeachOCR ocr -i document.pdf -f json -v
  PeachOCR ocr -i ./images --merge -f md -o combined_results.md

支持的OCR模型：
  v6       PP-OCRv6（推荐，默认，首次使用将自动下载模型）
  v5       PP-OCRv5
  v4       PP-OCRv4
  wechat   WeChat OCR
  online   PP-OCR-VL API（需要配置API密钥）

支持的输入格式：
  图片：JPG, PNG, BMP, TIFF, WEBP
  文档：PDF

更多信息请访问：https://github.com/your-repo/PeachOCR
");
        }

        private void ShowOcrHelp()
        {
            Console.WriteLine(@"
OCR 命令详解

用法：PeachOCR ocr -i <path> [options]

必需参数：
  -i, --input <path>    输入文件或目录路径

可选参数：
  -o, --output <path>   输出目录或文件路径
                        如果是目录，结果保存在该目录下
                        如果是文件路径，结果保存到该文件（需要配合 --merge）
                        默认值：与输入文件同目录的 OCR_Result 文件夹

  -m, --model <name>    OCR模型选择
                        可选值：v6, v5, v4, wechat, online
                        默认值：v6

  -f, --format <type>   输出格式
                        可选值：txt, md, json
                        默认值：txt

  --gpu                 启用GPU加速
                        使用Intel OpenVINO进行GPU推理

  -c, --concurrency <num>
                        并发处理的文件数量
                        默认值：2

  -v, --verbose         显示详细输出
                        包括处理进度、耗时统计等

  --merge               合并所有结果到单个文件
                        输出文件名由 -o 指定
");
        }

        private void ShowModelsHelp()
        {
            Console.WriteLine(@"
OCR 模型说明

v6 (PP-OCRv6)
  基于PaddleOCR的第六代模型
  最新的识别精度和速度平衡
  首次使用将自动下载模型文件（~15MB）
  推荐作为默认模型

v5 (PP-OCRv5)
  基于PaddleOCR的第五代模型
  更高的识别精度
  相对较慢的推理速度

v4 (PP-OCRv4)
  基于PaddleOCR的第四代模型
  平衡了速度和精度
  适用于大多数场景

wechat (WeChat OCR)
  使用Windows微信内置的OCR功能
  需要系统已安装微信
  通常具有较好的中文识别效果

online (PP-OCR-VL API)
  使用在线API进行识别
  需要配置API密钥
  支持更高级的文档理解功能
");
        }

        private void ShowFormatHelp()
        {
            Console.WriteLine(@"
输出格式说明

txt (纯文本格式)
  每行一个识别结果
  适用于简单的文本提取

md (Markdown格式)
  使用Markdown语法
  包含文件标题和层级结构
  便于文档整合

json (JSON格式)
  结构化输出
  包含文件路径、识别文本、可信度等信息
  适用于程序化处理和数据分析
");
        }
    }

    public class OcrOptions
    {
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Model { get; set; } = "v6";
        public string Format { get; set; } = "txt";
        public bool Gpu { get; set; }
        public int Concurrency { get; set; } = 2;
        public bool Verbose { get; set; }
        public bool Merge { get; set; }
    }
}
