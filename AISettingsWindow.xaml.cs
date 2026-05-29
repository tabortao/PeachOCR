using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace PeachOCR
{
    public partial class AISettingsWindow : Window
    {
        private AISettings _settings;
        private bool _isModified = false;
        private bool _isInitialized = false;
        private bool _isCapturingHotkey = false;
        private string _capturedHotkey = string.Empty;
        private Key _capturedKey = Key.None;
        private ModifierKeys _capturedModifiers = ModifierKeys.None;

        public AISettingsWindow(AISettings settings)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LoadSettings();
            _isInitialized = true;

            PreviewKeyDown += AISettingsWindow_PreviewKeyDown;
        }

        private void AISettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey) return;

            e.Handled = true;

            _capturedKey = e.Key == Key.System ? e.SystemKey : e.Key;
            _capturedModifiers = Keyboard.Modifiers;

            if (_capturedKey == Key.LeftCtrl || _capturedKey == Key.RightCtrl ||
                _capturedKey == Key.LeftAlt || _capturedKey == Key.RightAlt ||
                _capturedKey == Key.LeftShift || _capturedKey == Key.RightShift ||
                _capturedKey == Key.LWin || _capturedKey == Key.RWin)
            {
                return;
            }

            _capturedHotkey = BuildHotkeyString(_capturedModifiers, _capturedKey);
            TxtScreenshotHotkey.Text = _capturedHotkey;

            _isCapturingHotkey = false;
            BtnCaptureHotkey.Content = "点击设置";
        }

        private string BuildHotkeyString(ModifierKeys modifiers, Key key)
        {
            var parts = new System.Collections.Generic.List<string>();

            if ((modifiers & ModifierKeys.Control) != 0)
                parts.Add("Ctrl");
            if ((modifiers & ModifierKeys.Alt) != 0)
                parts.Add("Alt");
            if ((modifiers & ModifierKeys.Shift) != 0)
                parts.Add("Shift");
            if ((modifiers & ModifierKeys.Windows) != 0)
                parts.Add("Win");

            string keyString = key.ToString();
            if (keyString.StartsWith("D") && keyString.Length == 2)
            {
                keyString = keyString.Substring(1);
            }

            parts.Add(keyString);

            return string.Join("+", parts);
        }

        private void LoadSettings()
        {
            if (_settings.ServiceProvider == "DeepSeek")
            {
                ComboServiceProvider.SelectedIndex = 1;
            }
            else
            {
                ComboServiceProvider.SelectedIndex = 0;
            }
            TxtApiUrl.Text = _settings.ApiUrl;
            PwdApiKey.Password = _settings.ApiKey;
            TxtModelName.Text = _settings.ModelName;
            TxtOcrEnhancementPrompt.Text = _settings.OcrEnhancementPrompt;
            TxtAnalysisPrompt.Text = _settings.AnalysisPrompt;
            TxtTranslationPrompt.Text = _settings.TranslationPrompt;

            if (_settings.OutputFileFormat == "md文件")
            {
                ComboOutputFormat.SelectedIndex = 1;
            }
            else
            {
                ComboOutputFormat.SelectedIndex = 0;
            }

            ComboOcrServiceProvider.SelectedIndex = 0;
            TxtOcrApiUrl.Text = _settings.OcrApiUrl;
            PwdOcrApiKey.Password = _settings.OcrApiKey;
            TxtOcrModel.Text = _settings.OcrModel;

            TxtScreenshotHotkey.Text = _settings.ScreenshotHotkey;

            CheckMergeIntoSingleFile.IsChecked = _settings.MergeIntoSingleFile;
            CheckSaveProcessedImage.IsChecked = _settings.SaveProcessedImage;
            CheckEnableGpu.IsChecked = _settings.EnableGpu;

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

            _settings.ApiKey = GetApiKey();
            _settings.OcrEnhancementPrompt = TxtOcrEnhancementPrompt.Text;
            _settings.AnalysisPrompt = TxtAnalysisPrompt.Text;
            _settings.TranslationPrompt = TxtTranslationPrompt.Text;

            if (ComboOutputFormat.SelectedIndex == 1)
            {
                _settings.OutputFileFormat = "md文件";
            }
            else
            {
                _settings.OutputFileFormat = "txt标准格式";
            }

            _settings.OcrServiceProvider = "PaddleOCR（在线）";
            _settings.OcrApiUrl = TxtOcrApiUrl.Text.Trim();
            _settings.OcrApiKey = GetOcrApiKey();
            _settings.OcrModel = TxtOcrModel.Text.Trim();

            _settings.ScreenshotHotkey = TxtScreenshotHotkey.Text.Trim();

            _settings.MergeIntoSingleFile = CheckMergeIntoSingleFile.IsChecked ?? false;
            _settings.SaveProcessedImage = CheckSaveProcessedImage.IsChecked ?? false;
            _settings.EnableGpu = CheckEnableGpu.IsChecked ?? false;

            _isModified = true;
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(TxtApiUrl.Text) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(TxtModelName.Text))
            {
                MessageBox.Show("请填写完整的API配置信息", "配置不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtConnectionStatus.Text = "正在测试连接...";
                TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Yellow;

                var testSettings = new AISettings
                {
                    ApiUrl = TxtApiUrl.Text.Trim(),
                    ApiKey = apiKey,
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
            if (!_isInitialized || TxtApiUrl == null || TxtModelName == null) return;

            if (ComboServiceProvider.SelectedIndex == 1)
            {
                TxtApiUrl.Text = "https://api.deepseek.com";
                if (string.IsNullOrWhiteSpace(TxtModelName.Text) || TxtModelName.Text == "gpt-3.5-turbo")
                {
                    TxtModelName.Text = "deepseek-v4-flash";
                }
                TxtApiUrl.IsEnabled = false;
                TxtModelName.IsEnabled = true;
            }
            else
            {
                TxtApiUrl.IsEnabled = true;
                TxtModelName.IsEnabled = true;
            }
        }

        private async void BtnTestOcrConnection_Click(object sender, RoutedEventArgs e)
        {
            string ocrApiKey = GetOcrApiKey();
            if (string.IsNullOrWhiteSpace(TxtOcrApiUrl.Text) ||
                string.IsNullOrWhiteSpace(ocrApiKey))
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
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var testImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

                    byte[] imageBytes = Convert.FromBase64String(testImageBase64);

                    using var formData = new System.Net.Http.MultipartFormDataContent();

                    var fileContent = new System.Net.Http.ByteArrayContent(imageBytes);
                    fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");
                    formData.Add(fileContent, "file", "test.png");

                    formData.Add(new System.Net.Http.StringContent("PaddleOCR-VL-1.6"), "model");

                    var optionalPayload = new
                    {
                        useDocOrientationClassify = false,
                        useDocUnwarping = false,
                        useChartRecognition = false
                    };
                    var optionalPayloadJson = System.Text.Json.JsonSerializer.Serialize(optionalPayload);
                    formData.Add(new System.Net.Http.StringContent(optionalPayloadJson), "optionalPayload");

                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {ocrApiKey}");

                    var response = await httpClient.PostAsync(TxtOcrApiUrl.Text.Trim(), formData);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

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

        private void BtnCaptureHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (_isCapturingHotkey)
            {
                _isCapturingHotkey = false;
                BtnCaptureHotkey.Content = "点击设置";
                return;
            }

            _isCapturingHotkey = true;
            _capturedHotkey = string.Empty;
            _capturedKey = Key.None;
            _capturedModifiers = ModifierKeys.None;
            BtnCaptureHotkey.Content = "取消";
            TxtScreenshotHotkey.Text = "请按下快捷键...";
        }

        private void BtnClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            TxtScreenshotHotkey.Text = string.Empty;
            _capturedHotkey = string.Empty;
            _capturedKey = Key.None;
            _capturedModifiers = ModifierKeys.None;
        }

        private void BtnToggleApiKey_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var parent = btn.Parent as StackPanel;
            if (parent == null) return;

            string currentPassword = string.Empty;
            PasswordBox? existingPwdBox = null;
            TextBox? existingTextBox = null;

            foreach (var child in parent.Children)
            {
                if (child is PasswordBox pwdBox)
                {
                    existingPwdBox = pwdBox;
                    currentPassword = pwdBox.Password;
                }
                else if (child is TextBox textBox)
                {
                    existingTextBox = textBox;
                    currentPassword = textBox.Text;
                }
            }

            if (btn.Content.ToString() == "👁")
            {
                var textBox = new TextBox
                {
                    Text = currentPassword,
                    Width = 600,
                    Margin = new Thickness(0, 5, 0, 10),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Microsoft YaHei, Arial"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.DarkGray,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new System.Windows.Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4)
                };

                if (existingPwdBox != null)
                {
                    int index = parent.Children.IndexOf(existingPwdBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, textBox);
                }
                btn.Content = "🙈";
            }
            else
            {
                var pwdBox = new PasswordBox
                {
                    Password = currentPassword,
                    Width = 600,
                    Margin = new Thickness(0, 5, 0, 10),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Microsoft YaHei, Arial"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.DarkGray,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new System.Windows.Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4)
                };

                if (existingTextBox != null)
                {
                    int index = parent.Children.IndexOf(existingTextBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, pwdBox);
                }
                btn.Content = "👁";
            }
        }

        private void BtnToggleOcrApiKey_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var parent = btn.Parent as StackPanel;
            if (parent == null) return;

            string currentPassword = string.Empty;
            PasswordBox? existingPwdBox = null;
            TextBox? existingTextBox = null;

            foreach (var child in parent.Children)
            {
                if (child is PasswordBox pwdBox)
                {
                    existingPwdBox = pwdBox;
                    currentPassword = pwdBox.Password;
                }
                else if (child is TextBox textBox)
                {
                    existingTextBox = textBox;
                    currentPassword = textBox.Text;
                }
            }

            if (btn.Content.ToString() == "👁")
            {
                var textBox = new TextBox
                {
                    Text = currentPassword,
                    Width = 600,
                    Margin = new Thickness(0, 5, 0, 10),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Microsoft YaHei, Arial"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.DarkGray,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new System.Windows.Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4)
                };

                if (existingPwdBox != null)
                {
                    int index = parent.Children.IndexOf(existingPwdBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, textBox);
                }
                btn.Content = "🙈";
            }
            else
            {
                var pwdBox = new PasswordBox
                {
                    Password = currentPassword,
                    Width = 600,
                    Margin = new Thickness(0, 5, 0, 10),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Microsoft YaHei, Arial"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.DarkGray,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new System.Windows.Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4)
                };

                if (existingTextBox != null)
                {
                    int index = parent.Children.IndexOf(existingTextBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, pwdBox);
                }
                btn.Content = "👁";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(TxtApiUrl.Text) ||
                string.IsNullOrWhiteSpace(apiKey) ||
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
                if (ComboServiceProvider.SelectedIndex == 1)
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

        private string GetApiKey()
        {
            var stackPanel = BtnToggleApiKey.Parent as StackPanel;
            if (stackPanel != null)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is PasswordBox pwdBox)
                    {
                        return pwdBox.Password;
                    }
                    else if (child is TextBox textBox)
                    {
                        return textBox.Text;
                    }
                }
            }
            return string.Empty;
        }

        private string GetOcrApiKey()
        {
            var stackPanel = BtnToggleOcrApiKey.Parent as StackPanel;
            if (stackPanel != null)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is PasswordBox pwdBox)
                    {
                        return pwdBox.Password;
                    }
                    else if (child is TextBox textBox)
                    {
                        return textBox.Text;
                    }
                }
            }
            return string.Empty;
        }

        public bool IsModified => _isModified;
    }
}
