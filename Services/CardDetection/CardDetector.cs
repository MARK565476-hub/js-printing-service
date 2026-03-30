using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// Card detection orchestrator - uses strategy pattern to select best detection method
    /// </summary>
    public interface ICardDetector
    {
        Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image);
    }

    public class CardDetector : ICardDetector
    {
        private readonly ICardDetectionStrategy[] _strategies;
        private readonly ILogger<CardDetector> _logger;

        public CardDetector(
            YoloDetectionStrategy yoloStrategy,
            EdgeDetectionStrategy edgeStrategy,
            ILogger<CardDetector> logger)
        {
            _strategies = new ICardDetectionStrategy[] { yoloStrategy, edgeStrategy };
            _logger = logger;
        }

        public async Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image)
        {
            foreach (var strategy in _strategies)
            {
                if (strategy.IsAvailable)
                {
                    _logger.LogInformation($"[CardDetector] Using strategy: {strategy.StrategyName}");
                    return await strategy.DetectCardBoundariesAsync(image);
                }
            }

            _logger.LogError("[CardDetector] No detection strategies available");
            throw new InvalidOperationException("No card detection strategies available");
        }
    }
}
