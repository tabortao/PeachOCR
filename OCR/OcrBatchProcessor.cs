using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenVinoSharp.Extensions.model.PaddleOCR;

namespace OCR
{
    /// <summary>
    /// 批量OCR处理器，支持模型切换、批处理、GPU设置、批量图片识别、结果图片保存与显示。
    /// </summary>
    public class OcrBatchProcessor
    {
        // 模型路径
        private string? detModelPath;
        private string? clsModelPath;
        private string? recModelPath;
        private string? keyPath;
        // 批处理参数
        private int clsBatchNum = 1;
        private int recBatchNum = 1;
        // GPU设置
        private bool useGpuForCls = false;
        private bool useGpuForRec = false;
        private string deviceForCls = "CPU";
        private string deviceForRec = "CPU";
        // 图片路径列表
        private List<string> imagePaths = new List<string>();
        // OCR推理器
        private OCRPredictor? ocr;
        // 是否保存/显示结果图片
        private bool saveResultImage = true; // 默认保存
        private bool showResultImage = false; // 默认不显示

        /// <summary>
        /// PaddleOCR模型类型枚举
        /// </summary>
        public enum ModelType { PP_OCRv4, PP_OCRv5 }

        /// <summary>
        /// 设置模型类型（v4/v5）
        /// </summary>
        public void SetModel(ModelType type)
        {
            switch (type)
            {
                case ModelType.PP_OCRv4:
                    detModelPath = "./models/ch_PP-OCRv4/PP-OCRv4_mobile_det_onnx.onnx";
                    clsModelPath = "./models/ch_PP-OCRv4/PP-OCRv4_mobile_cls_onnx.onnx";
                    recModelPath = "./models/ch_PP-OCRv4/PP-OCRv4_mobile_rec_onnx.onnx";
                    keyPath = "./models/ch_PP-OCRv4/ppocr_keys_v1.txt";
                    break;
                case ModelType.PP_OCRv5:
                    detModelPath = "./models/ch_PP-OCRv5/PP-OCRv5_mobile_det_onnx.onnx";
                    clsModelPath = "./models/ch_PP-OCRv5/PP-OCRv5_mobile_cls_onnx.onnx";
                    recModelPath = "./models/ch_PP-OCRv5/PP-OCRv5_mobile_rec_onnx.onnx";
                    keyPath = "./models/ch_PP-OCRv5/ppocrv5_dict.txt";
                    break;
            }
        }

        /// <summary>
        /// 设置分类和识别阶段的 batch size
        /// </summary>
        public void SetBatchNum(int clsBatch, int recBatch)
        {
            clsBatchNum = clsBatch;
            recBatchNum = recBatch;
        }

        /// <summary>
        /// 设置是否使用GPU及设备名
        /// </summary>
        public void SetUseGpu(bool useGpuForCls, bool useGpuForRec, string device = "GPU")
        {
            this.useGpuForCls = useGpuForCls;
            this.useGpuForRec = useGpuForRec;
            if (useGpuForCls) deviceForCls = device;
            if (useGpuForRec) deviceForRec = device;
        }

        /// <summary>
        /// 添加单张图片路径
        /// </summary>
        public void AddImage(string imgPath)
        {
            imagePaths.Add(imgPath);
        }

        /// <summary>
        /// 批量添加图片路径
        /// </summary>
        public void AddImages(IEnumerable<string> imgPaths)
        {
            imagePaths.AddRange(imgPaths);
        }

        /// <summary>
        /// 设置是否保存结果图片
        /// </summary>
        public void SetSaveResultImage(bool save) => saveResultImage = save;
        /// <summary>
        /// 设置是否显示结果图片
        /// </summary>
        public void SetShowResultImage(bool show) => showResultImage = show;

