using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace JS_Printing_Service.Services.ImageFiltering
{
    /// <summary>
    /// Image filter service - handles brightness, contrast, and other image enhancements
    /// Separated from layout concerns for single responsibility
    /// </summary>
    public interface IImageFilterService
    {
        Task<Image<Rgba32>> BrightenImageAsync(Image<Rgba32> image, float factor = 1.6f);
        Task<Image<Rgba32>> EnhanceContrastAsync(Image<Rgba32> image, float contrast = 1.3f);
        Task<Image<Rgba32>> ConvertToGrayscaleAsync(Image<Rgba32> image);
    }

    public class ImageFilterService : IImageFilterService
    {
        private readonly ILogger<ImageFilterService> _logger;

        public ImageFilterService(ILogger<ImageFilterService> logger)
        {
            _logger = logger;
        }

        public async Task<Image<Rgba32>> BrightenImageAsync(Image<Rgba32> image, float factor = 1.6f)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = image.Clone();
                    result.Mutate(x => x.Brightness(factor));
                    _logger.LogInformation($"[BrightenImage] Applied {factor}x brightness");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[BrightenImage] Error: {ex.Message}");
                    return image.Clone();
                }
            });
        }

        public async Task<Image<Rgba32>> EnhanceContrastAsync(Image<Rgba32> image, float contrast = 1.3f)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = image.Clone();
                    result.Mutate(x => x.Contrast(contrast));
                    _logger.LogInformation($"[EnhanceContrast] Applied {contrast}x contrast");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[EnhanceContrast] Error: {ex.Message}");
                    return image.Clone();
                }
            });
        }

        public async Task<Image<Rgba32>> ConvertToGrayscaleAsync(Image<Rgba32> image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = image.Clone();
                    result.Mutate(x => x.Grayscale());
                    _logger.LogInformation("[ConvertToGrayscale] Applied grayscale conversion");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ConvertToGrayscale] Error: {ex.Message}");
                    return image.Clone();
                }
            });
        }
    }
}
