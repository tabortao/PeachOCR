

```bash
# 新建项目
dotnet new wpf -n PeachOCR --framework net8.0 -o PeachOCR
cd PeachOCR
# 添加依赖
dotnet add package OpenVINO.CSharp.API.Extensions.PaddleOCR
dotnet add package OpenVINO.runtime.win
dotnet add package OpenCvSharp4.runtime.win

# 清理项目
Remove-Item -Recurse -Force .\bin, .\obj
# 构建项目
dotnet build
# 启动项目
dotnet run --no-build
dotnet run --no-build --project PeachOCR.csproj
```