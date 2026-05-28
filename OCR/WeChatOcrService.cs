using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WeChatOcr;

namespace PeachOCR.OCR
{
    public class WeChatOcrService
    {
        public async Task<List<string>> RecognizeTextAsync(string imagePath)
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                using var ocr = new ImageOcr();
                
                ocr.Run(imagePath, (path, result) =>
                {
                    try
                    {
                        if (result == null)
                        {
                            tcs.SetResult(string.Empty);
                            return;
                        }

                        var list = result?.OcrResult?.SingleResult;
                        if (list == null)
                        {
                            tcs.SetResult("WeChatOCR get result is null");
                            return;
                        }

                        var sb = new StringBuilder();
                        for (var i = 0; i < list?.Count; i++)
                        {
                            if (list[i] is not { } item || string.IsNullOrEmpty(item.SingleStrUtf8))
                                continue;

                            sb.AppendLine(item.SingleStrUtf8);
                        }

                        tcs.SetResult(sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                string result = await tcs.Task;
                return string.IsNullOrEmpty(result) ? new List<string>() : result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"微信OCR识别失败: {ex.Message}", ex);
            }
        }
    }
}
