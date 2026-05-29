# PeachOCR

PeachOCR 是一个基于 .NET 10 WPF 框架开发的本地批量图片/PDF文字识别工具，集成了 PaddleOCR、OpenVINO、OpenCV等高性能推理与图像处理库，支持中文、英文等多语言文本识别，并集成AI功能增强文本处理能力。

关注公众号"**可持续学园**"，回复"**PeachOCR**"，获取软件下载地址。

![PeachOCR-UI](https://github.com/user-attachments/assets/08947dc5-1c1e-407f-a2a6-6d616b496ab0)

## 主要特性

- 支持批量图片（JPG/PNG/BMP/TIFF/WEBP）和 PDF 文件文字识别
- 支持多种OCR模型：PP-OCRv4/PP-OCRv5（本地）、WeChat-OCR（本地）、PP-OCR-VL（在线）
- 支持 GPU 加速与 CPU 推理
- 支持截图OCR，快捷键触发，支持选择区域
- 识别结果支持单文件或合并输出（txt/md/json格式，合并文件自动添加时间戳）
- **AI功能集成**：OCR增强、文本分析、翻译，右键菜单快速访问
- **CLI命令行支持**：支持脚本化和自动化批量识别
- 现代化深色UI，操作简洁

## 快速开始

### 环境要求
- Windows 10/11
- .NET 10.0 SDK
- 依赖包：OpenVINO.CSharp.API.Extensions.PaddleOCR、OpenVINO.runtime.win、OpenCvSharp4.runtime.win
- AI功能需要配置支持的AI服务API（如OpenAI兼容接口）

### 构建与运行

```bash
# 清理项目
Remove-Item -Recurse -Force .\bin, .\obj

# 构建项目
dotnet build

# 启动项目
dotnet run --no-build

# AI功能调试（详细日志）
dotnet run --no-build --verbosity detailed
```

### 主要界面说明
- 模型选择：PP-OCRv4/PP-OCRv5（本地）、WeChat-OCR（本地）、PP-OCR-VL（在线）
- ⚙️ AI设置：OCR选项（合并为单个文件、保存处理图片、启用GPU加速）及AI服务参数配置
- 截图：支持区域选择截图OCR
- 进度条/状态栏：显示识别进度与结果存储路径

### AI功能使用
- **文件操作**：双击文件列表中的文件可用系统默认程序打开
- **右键菜单**：在识别结果区域右键点击可访问：
  - 📋 复制文本：复制识别结果到剪贴板
  - 🔍 AI OCR增强：使用AI优化OCR识别质量
  - 📊 AI分析总结：智能分析文本内容并生成总结
  - 🌐 AI翻译：将文本翻译成其他语言
- **结果保存**：所有AI处理结果自动保存到对应文件，保留原始内容并追加处理结果

### 识别结果存储
- 每张图片识别结果会自动保存在同目录下 `OCR_Result` 文件夹内
- 合并模式下生成带时间戳的文件，如 `OCR_Result/2026-05-29_14-30-00_CombinedOCRResult.txt`
- AI处理结果会更新到对应的TXT文件中

## CLI命令行使用

PeachOCR同时提供完整的CLI命令行接口，支持脚本化和自动化OCR识别任务。

### 快速开始

```bash
# 编译项目
cd d:\MyData\01_Projects\Code\PeachOCR
dotnet build

# 运行CLI命令（方式一：使用dotnet run）
dotnet run -- --help

# 运行CLI命令（方式二：直接运行exe）
cd bin\Debug\net10.0-windows
.\PeachOCR.exe --help
```

### 常用命令

```bash
# 识别单张图片
PeachOCR ocr -i "D:\Yao\Pictures\test.webp"

# 显示版本
PeachOCR version

# 显示帮助
PeachOCR help
```

### 详细文档

更详细的CLI使用说明、编译指南、AI调用示例，请查看：
[CLI使用说明](./docs/CLI使用说明.md)

## 依赖模型
- `models/ch_PP-OCRv4/` 及 `models/ch_PP-OCRv5/` 下需放置对应 onnx 模型和字典文件

## AI功能配置
AI功能需要配置支持OpenAI兼容接口的AI服务：
- 服务提供商API地址
- API密钥
- 模型名称
- 自定义提示词模板（OCR增强、分析、翻译）

## 感谢
- [PaddleOCR-OpenVINO-CSharp](https://github.com/guojin-yan/PaddleOCR-OpenVINO-CSharp)

## 软件下载

- 欢迎关注公众号"可持续学园"，回复"PeachOCR"获取最新软件下载地址。

![微信公众号](./docs/images/微信公众号.jpg)
