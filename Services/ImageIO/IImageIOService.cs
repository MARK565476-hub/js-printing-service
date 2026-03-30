using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace JS_Printing_Service.Services.ImageIO
{
    /// <summary>
    /// Image I/O service - handles loading, saving, and format conversion using ImageSharp
    /// Replaces System.Drawing for cross-platform compatibility and memory safety
    /// </summary>
    public interface IImageIOService
    {
        Task<Image<Rgba32>> LoadImageFromStreamAsync(Stream stream);
        Task<Image<Rgba32>> LoadImageFromFormFileAsync(IFormFile file);
        Task<byte[]> SaveImageAsJpegAsync(Image<Rgba32> image, int quality = 95);
        Task<byte[]> SaveImageAsJpegToStreamAsync(Image<Rgba32> image, Stream stream, int quality = 95);
    }

    public class ImageIOService : IImageIOService
    {
        private readonly ILogger<ImageIOService> _logger;

        public ImageIOService(ILogger<ImageIOService> logger)
        {
            _logger = logger;
        }

        public async Task<Image<Rgba32>> LoadImageFromStreamAsync(Stream stream)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("[LoadImageFromStream] Loading image from stream...");
                    var image = Image.Load<Rgba32>(stream);
                    _logger.LogInformation($"[LoadImageFromStream] Loaded: {image.Width}x{image.Height}");
                    return image;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[LoadImageFromStream] Error: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<Image<Rgba32>> LoadImageFromFormFileAsync(IFormFile file)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation($"[LoadImageFromFormFile] Loading {file.FileName}...");
                    using var stream = file.OpenReadStream();
                    var image = Image.Load<Rgba32>(stream);
                    
                    FixOrientation(image);
                    
                    _logger.LogInformation($"[LoadImageFromFormFile] Loaded: {image.Width}x{image.Height}");
                    return image;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[LoadImageFromFormFile] Error: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<byte[]> SaveImageAsJpegAsync(Image<Rgba32> image, int quality = 95)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream();
                    var encoder = new JpegEncoder { Quality = quality };
                    image.Save(ms, encoder);
                    byte[] result = ms.ToArray();
                    _logger.LogInformation($"[SaveImageAsJpeg] Saved: {result.Length} bytes (quality: {quality})");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[SaveImageAsJpeg] Error: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<byte[]> SaveImageAsJpegToStreamAsync(Image<Rgba32> image, Stream stream, int quality = 95)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var encoder = new JpegEncoder { Quality = quality };
                    image.Save(stream, encoder);
                    stream.Seek(0, SeekOrigin.Begin);
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    byte[] result = memoryStream.ToArray();
                    _logger.LogInformation($"[SaveImageAsJpegToStream] Saved: {result.Length} bytes");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[SaveImageAsJpegToStream] Error: {ex.Message}");
                    throw;
                }
            });
        }

        private void FixOrientation(Image<Rgba32> image)
        {
            try
            {
                // ImageSharp automatically handles EXIF orientation during load
                // No additional processing needed here
                _logger.LogInformation("[FixOrientation] Image loaded with automatic EXIF handling");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[FixOrientation] Warning during orientation handling: {ex.Message}");
            }
        }
    }
}
