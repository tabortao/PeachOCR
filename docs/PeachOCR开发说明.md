

```bash
# 新建项目
dotnet new wpf -n PeachOCR --framework net8.0 -o PeachOCR
cd PeachOCR
# 添加依赖
dotnet add package OpenVINO.CSharp.API.Extensions.PaddleOCR
dotnet add package OpenVINO.runtime.win
dotnet add package OpenCvSharp4.runtime.win
dotnet add package Docnet.Core #用于PDF转图片
dotnet add package SixLabors.ImageSharp #用于PDF转图片


# 清理项目
Remove-Item -Recurse -Force .\bin, .\obj
# 构建项目
dotnet build
# 启动项目
dotnet run --no-build
# AOT编译
dotnet publish -c Release -r win-x64 --self-contained true
# 不包含.net运行时
dotnet publish -c Release -r win-x64 --self-contained false
```