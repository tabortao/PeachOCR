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
        private string _ocrEnhancementPrompt = "请对以下OCR识别结果进行优化：\n1. 纠正错别字和识别错误\n2. 优化文本排版和格式\n3. 根据语义进行合理的段落划分\n4. 保持原文意思不变\n\nOCR识别结果：";
        private string _analysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
        private string _translationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";

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

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiUrl) &&
                                   !string.IsNullOrWhiteSpace(ApiKey) &&
                                   !string.IsNullOrWhiteSpace(ModelName);

        public void ResetToDefaults()
        {
            ServiceProvider = "OpenAI兼容";
            ApiUrl = "https://api.openai.com/v1";
            ApiKey = string.Empty;
            ModelName = "gpt-3.5-turbo";
            OcrEnhancementPrompt = "请对以下OCR识别结果进行优化：\n1. 纠正错别字和识别错误\n2. 优化文本排版和格式\n3. 根据语义进行合理的段落划分\n4. 保持原文意思不变\n\nOCR识别结果：";
            AnalysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
            TranslationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";
        }

        public void ResetToDeepSeekDefaults()
        {
            ServiceProvider = "DeepSeek";
            ApiUrl = "https://api.deepseek.com";
            ApiKey = string.Empty;
            ModelName = "deepseek-v4-flash"; // Default DeepSeek model
            OcrEnhancementPrompt = "请对以下OCR识别结果进行优化：\n1. 纠正错别字和识别错误\n2. 优化文本排版和格式\n3. 根据语义进行合理的段落划分\n4. 保持原文意思不变\n\nOCR识别结果：";
            AnalysisPrompt = "请对以下文本进行分析和总结：\n1. 提取关键信息\n2. 总结主要内容\n3. 识别重要数据或要点\n4. 以清晰的结构呈现\n\n需要分析的文本：";
            TranslationPrompt = "请将以下文本翻译成中文：\n1. 保持专业术语的准确性\n2. 确保翻译的流畅性和可读性\n3. 保持原文的格式和结构\n\n需要翻译的文本：";
        }
    }
}