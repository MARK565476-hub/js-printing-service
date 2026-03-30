using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// Edge-based card detection fallback strategy
    /// Uses brightness analysis to find card boundaries
    /// </summary>
    public class EdgeDetectionStrategy : ICardDetectionStrategy
    {
        private readonly ILogger<EdgeDetectionStrategy> _logger;

        public string StrategyName => "EdgeDetection";
        public bool IsAvailable => true;

        public EdgeDetectionStrategy(ILogger<EdgeDetectionStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[EdgeDetectionStrategy] Starting edge detection...");

                    int minX = image.Width, maxX = 0;
                    int minY = image.Height, maxY = 0;
                    bool foundCard = false;

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = image[x, y];
                            int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                            if (brightness < 200)
                            {
                                foundCard = true;
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }
                        }
                    }

                    Rectangle result;

                    if (foundCard && (maxX - minX > 100) && (maxY - minY > 100))
                    {
                        int pad = 10;
                        int x = Math.Max(0, minX - pad);
                        int y = Math.Max(0, minY - pad);
                        int width = Math.Min(image.Width - x, (maxX - minX) + (pad * 2));
                        int height = Math.Min(image.Height - y, (maxY - minY) + (pad * 2));

                        result = new Rectangle(x, y, width, height);
                        _logger.LogInformation($"[EdgeDetectionStrategy] Card found: {result.Width}x{result.Height}");
                    }
                    else
                    {
                        int w = (int)(image.Width * 0.8);
                        int h = (int)(image.Height * 0.8);
                        int x = (image.Width - w) / 2;
                        int y = (image.Height - h) / 2;

                        result = new Rectangle(x, y, w, h);
                        _logger.LogInformation("[EdgeDetectionStrategy] Using center crop fallback");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[EdgeDetectionStrategy] Error: {ex.Message}");

                    int w = (int)(image.Width * 0.8);
                    int h = (int)(image.Height * 0.8);
                    int x = (image.Width - w) / 2;
                    int y = (image.Height - h) / 2;

                    return new Rectangle(x, y, w, h);
                }
            });
        }
    }
}
