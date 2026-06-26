# PeachOCR 更新日志

## TODO

- 新增Windows本地OCR功能
- 新增MinerU OCR功能

## 0.8.11 - 2026-06-26
- feat(模型): PP-OCRv4/v5模型预下载至本地models目录，减少首次运行等待
- feat(工具): 新增通用模型预下载工具 tools/download_models，支持 v4/v6 一键下载
- refactor(模型): PP-OCRv4/v5 使用 Paddle 格式 (.pdmodel)，PP-OCRv6 使用 ONNX 格式
- refactor(目录): v6模型统一收敛至 models/ch_PP-OCRv6/，与 v5 结构一致
- fix(设置): AI API 和 OCR API 配置解除互相绑定，任意填写一个即可保存
- docs: 新增模型下载工具说明 docs/模型下载工具说明.md
- chore: 清理废弃的 ch_PP-OCRv4/、ch_PP-OCRv5/ 空目录
- chore: 更新README模型相关说明

## 0.8.10 - 2026-06-14
- feat(OCR): 添加PP-OCRv6(本地)模型支持，基于Sdcb.OpenVINO.PaddleOCR，首次使用自动下载
- upgrade(OCR): 将OCR引擎从OpenVINO.CSharp.API.Extensions.PaddleOCR迁移至Sdcb.OpenVINO.PaddleOCR，减少冗余包和安装体积
- refactor(OCR): PP-OCRv4和PP-OCRv5迁移至新的Sdcb.OpenVINO.PaddleOCR API
- feat(UI): PP-OCRv6设为默认模型
- feat(CLI): CLI模型选项新增v6，默认模型改为v6
- fix(OCR): 修复OcrRegionResult属性引用的大小写问题(Text/Score/Box)

## 0.8.9 - 2026-05-28
- feat(OCR): 实现"合并为单个文件"功能，当设置中勾选此选项时，所有识别结果将合并保存到一个文件中
- feat(OCR): 合并文件支持txt和md格式，根据设置中的"输出文件格式"来决定保存为txt或md文件
- feat(OCR): 合并文件时，每个文件的识别结果前会添加"=== 文件名 ==="的标题，便于区分
- feat(OCR): 合并文件名包含时间戳，格式为"yyyy-MM-dd_HH-mm-ss_CombinedOCRResult"，避免覆盖之前的文件
- refactor(架构): 新增SaveOcrResults通用方法，统一处理两种模型（微信OCR和其他模型）的结果保存逻辑

## 0.8.8 - 2026-05-28
- refactor(界面): 将"合并为单个文件"、"保存处理图片"、"启用GPU加速"三个选项移动到设置页面
- refactor(架构): 新增AISettings.MergeIntoSingleFile、SaveProcessedImage、EnableGpu三个属性
- refactor(架构): 更新Settings.settings和Settings.Designer.cs，支持三个新设置项的持久化
- refactor(架构): 修改MainWindow使用AISettings中的属性替代原来的CheckBox控件

## 0.8.7 - 2026-05-28
- fix(微信OCR)：修复 WeChatOCR DLL 加载路径问题，在程序启动时设置正确的工作目录（https://www.nuget.org/packages/WeChatOcr、https://github.com/ZGGSONG/WeChatOCR/）
- feat(微信OCR)：支持批量处理多个文件，每个文件之间添加 100ms 间隔避免服务过载
- fix(微信OCR)：优化批量处理性能，缩短文件间延迟时间
- fix(WeChatOcrService)：简化实现，移除不必要的 DLL 复制逻辑（WeChatOCR 包已包含所需资源）

## 0.8.6 - 2026-05-28
- fix(微信OCR)：优化 WeChatOcr 服务，添加 DLL 文件检查复制、超时机制和任务防止重复
- fix(警告)：消除了所有编译警告

## 0.8.5 - 2026-05-28
- feat(快捷键截图)：快捷键截图 OCR 现在会使用与主界面模型选择下拉框相同的模型，包括 WeChat-OCR(本地)

## 0.8.4 - 2026-05-28
- feat(OCR结果): OCR完成后自动将识别结果复制到系统剪切板
- feat(OCR结果): 在结果显示窗口中自动复制结果到剪切板
- feat(状态栏): 更新状态栏文本，提示结果已复制到剪切板
- feat(微信OCR): 添加WeChat-OCR(本地)模型支持，使用WeChatOcr库调用电脑内置微信进行OCR识别

### 0.8.3 - 2026-05-27
- feat(截图): 新增截图OCR功能
- feat(截图): 新增区域选择截图功能（使用PracticalToolkit.Screenshot库 https://www.nuget.org/packages/PracticalToolkit.Screenshot）
- feat(快捷键): 添加截图OCR全局快捷键支持，可在设置中自定义快捷键，支持直接按键输入无需弹窗
- feat(快捷键): 优化快捷键功能，支持最小化到托盘时后台执行截图OCR，完成后弹出结果显示窗口
- feat(托盘): 添加系统托盘功能，点击关闭按钮最小化到托盘，双击托盘显示窗口，右键菜单可退出
- feat(设置): AI OCR配置中移除硅基流动选项，仅保留PaddleOCR（在线）
- feat(设置): 添加API密钥的显示/隐藏切换功能
- fix(快捷键): 修复快捷键触发时窗口激活问题，确保窗口正常响应
- 增加在线API进行OCR（MinerU、PaddleOCR API）的功能 <https://aistudio.baidu.com/paddleocr>

### 0.8.2 - 2026-05-08

> 需要安装 .NET 10 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

- feat(设置): 添加DeepSeek支持并优化模型选择逻辑，允许用户自定义DeepSeek模型名称，默认使用deepseek-v4-flash
- feat(设置): 添加输出文件格式选项并支持Markdown格式
- feat(OCR提示): 增强OCR优化提示文本的详细要求

### 0.8.1 - 2026-05-07

> 需要安装 .NET 10 桌面运行时，[点击下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

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
