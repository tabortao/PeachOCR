using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using Sdcb.OpenVINO;
using Sdcb.OpenVINO.PaddleOCR;
using Sdcb.OpenVINO.PaddleOCR.Models;
using Sdcb.OpenVINO.PaddleOCR.Models.Online;
using PeachOCR.OCR;

namespace OCR
{
    /// <summary>
    /// 批量OCR处理器，基于 Sdcb.OpenVINO.PaddleOCR，支持 PP-OCRv4/v5/v6 模型。
    /// 所有模型均通过 OnlineFullModels 下载并缓存到本地。
    /// </summary>
    public class OcrBatchProcessor
    {
        private ModelType modelType = ModelType.PP_OCRv6;
        // 缓存的模型实例（每种模型类型下载一次后缓存）
        private FullOcrModel? cachedV4Model;
        private FullOcrModel? cachedV5Model;
        private FullOcrModel? cachedV6Model;
        // 设备选项
        private string deviceName = "CPU";
        // 图片路径列表
        private List<string> imagePaths = new List<string>();
        // 是否保存/显示结果图片
        private bool saveResultImage = true;
        private string outputFileFormat = "txt标准格式";
        private string ocrServiceProvider = "";
        private string ocrApiUrl = "";
        private string ocrApiKey = "";
        private string ocrModel = "";
        private bool showResultImage = false;

        /// <summary>
        /// PaddleOCR模型类型枚举
        /// </summary>
        public enum ModelType { PP_OCRv4, PP_OCRv5, PP_OCRv6 }

        /// <summary>
        /// 设置模型类型（v4/v5/v6）。所有模型均通过 OnlineFullModels 下载，首次使用自动缓存。
        /// </summary>
        public void SetModel(ModelType type)
        {
            modelType = type;
        }

        /// <summary>
        /// 设置是否使用GPU及设备名
        /// </summary>
        public void SetUseGpu(bool useGpuForCls, bool useGpuForRec, string device = "GPU")
        {
            deviceName = (useGpuForCls || useGpuForRec) ? device : "CPU";
        }

        /// <summary>
        /// 添加单张图片路径
        /// </summary>
        public void AddImage(string imgPath) => imagePaths.Add(imgPath);

        /// <summary>
        /// 批量添加图片路径
        /// </summary>
        public void AddImages(IEnumerable<string> imgPaths) => imagePaths.AddRange(imgPaths);

        /// <summary>
        /// 设置是否保存结果图片
        /// </summary>
        public void SetSaveResultImage(bool save) => saveResultImage = save;

        /// <summary>
        /// 设置输出文件格式
        /// </summary>
        public void SetOutputFileFormat(string format) => outputFileFormat = format;

        /// <summary>
        /// 设置OCR服务配置
        /// </summary>
        public void SetOcrServiceConfig(string provider, string apiUrl, string apiKey, string model)
        {
            ocrServiceProvider = provider;
            ocrApiUrl = apiUrl;
            ocrApiKey = apiKey;
            ocrModel = model;
        }

        /// <summary>
        /// 设置是否显示结果图片
        /// </summary>
        public void SetShowResultImage(bool show) => showResultImage = show;

        /// <summary>
        /// 获取当前模型类型的缓存模型（下载一次后缓存）
        /// </summary>
        private async Task<FullOcrModel> GetModelAsync()
        {
            var modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
            Directory.CreateDirectory(modelsDir);

            return modelType switch
            {
                ModelType.PP_OCRv4 => cachedV4Model ??= await DownloadV4ModelAsync(modelsDir),
                ModelType.PP_OCRv5 => cachedV5Model ??= await DownloadV4ModelAsync(modelsDir),
                ModelType.PP_OCRv6 => cachedV6Model ??= await DownloadV6ModelAsync(modelsDir),
                _ => throw new InvalidOperationException($"未知模型类型: {modelType}")
            };
        }

        private static async Task<FullOcrModel> DownloadV4ModelAsync(string modelsDir)
        {
            Settings.GlobalModelDirectory = modelsDir;
            return await OnlineFullModels.ChineseV4.DownloadAsync();
        }

        private static async Task<FullOcrModel> DownloadV6ModelAsync(string modelsDir)
        {
            var v6Dir = Path.Combine(modelsDir, "ch_PP-OCRv6");
            Settings.GlobalModelDirectory = v6Dir;
            return await OnlineFullModels.ChineseV6Small.DownloadAsync();
        }

