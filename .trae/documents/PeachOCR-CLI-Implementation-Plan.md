# PeachOCR CLI 功能实现计划

## 需求分析

为PeachOCR添加CLI（命令行界面）功能，使AI或自动化工具能够通过命令行调用进行OCR识别，而无需启动GUI界面。

## 技术方案

### 方案选择：混合启动模式
在现有WPF项目中集成CLI功能，通过启动参数判断：
- **无参数或特定GUI参数**：启动原有WPF界面
- **CLI参数**：以命令行模式执行OCR任务

### 选择理由
1. **代码复用**：直接复用现有的`OcrBatchProcessor`核心逻辑
2. **单一项目**：无需维护多个项目
3. **灵活性**：用户可根据需要选择GUI或CLI模式
4. **简化部署**：一个可执行文件支持两种模式

## 实现步骤

### 1. 添加CLI解析库
- 使用 `System.CommandLine`（微软官方库，.NET 10内置支持）
- NuGet包：`System.CommandLine`

### 2. 创建CLI入口和命令解析模块
新建文件：`CLI/PeachOcrCli.cs`
- 命令行参数定义
- 命令解析逻辑
- 帮助信息生成

### 3. 设计CLI命令结构
```
PeachOCR <command> [options]

Commands:
  ocr          识别图片或PDF文件中的文字
  version      显示版本信息
  help         显示帮助信息

OCR命令选项:
  -i, --input <path>           输入文件或目录路径（必需）
  -o, --output <path>          输出目录（默认：与输入文件同目录）
  -m, --model <name>           OCR模型：v4, v5, wechat, online（默认：v4）
  -f, --format <type>          输出格式：txt, md, json（默认：txt）
  --gpu                        启用GPU加速（默认：禁用）
  -c, --concurrency <num>      并发处理数（默认：2）
  -v, --verbose                显示详细输出
  --merge                      合并所有结果到单个文件
  -h, --help                   显示帮助信息
```

### 4. 修改应用启动逻辑
修改文件：`App.xaml.cs`
- 检测启动参数
- 根据参数决定启动模式（CLI/GUI）
- CLI模式下处理完成后直接退出

### 5. 实现OCR命令处理器
新建文件：`CLI/OcrCommandHandler.cs`
- 解析输入文件/目录
- 处理PDF转换（如需要）
- 调用OcrBatchProcessor执行识别
- 管理输出格式和文件保存
- 显示进度信息

### 6. 添加JSON输出支持
扩展现有OCR处理逻辑：
- 添加JSON序列化支持
- 输出结构化结果（文件路径、识别文本、可信度等）

### 7. 测试CLI功能
- 测试各种命令组合
- 验证输出格式
- 验证错误处理

## 核心功能特性

### 文件支持
- 图片格式：JPG, PNG, BMP, TIFF, WEBP
- PDF文件：自动转换为图片后识别

### OCR模型
| 模型 | 说明 | 本地/在线 |
|------|------|-----------|
| v4 | PP-OCRv4（推荐） | 本地 |
| v5 | PP-OCRv5 | 本地 |
| wechat | WeChat OCR | 本地 |
| online | PP-OCR-VL API | 在线 |

### 输出格式
- **TXT**：纯文本，每行一个识别结果
- **MD**：Markdown格式，带文件标题
- **JSON**：结构化数据，包含详细信息

### 使用场景示例

**单文件识别：**
```bash
PeachOCR ocr -i screenshot.png
```

**目录批量识别：**
```bash
PeachOCR ocr -i ./images -o ./results -m v5 --gpu
```

**PDF识别并输出JSON：**
```bash
PeachOCR ocr -i document.pdf -f json -v
```

**批量处理并合并结果：**
```bash
PeachOCR ocr -i ./images --merge -f md -o combined_results.md
```

## 文件结构

```
PeachOCR/
├── App.xaml.cs                      # 修改：添加CLI启动逻辑
├── CLI/
│   ├── PeachOcrCli.cs               # 新建：CLI入口和命令定义
│   └── OcrCommandHandler.cs        # 新建：OCR命令处理器
├── OCR/
│   └── OcrBatchProcessor.cs         # 修改：添加JSON输出支持
└── PeachOCR.csproj                 # 修改：添加System.CommandLine依赖
```

## 技术细节

### 启动参数检测
```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    if (e.Args.Length > 0 && e.Args[0] != "gui")
    {
        // CLI模式：处理完成后退出
        var cli = new PeachOcrCli();
        int exitCode = cli.Run(e.Args);
        Shutdown(exitCode);
        return;
    }
    // GUI模式：正常启动
    base.OnStartup(e);
}
```

### 进度显示
- CLI模式使用Console.WriteLine输出进度
- 支持 `-v/--verbose` 参数控制详细程度

### 错误处理
- 统一的错误输出（Console.Error）
- 详细的错误信息和建议
- 异常不会被吞掉，确保CLI返回正确的退出码

## 验证方法

1. **编译验证**：`dotnet build` 无错误
2. **帮助信息**：`PeachOCR --help`
3. **版本信息**：`PeachOCR version`
4. **功能测试**：
   - 单文件OCR识别
   - 目录批量处理
   - PDF文件处理
   - JSON格式输出
   - GPU加速开关

## 注意事项

1. **工作目录**：CLI需要正确设置工作目录以访问models文件夹
2. **依赖项**：确保所有模型文件存在于models/目录
3. **在线OCR**：使用online模型时需要配置API密钥
4. **并发控制**：合理设置并发数避免资源耗尽

## 实施优先级

1. **Phase 1（核心功能）**
   - 添加依赖库
   - 创建CLI基础框架
   - 实现基础ocr命令

2. **Phase 2（完善功能）**
   - 添加JSON输出
   - 实现目录批量处理
   - 添加进度显示

3. **Phase 3（优化体验）**
   - 完善帮助信息
   - 错误处理优化
   - 测试验证

## 预期成果

实现后，用户和AI可以通过以下方式调用PeachOCR：
- `PeachOCR ocr -i image.png`
- `PeachOCR ocr -i ./folder --model v5 --gpu`
- `PeachOCR ocr -i doc.pdf -f json`

无需启动GUI，直接返回OCR结果或保存到文件。
