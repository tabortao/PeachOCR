using System.Windows;

namespace PeachOCR
{
    /// <summary>
    /// OcrResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OcrResultWindow : Window
    {
        public OcrResultWindow(string ocrResultText)
        {
            InitializeComponent();
            TxtResult.Text = ocrResultText;
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtResult.Text);
                MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
