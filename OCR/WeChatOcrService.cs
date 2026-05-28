using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WeChatOcr;

namespace PeachOCR.OCR
{
    public class WeChatOcrService
    {
        private static TaskCompletionSource<string>? _currentTcs;

        public async Task<List<string>> RecognizeTextAsync(string imagePath)
        {
            if (_currentTcs != null && !_currentTcs.Task.IsCompleted)
            {
                throw new Exception("请等待上一次OCR操作完成。");
            }
            
            var tcs = new TaskCompletionSource<string>();
            _currentTcs = tcs;

            try
            {
                // App.xaml.cs 中已经正确设置了 DataLocation.BaseDirectory
                using var ocr = new ImageOcr();
                
                ocr.Run(imagePath, (path, result) =>
                {
                    try
                    {
                        if (result == null)
                        {
                            if (!tcs.Task.IsCompleted)
                                tcs.SetResult(string.Empty);
                            return;
                        }

                        var list = result?.OcrResult?.SingleResult;
                        if (list == null)
                        {
                            if (!tcs.Task.IsCompleted)
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

                        // 清理临时文件
                        try
                        {
                            if (File.Exists(path))
                                File.Delete(path);
                        }
                        catch
                        {
                            // ignore
                        }

                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (!tcs.Task.IsCompleted)
                            tcs.SetException(ex);
                    }
                });

                var timeoutTask = Task.Delay(8000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    tcs.TrySetCanceled();
                    throw new TimeoutException("WeChatOCR操作超时。");
                }

                string result = await tcs.Task;
                return string.IsNullOrEmpty(result) ? new List<string>() : result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"微信OCR识别失败: {ex.Message}", ex);
            }
            finally
            {
                _currentTcs = null;
            }
        }
    }
}
