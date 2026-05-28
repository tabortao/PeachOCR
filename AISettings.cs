using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PeachOCR
{
    public class AISettings : INotifyPropertyChanged
    {
        private string _serviceProvider = "OpenAI兼容";
        private string _apiUrl = "https://api.openai.com/v1";
        private string _apiKey = string.Empty;
        private string _modelName = "gpt-3.5-turbo";
        private string _ocrEnhancementPrompt = "请对以下OCR识别结果进行文本增强优化，要求：\n1. 保持原文语言不变（中文保持中文，英文保持英文，不进行翻译）\n2. 纠正错别字、OCR识别错误和明显的拼写错误\n3. 优化文本排版：规范标点符号使用，修正空格和换行\n4. 根据语义进行合理的段落划分：按内容逻辑分段，保持段落连贯性\n5. 保持原文意思和专业术语不变\n6. 只返回优化后的文本内容，不要添加任何解释或说明\n\nOCR识别结果：";
        private string _analysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
        private string _translationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";
        private string _outputFileFormat = "txt标准格式";

        private string _ocrServiceProvider = "PaddleOCR（在线）";
        private string _ocrApiUrl = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs";
        private string _ocrApiKey = string.Empty;
        private string _ocrModel = "PaddleOCR-VL-1.6";

        private string _screenshotHotkey = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string ServiceProvider
        {
            get => _serviceProvider;
            set => SetField(ref _serviceProvider, value);
        }

        public string ApiUrl
        {
            get => _apiUrl;
            set => SetField(ref _apiUrl, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetField(ref _apiKey, value);
        }

        public string ModelName
        {
            get => _modelName;
            set => SetField(ref _modelName, value);
        }

        public string OcrEnhancementPrompt
        {
            get => _ocrEnhancementPrompt;
            set => SetField(ref _ocrEnhancementPrompt, value);
        }

        public string AnalysisPrompt
        {
            get => _analysisPrompt;
            set => SetField(ref _analysisPrompt, value);
        }

        public string TranslationPrompt
        {
            get => _translationPrompt;
            set => SetField(ref _translationPrompt, value);
        }

        public string OutputFileFormat
        {
            get => _outputFileFormat;
            set => SetField(ref _outputFileFormat, value);
        }

        public string OcrServiceProvider
        {
            get => _ocrServiceProvider;
            set => SetField(ref _ocrServiceProvider, value);
        }

        public string OcrApiUrl
        {
            get => _ocrApiUrl;
            set => SetField(ref _ocrApiUrl, value);
        }

        public string OcrApiKey
        {
            get => _ocrApiKey;
            set => SetField(ref _ocrApiKey, value);
        }

        public string OcrModel
        {
            get => _ocrModel;
            set => SetField(ref _ocrModel, value);
        }

        public string ScreenshotHotkey
        {
            get => _screenshotHotkey;
            set => SetField(ref _screenshotHotkey, value);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiUrl) &&
                                   !string.IsNullOrWhiteSpace(ApiKey) &&
                                   !string.IsNullOrWhiteSpace(ModelName);

        public void ResetToDefaults()
        {
            ServiceProvider = "OpenAI兼容";
            ApiUrl = "https://api.openai.com/v1";
            ApiKey = string.Empty;
            ModelName = "gpt-3.5-turbo";
            OcrEnhancementPrompt = "请对以下OCR识别结果进行文本增强优化，要求：\n1. 保持原文语言不变（中文保持中文，英文保持英文，不进行翻译）\n2. 纠正错别字、OCR识别错误和明显的拼写错误\n3. 优化文本排版：规范标点符号使用，修正空格和换行\n4. 根据语义进行合理的段落划分：按内容逻辑分段，保持段落连贯性\n5. 保持原文意思和专业术语不变\n6. 只返回优化后的文本内容，不要添加任何解释或说明\n\nOCR识别结果：";
            AnalysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
            TranslationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";
            OutputFileFormat = "txt标准格式";
            OcrServiceProvider = "PaddleOCR（在线）";
            OcrApiUrl = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs";
            OcrApiKey = string.Empty;
            OcrModel = "PaddleOCR-VL-1.6";
            ScreenshotHotkey = string.Empty;
        }

        public void ResetToDeepSeekDefaults()
        {
            ServiceProvider = "DeepSeek";
            ApiUrl = "https://api.deepseek.com";
            ApiKey = string.Empty;
            ModelName = "deepseek-v4-flash";
            OcrEnhancementPrompt = "请对以下OCR识别结果进行文本增强优化，要求：\n1. 保持原文语言不变（中文保持中文，英文保持英文，不进行翻译）\n2. 纠正错别字、OCR识别错误和明显的拼写错误\n3. 优化文本排版：规范标点符号使用，修正空格和换行\n4. 根据语义进行合理的段落划分：按内容逻辑分段，保持段落连贯性\n5. 保持原文意思和专业术语不变\n6. 只返回优化后的文本内容，不要添加任何解释或说明\n\nOCR识别结果：";
            AnalysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
            TranslationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";
            OutputFileFormat = "txt标准格式";
            OcrServiceProvider = "PaddleOCR（在线）";
            OcrApiUrl = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs";
            OcrApiKey = string.Empty;
            OcrModel = "PaddleOCR-VL-1.6";
            ScreenshotHotkey = string.Empty;
        }
    }
}
