using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// Strategy interface for card detection methods
    /// </summary>
    public interface ICardDetectionStrategy
    {
        Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image);
        string StrategyName { get; }
        bool IsAvailable { get; }
    }
}
