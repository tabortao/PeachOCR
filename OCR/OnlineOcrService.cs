using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PeachOCR.OCR
{
    public class OnlineOcrResult
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<OcrBox> Boxes { get; set; } = new List<OcrBox>();
    }

    public class OcrBox
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class OnlineOcrService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _serviceProvider;

        public OnlineOcrService(string serviceProvider, string apiUrl, string apiKey, string model)
        {
            _serviceProvider = serviceProvider;
            _apiUrl = apiUrl;
            _apiKey = apiKey;
            _model = model;
            _httpClient = new HttpClient();
        }

        public async Task<List<OnlineOcrResult>> ProcessImageAsync(string imagePath, string outputFormat = "txt标准格式")
        {
            if (_serviceProvider == "PaddleOCR（在线）")
            {
                return await ProcessPaddleOcrOnlineAsync(imagePath, outputFormat);
            }
            else if (_serviceProvider == "硅基流动")
            {
                return await ProcessSiliconFlowAsync(imagePath, outputFormat);
            }
            else
            {
                throw new NotSupportedException($"不支持的OCR服务提供商: {_serviceProvider}");
            }
        }

        private async Task<List<OnlineOcrResult>> ProcessPaddleOcrOnlineAsync(string imagePath, string outputFormat)
        {
            try
            {
                bool prettifyMarkdown = outputFormat == "md文件";

                // Read file bytes
                byte[] fileBytes = File.ReadAllBytes(imagePath);

                // Prepare headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {_apiKey}");

                // Create multipart form data
                using var formData = new System.Net.Http.MultipartFormDataContent();

                // Add file
                var fileContent = new System.Net.Http.ByteArrayContent(fileBytes);
                string fileName = System.IO.Path.GetFileName(imagePath);
                string contentType = IsPdfFile(imagePath) ? "application/pdf" : "image/png";
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                formData.Add(fileContent, "file", fileName);

                // Add model parameter
                formData.Add(new System.Net.Http.StringContent(_model), "model");

                // Add optional payload as JSON string
                var optionalPayload = new
                {
                    useDocOrientationClassify = false,
                    useDocUnwarping = false,
                    useChartRecognition = false
                };
                var optionalPayloadJson = JsonSerializer.Serialize(optionalPayload);
                formData.Add(new System.Net.Http.StringContent(optionalPayloadJson), "optionalPayload");

                // Add prettifyMarkdown as separate parameter (not in optionalPayload)
                if (prettifyMarkdown)
                {
                    formData.Add(new System.Net.Http.StringContent("true"), "prettifyMarkdown");
                }

                // Send request
                var response = await _httpClient.PostAsync(_apiUrl, formData);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OCR request failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var responseDoc = JsonDocument.Parse(responseContent);
                var root = responseDoc.RootElement;

                // Check for job submission success (异步API返回job信息)
                if (root.TryGetProperty("data", out var dataElement))
                {
                    // 这是一个异步作业提交响应，需要获取jobId并轮询结果
                    if (dataElement.TryGetProperty("jobId", out var jobIdElement))
                    {
                        string jobId = jobIdElement.GetString() ?? string.Empty;
                        return await PollForJobResultAsync(jobId);
                    }
                }

                // 如果不是异步响应，尝试解析同步响应格式
                var results = new List<OnlineOcrResult>();

                if (root.TryGetProperty("result", out var resultElement) &&
                    resultElement.TryGetProperty("layoutParsingResults", out var layoutResultsElement))
                {
                    foreach (var layoutResult in layoutResultsElement.EnumerateArray())
                    {
                        if (layoutResult.TryGetProperty("markdown", out var markdownElement) &&
                            markdownElement.TryGetProperty("text", out var textElement))
                        {
                            string markdownText = textElement.GetString() ?? string.Empty;

                            results.Add(new OnlineOcrResult
                            {
                                Text = markdownText,
                                Confidence = 0.95 // Default confidence for online OCR
                            });
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"PaddleOCR在线处理失败: {ex.Message}", ex);
            }
        }

        private async Task<List<OnlineOcrResult>> PollForJobResultAsync(string jobId)
        {
            string jobStatusUrl = $"{_apiUrl}/{jobId}";

            for (int attempt = 0; attempt < 60; attempt++) // 最多等待5分钟 (60 * 5秒)
            {
                try
                {
                    var statusResponse = await _httpClient.GetAsync(jobStatusUrl);
                    if (!statusResponse.IsSuccessStatusCode)
                    {
                        await Task.Delay(5000); // 等待5秒
                        continue;
                    }

                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    using var statusDoc = JsonDocument.Parse(statusContent);
                    var root = statusDoc.RootElement;

                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        string state = dataElement.TryGetProperty("state", out var stateElement)
                            ? stateElement.GetString() ?? "" : "";

                        if (state == "done")
                        {
                            // 作业完成，获取结果
                            if (dataElement.TryGetProperty("resultUrl", out var resultUrlElement) &&
                                resultUrlElement.TryGetProperty("jsonUrl", out var jsonUrlElement))
                            {
                                string jsonUrl = jsonUrlElement.GetString() ?? string.Empty;
                                return await FetchJobResultsAsync(jsonUrl);
                            }
                        }
                        else if (state == "failed")
                        {
                            string errorMsg = dataElement.TryGetProperty("errorMsg", out var errorMsgElement)
                                ? errorMsgElement.GetString() ?? "Unknown error" : "Unknown error";
                            throw new Exception($"OCR job failed: {errorMsg}");
                        }
                    }

                    await Task.Delay(5000); // 等待5秒后重试
                }
                catch
                {
                    // 如果出现错误，等待后重试
                    await Task.Delay(5000);
                }
            }

            throw new Exception("OCR job timed out after 5 minutes");
        }

        private async Task<List<OnlineOcrResult>> FetchJobResultsAsync(string jsonUrl)
        {
            using var tempClient = new HttpClient();
            var resultResponse = await tempClient.GetAsync(jsonUrl);

            if (!resultResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch results: {resultResponse.StatusCode}");
            }

            var resultContent = await resultResponse.Content.ReadAsStringAsync();
            var results = new List<OnlineOcrResult>();

            // 解析JSONL格式（每行一个JSON对象）
            var lines = resultContent.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var lineDoc = JsonDocument.Parse(line);
                    var root = lineDoc.RootElement;

                    if (root.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("layoutParsingResults", out var layoutResultsElement))
                    {
                        foreach (var layoutResult in layoutResultsElement.EnumerateArray())
                        {
                            if (layoutResult.TryGetProperty("markdown", out var markdownElement) &&
                                markdownElement.TryGetProperty("text", out var textElement))
                            {
                                string markdownText = textElement.GetString() ?? string.Empty;

                                results.Add(new OnlineOcrResult
                                {
                                    Text = markdownText,
                                    Confidence = 0.95
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // 跳过解析失败的行
                    continue;
                }
            }

            return results;
        }

        private async Task<List<OnlineOcrResult>> ProcessSiliconFlowAsync(string imagePath, string outputFormat)
        {
            // TODO: Implement SiliconFlow OCR API
            throw new NotImplementedException("硅基流动OCR服务暂未实现");
        }

        private bool IsPdfFile(string filePath)
        {
            return filePath.ToLower().EndsWith(".pdf");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}