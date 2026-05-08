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

            // Load OCR settings
            if (_settings.OcrServiceProvider == "硅基流动")
            {
                ComboOcrServiceProvider.SelectedIndex = 1;
            }
            else
            {
                ComboOcrServiceProvider.SelectedIndex = 0; // PaddleOCR（在线）
            }
            TxtOcrApiUrl.Text = _settings.OcrApiUrl;
            PwdOcrApiKey.Password = _settings.OcrApiKey;
            TxtOcrModel.Text = _settings.OcrModel;

            // Clear connection status
            TxtConnectionStatus.Text = string.Empty;
            TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Gray;
            TxtOcrConnectionStatus.Text = string.Empty;
            TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Gray;
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

            // Save OCR settings
            if (ComboOcrServiceProvider.SelectedIndex == 1)
            {
                _settings.OcrServiceProvider = "硅基流动";
            }
            else
            {
                _settings.OcrServiceProvider = "PaddleOCR（在线）";
            }
            _settings.OcrApiUrl = TxtOcrApiUrl.Text.Trim();
            _settings.OcrApiKey = PwdOcrApiKey.Password;
            _settings.OcrModel = TxtOcrModel.Text.Trim();

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

        private void ComboOcrServiceProvider_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Prevent execution during initialization
            if (!_isInitialized || TxtOcrApiUrl == null || TxtOcrModel == null) return;

            if (ComboOcrServiceProvider.SelectedIndex == 1) // 硅基流动
            {
                TxtOcrApiUrl.Text = "https://api.siliconflow.cn/v1/ocr";
                TxtOcrModel.Text = "Qwen/Qwen3-VL-235B-A22B-Instruct";
            }
            else // PaddleOCR（在线）
            {
                TxtOcrApiUrl.Text = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs";
                TxtOcrModel.Text = "PaddleOCR-VL-1.5";
            }
        }

        private async void BtnTestOcrConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtOcrApiUrl.Text) ||
                string.IsNullOrWhiteSpace(PwdOcrApiKey.Password))
            {
                MessageBox.Show("请填写完整的OCR配置信息", "配置不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtOcrConnectionStatus.Text = "正在测试OCR连接...";
                TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Yellow;

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间

                    // Prepare test request with a simple valid image
                    var testImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="; // 1x1 transparent PNG

                    // Convert base64 to bytes
                    byte[] imageBytes = Convert.FromBase64String(testImageBase64);

                    // Create multipart form data
                    using var formData = new System.Net.Http.MultipartFormDataContent();

                    // Add file
                    var fileContent = new System.Net.Http.ByteArrayContent(imageBytes);
                    fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");
                    formData.Add(fileContent, "file", "test.png");

                    // Add model parameter
                    formData.Add(new System.Net.Http.StringContent("PaddleOCR-VL-1.5"), "model");

                    // Add optional payload as JSON string
                    var optionalPayload = new
                    {
                        useDocOrientationClassify = false,
                        useDocUnwarping = false,
                        useChartRecognition = false
                    };
                    var optionalPayloadJson = System.Text.Json.JsonSerializer.Serialize(optionalPayload);
                    formData.Add(new System.Net.Http.StringContent(optionalPayloadJson), "optionalPayload");

                    // Add authorization header
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {PwdOcrApiKey.Password}");

                    // Send test request
                    var response = await httpClient.PostAsync(TxtOcrApiUrl.Text.Trim(), formData);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

                            // 检查异步API响应格式 (code/data)
                            if (root.TryGetProperty("code", out var codeElement))
                            {
                                int code = codeElement.GetInt32();
                                if (code == 0)
                                {
                                    TxtOcrConnectionStatus.Text = "✅ OCR连接测试成功";
                                    TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                                }
                                else
                                {
                                    var msg = root.TryGetProperty("msg", out var msgElement)
                                        ? msgElement.GetString()
                                        : $"错误码: {code}";
                                    TxtOcrConnectionStatus.Text = $"❌ OCR连接失败: {msg}";
                                    TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                                }
                            }
                            // 检查同步API响应格式 (errorCode/result)
                            else if (root.TryGetProperty("errorCode", out var errorCodeElement))
                            {
                                int errorCode = errorCodeElement.GetInt32();
                                if (errorCode == 0)
                                {
                                    TxtOcrConnectionStatus.Text = "✅ OCR连接测试成功";
                                    TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                                }
                                else
                                {
                                    var errorMsg = root.TryGetProperty("errorMsg", out var errorMsgElement)
                                        ? errorMsgElement.GetString()
                                        : $"错误码: {errorCode}";
                                    TxtOcrConnectionStatus.Text = $"❌ OCR连接失败: {errorMsg}";
                                    TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                                }
                            }
                            else
                            {
                                // 如果没有找到已知的响应格式字段
                                TxtOcrConnectionStatus.Text = "❌ OCR响应格式异常";
                                TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            TxtOcrConnectionStatus.Text = $"❌ JSON解析错误: {jsonEx.Message}";
                            TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        TxtOcrConnectionStatus.Text = $"❌ OCR连接失败: {response.StatusCode} - {errorContent}";
                        TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                TxtOcrConnectionStatus.Text = "❌ OCR连接超时";
                TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                TxtOcrConnectionStatus.Text = "❌ OCR连接错误: " + ex.Message;
                TxtOcrConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
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