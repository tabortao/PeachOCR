using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PeachOCR
{
    public class TestOcrApi
    {
        public static async Task TestConnection(string apiUrl, string apiKey)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Prepare test request with a simple valid image
                var testImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="; // 1x1 transparent PNG

                // Convert base64 to bytes
                byte[] imageBytes = Convert.FromBase64String(testImageBase64);

                // Create multipart form data
                using var formData = new MultipartFormDataContent();

                // Add file
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");
                formData.Add(fileContent, "file", "test.png");

                // Add model parameter
                formData.Add(new StringContent("PaddleOCR-VL-1.6"), "model");

                // Add optional payload as JSON string
                var optionalPayload = new
                {
                    useDocOrientationClassify = false,
                    useDocUnwarping = false,
                    useChartRecognition = false
                };
                var optionalPayloadJson = System.Text.Json.JsonSerializer.Serialize(optionalPayload);
                formData.Add(new StringContent(optionalPayloadJson), "optionalPayload");

                // Add authorization header
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {apiKey}");

                Console.WriteLine($"Testing API: {apiUrl}");
                Console.WriteLine($"Authorization: bearer {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");

                // Send test request
                var response = await httpClient.PostAsync(apiUrl, formData);

                Console.WriteLine($"Response Status: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ API Test Successful!");

                    // Try to parse job response
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("data", out var dataElement) &&
                            dataElement.TryGetProperty("jobId", out var jobIdElement))
                        {
                            string jobId = jobIdElement.GetString() ?? "";
                            Console.WriteLine($"✅ Job submitted successfully. Job ID: {jobId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️  Job response parsing failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ API Test Failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ API Test Error: {ex.Message}");
            }
        }

        public static async Task TestSyncApi(string apiUrl, string apiKey)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Prepare test request with a simple valid image
                var testImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="; // 1x1 transparent PNG

                // Create JSON payload for sync API
                var payload = new
                {
                    file = testImageBase64,
                    fileType = 1, // Image file
                    useDocOrientationClassify = false,
                    useDocUnwarping = false,
                    useChartRecognition = false
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Add authorization header for sync API
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}");

                Console.WriteLine($"Testing Sync API: {apiUrl}");
                Console.WriteLine($"Authorization: token {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");

                // Send test request
                var response = await httpClient.PostAsync(apiUrl, content);

                Console.WriteLine($"Response Status: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Sync API Test Successful!");
                }
                else
                {
                    Console.WriteLine($"❌ Sync API Test Failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Sync API Test Error: {ex.Message}");
            }
        }
    }
}