using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace JS_Printing_Service.Services.AdvancedLayout
{
    /// <summary>
    /// DETERMINISTIC ONLY - Single Meta System Override
    /// NO conditions, NO branching, NO adaptive logic
    /// Everything hardcoded to constants
    /// </summary>
    public interface ISimpleDeterministicCompositionEngine
    {
        /// <summary>
        /// Compose Aadhaar images side-by-side on A4 canvas
        /// left: First image (always on LEFT side)
        /// right: Second image (always on RIGHT side)
        /// </summary>
        Task<Image<Rgba32>> ComposeAadhaarSideBySideAsync(Image<Rgba32> left, Image<Rgba32> right);
    }

    public class SimpleDeterministicCompositionEngine : ISimpleDeterministicCompositionEngine
    {
        private readonly ILogger<SimpleDeterministicCompositionEngine> _logger;

        // HARDCODED CONSTANTS ONLY
        private const int A4_WIDTH = 2480;
        private const int A4_HEIGHT = 3508;
        private const int FIXED_WIDTH = 900;
        private const int FIXED_HEIGHT = 570;
        private const int TOP_MARGIN = 120;
        private const int GAP = 80;
        private const int LEFT_X = 300;   // First image goes to LEFT at X=300
        private const int RIGHT_X = 1280; // Second image goes to RIGHT at X=1280 (300 + 900 + 80)
        private const int IMAGE_Y = 120;  // Both images at Y=120 (top margin)

        public SimpleDeterministicCompositionEngine(ILogger<SimpleDeterministicCompositionEngine> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// DETERMINISTIC ONLY
        /// - Canvas: 2480x3508 white (A4, 300 DPI)
        /// - Left Image: 900x570 at (300, 120) - FIRST UPLOADED IMAGE
        /// - Right Image: 900x570 at (1280, 120) - SECOND UPLOADED IMAGE
        /// NO exceptions, NO branching
        /// </summary>
        public async Task<Image<Rgba32>> ComposeAadhaarSideBySideAsync(Image<Rgba32> left, Image<Rgba32> right)
        {
            return await Task.Run(() =>
            {
                _logger.LogInformation("[SimpleDeterministic] DETERMINISTIC side-by-side layout");
                _logger.LogInformation($"[SimpleDeterministic] LEFT image (1st upload): {left.Width}x{left.Height}");
                _logger.LogInformation($"[SimpleDeterministic] RIGHT image (2nd upload): {right.Width}x{right.Height}");

                // Step 1: Create white A4 canvas
                var canvas = new Image<Rgba32>(A4_WIDTH, A4_HEIGHT, Color.White);
                _logger.LogInformation("[SimpleDeterministic] ✓ A4 Canvas created (2480×3508)");

                // Step 2: Resize LEFT image to fixed size (center crop if needed)
                var leftResized = ResizeToFixedSize(left);
                _logger.LogInformation($"[SimpleDeterministic] ✓ LEFT image resized to {FIXED_WIDTH}×{FIXED_HEIGHT}");

                // Step 3: Resize RIGHT image to fixed size (center crop if needed)
                var rightResized = ResizeToFixedSize(right);
                _logger.LogInformation($"[SimpleDeterministic] ✓ RIGHT image resized to {FIXED_WIDTH}×{FIXED_HEIGHT}");

                // Step 4: Place at hardcoded positions
                canvas.Mutate(ctx =>
                {
                    // Place LEFT image (first uploaded) at X=300
                    ctx.DrawImage(leftResized, new Point(LEFT_X, IMAGE_Y), opacity: 1.0f);
                    _logger.LogInformation($"[SimpleDeterministic] → Image 1 (LEFT) placed at ({LEFT_X}, {IMAGE_Y})");

                    // Place RIGHT image (second uploaded) at X=1280
                    ctx.DrawImage(rightResized, new Point(RIGHT_X, IMAGE_Y), opacity: 1.0f);
                    _logger.LogInformation($"[SimpleDeterministic] → Image 2 (RIGHT) placed at ({RIGHT_X}, {IMAGE_Y})");
                });

                leftResized.Dispose();
                rightResized.Dispose();

                _logger.LogInformation("[SimpleDeterministic] ✓ Layout complete: Perfect side-by-side alignment");
                return canvas;
            });
        }

        /// <summary>
        /// DETERMINISTIC RESIZE - no conditions
        /// 1. Calculate crop to maintain aspect ratio
        /// 2. Crop center
        /// 3. Resize to exactly 900x570
        /// </summary>
        private Image<Rgba32> ResizeToFixedSize(Image<Rgba32> image)
        {
            float targetRatio = (float)FIXED_WIDTH / FIXED_HEIGHT;
            float sourceRatio = (float)image.Width / image.Height;

            Rectangle cropRect;

            // Crop to match target aspect ratio
            if (sourceRatio > targetRatio)
            {
                // Source wider - crop sides
                int newWidth = (int)(image.Height * targetRatio);
                int cropX = (image.Width - newWidth) / 2;
                cropRect = new Rectangle(cropX, 0, newWidth, image.Height);
            }
            else
            {
                // Source taller - crop top/bottom
                int newHeight = (int)(image.Width / targetRatio);
                int cropY = (image.Height - newHeight) / 2;
                cropRect = new Rectangle(0, cropY, image.Width, newHeight);
            }

            // Crop
            var cropped = image.Clone(x => x.Crop(cropRect));

            // Resize to FIXED size
            var resized = cropped.Clone(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(FIXED_WIDTH, FIXED_HEIGHT),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.Bicubic
                }
            ));

            cropped.Dispose();
            return resized;
        }
    }
}