        /// <summary>
        /// 单张图片OCR结果详情
        /// </summary>
        public class OcrResultDetail
        {
            public string ImgPath { get; set; } = string.Empty;
            public List<OcrRegionResult>? Result { get; set; }
            public string? ResultImgPath { get; set; }
            public long OcrMs { get; set; }
        }

        /// <summary>
        /// OCR区域识别结果
        /// </summary>
        public class OcrRegionResult
        {
            public string Text { get; set; } = "";
            public float Score { get; set; }
            public List<List<int>> Box { get; set; } = new();
        }

        /// <summary>
        /// 批量执行OCR，支持有限并发处理多张图片。
        /// </summary>
        public async Task<(List<OcrResultDetail> details, long totalMs)> RunBatchOcrAsync(
            int maxDegreeOfParallelism = 2, Action<int, int>? onProgress = null)
        {
            // 预下载模型
            FullOcrModel model = await GetModelAsync();

            var details = new List<OcrResultDetail>();
            var showImgs = new List<(string, Mat)>();
            var lockObj = new object();
            Stopwatch swAll = new Stopwatch();
            swAll.Start();
            int finished = 0;
            int total = imagePaths.Count;

            using (var semaphore = new System.Threading.SemaphoreSlim(maxDegreeOfParallelism))
            {
                var tasks = imagePaths.Select(async imgPath =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        Mat img = Cv2.ImRead(imgPath);
                        if (img.Empty())
                        {
                            lock (lockObj)
                            {
                                details.Add(new OcrResultDetail { ImgPath = imgPath, Result = null, ResultImgPath = null, OcrMs = 0 });
                                finished++;
                                onProgress?.Invoke(finished, total);
                            }
                            return;
                        }

                        // 每个任务创建独立的 PaddleOcrAll 实例，保证线程安全
                        DeviceOptions devOptions = new DeviceOptions(deviceName);
                        using var ocr = new PaddleOcrAll(model, new PaddleOcrOptions(devOptions));

                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        List<OcrRegionResult>? ocrResult = null;
                        bool usedOnlineOcr = false;
                        string processingMethod = "本地OCR";

                        // Check if online OCR should be used
                        if (!string.IsNullOrEmpty(ocrServiceProvider) && !string.IsNullOrEmpty(ocrApiUrl))
                        {
                            try
                            {
                                using var onlineOcr = new OnlineOcrService(ocrServiceProvider, ocrApiUrl, ocrApiKey, ocrModel);
                                var onlineResults = await onlineOcr.ProcessImageAsync(imgPath, outputFileFormat);

                                ocrResult = new List<OcrRegionResult>();
                                foreach (var onlineResult in onlineResults)
                                {
                                    ocrResult.Add(new OcrRegionResult
                                    {
                                        Text = onlineResult.Text,
                                        Score = (float)onlineResult.Confidence,
                                        Box = new List<List<int>> { new List<int> { 0, 0, 100, 100 } }
                                    });
                                }
                                usedOnlineOcr = true;
                                processingMethod = "在线OCR";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"在线OCR处理失败 {imgPath}: {ex.Message}");
                                processingMethod = $"在线OCR失败，降级使用本地OCR ({ex.Message})";
                                // Fall back to local OCR
                                var localResult = await Task.Run(() => ocr.Run(img));
                                ocrResult = ConvertResult(localResult);
                            }
                        }
                        else
                        {
                            // 使用本地OCR
                            var localResult = await Task.Run(() => ocr.Run(img));
                            ocrResult = ConvertResult(localResult);
                        }

                        // Add processing method note
                        if (ocrResult != null && !usedOnlineOcr && !string.IsNullOrEmpty(ocrServiceProvider))
                        {
                            ocrResult.Insert(0, new OcrRegionResult
                            {
                                Text = $"[处理方式: {processingMethod}]",
                                Score = 1.0f,
                                Box = new List<List<int>> { new List<int> { 0, 0, 100, 100 } }
                            });
                        }

                        sw.Stop();
                        string? resultImgPath = null;
                        Mat? resultImg = null;

                        // 可视化（仅本地OCR）
                        if (ocrResult != null && ocrResult.Count > 0 && !usedOnlineOcr)
                        {
                            try
                            {
                                resultImg = VisualizeBboxes(img, ocrResult);
                                string directory = Path.GetDirectoryName(imgPath) ?? string.Empty;
                                string resultPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(imgPath) + "_result.jpg");
                                if (saveResultImage)
                                {
                                    Cv2.ImWrite(resultPath, resultImg);
                                    resultImgPath = resultPath;
                                }
                                if (showResultImage)
                                {
                                    lock (lockObj)
                                        showImgs.Add((imgPath, resultImg));
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"可视化处理失败 {imgPath}: {ex.Message}");
                            }
                        }

                        // 保存识别文本到 OCR_Result 文件夹
                        if (ocrResult != null)
                        {
                            string directory = Path.GetDirectoryName(imgPath) ?? string.Empty;
                            string ocrResultDir = Path.Combine(directory, "OCR_Result");
                            Directory.CreateDirectory(ocrResultDir);

                            if (outputFileFormat == "md文件")
                            {
                                string mdPath = Path.Combine(ocrResultDir, Path.GetFileNameWithoutExtension(imgPath) + ".md");
                                using (var writer = new StreamWriter(mdPath, false))
                                {
                                    writer.WriteLine($"# {Path.GetFileNameWithoutExtension(imgPath)}");
                                    writer.WriteLine();
                                    writer.WriteLine("## OCR 识别结果");
                                    writer.WriteLine();
                                    foreach (var item in ocrResult)
                                    {
                                        if (!string.IsNullOrWhiteSpace(item.Text))
                                        {
                                            writer.WriteLine(item.Text);
                                            writer.WriteLine();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string txtPath = Path.Combine(ocrResultDir, Path.GetFileNameWithoutExtension(imgPath) + ".txt");
                                using (var writer = new StreamWriter(txtPath, false))
                                {
                                    foreach (var item in ocrResult)
                                    {
                                        writer.WriteLine(item.Text);
                                    }
                                }
                            }
                        }

                        lock (lockObj)
                        {
                            details.Add(new OcrResultDetail
                            {
                                ImgPath = imgPath,
                                Result = ocrResult,
                                ResultImgPath = resultImgPath,
                                OcrMs = sw.ElapsedMilliseconds
                            });
                            finished++;
                            onProgress?.Invoke(finished, total);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();
                await Task.WhenAll(tasks);
            }

            swAll.Stop();
            // 计时结束后再显示图片
            if (showResultImage)
            {
                foreach (var (imgPath, resultImg) in showImgs)
                {
                    Cv2.ImShow($"result: {Path.GetFileName(imgPath)}", resultImg);
                    Cv2.WaitKey(0);
                    Cv2.DestroyAllWindows();
                }
            }
            // 保证输出顺序与输入顺序一致
            details.Sort((a, b) => string.Compare(a.ImgPath, b.ImgPath, StringComparison.OrdinalIgnoreCase));
            return (details, swAll.ElapsedMilliseconds);
        }

        /// <summary>
        /// 将 PaddleOcrResult 转换为 OcrRegionResult 列表
        /// </summary>
        private static List<OcrRegionResult> ConvertResult(PaddleOcrResult result)
        {
            var list = new List<OcrRegionResult>();
            foreach (var region in result.Regions)
            {
                list.Add(new OcrRegionResult
                {
                    Text = region.Text,
                    Score = region.Score,
                    Box = new List<List<int>>
                    {
                        new List<int> { (int)region.Rect.Center.X, (int)region.Rect.Center.Y,
                                        (int)region.Rect.Size.Width, (int)region.Rect.Size.Height }
                    }
                });
            }
            return list;
        }

        /// <summary>
        /// 使用 OpenCV 绘制检测框
        /// </summary>
        private static Mat VisualizeBboxes(Mat src, List<OcrRegionResult> results)
        {
            Mat vis = src.Clone();
            foreach (var r in results)
            {
                if (r.Box != null && r.Box.Count > 0)
                {
                    var box = r.Box[0];
                    if (box.Count >= 4)
                    {
                        // box format: [cx, cy, w, h]
                        int cx = box[0], cy = box[1], w = box[2], h = box[3];
                        int x = cx - w / 2;
                        int y = cy - h / 2;
                        var rect = new OpenCvSharp.Rect(x, y, w, h);
                        Cv2.Rectangle(vis, rect, Scalar.Red, 2);
                        Cv2.PutText(vis, r.Text,
                            new OpenCvSharp.Point(x, y - 5),
                            HersheyFonts.HersheySimplex, 0.5, Scalar.Red, 1);
                    }
                }
            }
            return vis;
        }
    }
}