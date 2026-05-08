# PeachOCR 更新日志

## TODO

- 增加在线API进行OCR（MinerU、PaddleOCR API）的功能
- <https://aistudio.baidu.com/paddleocr>
- 新增Windows本地OCR功能
- 探索更多AI功能集成可能性

### 0.8.2 - 2026-05-08

> 需要安装 .NET 10 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

- feat(设置): 添加DeepSeek支持并优化模型选择逻辑，允许用户自定义DeepSeek模型名称，默认使用deepseek-v4-flash
- feat(设置): 添加输出文件格式选项并支持Markdown格式

### 0.8.1 - 2026-05-07

c

![PeachOCR-UI](https://github.com/user-attachments/assets/08947dc5-1c1e-407f-a2a6-6d616b496ab0)

#### AI功能增强
- **feat(AI)**: 实现右键菜单功能并自动保存处理结果
- **feat(AI)**: 添加右键菜单的AI OCR增强功能
  - 实现通过右键菜单调用AI服务增强OCR识别文本的功能
  - 移除原有的AI增强按钮，改为右键菜单选项
  - 添加输入验证和AI配置检查
  - 优化界面布局，调整垂直对齐方式

#### 文本处理功能
- **feat(文本处理)**: 为OCR结果文本框添加右键菜单功能
  - 添加包含复制文本、AI分析总结和AI翻译功能的右键菜单
  - 实现菜单项点击事件处理逻辑，包括错误处理和状态更新
  - 所有AI处理结果自动保存到对应文件

#### 用户体验优化
- **feat(UI)**: 在文件列表中添加双击打开功能，使用系统默认程序打开文件
- **fix(布局)**: 调整待处理文件列表和识别结果文本框高度一致


### 0.8.0 - 2026-05-06

> 需要安装 .NET 10 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

#### 框架升级
- **upgrade**: 将项目从.NET 8升级到.NET 10
- **update**: 更新项目版本号至0.8.0

#### AI功能集成
- **feat(AI)**: 添加AI集成功能支持OCR增强、文本分析和翻译
  - 集成OpenAI兼容API接口
  - 实现AIService服务类
  - 添加AI设置窗口和配置管理
  - 支持自定义提示词模板

### 0.7.0 - 2026-05-06

> 需要安装 .NET 8 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)

#### 依赖更新
- **update**: 更新项目依赖包至最新版本
  - OpenCvSharp4最新版
  - OpenVINO最新版
  - PDFtoImage最新版
  - SixLabors.ImageSharp升级至3.1.12版本

#### 功能增强
- **feat(文件管理)**: 添加右键菜单删除选中文件功能
  - 支持从文件列表中删除单个文件
  - 保持UI状态同步更新

### 0.6.0 - 2025-07-23

#### 性能优化
- **perf(PDF)**: 优化PDF转图片功能
  - 提升转换清晰度
  - 优化转换速度
  - 改进内存管理

### 0.5.0 - 2025-07-07

> 需要安装 .NET 8 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)

![PeachOCR-UI](https://github.com/user-attachments/assets/020efca7-3cfe-4bdc-b518-78ff2ef809e8)

#### 用户体验
- **fix(UI)**: 修复UI显示不全的问题
- **feat(交互)**: 双击结果打开对应的txt文件
  - 使用系统默认程序打开识别结果

### 0.4.0 - 2025-07-07

#### 功能扩展
- **feat(PDF)**: 支持PDF文件识别
  - 集成PDFtoImage库进行PDF转换
  - 支持多页PDF文件处理

#### 问题修复
- **fix(显示)**: 修复识别结果显示不全的问题

### 0.3.0 - 2025-07-02

#### 版本管理
- **feat(版本)**: 支持动态显示软件版本
  - 自动读取程序集版本信息
  - 在窗口标题中显示版本号

#### 文档完善
- **docs**: 完善项目版本说明和文档

### 0.2.0 - 2025-07-02

#### 交互优化
- **feat(拖拽)**: 支持拖入文件功能
  - 支持拖拽添加图片和PDF文件
  - 自动过滤支持的文件类型

#### UI改进
- **ui(进度条)**: 优化进度条显示
  - 改进进度条样式和动画效果
  - 实时显示处理进度百分比

### 0.1.0 - 2025-07-01

#### OCR引擎
- **feat(模型)**: 添加PP-OCRv5模型支持
  - 支持PP-OCRv4和PP-OCRv5模型切换
  - 优化模型加载和推理性能

#### 性能优化
- **feat(GPU)**: 添加GPU加速支持
  - 集成OpenVINO GPU推理
  - 自动检测GPU可用性

#### 用户界面
- **ui**: 优化UI显示
  - 现代化深色主题
  - 改进控件布局和样式

