using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JS_Printing_Service.Services.OcrValidation
{
    /// <summary>
    /// OCR-based validation service for Aadhaar card images
    /// 
    /// Responsibilities:
    /// - Extract text from images using OCR
    /// - Validate Aadhaar number pattern (XXXX XXXX XXXX)
    /// - Check text readability and clarity
    /// - Generate quality warnings for low-confidence OCR
    /// </summary>
    public interface IOcrValidationService
    {
        /// <summary>
        /// Extract text from image and validate readability
        /// Returns extracted text and confidence metrics
        /// </summary>
        Task<OcrValidationResult> ValidateImageAsync(Image<Rgba32> image);

        /// <summary>
        /// Check if text contains Aadhaar number pattern (XXXX XXXX XXXX)
        /// </summary>
        bool ContainsAadhaarNumber(string text);

        /// <summary>
        /// Analyze text density and readability
        /// Returns confidence score (0-100)
        /// </summary>
        int AnalyzeTextClarity(string text);
    }

    /// <summary>
    /// Result of OCR validation on a single image
    /// </summary>
    public class OcrValidationResult
    {
        /// <summary>
        /// Extracted text from image
        /// </summary>
        public string ExtractedText { get; set; } = string.Empty;

        /// <summary>
        /// Overall confidence in OCR results (0-100)
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// Whether Aadhaar number pattern was found
        /// </summary>
        public bool HasAadhaarNumber { get; set; }

        /// <summary>
        /// Text clarity assessment (0-100)
        /// </summary>
        public int TextClarity { get; set; }

        /// <summary>
        /// Whether image quality is acceptable
        /// </summary>
        public bool IsQualityAcceptable => ConfidenceScore >= 70;

        /// <summary>
        /// Warning message if quality is low
        /// </summary>
        public string? QualityWarning { get; set; }

        /// <summary>
        /// Keywords found in text (to help with front/back detection)
        /// </summary>
        public List<string> KeywordsFound { get; set; } = new();
    }
}
