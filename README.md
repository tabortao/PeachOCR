# PeachOCR

PeachOCR 是一个基于 .NET 10 WPF 框架开发的本地批量图片/PDF文字识别工具，集成了 PaddleOCR、OpenVINO、OpenCV等高性能推理与图像处理库，支持中文、英文等多语言文本识别，并集成AI功能增强文本处理能力。

关注公众号“**可持续学园**”，回复“**PeachOCR**”，获取软件下载地址。

![PeachOCR-UI](https://github.com/user-attachments/assets/63c8c3c4-5fda-4ebd-879d-7fc2d47eed16)

## 主要特性

- 支持批量图片（JPG/PNG/BMP/TIFF/WEBP）和 PDF 文件文字识别
- 支持 PP-OCRv4 / PP-OCRv5 PaddleOCR 模型切换
- 支持 GPU 加速与 CPU 推理
- 支持识别结果图片保存、单文件/合并TXT导出
- 识别进度实时显示，结果可按文件名切换查看
- 现代化深色UI，操作简洁
- **AI功能集成**：
  - AI OCR增强：提升识别文本质量
  - AI文本分析：智能分析和总结文本内容
  - AI翻译：多语言文本翻译
  - 右键菜单快速访问所有AI功能
  - 自动保存AI处理结果到文件

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
- 选择图片或PDF：批量添加待识别文件
- 清除列表：清空待识别文件
- 模型选择：PP-OCRv4/PP-OCRv5
- 合并为单个文件：将所有识别文本合并导出为一个TXT
- 保存处理图片：保存带检测框的结果图片
- 启用GPU加速：如显卡支持可勾选
- ⚙️ AI设置：配置AI服务参数
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
- 每张图片识别结果会自动保存在同目录下 `OCR_Result` 文件夹内（TXT/图片）
- 合并TXT会自动保存到 `OCR_Result/OCR_Result_Merged.txt`
- AI处理结果会更新到对应的TXT文件中，格式为：原文 + 分隔符 + AI处理结果

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

- 欢迎关注公众号“可持续学园”，回复“PeachOCR”获取最新软件下载地址。

![微信公众号](./docs/images/微信公众号.jpg)