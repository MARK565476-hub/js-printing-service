using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using JS_Printing_Service.Services.OcrValidation;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// Smart Front/Back Detection Service
    /// 
    /// Determines which image is FRONT (with photo/face) and which is BACK (with QR/address)
    /// 
    /// Detection Strategy:
    /// 1. Use OCR keyword matching
    /// 2. Look for front indicators (face region, "Government of India", name, DOB)
    /// 3. Look for back indicators (QR code, address, pin code)
    /// 4. Return detected order or fallback to input order
    /// </summary>
    public interface IFrontBackDetectionService
    {
        /// <summary>
        /// Detect and sort images into front and back
        /// Returns tuple: (frontImage, backImage, detectionConfidence, detectionMethod)
        /// </summary>
        Task<CardDetectionResult> DetectAndSortAsync(
            Image<Rgba32> image1, 
            Image<Rgba32> image2,
            OcrValidationResult ocr1,
            OcrValidationResult ocr2);
    }

    /// <summary>
    /// Result of front/back detection
    /// </summary>
    public class CardDetectionResult
    {
        /// <summary>
        /// Index of image identified as FRONT (0 or 1)
        /// </summary>
        public int FrontImageIndex { get; set; }

        /// <summary>
        /// Index of image identified as BACK (0 or 1)
        /// </summary>
        public int BackImageIndex { get; set; }

        /// <summary>
        /// Confidence in detection (0-100)
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// Method used for detection
        /// </summary>
        public string DetectionMethod { get; set; } = "keyword_matching";

        /// <summary>
        /// Whether detection was successful (confidence > 50)
        /// </summary>
        public bool IsDetectionSuccessful => ConfidenceScore > 50;

        /// <summary>
        /// Reasons for the detection
        /// </summary>
        public List<string> DetectionReasons { get; set; } = new();
    }
}
