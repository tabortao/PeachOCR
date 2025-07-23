# PeachOCR

PeachOCR 是一个基于 .NET 8 WPF 框架开发的本地批量图片/PDF文字识别工具，集成了 PaddleOCR、OpenVINO、OpenCV等高性能推理与图像处理库，支持中文、英文等多语言文本识别。

关注公众号“**可持续学园**”，回复“**PeachOCR**”，获取软件下载地址。

![PeachOCR-UI](https://github.com/user-attachments/assets/63c8c3c4-5fda-4ebd-879d-7fc2d47eed16)

## 主要特性

- 支持批量图片（JPG/PNG/BMP/TIFF/WEBP）和 PDF 文件文字识别
- 支持 PP-OCRv4 / PP-OCRv5 PaddleOCR 模型切换
- 支持 GPU 加速与 CPU 推理
- 支持识别结果图片保存、单文件/合并TXT导出
- 识别进度实时显示，结果可按文件名切换查看
- 现代化深色UI，操作简洁

## 快速开始

### 环境要求
- Windows 10/11
- .NET 8.0 SDK
- 依赖包：OpenVINO.CSharp.API.Extensions.PaddleOCR、OpenVINO.runtime.win、OpenCvSharp4.runtime.win

### 构建与运行

```bash
# 新建项目
# dotnet new wpf -n PeachOCR --framework net8.0 -o PeachOCR
# cd PeachOCR
# 添加依赖
# dotnet add package OpenVINO.CSharp.API.Extensions.PaddleOCR
# dotnet add package OpenVINO.runtime.win
# dotnet add package OpenCvSharp4.runtime.win

# 清理项目
Remove-Item -Recurse -Force .\bin, .\obj
# 构建项目
dotnet build
# 启动项目
dotnet run --no-build
```

### 主要界面说明
- 选择图片或PDF：批量添加待识别文件
- 清除列表：清空待识别文件
- 模型选择：PP-OCRv4/PP-OCRv5
- 合并为单个文件：将所有识别文本合并导出为一个TXT
- 保存处理图片：保存带检测框的结果图片
- 启用GPU加速：如显卡支持可勾选
- 进度条/状态栏：显示识别进度与结果存储路径

### 识别结果存储
- 每张图片识别结果会自动保存在同目录下 `OCR_Result` 文件夹内（TXT/图片）
- 合并TXT会自动保存到 `OCR_Result/OCR_Result_Merged.txt`

## 依赖模型
- `models/ch_PP-OCRv4/` 及 `models/ch_PP-OCRv5/` 下需放置对应 onnx 模型和字典文件

## 🙏感谢
- [PaddleOCR-OpenVINO-CSharp](https://github.com/guojin-yan/PaddleOCR-OpenVINO-CSharp)
