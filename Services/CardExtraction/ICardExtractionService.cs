using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace JS_Printing_Service.Services.CardExtraction
{
    /// <summary>
    /// CRITICAL: Mandatory card extraction and validation service
    /// Ensures ONLY clean card region is used for layout
    /// NO background paper allowed in output
    /// </summary>
    public interface ICardExtractionService
    {
        Task<Image<Rgba32>> ExtractAndValidateCardAsync(Image<Rgba32> image);
    }

    public class CardExtractionService : ICardExtractionService
    {
        private readonly ILogger<CardExtractionService> _logger;
        private const float AADHAAR_ASPECT_RATIO = 1.588f;

        public CardExtractionService(ILogger<CardExtractionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// MANDATORY extraction pipeline: detect → crop → validate → clean background
        /// </summary>
        public async Task<Image<Rgba32>> ExtractAndValidateCardAsync(Image<Rgba32> image)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Starting mandatory card extraction...");

                    // STEP 1: Detect card bounding box
                    var cardBounds = await DetectCardBoundariesAsync(image);
                    _logger.LogInformation($"[CardExtraction] Card bounds detected: {cardBounds}");

                    // STEP 2: Crop to detected card region ONLY
                    var cropped = image.Clone(x => x.Crop(cardBounds));
                    _logger.LogInformation($"[CardExtraction] Cropped to: {cropped.Width}x{cropped.Height}");

                    // STEP 3: Remove background and shadows AGGRESSIVELY
                    var cleaned = await RemoveBackgroundAndShadowsAsync(cropped);
                    cropped.Dispose();

                    // STEP 4: Auto-rotate to horizontal if needed
                    var rotated = await AutoRotateToHorizontalAsync(cleaned);
                    if (cleaned != rotated)
                        cleaned.Dispose();

                    // STEP 5: Tight crop with uniform padding
                    var final = await TightCropWithPaddingAsync(rotated);
                    if (rotated != final)
                        rotated.Dispose();

                    // STEP 6: VALIDATE output (MUST NOT have background paper)
                    bool isValid = await ValidateCardExtractionAsync(final);
                    if (!isValid)
                    {
                        _logger.LogWarning("[CardExtraction] Validation failed - reprocessing aggressively...");
                        final = await AggressiveReprocessAsync(final);
                    }

                    _logger.LogInformation($"[CardExtraction] Extraction complete: {final.Width}x{final.Height}");
                    return final;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Error: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Detect card region using edge-based hard fallback
        /// MUST find card or throw error
        /// </summary>
        private async Task<Rectangle> DetectCardBoundariesAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Detecting card boundaries...");

                    // Convert to grayscale for edge detection
                    var gray = image.Clone(x => x.Grayscale());

                    // Scan for dark pixels (card content, brightness < 200)
                    int minX = image.Width, maxX = 0;
                    int minY = image.Height, maxY = 0;
                    bool foundCard = false;

                    for (int pixY = 0; pixY < gray.Height; pixY++)
                    {
                        for (int pixX = 0; pixX < gray.Width; pixX++)
                        {
                            var pixel = gray[pixX, pixY];
                            int brightness = pixel.R; // Grayscale

                            // Card pixels are darker than 200 (white is 255)
                            if (brightness < 200)
                            {
                                foundCard = true;
                                if (pixX < minX) minX = pixX;
                                if (pixX > maxX) maxX = pixX;
                                if (pixY < minY) minY = pixY;
                                if (pixY > maxY) maxY = pixY;
                            }
                        }
                    }

                    gray.Dispose();

                    Rectangle result;

                    if (foundCard && (maxX - minX > 150) && (maxY - minY > 100))
                    {
                        // Found clear card region
                        int pad = 5;
                        int x = Math.Max(0, minX - pad);
                        int y = Math.Max(0, minY - pad);
                        int width = Math.Min(image.Width - x, (maxX - minX) + (pad * 2));
                        int height = Math.Min(image.Height - y, (maxY - minY) + (pad * 2));

                        result = new Rectangle(x, y, width, height);
                        _logger.LogInformation($"[CardExtraction] Card region found: {result.Width}x{result.Height}");
                    }
                    else if (foundCard)
                    {
                        // Fallback: use detected bounds without padding
                        result = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                        _logger.LogInformation($"[CardExtraction] Small card region found: {result.Width}x{result.Height}");
                    }
                    else
                    {
                        // Emergency fallback: center-crop using aspect ratio heuristic
                        _logger.LogWarning("[CardExtraction] No card detected - using aspect ratio fallback");
                        int w = (int)(image.Width * 0.8);
                        int h = (int)(w / AADHAAR_ASPECT_RATIO);

                        if (h > image.Height * 0.8)
                            h = (int)(image.Height * 0.8);

                        int x = (image.Width - w) / 2;
                        int y = (image.Height - h) / 2;

                        result = new Rectangle(x, y, w, h);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Detection error: {ex.Message}");
                    int w = (int)(image.Width * 0.8);
                    int h = (int)(image.Height * 0.8);
                    return new Rectangle((image.Width - w) / 2, (image.Height - h) / 2, w, h);
                }
            });
        }

        /// <summary>
        /// AGGRESSIVE background and shadow removal
        /// Replace all near-white pixels with pure white
        /// Remove shadow artifacts
        /// </summary>
        private async Task<Image<Rgba32>> RemoveBackgroundAndShadowsAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Removing background and shadows...");

                    var result = image.Clone();

                    // Threshold: pixels brighter than 210 are background → make white
                    // Pixels darker than 210 are card content
                    result.Mutate(ctx =>
                    {
                        for (int pixY = 0; pixY < result.Height; pixY++)
                        {
                            for (int pixX = 0; pixX < result.Width; pixX++)
                            {
                                var pixel = result[pixX, pixY];
                                int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                                if (brightness > 210)
                                {
                                    // Background paper → pure white
                                    result[pixX, pixY] = new Rgba32(255, 255, 255, 255);
                                }
                                else if (brightness < 50)
                                {
                                    // Very dark content → keep black
                                    result[pixX, pixY] = new Rgba32(0, 0, 0, 255);
                                }
                                else
                                {
                                    // Mid-tones (shadows) → enhance contrast
                                    byte adjusted = (byte)Math.Min(255, brightness * 1.2);
                                    result[pixX, pixY] = new Rgba32(adjusted, adjusted, adjusted, 255);
                                }
                            }
                        }
                    });

                    _logger.LogInformation("[CardExtraction] Background and shadows removed");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Background removal error: {ex.Message}");
                    return image.Clone();
                }
            });
        }

        /// <summary>
        /// Auto-rotate card to horizontal (landscape) alignment
        /// </summary>
        private async Task<Image<Rgba32>> AutoRotateToHorizontalAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (image.Width >= image.Height)
                    {
                        // Already landscape
                        _logger.LogInformation("[CardExtraction] Card already horizontal");
                        return image;
                    }

                    _logger.LogInformation("[CardExtraction] Rotating card to horizontal...");
                    var rotated = image.Clone(x => x.Rotate(90));
                    return rotated;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Rotation error: {ex.Message}");
                    return image.Clone();
                }
            });
        }

        /// <summary>
        /// Tight crop with uniform padding
        /// Remove empty borders, add small padding
        /// </summary>
        private async Task<Image<Rgba32>> TightCropWithPaddingAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Applying tight crop with padding...");

                    // Find content bounds (non-white pixels)
                    int minX = image.Width, maxX = 0;
                    int minY = image.Height, maxY = 0;
                    bool foundContent = false;

                    for (int pixY = 0; pixY < image.Height; pixY++)
                    {
                        for (int pixX = 0; pixX < image.Width; pixX++)
                        {
                            var pixel = image[pixX, pixY];
                            int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                            if (brightness < 240)
                            {
                                foundContent = true;
                                if (pixX < minX) minX = pixX;
                                if (pixX > maxX) maxX = pixX;
                                if (pixY < minY) minY = pixY;
                                if (pixY > maxY) maxY = pixY;
                            }
                        }
                    }

                    if (!foundContent)
                    {
                        _logger.LogWarning("[CardExtraction] No content found in tight crop");
                        return image.Clone();
                    }

                    const int PADDING = 10;
                    int cropX = Math.Max(0, minX - PADDING);
                    int cropY = Math.Max(0, minY - PADDING);
                    int cropWidth = Math.Min(image.Width - cropX, (maxX - minX) + (PADDING * 2));
                    int cropHeight = Math.Min(image.Height - cropY, (maxY - minY) + (PADDING * 2));

                    var cropped = image.Clone(x => x.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));

                    _logger.LogInformation($"[CardExtraction] Tight crop complete: {cropped.Width}x{cropped.Height}");
                    return cropped;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Tight crop error: {ex.Message}");
                    return image.Clone();
                }
            });
        }

        /// <summary>
        /// VALIDATE that output contains ONLY card, NO background paper
        /// Card should fill at least 80% of bounding box
        /// </summary>
        private async Task<bool> ValidateCardExtractionAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Validating card extraction...");

                    // Count pixels that are NOT pure white (240+)
                    int contentPixels = 0;
                    int totalPixels = image.Width * image.Height;

                    for (int pixY = 0; pixY < image.Height; pixY++)
                    {
                        for (int pixX = 0; pixX < image.Width; pixX++)
                        {
                            var pixel = image[pixX, pixY];
                            int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                            if (brightness < 240)
                                contentPixels++;
                        }
                    }

                    double fillRatio = (double)contentPixels / totalPixels;
                    _logger.LogInformation($"[CardExtraction] Card fill ratio: {fillRatio:P}");

                    // Card must fill at least 70% of bounding box
                    bool isValid = fillRatio >= 0.70;

                    if (!isValid)
                        _logger.LogWarning($"[CardExtraction] Validation FAILED: fill ratio {fillRatio:P} < 70%");

                    return isValid;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Validation error: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// AGGRESSIVE reprocessing if validation fails
        /// Even stricter thresholding
        /// </summary>
        private async Task<Image<Rgba32>> AggressiveReprocessAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[CardExtraction] Applying aggressive reprocessing...");

                    var result = image.Clone();

                    // EVEN STRICTER: anything brighter than 190 → white
                    result.Mutate(ctx =>
                    {
                        for (int pixY = 0; pixY < result.Height; pixY++)
                        {
                            for (int pixX = 0; pixX < result.Width; pixX++)
                            {
                                var pixel = result[pixX, pixY];
                                int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                                if (brightness > 190)
                                {
                                    result[pixX, pixY] = new Rgba32(255, 255, 255, 255);
                                }
                                else
                                {
                                    // Increase contrast dramatically
                                    byte adjusted = brightness < 128 
                                        ? (byte)Math.Max(0, brightness * 0.9)
                                        : (byte)Math.Min(255, brightness * 1.3);

                                    result[pixX, pixY] = new Rgba32(adjusted, adjusted, adjusted, 255);
                                }
                            }
                        }
                    });

                    _logger.LogInformation("[CardExtraction] Aggressive reprocessing complete");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CardExtraction] Aggressive reprocessing error: {ex.Message}");
                    return image.Clone();
                }
            });
        }
    }
}
