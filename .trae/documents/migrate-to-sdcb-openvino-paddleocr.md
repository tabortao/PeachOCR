# Plan: Migrate to Sdcb.OpenVINO.PaddleOCR & Add PP-OCRv6

## Summary

Replace the current `OpenVINO.CSharp.API.Extensions.PaddleOCR` + `OpenVINO.runtime.win` stack with `Sdcb.OpenVINO.PaddleOCR` + `Sdcb.OpenVINO.PaddleOCR.Models.Online` + `Sdcb.OpenVINO.runtime.win-x64`. Add PP-OCRv6 as a new local model option and set it as the default. Migrate PP-OCRv4 and PP-OCRv5 to also use the new library, reducing redundant packages and install size.

## Current State (已完成的工作)

### NuGet Packages (`PeachOCR.csproj`)
- 已移除: `OpenVINO.CSharp.API.Extensions.PaddleOCR` (1.0.3), `OpenVINO.runtime.win` (2025.1.0.1)
- 已添加: `Sdcb.OpenVINO.PaddleOCR` (0.8.0), `Sdcb.OpenVINO.PaddleOCR.Models.Online` (0.8.0), `Sdcb.OpenVINO.runtime.win-x64` (2026.2.0)

### UI Changes (`MainWindow.xaml`)
- ComboBox 已更新: PP-OCRv6(本地) [index 0], PP-OCRv5(本地) [1], PP-OCRv4(本地) [2], WeChat-OCR(本地) [3], PP-OCR-VL(在线) [4]
- 默认 SelectedIndex="0"

### MainWindow.xaml.cs
- 模型分派逻辑已更新: SelectedIndex 0→v6, 1→v5, 2→v4, 3→WeChat, 4→online

### CLI
- `CLI/PeachOcrCli.cs`: 帮助文本已更新，默认模型已改为 v6
- `CLI/OcrCommandHandler.cs`: 已添加 v6 case，默认改为 v6

### ChangeLogs
- `docs/ChangeLogs.md`: 0.8.10 条目已添加

### 待修复: OcrBatchProcessor.cs 编译错误

`dotnet build` 报错:
```
OCR\OcrBatchProcessor.cs(105,80): error CS0117: "OnlineFullModels"未包含"ChineseV5"的定义
```

## API 发现 (通过反射检查 Sdcb.OpenVINO.PaddleOCR 0.8.0)

### OnlineFullModels 可用字段
| 字段 | 是否存在 |
|------|---------|
| `ChineseV4` | **存在** |
| `ChineseV5` | **不存在** |
| `ChineseV6Small` | **存在** |
| `ChineseV6Medium` | **存在** |
| `ChineseV6Tiny` | **存在** |

### ModelVersion 枚举值
`V2, V3, V4, V6` — **没有 V5**

### FullOcrModel.FromDirectory 重载
1. `FromDirectory(string modelFolderPath, string labelFilePath, ModelVersion version)` — 单目录
2. `FromDirectory(string detectionModelDir, string classificationModelDir, string recognitionModelDir, string labelFilePath, ModelVersion version)` — 分离目录

## 待修复: OcrBatchProcessor.cs 模型加载策略

### 当前代码 (有编译错误)
```csharp
ModelType.PP_OCRv4 => cachedV4Model ??= await OnlineFullModels.ChineseV4.DownloadAsync(),  // OK
ModelType.PP_OCRv5 => cachedV5Model ??= await OnlineFullModels.ChineseV5.DownloadAsync(),  // ❌ 编译错误
ModelType.PP_OCRv6 => cachedV6Model ??= await OnlineFullModels.ChineseV6Small.DownloadAsync(), // OK
```

### 修复方案

**PP-OCRv4**: 使用 `OnlineFullModels.ChineseV4.DownloadAsync()` — 字段存在，首次使用自动下载并缓存，无需本地模型文件。

**PP-OCRv5**: 由于 `OnlineFullModels.ChineseV5` 不存在且 `ModelVersion` 无 `V5`，改用 `FullOcrModel.FromDirectory` 加载本地 ONNX 模型文件。使用 `ModelVersion.V4` 兼容模式（v4 和 v5 的 ONNX 模型结构兼容）。

**PP-OCRv6**: 使用 `OnlineFullModels.ChineseV6Small.DownloadAsync()` — 已正确。

### 修复后代码
```csharp
private async Task<FullOcrModel> GetModelAsync()
{
    return modelType switch
    {
        ModelType.PP_OCRv4 => cachedV4Model ??= await OnlineFullModels.ChineseV4.DownloadAsync(),
        ModelType.PP_OCRv5 => cachedV5Model ??= Task.Run(() => 
            FullOcrModel.FromDirectory(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ch_PP-OCRv5"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ch_PP-OCRv5", "ppocrv5_dict.txt"),
                ModelVersion.V4)).Result,
        ModelType.PP_OCRv6 => cachedV6Model ??= await OnlineFullModels.ChineseV6Small.DownloadAsync(),
        _ => throw new InvalidOperationException($"未知模型类型: {modelType}")
    };
}
```

注意：由于 `FullOcrModel.FromDirectory` 是同步方法，而 `GetModelAsync` 是 async，需要用 `Task.Run` 包装以避免阻塞。

## 修改文件清单

| 文件 | 修改内容 | 状态 |
|------|---------|------|
| `PeachOCR.csproj` | 替换 NuGet 包 | ✅ 已完成 |
| `OCR/OcrBatchProcessor.cs` | 修复 v5 模型加载 (line 105) | ❌ 待修复 |
| `MainWindow.xaml` | 更新 ComboBox | ✅ 已完成 |
| `MainWindow.xaml.cs` | 更新模型分派逻辑 | ✅ 已完成 |
| `CLI/PeachOcrCli.cs` | 更新帮助文本和默认模型 | ✅ 已完成 |
| `CLI/OcrCommandHandler.cs` | 添加 v6 case | ✅ 已完成 |
| `docs/ChangeLogs.md` | 添加 0.8.10 条目 | ✅ 已完成 |

## 验证步骤

1. `dotnet build` — 确认无编译错误
2. `dotnet run` — 确认 UI 显示 PP-OCRv6 为默认
3. 测试 PP-OCRv6 OCR（首次运行会下载模型）
4. 测试 PP-OCRv5 OCR（验证本地模型加载）
5. 测试 PP-OCRv4 OCR（验证在线下载）
6. 测试 CLI: `PeachOCR ocr -i test.jpg -m v6`