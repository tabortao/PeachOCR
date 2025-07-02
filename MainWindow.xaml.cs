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
        public MainWindow()
        {
            InitializeComponent();
            // 绑定所有控件字段，防止XAML自动生成失效
            BtnSelectImages = (Button)FindName("BtnSelectImages");
            BtnClear = (Button)FindName("BtnClear");
            BtnOcr = (Button)FindName("BtnOcr");
            ListImages = (ListBox)FindName("ListImages");
            ListResults = (ListBox)FindName("ListResults");
            ComboModel = (ComboBox)FindName("ComboModel");
            CheckGpu = (CheckBox)FindName("CheckGpu");
            CheckSaveResult = (CheckBox)FindName("CheckSaveResult");
            CheckMergeTxt = (CheckBox)FindName("CheckMergeTxt");
            ProgressOcr = (ProgressBar)FindName("ProgressOcr");
            TxtFileStatus = (TextBlock)FindName("TxtFileStatus");
            CheckSaveResult.IsChecked = true;
            CheckMergeTxt.IsChecked = false;
            TxtFileStatus.Text = "未选择文件";
            this.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); };
            ListImages.SelectionChanged += ListImages_SelectionChanged;
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
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
                ListImages.ItemsSource = selectedImages.Select(f => System.IO.Path.GetFileName(f));
                TxtFileStatus.Text = selectedImages.Count > 0 ? $"已选择 {selectedImages.Count} 个文件" : "未选择文件";
                fileResultMap.Clear();
                ListResults.ItemsSource = null;
            }
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedImages.Clear();
            ListImages.ItemsSource = null;
            TxtFileStatus.Text = "未选择文件";
            ListResults.ItemsSource = null;
            fileResultMap.Clear();
            ProgressOcr.Value = 0;
            StatusBarText.Text = string.Empty;
        }
        private string? lastMergedTxtPath = null;
        private async void BtnOcr_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImages.Count == 0)
            {
                MessageBox.Show("请先选择图片或PDF文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            BtnOcr.IsEnabled = false;
            ProgressOcr.Value = 0;
            ListResults.ItemsSource = null;
            fileResultMap.Clear();
            lastMergedTxtPath = null;
            StatusBarText.Text = "正在识别...";
            var processor = new OCR.OcrBatchProcessor();
            processor.SetModel(ComboModel.SelectedIndex == 0 ? OCR.OcrBatchProcessor.ModelType.PP_OCRv4 : OCR.OcrBatchProcessor.ModelType.PP_OCRv5);
            processor.SetUseGpu(CheckGpu.IsChecked == true, CheckGpu.IsChecked == true);
            processor.SetSaveResultImage(CheckSaveResult.IsChecked == true);
            processor.AddImages(selectedImages);
            int total = selectedImages.Count;
            int finished = 0;
            var task = Task.Run(async () =>
            {
                var (details, totalMs) = await processor.RunBatchOcrAsync(2);
                return (details, totalMs);
            });
            var result = await task;
            // 记录每个文件的识别结果
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
                fileResultMap[fileName] = lines;
                finished++;
                ProgressOcr.Value = finished * 100.0 / total;
            }
            ProgressOcr.Value = 100;
            // 默认选中第一个文件并显示其结果
            if (selectedImages.Count > 0)
            {
                ListImages.SelectedIndex = 0;
                var firstFile = System.IO.Path.GetFileName(selectedImages[0]);
                if (fileResultMap.ContainsKey(firstFile))
                    ListResults.ItemsSource = fileResultMap[firstFile];
            }
            BtnOcr.IsEnabled = true;
            // 合并txt功能
            if (CheckMergeTxt.IsChecked == true && result.details.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var detail in result.details)
                {
                    if (detail.Result != null)
                        sb.AppendLine(string.Join(" ", detail.Result.Select(r => r.text)));
                }
                var saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "文本文件|*.txt",
                    FileName = "OCR_Result_Merged.txt"
                };
                if (saveDlg.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDlg.FileName, sb.ToString());
                    lastMergedTxtPath = saveDlg.FileName;
                }
            }
            // 状态栏提示（耗时用秒，显示存储文件夹路径，自动推断）
            double seconds = result.totalMs / 1000.0;
            string resultDir = "(未合并txt)";
            if (lastMergedTxtPath != null)
            {
                try
                {
                    resultDir = System.IO.Path.GetDirectoryName(lastMergedTxtPath) ?? lastMergedTxtPath;
                }
                catch { resultDir = lastMergedTxtPath; }
            }
            else if (selectedImages.Count > 0)
            {
                // 推断结果文件夹为用户输入图片的同级目录下的OCR_Result
                string firstImg = selectedImages[0];
                string? parentDir = System.IO.Path.GetDirectoryName(firstImg);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    resultDir = System.IO.Path.Combine(parentDir, "OCR_Result");
                }
            }
            StatusBarText.Text = $"识别完成，耗时 {seconds:F2} 秒，识别结果存储于 {resultDir}";
        }

        // 文件列表选中项变化时，显示对应识别结果
        private void ListImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListImages.SelectedItem is string fileName)
            {
                // fileName 可能只是文件名
                if (fileResultMap.TryGetValue(fileName, out var lines))
                {
                    ListResults.ItemsSource = lines;
                }
                else
                {
                    ListResults.ItemsSource = Array.Empty<string>();
                }
            }
            else
            {
                ListResults.ItemsSource = Array.Empty<string>();
            }
        }
    }
}