        /// <summary>
        /// 初始化OCR配置和推理器
        /// </summary>
        private void InitOcr()
        {
            // 这里假定在调用前已通过 SetModel 设置模型路径，否则会抛出异常
            if (detModelPath is null || clsModelPath is null || recModelPath is null || keyPath is null)
                throw new InvalidOperationException("请先调用 SetModel 设置模型路径");
            OcrConfig config = new OcrConfig();
            config.set_det_model_path(detModelPath!); // 非空断言，已做前置检查
            config.set_cls_model_path(clsModelPath!); // 非空断言，已做前置检查
            config.set_rec_model_path(recModelPath!); // 非空断言，已做前置检查
            config.set_rec_dict_path(keyPath!);       // 非空断言，已做前置检查
            // 正确设置推理参数（静态全局方式）
            RuntimeOption.ClsOption.batch_num = clsBatchNum;
            RuntimeOption.RecOption.batch_num = recBatchNum;
            RuntimeOption.ClsOption.use_gpu = useGpuForCls;
            RuntimeOption.ClsOption.device = deviceForCls;
            RuntimeOption.RecOption.use_gpu = useGpuForRec;
            RuntimeOption.RecOption.device = deviceForRec;
            ocr = new OCRPredictor(config);
        }

        /// <summary>
        /// 单张图片OCR结果详情
        /// </summary>
        public class OcrResultDetail
        {
            public string ImgPath { get; set; } = string.Empty; // 图片路径
            public List<OCRPredictResult>? Result { get; set; } // 识别结果
            public string? ResultImgPath { get; set; } // 结果图片保存路径
            public long OcrMs { get; set; } // 单张图片OCR耗时
        }

        /// <summary>
        /// 批量执行OCR，支持有限并发处理多张图片，返回每张图片的详细结果和总耗时。
        /// 总耗时不受图片窗口阻塞影响。
        /// </summary>
        /// <param name="maxDegreeOfParallelism">最大并发数，建议2~4，过大易崩溃</param>
        public async Task<(List<OcrResultDetail> details, long totalMs)> RunBatchOcrAsync(int maxDegreeOfParallelism = 2, Action<int, int>? onProgress = null)
        {
            // InitOcr 只用于获取配置参数，不再全局 new OCRPredictor
            InitOcr();
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
                        // 每个任务独立 new OcrConfig 和 OCRPredictor，保证线程安全
                        var config = new OcrConfig();
                        config.set_det_model_path(detModelPath!);
                        config.set_cls_model_path(clsModelPath!);
                        config.set_rec_model_path(recModelPath!);
                        config.set_rec_dict_path(keyPath!);
                        RuntimeOption.ClsOption.batch_num = clsBatchNum;
                        RuntimeOption.RecOption.batch_num = recBatchNum;
                        RuntimeOption.ClsOption.use_gpu = useGpuForCls;
                        RuntimeOption.ClsOption.device = deviceForCls;
                        RuntimeOption.RecOption.use_gpu = useGpuForRec;
                        RuntimeOption.RecOption.device = deviceForRec;
                        var ocrPredictor = new OCRPredictor(config);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var ocrResult = await Task.Run(() => ocrPredictor.ocr(img, true, true, true));
                        sw.Stop();
                        string? resultImgPath = null;
                        Mat? resultImg = null;
                        if (ocrResult != null)
                        {
                            resultImg = PaddleOcrUtility.visualize_bboxes(img, ocrResult);
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
                        // 保存识别文本到 OCR_Result 文件夹
                        if (ocrResult != null)
                        {
                            string directory = Path.GetDirectoryName(imgPath) ?? string.Empty;
                            string ocrResultDir = Path.Combine(directory, "OCR_Result");
                            Directory.CreateDirectory(ocrResultDir);
                            string txtPath = Path.Combine(ocrResultDir, Path.GetFileNameWithoutExtension(imgPath) + ".txt");
                            using (var writer = new StreamWriter(txtPath, false))
                            {
                                foreach (var item in ocrResult)
                                {
                                    writer.WriteLine(item.text);
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
            // 计时结束后再显示图片，保证总推理时间输出不被阻塞
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
    }
}
