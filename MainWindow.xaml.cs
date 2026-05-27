using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using OCR;
using PDF;
using Microsoft.Extensions.AI;
using PracticalToolkit.Screenshot;

namespace PeachOCR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> selectedImages = new();
        // 存储每个文件的识别结果
        private Dictionary<string, List<string>> fileResultMap = new();
        // AI相关字段
        private AISettings? _aiSettings;
        private AIService? _aiService;
        // 注意：不要声明任何和XAML控件同名的字段，否则会导致自动生成失效

        // 双击识别结果区域，打开对应的txt文件

        private void ListResultsTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages?.SelectedIndex is int idx && idx >= 0 && idx < selectedImages.Count)
            {
                string filePath = selectedImages[idx];
                if (System.IO.File.Exists(filePath))
                {
                    string srcDir = System.IO.Path.GetDirectoryName(filePath) ?? string.Empty;
                    string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                    string name = System.IO.Path.GetFileNameWithoutExtension(filePath);

                    // Determine file extension based on output format setting
                    string extension = _aiSettings?.OutputFileFormat == "md文件" ? ".md" : ".txt";
                    string resultPath = System.IO.Path.Combine(resultDir, name + extension);

                    if (System.IO.File.Exists(resultPath))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = resultPath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"无法打开文件：{resultPath}\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"未找到对应的{extension}文件：{resultPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            // 初始化AI设置
            LoadAISettings();

            // 初始化控件状态（全部用FindName方式访问，避免partial字段丢失问题）
            var checkSaveResult = this.FindName("CheckSaveResult") as CheckBox;
            if (checkSaveResult != null) checkSaveResult.IsChecked = false;
            var checkMergeTxt = this.FindName("CheckMergeTxt") as CheckBox;
            if (checkMergeTxt != null) checkMergeTxt.IsChecked = false;
            var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
            if (txtFileStatus != null) txtFileStatus.Text = "未选择文件";
            this.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); };
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages != null) listImages.SelectionChanged += ListImages_SelectionChanged;
            UpdateListImagesHint();

            // 动态设置窗口标题，显示程序集版本
            try
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Title = $"PeachOCR 批量识别 v{ver?.ToString(3) ?? "?"}";
            }
            catch { /* ignore */ }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btnScreenshot = this.FindName("BtnScreenshot") as Button;
                if (btnScreenshot != null) btnScreenshot.IsEnabled = false;

                using var runner = new ScreenshotRunner();
                using var bitmap = runner.Screenshot();

                if (bitmap == null)
                {
                    if (btnScreenshot != null) btnScreenshot.IsEnabled = true;
                    return;
                }

                string tempDir = System.IO.Path.GetTempPath();
                string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = System.IO.Path.Combine(tempDir, fileName);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                selectedImages.Add(filePath);

                var listImages = this.FindName("ListImages") as ListBox;
                if (listImages != null)
                {
                    listImages.ItemsSource = null;
                    listImages.ItemsSource = selectedImages.Select(f => System.IO.Path.GetFileName(f));
                    listImages.SelectedIndex = selectedImages.Count - 1;
                }

                var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
                if (txtFileStatus != null) txtFileStatus.Text = $"已选择 {selectedImages.Count} 个文件";

                var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
                if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;

                fileResultMap.Clear();
                UpdateListImagesHint();

                if (btnScreenshot != null) btnScreenshot.IsEnabled = true;

                await PerformOcrAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                var btnScreenshot = this.FindName("BtnScreenshot") as Button;
                if (btnScreenshot != null) btnScreenshot.IsEnabled = true;
            }
        }

        private async Task PerformOcrAsync()
        {
            var comboModel = this.FindName("ComboModel") as ComboBox;
            var checkGpu = this.FindName("CheckGpu") as CheckBox;
            var checkSaveResult = this.FindName("CheckSaveResult") as CheckBox;
            var progressOcr = this.FindName("ProgressOcr") as ProgressBar;
            var listImages = this.FindName("ListImages") as ListBox;
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var btnOcr = this.FindName("BtnOcr") as Button;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;
            var btnScreenshot = this.FindName("BtnScreenshot") as Button;

            if (btnOcr != null) btnOcr.IsEnabled = false;
            if (btnScreenshot != null) btnScreenshot.IsEnabled = false;
            if (progressOcr != null) progressOcr.Value = 0;
            if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
            fileResultMap.Clear();
            if (statusBarText != null) statusBarText.Text = "正在识别...";

            var ocrWatch = System.Diagnostics.Stopwatch.StartNew();

            var pdfExts = new[] { ".pdf" };
            var pdfFiles = selectedImages.Where(f => pdfExts.Contains(System.IO.Path.GetExtension(f).ToLower())).ToList();
            var imageFiles = selectedImages.Where(f => !pdfExts.Contains(System.IO.Path.GetExtension(f).ToLower())).ToList();
            var allOcrImages = new List<string>();
            var pdfToTxtMap = new Dictionary<string, List<string>>();

            if (pdfFiles.Count > 0)
            {
                foreach (var pdf in pdfFiles)
                {
                    string pdfDir = System.IO.Path.GetDirectoryName(pdf) ?? "";
                    string pdfName = System.IO.Path.GetFileNameWithoutExtension(pdf);
                    string outDir = System.IO.Path.Combine(pdfDir, pdfName);
                    string imageFormat = "jpg";
                    int dpi = 250;
                    int jpegQuality = 90;
                    var pdfToImageTask = PDF.Convert.PDFToImagesAsync(new[] { pdf }, outDir, dpi, imageFormat, jpegQuality);
                    await pdfToImageTask;
                    List<string> imgs = new List<string>();
                    if (System.IO.Directory.Exists(outDir))
                    {
                        imgs = System.IO.Directory.GetFiles(outDir, $"*_page_*.{imageFormat}").OrderBy(f => f).ToList();
                    }
                    allOcrImages.AddRange(imgs);
                    pdfToTxtMap[pdf] = imgs;
                }
            }
            allOcrImages.AddRange(imageFiles);

            var processor = new OcrBatchProcessor();
            processor.SetModel(comboModel != null && comboModel.SelectedIndex == 0 ? OcrBatchProcessor.ModelType.PP_OCRv4 : OcrBatchProcessor.ModelType.PP_OCRv5);
            processor.SetUseGpu(checkGpu != null && checkGpu.IsChecked == true, checkGpu != null && checkGpu.IsChecked == true);
            processor.SetSaveResultImage(checkSaveResult != null && checkSaveResult.IsChecked == true);
            processor.SetOutputFileFormat(_aiSettings?.OutputFileFormat ?? "txt标准格式");

            if (comboModel != null && comboModel.SelectedIndex >= 2 && _aiSettings != null && !string.IsNullOrEmpty(_aiSettings.OcrApiUrl))
            {
                processor.SetOcrServiceConfig(_aiSettings.OcrServiceProvider, _aiSettings.OcrApiUrl, _aiSettings.OcrApiKey, _aiSettings.OcrModel);
            }

            processor.AddImages(allOcrImages);
            int total = allOcrImages.Count;
            var task = Task.Run(async () =>
            {
                var result = await processor.RunBatchOcrAsync(2, (done, all) =>
                {
                    if (progressOcr != null)
                        Dispatcher.Invoke(() =>
                        {
                            progressOcr.Value = all > 0 ? done * 100.0 / all : 0;
                        });
                });
                return result;
            });
            var result = await task;

            var imgToText = new Dictionary<string, List<string>>();
            foreach (var detail in result.details)
            {
                string fileName = System.IO.Path.GetFileName(detail.ImgPath);
                List<string> lines = new();
                if (detail.Result == null)
                {
                    lines.Add("识别失败");
                }
                else
                {
                    foreach (var r in detail.Result)
                    {
                        lines.Add(r.text);
                    }
                }
                imgToText[detail.ImgPath] = lines;
            }

            var txtPaths = new List<string>();
            var createdResultDirs = new HashSet<string>();

            foreach (var kv in pdfToTxtMap)
            {
                string pdfPath = kv.Key;
                var imgs = kv.Value;
                var allLines = new List<string>();
                foreach (var img in imgs)
                {
                    if (imgToText.TryGetValue(img, out var lines))
                        allLines.AddRange(lines);
                }
                string srcDir = System.IO.Path.GetDirectoryName(pdfPath) ?? "";
                string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                if (!createdResultDirs.Contains(resultDir))
                {
                    System.IO.Directory.CreateDirectory(resultDir);
                    createdResultDirs.Add(resultDir);
                }
                string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(pdfPath) + ".txt");
                System.IO.File.WriteAllLines(txtPath, allLines);
                fileResultMap[System.IO.Path.GetFileName(pdfPath)] = allLines;
                txtPaths.Add(txtPath);
            }

            foreach (var img in imageFiles)
            {
                if (imgToText.TryGetValue(img, out var lines))
                {
                    string srcDir = System.IO.Path.GetDirectoryName(img) ?? "";
                    string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                    if (!createdResultDirs.Contains(resultDir))
                    {
                        System.IO.Directory.CreateDirectory(resultDir);
                        createdResultDirs.Add(resultDir);
                    }
                    string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(img) + ".txt");
                    System.IO.File.WriteAllLines(txtPath, lines);
                    fileResultMap[System.IO.Path.GetFileName(img)] = lines;
                    txtPaths.Add(txtPath);
                }
            }

            if (progressOcr != null) progressOcr.Value = 100;
            ocrWatch.Stop();
            double seconds = ocrWatch.Elapsed.TotalSeconds;

            if (selectedImages.Count > 0 && listImages != null)
            {
                listImages.SelectedIndex = selectedImages.Count - 1;
                var lastFile = System.IO.Path.GetFileName(selectedImages[selectedImages.Count - 1]);
                if (fileResultMap.ContainsKey(lastFile) && listResultsTextBox != null)
                    listResultsTextBox.Text = string.Join(Environment.NewLine, fileResultMap[lastFile]);
            }

            if (btnOcr != null) btnOcr.IsEnabled = true;
            if (btnScreenshot != null) btnScreenshot.IsEnabled = true;
            if (statusBarText != null)
            {
                string txtInfo = txtPaths.Count == 1 ? txtPaths[0] : string.Join("; ", txtPaths);
                statusBarText.Text = $"识别完成，耗时{seconds:F1}秒，结果txt路径：{txtInfo}";
            }
        }

        private void BtnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "图片或PDF文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.webp;*.pdf|所有文件|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                selectedImages = dlg.FileNames.ToList();
                var listImages = this.FindName("ListImages") as ListBox;
                if (listImages != null) listImages.ItemsSource = selectedImages.Select(f => System.IO.Path.GetFileName(f));
                var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
                if (txtFileStatus != null) txtFileStatus.Text = selectedImages.Count > 0 ? $"已选择 {selectedImages.Count} 个文件" : "未选择文件";
                fileResultMap.Clear();
                var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
                if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
                UpdateListImagesHint();
            }
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedImages.Clear();
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages != null) listImages.ItemsSource = null;
            var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
            if (txtFileStatus != null) txtFileStatus.Text = "未选择文件";
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
            fileResultMap.Clear();
            var progressOcr = this.FindName("ProgressOcr") as ProgressBar;
            if (progressOcr != null) progressOcr.Value = 0;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;
            if (statusBarText != null) statusBarText.Text = string.Empty;
            UpdateListImagesHint();
        }
             // 支持拖拽文件到文件列表
        private void ListImages_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ListImages_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var supported = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".webp", ".pdf" };
                var addFiles = files.Where(f => supported.Contains(System.IO.Path.GetExtension(f).ToLower())).ToList();
                if (addFiles.Count > 0)
                {
                    selectedImages.AddRange(addFiles);
                    var listImages = this.FindName("ListImages") as ListBox;
                    if (listImages != null)
                    {
                        listImages.ItemsSource = null;
                        listImages.ItemsSource = selectedImages.Select(f => System.IO.Path.GetFileName(f));
                    }
                    var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
                    if (txtFileStatus != null) txtFileStatus.Text = $"已选择 {selectedImages.Count} 个文件";
                    UpdateListImagesHint();
                }
            }
        }

        // 文件列表为空时显示提示
        private void UpdateListImagesHint()
        {
            var listImagesEmptyHint = this.FindName("ListImagesEmptyHint") as TextBlock;
            if (listImagesEmptyHint != null)
                listImagesEmptyHint.Visibility = (selectedImages.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        }
        // 双击文件列表中的文件，使用系统默认程序打开
        private void ListImages_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages?.SelectedIndex is int idx && idx >= 0 && idx < selectedImages.Count)
            {
                string filePath = selectedImages[idx];
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件：{filePath}\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"文件不存在：{filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ListBox项的双击事件处理
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages?.SelectedIndex is int idx && idx >= 0 && idx < selectedImages.Count)
            {
                string filePath = selectedImages[idx];
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件：{filePath}\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"文件不存在：{filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 已废弃字段：lastMergedTxtPath
        private async void BtnOcr_Click(object sender, RoutedEventArgs e)
        {
            // 通过FindName获取所有控件，兼容partial字段丢失的情况
            var comboModel = this.FindName("ComboModel") as ComboBox;
            var checkGpu = this.FindName("CheckGpu") as CheckBox;
            var checkSaveResult = this.FindName("CheckSaveResult") as CheckBox;
            var progressOcr = this.FindName("ProgressOcr") as ProgressBar;
            var listImages = this.FindName("ListImages") as ListBox;
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var btnOcr = this.FindName("BtnOcr") as Button;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;

            if (selectedImages.Count == 0)
            {
                MessageBox.Show("请先选择图片或PDF文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (btnOcr != null) btnOcr.IsEnabled = false;
            if (progressOcr != null) progressOcr.Value = 0;
            if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
            fileResultMap.Clear();
            // lastMergedTxtPath = null; // 已废弃，无需赋值
            if (statusBarText != null) statusBarText.Text = "正在识别...";

            // 计时开始
            var ocrWatch = System.Diagnostics.Stopwatch.StartNew();
            // 1. 处理PDF文件，先转图片
            var pdfExts = new[] { ".pdf" };
            var pdfFiles = selectedImages.Where(f => pdfExts.Contains(System.IO.Path.GetExtension(f).ToLower())).ToList();
            var imageFiles = selectedImages.Where(f => !pdfExts.Contains(System.IO.Path.GetExtension(f).ToLower())).ToList();
            var allOcrImages = new List<string>();
            var pdfToTxtMap = new Dictionary<string, List<string>>(); // pdf文件名->所有识别文本
            if (pdfFiles.Count > 0)
            {
                foreach (var pdf in pdfFiles)
                {
                    string pdfDir = System.IO.Path.GetDirectoryName(pdf) ?? "";
                    string pdfName = System.IO.Path.GetFileNameWithoutExtension(pdf);
                    string outDir = System.IO.Path.Combine(pdfDir, pdfName);
                    string imageFormat = "jpg";
                    int dpi = 250;
                    int jpegQuality = 90;
                    // 统一输出目录为 outDir，确保 OcrBatchProcessor 能读取
                    var pdfToImageTask = PDF.Convert.PDFToImagesAsync(new[] { pdf }, outDir, dpi, imageFormat, jpegQuality);
                    await pdfToImageTask;
                    List<string> imgs = new List<string>();
                    if (System.IO.Directory.Exists(outDir))
                    {
                        imgs = System.IO.Directory.GetFiles(outDir, $"*_page_*.{imageFormat}").OrderBy(f => f).ToList();
                    }
                    allOcrImages.AddRange(imgs);
                    pdfToTxtMap[pdf] = imgs;
                }
            }
            allOcrImages.AddRange(imageFiles);

            var processor = new OcrBatchProcessor();
            processor.SetModel(comboModel != null && comboModel.SelectedIndex == 0 ? OcrBatchProcessor.ModelType.PP_OCRv4 : OcrBatchProcessor.ModelType.PP_OCRv5);
            processor.SetUseGpu(checkGpu != null && checkGpu.IsChecked == true, checkGpu != null && checkGpu.IsChecked == true);
            processor.SetSaveResultImage(checkSaveResult != null && checkSaveResult.IsChecked == true);
            processor.SetOutputFileFormat(_aiSettings?.OutputFileFormat ?? "txt标准格式");

            // 设置在线OCR服务配置（当选择在线模型时）
            if (comboModel != null && comboModel.SelectedIndex >= 2 && _aiSettings != null && !string.IsNullOrEmpty(_aiSettings.OcrApiUrl))
            {
                processor.SetOcrServiceConfig(_aiSettings.OcrServiceProvider, _aiSettings.OcrApiUrl, _aiSettings.OcrApiKey, _aiSettings.OcrModel);
            }

            processor.AddImages(allOcrImages);
            int total = allOcrImages.Count;
            var task = Task.Run(async () =>
            {
                var result = await processor.RunBatchOcrAsync(2, (done, all) =>
                {
                    if (progressOcr != null)
                        Dispatcher.Invoke(() =>
                        {
                            progressOcr.Value = all > 0 ? done * 100.0 / all : 0;
                        });
                });
                return result;
            });
            var result = await task;

            // 2. 结果分发：PDF输出合并txt，图片输出单文件txt
            var imgToText = new Dictionary<string, List<string>>();
            foreach (var detail in result.details)
            {
                string fileName = System.IO.Path.GetFileName(detail.ImgPath);
                List<string> lines = new();
                if (detail.Result == null)
                {
                    lines.Add("识别失败");
                }
                else
                {
                    foreach (var r in detail.Result)
                    {
                        lines.Add(r.text);
                    }
                }
                imgToText[detail.ImgPath] = lines;
            }
            // 统一将所有txt输出到源文件同级的OCR_Result文件夹，避免重复
            var txtPaths = new List<string>();
            var createdResultDirs = new HashSet<string>();
            // PDF合并txt输出
            foreach (var kv in pdfToTxtMap)
            {
                string pdfPath = kv.Key;
                var imgs = kv.Value;
                var allLines = new List<string>();
                foreach (var img in imgs)
                {
                    if (imgToText.TryGetValue(img, out var lines))
                        allLines.AddRange(lines);
                }
                string srcDir = System.IO.Path.GetDirectoryName(pdfPath) ?? "";
                string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                if (!createdResultDirs.Contains(resultDir))
                {
                    System.IO.Directory.CreateDirectory(resultDir);
                    createdResultDirs.Add(resultDir);
                }
                string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(pdfPath) + ".txt");
                System.IO.File.WriteAllLines(txtPath, allLines);
                fileResultMap[System.IO.Path.GetFileName(pdfPath)] = allLines;
                txtPaths.Add(txtPath);
            }
            // 普通图片单独txt输出
            foreach (var img in imageFiles)
            {
                if (imgToText.TryGetValue(img, out var lines))
                {
                    string srcDir = System.IO.Path.GetDirectoryName(img) ?? "";
                    string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                    if (!createdResultDirs.Contains(resultDir))
                    {
                        System.IO.Directory.CreateDirectory(resultDir);
                        createdResultDirs.Add(resultDir);
                    }
                    string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(img) + ".txt");
                    System.IO.File.WriteAllLines(txtPath, lines);
                    fileResultMap[System.IO.Path.GetFileName(img)] = lines;
                    txtPaths.Add(txtPath);
                }
            }

            if (progressOcr != null) progressOcr.Value = 100;
            // 计时结束
            ocrWatch.Stop();
            double seconds = ocrWatch.Elapsed.TotalSeconds;
            // 默认选中第一个文件并显示其结果
            if (selectedImages.Count > 0 && listImages != null)
            {
                listImages.SelectedIndex = 0;
                var firstFile = System.IO.Path.GetFileName(selectedImages[0]);
                if (fileResultMap.ContainsKey(firstFile) && listResultsTextBox != null)
                    listResultsTextBox.Text = string.Join(Environment.NewLine, fileResultMap[firstFile]);
            }
            if (btnOcr != null) btnOcr.IsEnabled = true;
            if (statusBarText != null)
            {
                string txtInfo = txtPaths.Count == 1 ? txtPaths[0] : string.Join("; ", txtPaths);
                statusBarText.Text = $"识别完成，耗时{seconds:F1}秒，结果txt路径：{txtInfo}";
            }
        }

        // 文件列表选中项变化时，显示对应识别结果
        private void ListImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listImages = this.FindName("ListImages") as ListBox;
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            if (listImages?.SelectedItem is string fileName)
            {
                if (fileResultMap.TryGetValue(fileName, out var lines))
                {
                    if (listResultsTextBox != null) listResultsTextBox.Text = string.Join(Environment.NewLine, lines);
                }
                else
                {
                    if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
                }
            }
            else
            {
                if (listResultsTextBox != null) listResultsTextBox.Text = string.Empty;
            }
        }

        // 右键菜单：删除选中的文件
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var listImages = this.FindName("ListImages") as ListBox;
            if (listImages?.SelectedIndex is int idx && idx >= 0 && idx < selectedImages.Count)
            {
                // 先获取要删除的文件名
                string deletedFileName = System.IO.Path.GetFileName(selectedImages[idx]);

                // 删除选中项
                selectedImages.RemoveAt(idx);

                // 更新ListBox显示
                if (listImages != null)
                {
                    listImages.ItemsSource = null;
                    listImages.ItemsSource = selectedImages.Select(f => System.IO.Path.GetFileName(f));
                }

                // 更新文件状态显示
                var txtFileStatus = this.FindName("TxtFileStatus") as TextBlock;
                if (txtFileStatus != null)
                    txtFileStatus.Text = selectedImages.Count > 0 ? $"已选择 {selectedImages.Count} 个文件" : "未选择文件";

                // 清除结果
                var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
                if (listResultsTextBox != null)
                    listResultsTextBox.Text = string.Empty;

                // 从结果映射中删除对应项
                if (!string.IsNullOrEmpty(deletedFileName))
                {
                    fileResultMap.Remove(deletedFileName);
                }

                // 更新空提示
                UpdateListImagesHint();
            }
        }

        // AI相关方法
        private void LoadAISettings()
        {
            _aiSettings = new AISettings
            {
                ServiceProvider = Properties.Settings.Default.AIServiceProvider,
                ApiUrl = Properties.Settings.Default.AIApiUrl,
                ApiKey = Properties.Settings.Default.AIApiKey,
                ModelName = Properties.Settings.Default.AIModelName,
                OcrEnhancementPrompt = Properties.Settings.Default.AIOcrEnhancementPrompt,
                AnalysisPrompt = Properties.Settings.Default.AIAnalysisPrompt,
                TranslationPrompt = Properties.Settings.Default.AITranslationPrompt,
                OutputFileFormat = Properties.Settings.Default.AIOutputFileFormat,
                OcrServiceProvider = Properties.Settings.Default.OCRServiceProvider,
                OcrApiUrl = Properties.Settings.Default.OCRApiUrl,
                OcrApiKey = Properties.Settings.Default.OCRApiKey,
                OcrModel = Properties.Settings.Default.OCRModel
            }!;
        }

        private void SaveAISettings()
        {
            if (_aiSettings == null) return;

            Properties.Settings.Default.AIServiceProvider = _aiSettings.ServiceProvider;
            Properties.Settings.Default.AIApiUrl = _aiSettings.ApiUrl;
            Properties.Settings.Default.AIApiKey = _aiSettings.ApiKey;
            Properties.Settings.Default.AIModelName = _aiSettings.ModelName;
            Properties.Settings.Default.AIOcrEnhancementPrompt = _aiSettings.OcrEnhancementPrompt;
            Properties.Settings.Default.AIAnalysisPrompt = _aiSettings.AnalysisPrompt;
            Properties.Settings.Default.AITranslationPrompt = _aiSettings.TranslationPrompt;
            Properties.Settings.Default.AIOutputFileFormat = _aiSettings.OutputFileFormat;
            Properties.Settings.Default.OCRServiceProvider = _aiSettings.OcrServiceProvider;
            Properties.Settings.Default.OCRApiUrl = _aiSettings.OcrApiUrl;
            Properties.Settings.Default.OCRApiKey = _aiSettings.OcrApiKey;
            Properties.Settings.Default.OCRModel = _aiSettings.OcrModel;
            Properties.Settings.Default.Save();
        }

        private void BtnAISettings_Click(object sender, RoutedEventArgs e)
        {
            if (_aiSettings == null) return;

            var settingsWindow = new AISettingsWindow(_aiSettings);
            if (settingsWindow.ShowDialog() == true)
            {
                SaveAISettings();
                // 重置AI服务以使用新设置
                _aiService?.Dispose();
                _aiService = null;
            }
        }

        private async void BtnAIEnhance_Click(object sender, RoutedEventArgs e)
        {
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;

            if (listResultsTextBox == null || string.IsNullOrWhiteSpace(listResultsTextBox.Text))
            {
                MessageBox.Show("请先进行OCR识别以获取文本内容", "无内容可处理", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_aiSettings == null || !_aiSettings.IsConfigured)
            {
                var result = MessageBox.Show("AI功能尚未配置，是否现在配置？", "AI配置", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    BtnAISettings_Click(sender, e);
                    if (_aiSettings == null || !_aiSettings.IsConfigured)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (statusBarText != null) statusBarText.Text = "正在使用AI优化文本...";

                // 创建或获取AI服务
                if (_aiService == null)
                {
                    _aiService = new AIService(_aiSettings);
                }

                string originalText = listResultsTextBox.Text;
                string enhancedText = await _aiService.EnhanceOCRTextAsync(originalText);

                if (!string.IsNullOrEmpty(enhancedText))
                {
                    listResultsTextBox.Text = enhancedText;
                    if (statusBarText != null) statusBarText.Text = "AI文本优化完成";
                }
                else
                {
                    MessageBox.Show("AI处理未返回结果，请检查API配置", "处理失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (statusBarText != null) statusBarText.Text = "AI处理失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if (statusBarText != null) statusBarText.Text = $"AI处理错误：{ex.Message}";
            }
        }

        // 右键菜单：AI OCR增强
        private async void MenuItem_EnhanceOCR_Click(object sender, RoutedEventArgs e)
        {
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var listImages = this.FindName("ListImages") as ListBox;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;

            if (listResultsTextBox == null || string.IsNullOrWhiteSpace(listResultsTextBox.Text))
            {
                MessageBox.Show("请先进行OCR识别以获取文本内容", "无内容可增强", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_aiSettings == null || !_aiSettings.IsConfigured)
            {
                var result = MessageBox.Show("AI功能尚未配置，是否现在配置？", "AI配置", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    BtnAISettings_Click(sender, e);
                    if (_aiSettings == null || !_aiSettings.IsConfigured)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (statusBarText != null) statusBarText.Text = "正在使用AI增强OCR文本...";

                // 创建或获取AI服务
                if (_aiService == null)
                {
                    _aiService = new AIService(_aiSettings);
                }

                string originalText = listResultsTextBox.Text;
                string enhancedText = await _aiService.EnhanceOCRTextAsync(originalText);

                if (!string.IsNullOrEmpty(enhancedText))
                {
                    // 替换原文本为增强后的文本
                    listResultsTextBox.Text = enhancedText;

                    // 更新fileResultMap并保存到文件
                    if (listImages?.SelectedItem is string fileName)
                    {
                        var lines = new List<string>(enhancedText?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>());
                        fileResultMap[fileName] = lines;

                        // 保存到对应文件（根据输出格式设置）
                        string? originalFilePath = selectedImages.FirstOrDefault(f => System.IO.Path.GetFileName(f) == fileName);
                        if (!string.IsNullOrEmpty(originalFilePath))
                        {
                            string srcDir = System.IO.Path.GetDirectoryName(originalFilePath) ?? string.Empty;
                            string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                            System.IO.Directory.CreateDirectory(resultDir);

                            // 确定文件扩展名基于当前输出格式设置
                            string extension = _aiSettings?.OutputFileFormat == "md文件" ? ".md" : ".txt";

                            if (extension == ".md")
                            {
                                // 保存为Markdown格式
                                string mdPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(originalFilePath) + ".md");
                                using (var writer = new System.IO.StreamWriter(mdPath, false))
                                {
                                    writer.WriteLine($"# {System.IO.Path.GetFileNameWithoutExtension(originalFilePath)}");
                                    writer.WriteLine();
                                    writer.WriteLine("## OCR 识别结果（AI增强）");
                                    writer.WriteLine();
                                    foreach (var line in lines)
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            writer.WriteLine(line);
                                            writer.WriteLine();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // 保存为TXT格式
                                string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(originalFilePath) + ".txt");
                                System.IO.File.WriteAllLines(txtPath, lines);
                            }
                        }
                    }

                    if (statusBarText != null) statusBarText.Text = "AI OCR增强完成并已保存";
                }
                else
                {
                    MessageBox.Show("AI OCR增强未返回结果，请检查API配置", "增强失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (statusBarText != null) statusBarText.Text = "AI OCR增强失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI OCR增强失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if (statusBarText != null) statusBarText.Text = $"AI OCR增强错误：{ex.Message}";
            }
        }

        // 右键菜单：复制文本
        private void MenuItem_CopyText_Click(object sender, RoutedEventArgs e)
        {
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            if (listResultsTextBox != null && !string.IsNullOrEmpty(listResultsTextBox.Text))
            {
                try
                {
                    Clipboard.SetText(listResultsTextBox.Text);
                    var statusBarText = this.FindName("StatusBarText") as TextBlock;
                    if (statusBarText != null) statusBarText.Text = "文本已复制到剪贴板";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("没有可复制的文本内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 右键菜单：AI分析总结
        private async void MenuItem_AnalyzeText_Click(object sender, RoutedEventArgs e)
        {
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var listImages = this.FindName("ListImages") as ListBox;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;

            if (listResultsTextBox == null || string.IsNullOrWhiteSpace(listResultsTextBox.Text))
            {
                MessageBox.Show("请先进行OCR识别以获取文本内容", "无内容可分析", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_aiSettings == null || !_aiSettings.IsConfigured)
            {
                var result = MessageBox.Show("AI功能尚未配置，是否现在配置？", "AI配置", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    BtnAISettings_Click(sender, e);
                    if (_aiSettings == null || !_aiSettings.IsConfigured)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (statusBarText != null) statusBarText.Text = "正在使用AI分析总结...";

                // 创建或获取AI服务
                if (_aiService == null)
                {
                    _aiService = new AIService(_aiSettings);
                }

                string originalText = listResultsTextBox.Text;
                string analysisResult = await _aiService.AnalyzeTextAsync(originalText);

                if (!string.IsNullOrEmpty(analysisResult))
                {
                    // 在原文后添加分析结果
                    string separator = "\n\n═══════════════════════════════════\nAI分析总结\n═══════════════════════════════════\n";
                    string finalText = originalText + separator + analysisResult;
                    listResultsTextBox.Text = finalText;

                    // 更新fileResultMap并保存到文件
                    if (listImages?.SelectedItem is string fileName)
                    {
                        var lines = new List<string>(finalText?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>());
                        fileResultMap[fileName] = lines;

                        // 保存到对应的txt文件
                        string? originalFilePath = selectedImages.FirstOrDefault(f => System.IO.Path.GetFileName(f) == fileName);
                        if (!string.IsNullOrEmpty(originalFilePath))
                        {
                            string srcDir = System.IO.Path.GetDirectoryName(originalFilePath) ?? string.Empty;
                            string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                            string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(originalFilePath) + ".txt");

                            System.IO.Directory.CreateDirectory(resultDir);
                            System.IO.File.WriteAllLines(txtPath, lines);
                        }
                    }

                    if (statusBarText != null) statusBarText.Text = "AI分析总结完成并已保存";
                }
                else
                {
                    MessageBox.Show("AI分析未返回结果，请检查API配置", "分析失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (statusBarText != null) statusBarText.Text = "AI分析失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if (statusBarText != null) statusBarText.Text = $"AI分析错误：{ex.Message}";
            }
        }

        // 右键菜单：AI翻译
        private async void MenuItem_TranslateText_Click(object sender, RoutedEventArgs e)
        {
            var listResultsTextBox = this.FindName("ListResultsTextBox") as TextBox;
            var listImages = this.FindName("ListImages") as ListBox;
            var statusBarText = this.FindName("StatusBarText") as TextBlock;

            if (listResultsTextBox == null || string.IsNullOrWhiteSpace(listResultsTextBox.Text))
            {
                MessageBox.Show("请先进行OCR识别以获取文本内容", "无内容可翻译", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_aiSettings == null || !_aiSettings.IsConfigured)
            {
                var result = MessageBox.Show("AI功能尚未配置，是否现在配置？", "AI配置", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    BtnAISettings_Click(sender, e);
                    if (_aiSettings == null || !_aiSettings.IsConfigured)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (statusBarText != null) statusBarText.Text = "正在使用AI翻译...";

                // 创建或获取AI服务
                if (_aiService == null)
                {
                    _aiService = new AIService(_aiSettings);
                }

                string originalText = listResultsTextBox.Text;
                string translationResult = await _aiService.TranslateTextAsync(originalText);

                if (!string.IsNullOrEmpty(translationResult))
                {
                    // 在原文后添加翻译结果
                    string separator = "\n\n═══════════════════════════════════\nAI翻译结果\n═══════════════════════════════════\n";
                    string finalText = originalText + separator + translationResult;
                    listResultsTextBox.Text = finalText;

                    // 更新fileResultMap并保存到文件
                    if (listImages?.SelectedItem is string fileName)
                    {
                        var lines = new List<string>(finalText?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>());
                        fileResultMap[fileName] = lines;

                        // 保存到对应的txt文件
                        string? originalFilePath = selectedImages.FirstOrDefault(f => System.IO.Path.GetFileName(f) == fileName);
                        if (!string.IsNullOrEmpty(originalFilePath))
                        {
                            string srcDir = System.IO.Path.GetDirectoryName(originalFilePath) ?? string.Empty;
                            string resultDir = System.IO.Path.Combine(srcDir, "OCR_Result");
                            string txtPath = System.IO.Path.Combine(resultDir, System.IO.Path.GetFileNameWithoutExtension(originalFilePath) + ".txt");

                            System.IO.Directory.CreateDirectory(resultDir);
                            System.IO.File.WriteAllLines(txtPath, lines);
                        }
                    }

                    if (statusBarText != null) statusBarText.Text = "AI翻译完成并已保存";
                }
                else
                {
                    MessageBox.Show("AI翻译未返回结果，请检查API配置", "翻译失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (statusBarText != null) statusBarText.Text = "AI翻译失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI翻译失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if (statusBarText != null) statusBarText.Text = $"AI翻译错误：{ex.Message}";
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 清理AI服务资源
            _aiService?.Dispose();
            base.OnClosing(e);
        }
    }
}