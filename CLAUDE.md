# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PeachOCR is a .NET 8 WPF application for batch OCR (Optical Character Recognition) of images and PDF files. It integrates PaddleOCR with OpenVINO for high-performance text recognition, supporting both Chinese and English languages.

## Key Technologies

- **Framework**: .NET 8.0 WPF (Windows Presentation Foundation)
- **OCR Engine**: PaddleOCR with OpenVINO acceleration
- **Image Processing**: OpenCV (OpenCvSharp4)
- **PDF Processing**: PDFtoImage library with SkiaSharp
- **UI**: Modern dark-themed WPF interface

## Project Structure

```
PeachOCR/
├── App.xaml/cs              # Application entry point and startup
├── MainWindow.xaml/cs       # Main UI logic and event handlers
├── OCR/
│   └── OcrBatchProcessor.cs # Core OCR processing engine
├── PDF/
│   └── PDF2Image.cs         # PDF to image conversion utilities
├── models/                  # PaddleOCR model files (PP-OCRv4/v5)
├── Resources/               # Application resources (icons, images)
└── docs/                    # Documentation and screenshots
```

## Common Commands

### Build and Run
```bash
# Clean build artifacts
Remove-Item -Recurse -Force .\bin, .\obj

# Build the project
dotnet build

# Run the application
dotnet run --no-build

# Run with detailed logging
dotnet run --no-build --verbosity detailed
```

### Package Management
```bash
# Add required packages
dotnet add package OpenVINO.CSharp.API.Extensions.PaddleOCR
dotnet add package OpenVINO.runtime.win
dotnet add package OpenCvSharp4.runtime.win
dotnet add package PDFtoImage
dotnet add package Docnet.Core
dotnet add package SixLabors.ImageSharp

# Restore packages
dotnet restore
```

### Development Tasks
```bash
# Check for compilation errors
dotnet build --no-restore

# Clean solution
dotnet clean

# List project dependencies
dotnet list package
```

## Architecture Overview

### Core Components

1. **MainWindow.xaml.cs** - Main application logic
   - Handles file selection, drag-and-drop
   - Manages OCR processing workflow
   - Displays results and progress
   - Coordinates between UI and OCR engine

2. **OcrBatchProcessor.cs** - OCR processing engine
   - Supports PP-OCRv4 and PP-OCRv5 models
   - Handles GPU/CPU inference options
   - Processes images in parallel with configurable concurrency
   - Saves results to OCR_Result directories

3. **PDF2Image.cs** - PDF conversion utility
   - Converts PDF pages to images using PDFtoImage library
   - Supports multiple output formats (JPG, PNG, WebP, etc.)
   - Handles batch processing with concurrency control

### Key Features Implementation

- **Model Selection**: Switch between PP-OCRv4 and PP-OCRv5 via `OcrBatchProcessor.SetModel()`
- **GPU Acceleration**: Configured through `OcrBatchProcessor.SetUseGpu()`
- **Batch Processing**: Parallel OCR with semaphore-controlled concurrency
- **Result Management**: Automatic saving to OCR_Result folders with both individual and merged outputs
- **Progress Tracking**: Real-time progress updates via callback mechanism

## File Processing Workflow

1. **Input**: User selects images (JPG/PNG/BMP/TIFF/WEBP) or PDF files
2. **PDF Conversion**: PDF files are converted to images using PDF2Image.cs
3. **OCR Processing**: All images processed through OcrBatchProcessor with selected model
4. **Output**: Results saved as:
   - Individual TXT files per image in OCR_Result folder
   - Optional merged TXT file for all results
   - Optional annotated images with detection boxes

## Model Configuration

The application expects model files in the `models/` directory:

```
models/
├── ch_PP-OCRv4/
│   ├── PP-OCRv4_mobile_det_onnx.onnx
│   ├── PP-OCRv4_mobile_cls_onnx.onnx
│   ├── PP-OCRv4_mobile_rec_onnx.onnx
│   └── ppocr_keys_v1.txt
└── ch_PP-OCRv5/
    ├── PP-OCRv5_mobile_det_onnx.onnx
    ├── PP-OCRv5_mobile_cls_onnx.onnx
    ├── PP-OCRv5_mobile_rec_onnx.onnx
    └── ppocrv5_dict.txt
```

## Important Code Patterns

### Thread Safety
- OCR processing uses separate `OCRPredictor` instances per thread
- Progress updates use `Dispatcher.Invoke()` for UI thread safety
- File operations are protected with locks when necessary

### Error Handling
- Comprehensive parameter validation in PDF conversion methods
- Try-catch blocks around file operations and external process calls
- Graceful handling of missing files and invalid inputs

### Resource Management
- Proper disposal of `SKBitmap` objects in PDF conversion
- Using statements for file streams and other disposable resources
- Semaphore-based concurrency control to prevent resource exhaustion

## UI Controls Reference

Access UI elements using `FindName()` pattern to avoid partial class issues:
```csharp
var button = this.FindName("BtnOcr") as Button;
var progressBar = this.FindName("ProgressOcr") as ProgressBar;
var textBox = this.FindName("ListResultsTextBox") as TextBox;
```

Key UI elements:
- `ListImages` - File list display
- `ListResultsTextBox` - OCR results display
- `ProgressOcr` - Progress bar
- `ComboModel` - Model selection dropdown
- `CheckGpu` - GPU acceleration checkbox
- `CheckSaveResult` - Save result images checkbox

## Performance Considerations

- Default concurrency limit of 2 for OCR processing (configurable)
- GPU acceleration available but may not be supported on all systems
- Large PDF files are processed page-by-page to manage memory
- Results are saved incrementally during processing

## Common Issues and Solutions

1. **Missing Model Files**: Ensure model files are present in `models/` directory
2. **GPU Not Available**: Falls back to CPU processing automatically
3. **Large File Processing**: Consider reducing concurrency for memory-constrained systems
4. **PDF Conversion Errors**: Verify PDF file integrity and permissions

## Development Guidelines

- Use `async/await` pattern for all long-running operations
- Implement proper error handling with user-friendly messages
- Follow the existing `FindName()` pattern for UI element access
- Maintain thread safety when updating UI from background tasks
- Use the established folder structure for output files