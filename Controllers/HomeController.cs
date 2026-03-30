using JS_Printing_Service.Models;
using JS_Printing_Service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JS_Printing_Service.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPdfExportServiceFinal _pdfExportServiceFinal;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IPdfExportServiceFinal pdfExportServiceFinal,
            ILogger<HomeController> logger)
        {
            _pdfExportServiceFinal = pdfExportServiceFinal;
            _logger = logger;
        }

        /// <summary>
        /// Display upload page
        /// </summary>
        public IActionResult Index()
        {
            return View(new UploadViewModel());
        }

        /// <summary>
        /// Export Aadhaar images using DUAL-MODE AI-ENHANCED pipeline
        /// 
        /// DUAL-MODE INPUT HANDLING:
        /// - 1 image: Split into front and back automatically
        /// - 2 images: Process as separate front and back cards
        /// 
        /// PROCESSING:
        /// - Load → Enhance (AI) → Split (if needed) → Layout (fixed) → Save as JPEG
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> ExportAadhaarPdf(UploadViewModel model)
        {
            try
            {
                if (model == null || model.ImageFiles == null || model.ImageFiles.Count < 1 || model.ImageFiles.Count > 2)
                    return BadRequest(new { error = "Upload 1 image (combined) or 2 images (front and back)" });

                _logger.LogInformation("[ExportAadhaarPdf] Starting Aadhaar export...");

                if (model.ImageFiles.Count == 2)
                {
                    _logger.LogInformation("[ExportAadhaarPdf] Dual image mode detected");
                    _logger.LogInformation("[ExportAadhaarPdf] Image 1 (uploaded first) → LEFT");
                    _logger.LogInformation("[ExportAadhaarPdf] Image 2 (uploaded second) → RIGHT");

                    var result = await _pdfExportServiceFinal.CreateAadhaarPdfAsync(
                        model.ImageFiles[0],  // First image → LEFT
                        model.ImageFiles[1]); // Second image → RIGHT

                    _logger.LogInformation($"[ExportAadhaarPdf] Export complete: {result.Length} bytes (JPEG)");
                    return File(result, "image/jpeg", $"aadhaar_{DateTime.Now:yyyyMMdd-HHmmss}.jpg");
                }
                else
                {
                    _logger.LogInformation("[ExportAadhaarPdf] Single image mode detected - will split");

                    var result = await _pdfExportServiceFinal.CreateAadhaarPdfAsync(
                        model.ImageFiles[0],  // Single combined image to split
                        null);                // No second image

                    _logger.LogInformation($"[ExportAadhaarPdf] Export complete: {result.Length} bytes (JPEG)");
                    return File(result, "image/jpeg", $"aadhaar_{DateTime.Now:yyyyMMdd-HHmmss}.jpg");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExportAadhaarPdf] Error: {ex.Message}");
                return StatusCode(500, new { error = "Export failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Generate preview for Aadhaar images (DUAL-MODE)
        /// Returns instant preview (50% scale) + full-resolution for download
        /// 
        /// DUAL-MODE INPUT HANDLING:
        /// - 1 image: Split into front and back automatically
        /// - 2 images: Process as separate front and back cards
        /// 
        /// PREVIEW OUTPUT:
        /// - Base64 encoded preview (50% scale) for instant display
        /// - Base64 encoded full (100% scale) for download
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> PreviewAadhaarPdf(UploadViewModel model)
        {
            try
            {
                if (model == null || model.ImageFiles == null || model.ImageFiles.Count < 1 || model.ImageFiles.Count > 2)
                    return BadRequest(new { error = "Upload 1 image (combined) or 2 images (front and back)" });

                _logger.LogInformation("[PreviewAadhaarPdf] Generating preview...");

                (byte[] preview, byte[] full) result;

                if (model.ImageFiles.Count == 2)
                {
                    _logger.LogInformation("[PreviewAadhaarPdf] Dual image mode - processing separate images");
                    _logger.LogInformation("[PreviewAadhaarPdf] Image 1 (uploaded first) → LEFT");
                    _logger.LogInformation("[PreviewAadhaarPdf] Image 2 (uploaded second) → RIGHT");

                    result = await _pdfExportServiceFinal.GenerateAadhaarPreviewAndFullAsync(
                        model.ImageFiles[0],  // First image → LEFT
                        model.ImageFiles[1]); // Second image → RIGHT
                }
                else
                {
                    _logger.LogInformation("[PreviewAadhaarPdf] Single image mode - will split");

                    result = await _pdfExportServiceFinal.GenerateAadhaarPreviewAndFullAsync(
                        model.ImageFiles[0],  // Single combined image to split
                        null);                // No second image
                }

                _logger.LogInformation($"[PreviewAadhaarPdf] Preview: {result.preview.Length} bytes | Full: {result.full.Length} bytes");

                // Return preview as base64 for instant display + full for download
                return Ok(new
                {
                    success = true,
                    preview = Convert.ToBase64String(result.preview),
                    full = Convert.ToBase64String(result.full),
                    previewSize = result.preview.Length,
                    fullSize = result.full.Length,
                    mode = model.ImageFiles.Count == 2 ? "DUAL_IMAGE" : "SINGLE_COMBINED",
                    layout = "LEFT: Front card | RIGHT: Back card"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PreviewAadhaarPdf] Error: {ex.Message}");
                return StatusCode(500, new { error = "Preview generation failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Download Aadhaar image as JPEG (DUAL-MODE)
        /// Supports 1 combined image (split automatically) or 2 separate images
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> DownloadAadhaarJpeg(UploadViewModel model)
        {
            try
            {
                _logger.LogInformation("[DownloadAadhaarJpeg] Generating downloadable JPEG...");

                if (model == null || model.ImageFiles == null || model.ImageFiles.Count < 1 || model.ImageFiles.Count > 2)
                    return BadRequest(new { error = "Upload 1 image (combined) or 2 images (front and back)" });

                byte[] result;

                if (model.ImageFiles.Count == 2)
                {
                    _logger.LogInformation("[DownloadAadhaarJpeg] Dual image mode");
                    result = await _pdfExportServiceFinal.CreateAadhaarPdfAsync(
                        model.ImageFiles[0],
                        model.ImageFiles[1]);
                }
                else
                {
                    _logger.LogInformation("[DownloadAadhaarJpeg] Single image mode - will split");
                    result = await _pdfExportServiceFinal.CreateAadhaarPdfAsync(
                        model.ImageFiles[0],
                        null);
                }

                _logger.LogInformation($"[DownloadAadhaarJpeg] JPEG ready: {result.Length} bytes");
                return File(result, "image/jpeg", $"aadhaar_{DateTime.Now:yyyyMMdd-HHmmss}.jpg");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DownloadAadhaarJpeg] Error: {ex.Message}");
                return StatusCode(500, new { error = "JPEG download failed: " + ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
