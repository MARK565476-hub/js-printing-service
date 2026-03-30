using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// YOLO-based card detection strategy using ONNX Runtime
    /// </summary>
    public class YoloDetectionStrategy : ICardDetectionStrategy
    {
        private object? _session;
        private readonly string _modelPath;
        private bool _modelLoaded = false;
        private readonly ILogger<YoloDetectionStrategy> _logger;
        private Type? _onnxRuntimeType;

        private const int MODEL_INPUT_SIZE = 640;
        private const float CONFIDENCE_THRESHOLD = 0.45f;
        private const float IOU_THRESHOLD = 0.45f;

        public string StrategyName => "YoloDetection";
        public bool IsAvailable => _modelLoaded;

        public YoloDetectionStrategy(ILogger<YoloDetectionStrategy> logger)
        {
            _logger = logger;
            _modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "model.onnx");
            LoadModel();
        }

        private void LoadModel()
        {
            try
            {
                if (!File.Exists(_modelPath))
                {
                    _logger.LogWarning($"[YoloDetectionStrategy] Model not found at: {_modelPath}");
                    return;
                }

                _onnxRuntimeType = Type.GetType("Microsoft.ML.OnnxRuntime.InferenceSession, Microsoft.ML.OnnxRuntime");
                if (_onnxRuntimeType == null)
                {
                    _logger.LogWarning("[YoloDetectionStrategy] ONNX Runtime not installed");
                    return;
                }

                var ctor = _onnxRuntimeType.GetConstructor(new[] { typeof(string) });
                if (ctor != null)
                {
                    _session = ctor.Invoke(new object[] { _modelPath });
                    _modelLoaded = true;
                    _logger.LogInformation("[YoloDetectionStrategy] Model loaded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[YoloDetectionStrategy] Failed to load model: {ex.Message}");
                _modelLoaded = false;
            }
        }

        public async Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                if (!IsAvailable)
                {
                    _logger.LogWarning("[YoloDetectionStrategy] Model not available");
                    return GetFallbackBoundary(image);
                }

                try
                {
                    _logger.LogInformation("[YoloDetectionStrategy] Detecting card boundaries...");
                    // YOLO detection would be implemented here
                    // For now, fall back to edge detection
                    return GetFallbackBoundary(image);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[YoloDetectionStrategy] Error: {ex.Message}");
                    return GetFallbackBoundary(image);
                }
            });
        }

        private Rectangle GetFallbackBoundary(Image<Rgba32> image)
        {
            int w = (int)(image.Width * 0.8);
            int h = (int)(image.Height * 0.8);
            int x = (image.Width - w) / 2;
            int y = (image.Height - h) / 2;

            return new Rectangle(x, y, w, h);
        }
    }
}
