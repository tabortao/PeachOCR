# PeachOCR CLI 命令行使用说明

## 目录

- [编译](#编译)
- [运行方式](#运行方式)
- [CLI命令说明](#cli命令说明)
- [使用示例](#使用示例)
- [输入输出格式](#输入输出格式)
- [AI调用指南](#ai调用指南)

---

## 编译

### 前提条件
- .NET 10 SDK (必须安装)
- 项目代码已克隆到本地

### 编译步骤

```bash
# 进入项目目录
cd d:\MyData\01_Projects\Code\PeachOCR

# 编译项目
dotnet build

# 或者编译 Release 版本
dotnet build -c Release
```

### 编译输出位置
- Debug版本：`d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows\PeachOCR.exe`
- Release版本：`d:\MyData\01_Projects\Code\PeachOCR\bin\Release\net10.0-windows\PeachOCR.exe`

---

## 运行方式

### 方式一：dotnet run（推荐开发时使用）
```bash
cd d:\MyData\01_Projects\Code\PeachOCR
dotnet run -- --help
dotnet run -- ocr -i "path/to/image.webp"
```

### 方式二：直接运行exe
```bash
cd d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows
.\PeachOCR.exe --help
.\PeachOCR.exe ocr -i "path/to/image.webp"
```

### 方式三：添加到PATH环境变量（推荐）

1. 右键「此电脑」→「属性」→「高级系统设置」→「环境变量」
2. 在「系统变量」中找到「Path」，点击「编辑」
3. 点击「新建」，添加以下路径：
   ```
   d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows
   ```
4. 保存后，重启终端，即可在任意位置运行：
   ```bash
   PeachOCR --help
   ```

---

## CLI命令说明

### 命令列表

| 命令 | 说明 |
|-----|------|
| `ocr` | 识别图片或PDF文件中的文字 |
| `version` | 显示版本信息 |
| `help [topic]` | 显示帮助信息 |

---

### OCR命令参数

| 参数 | 简写 | 说明 | 必需 | 默认值 |
|-----|-----|-----|-----|-----|
| `--input` | `-i` | 输入文件或目录路径 | ✅ | - |
| `--output` | `-o` | 输出目录或文件路径 | ❌ | 与输入同目录 |
| `--model` | `-m` | OCR模型（v4/v5/wechat/online） | ❌ | v4 |
| `--format` | `-f` | 输出格式（txt/md/json） | ❌ | txt |
| `--gpu` | - | 启用GPU加速 | ❌ | false |
| `--concurrency` | `-c` | 并发处理数 | ❌ | 2 |
| `--verbose` | `-v` | 显示详细输出 | ❌ | false |
| `--merge` | - | 合并所有结果到单个文件 | ❌ | false |

---

## 使用示例

### 基础使用

#### 单文件识别
```bash
# 识别图片
PeachOCR ocr -i "D:\Yao\Pictures\screenshot.webp"

# 识别PDF
PeachOCR ocr -i "D:\Documents\report.pdf"
```

#### 批量识别
```bash
# 识别整个目录
PeachOCR ocr -i "D:\Pictures\Screenshots"

# 指定输出目录
PeachOCR ocr -i "D:\Pictures\Screenshots" -o "D:\OCR_Results"
```

---

### 模型选择

| 模型 | 说明 | 命令 |
|-----|------|------|
| v4 | PP-OCRv4（默认，推荐） | `-m v4` |
| v5 | PP-OCRv5（高精度） | `-m v5` |
| wechat | WeChat-OCR | `-m wechat` |
| online | PP-OCR-VL API | `-m online` |

```bash
# 使用v5模型
PeachOCR ocr -i "image.webp" -m v5

# 启用GPU加速
PeachOCR ocr -i "image.webp" --gpu

# 组合使用
PeachOCR ocr -i "image.webp" -m v5 --gpu -v
```

---

### 输出格式

| 格式 | 说明 | 命令 |
|-----|------|------|
| txt | 纯文本（默认） | `-f txt` |
| md | Markdown格式 | `-f md` |
| json | JSON格式（适合程序处理） | `-f json` |

```bash
# TXT格式
PeachOCR ocr -i "image.webp" -f txt

# Markdown格式
PeachOCR ocr -i "image.webp" -f md

# JSON格式
PeachOCR ocr -i "image.webp" -f json
```

#### JSON格式示例输出
```json
[
  {
    "filePath": "D:\\Yao\\Pictures\\test.webp",
    "fileName": "test.webp",
    "processingTimeMs": 1234,
    "textCount": 5,
    "texts": [
      {"text": "第一行文字", "confidence": 0.95},
      {"text": "第二行文字", "confidence": 0.92}
    ]
  }
]
```

---

### 合并结果

```bash
# 合并到单个文件
PeachOCR ocr -i "D:\Pictures\Screenshots" --merge -o "combined_result.txt"

# Markdown格式合并
PeachOCR ocr -i "D:\Pictures\Screenshots" --merge -f md -o "report.md"
```

---

### 帮助命令

```bash
# 显示通用帮助
PeachOCR help

# 显示OCR命令详细帮助
PeachOCR help ocr

# 显示模型说明
PeachOCR help models

# 显示输出格式说明
PeachOCR help format
```

---

## 输入输出格式

### 支持的输入格式
- **图片格式**：JPG、PNG、BMP、TIFF、WEBP
- **文档格式**：PDF（自动转换为图片）

### 输出位置规则
- 如果未指定`-o`：结果保存在输入文件同目录的 `OCR_Result` 文件夹内
- 如果指定`-o`为目录：结果保存在指定目录内
- 如果指定`-o`为文件且使用`--merge`：所有结果合并到该文件

---

## AI调用指南

### 方式一：通过文件获取结果（推荐）

AI调用CLI后，读取输出文件中的结果：

```python
import subprocess
import os

def run_ocr(image_path, output_dir=None):
    """
    调用PeachOCR CLI进行OCR识别
    
    Args:
        image_path: 图片路径
        output_dir: 输出目录（可选）
    
    Returns:
        (成功标识, 识别结果, 输出文件路径)
    """
    exe_path = r"d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows\PeachOCR.exe"
    
    # 构建命令
    cmd = [exe_path, "ocr", "-i", image_path, "-f", "txt", "-v"]
    if output_dir:
        cmd.extend(["-o", output_dir])
    
    # 执行命令
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    # 解析结果
    if result.returncode == 0:
        # 获取输出文件路径
        image_dir = os.path.dirname(image_path)
        image_name = os.path.splitext(os.path.basename(image_path))[0]
        if output_dir:
            result_dir = output_dir
        else:
            result_dir = os.path.join(image_dir, "OCR_Result")
        
        result_file = os.path.join(result_dir, f"{image_name}.txt")
        
        # 读取结果
        if os.path.exists(result_file):
            with open(result_file, "r", encoding="utf-8") as f:
                ocr_text = f.read()
            return True, ocr_text, result_file
    
    return False, result.stderr, None

# 使用示例
success, text, result_file = run_ocr(r"D:\Yao\Pictures\test.webp")
if success:
    print(f"识别成功！结果保存在: {result_file}")
    print("识别内容:")
    print(text)
else:
    print(f"识别失败: {text}")
```

### 方式二：通过JSON获取结构化结果

```python
import subprocess
import json
import os

def run_ocr_json(image_path):
    exe_path = r"d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows\PeachOCR.exe"
    
    cmd = [exe_path, "ocr", "-i", image_path, "-f", "json", "-v"]
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode == 0:
        image_dir = os.path.dirname(image_path)
        image_name = os.path.splitext(os.path.basename(image_path))[0]
        result_file = os.path.join(image_dir, "OCR_Result", f"{image_name}.json")
        
        if os.path.exists(result_file):
            with open(result_file, "r", encoding="utf-8") as f:
                ocr_result = json.load(f)
            return True, ocr_result
    
    return False, result.stderr

# 使用示例
success, ocr_data = run_ocr_json(r"D:\Yao\Pictures\test.webp")
if success:
    print("识别成功！")
    for item in ocr_data:
        print(f"文件: {item['fileName']}")
        print(f"识别文字数: {item['textCount']}")
        print("内容:")
        for text in item['texts']:
            print(f"  - {text['text']} (置信度: {text['confidence']})")
```

### 方式三：批量识别

```python
import subprocess
import os

def batch_ocr(input_dir, output_dir, model="v4", format="txt"):
    exe_path = r"d:\MyData\01_Projects\Code\PeachOCR\bin\Debug\net10.0-windows\PeachOCR.exe"
    
    cmd = [
        exe_path, "ocr",
        "-i", input_dir,
        "-o", output_dir,
        "-m", model,
        "-f", format,
        "-v"
    ]
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.returncode == 0, result.stdout, result.stderr

# 使用示例
success, stdout, stderr = batch_ocr(
    r"D:\Pictures\Screenshots",
    r"D:\OCR_Results",
    model="v5",
    format="md"
)
print(success, stdout, stderr)
```

---

## 故障排除

### 问题：无法找到PeachOCR命令
**解决**：使用完整路径运行，或将输出目录添加到PATH环境变量

### 问题：识别后没有输出文件
**解决**：
1. 检查输入路径是否正确
2. 使用 `-v` 参数查看详细输出
3. 确认OCR模型是否正确配置

### 问题：PDF识别失败
**解决**：确认PDF库是否正确安装，或先将PDF转换为图片

---

## 更新日志

- 2026-05-29: 初始版本
