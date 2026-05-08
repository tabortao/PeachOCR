using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PeachOCR
{
    public partial class AISettingsWindow : Window
    {
        private AISettings _settings;
        private bool _isModified = false;
        private bool _isInitialized = false;

        public AISettingsWindow(AISettings settings)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LoadSettings();
            _isInitialized = true;
        }

        private void LoadSettings()
        {
            // Load current settings into UI
            if (_settings.ServiceProvider == "DeepSeek")
            {
                ComboServiceProvider.SelectedIndex = 1;
            }
            else
            {
                ComboServiceProvider.SelectedIndex = 0; // OpenAI compatible
            }
            TxtApiUrl.Text = _settings.ApiUrl;
            PwdApiKey.Password = _settings.ApiKey;
            TxtModelName.Text = _settings.ModelName;
            TxtOcrEnhancementPrompt.Text = _settings.OcrEnhancementPrompt;
            TxtAnalysisPrompt.Text = _settings.AnalysisPrompt;
            TxtTranslationPrompt.Text = _settings.TranslationPrompt;

            // Load output format setting
            if (_settings.OutputFileFormat == "md文件")
            {
                ComboOutputFormat.SelectedIndex = 1;
            }
            else
            {
                ComboOutputFormat.SelectedIndex = 0; // txt标准格式
            }

            // Clear connection status
            TxtConnectionStatus.Text = string.Empty;
            TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void SaveSettings()
        {
            if (ComboServiceProvider.SelectedIndex == 1)
            {
                _settings.ServiceProvider = "DeepSeek";
                _settings.ApiUrl = "https://api.deepseek.com";
                // Allow custom model name for DeepSeek, use deepseek-v4-flash as default if empty
                string modelName = TxtModelName.Text.Trim();
                _settings.ModelName = string.IsNullOrWhiteSpace(modelName) ? "deepseek-v4-flash" : modelName;
                TxtApiUrl.Text = _settings.ApiUrl;
                TxtModelName.Text = _settings.ModelName;
            }
            else
            {
                _settings.ServiceProvider = "OpenAI兼容";
                _settings.ApiUrl = TxtApiUrl.Text.Trim();
                _settings.ModelName = TxtModelName.Text.Trim();
            }

            _settings.ApiKey = PwdApiKey.Password;
            _settings.OcrEnhancementPrompt = TxtOcrEnhancementPrompt.Text;
            _settings.AnalysisPrompt = TxtAnalysisPrompt.Text;
            _settings.TranslationPrompt = TxtTranslationPrompt.Text;

            // Save output format setting
            if (ComboOutputFormat.SelectedIndex == 1)
            {
                _settings.OutputFileFormat = "md文件";
            }
            else
            {
                _settings.OutputFileFormat = "txt标准格式";
            }

            _isModified = true;
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtApiUrl.Text) ||
                string.IsNullOrWhiteSpace(PwdApiKey.Password) ||
                string.IsNullOrWhiteSpace(TxtModelName.Text))
            {
                MessageBox.Show("请填写完整的API配置信息", "配置不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtConnectionStatus.Text = "正在测试连接...";
                TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Yellow;

                // Create temporary settings for testing
                var testSettings = new AISettings
                {
                    ApiUrl = TxtApiUrl.Text.Trim(),
                    ApiKey = PwdApiKey.Password,
                    ModelName = TxtModelName.Text.Trim()
                };

                using (var aiService = new AIService(testSettings))
                {
                    bool isConnected = await aiService.TestConnectionAsync();

                    if (isConnected)
                    {
                        TxtConnectionStatus.Text = "✅ 连接成功";
                        TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    }
                    else
                    {
                        TxtConnectionStatus.Text = "❌ 连接失败";
                        TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                TxtConnectionStatus.Text = "❌ 连接错误: " + ex.Message;
                TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void ComboServiceProvider_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Prevent execution during initialization
            if (!_isInitialized || TxtApiUrl == null || TxtModelName == null) return;

            if (ComboServiceProvider.SelectedIndex == 1) // DeepSeek
            {
                TxtApiUrl.Text = "https://api.deepseek.com";
                // Use deepseek-v4-flash as default but allow user customization
                if (string.IsNullOrWhiteSpace(TxtModelName.Text) || TxtModelName.Text == "gpt-3.5-turbo")
                {
                    TxtModelName.Text = "deepseek-v4-flash";
                }
                TxtApiUrl.IsEnabled = false;
                TxtModelName.IsEnabled = true; // Allow custom model names
            }
            else // OpenAI compatible
            {
                TxtApiUrl.IsEnabled = true;
                TxtModelName.IsEnabled = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtApiUrl.Text) ||
                string.IsNullOrWhiteSpace(PwdApiKey.Password) ||
                string.IsNullOrWhiteSpace(TxtModelName.Text))
            {
                MessageBox.Show("请填写完整的API配置信息", "配置不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要重置所有设置为默认值吗？", "重置确认",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset based on current service provider
                if (ComboServiceProvider.SelectedIndex == 1) // DeepSeek
                {
                    _settings.ResetToDeepSeekDefaults();
                }
                else
                {
                    _settings.ResetToDefaults();
                }
                LoadSettings();
            }
        }

        public bool IsModified => _isModified;
    }
}