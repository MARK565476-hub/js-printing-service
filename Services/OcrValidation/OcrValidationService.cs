using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;

namespace JS_Printing_Service.Services.OcrValidation
{
    /// <summary>
    /// OCR Validation Service Implementation
    /// 
    /// Uses keyword matching and text pattern detection to validate Aadhaar card images
    /// Note: For production, integrate Tesseract OCR or Azure Computer Vision API
    /// </summary>
    public class OcrValidationService : IOcrValidationService
    {
        private readonly ILogger<OcrValidationService> _logger;

        // Keywords for FRONT card detection
        private readonly List<string> _frontKeywords = new()
        {
            "government of india",
            "unique identity",
            "aadhaar",
            "name",
            "date of birth",
            "dob",
            "father"
        };

        // Keywords for BACK card detection
        private readonly List<string> _backKeywords = new()
        {
            "address",
            "state",
            "pin code",
            "postcode",
            "district",
            "signature"
        };

        // Aadhaar number pattern (12 digits, typically grouped as XXXX XXXX XXXX)
        private readonly Regex _aadhaarPattern = new(@"\d{4}[\s\-]?\d{4}[\s\-]?\d{4}", RegexOptions.Compiled);

        public OcrValidationService(ILogger<OcrValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate image through OCR-like text analysis
        /// In production, this would call Tesseract OCR or Azure Computer Vision
        /// </summary>
        public async Task<OcrValidationResult> ValidateImageAsync(Image<Rgba32> image)
        {
            _logger.LogInformation("[OcrValidation] Starting OCR validation...");

            var result = new OcrValidationResult();

            // STEP 1: Extract text (simulated - production would use real OCR)
            result.ExtractedText = await ExtractTextFromImageAsync(image);
            _logger.LogInformation($"[OcrValidation] Extracted text length: {result.ExtractedText.Length} chars");

            // STEP 2: Check for Aadhaar number pattern
            result.HasAadhaarNumber = ContainsAadhaarNumber(result.ExtractedText);
            _logger.LogInformation($"[OcrValidation] Aadhaar number found: {result.HasAadhaarNumber}");

            // STEP 3: Analyze text clarity
            result.TextClarity = AnalyzeTextClarity(result.ExtractedText);
            _logger.LogInformation($"[OcrValidation] Text clarity score: {result.TextClarity}/100");

            // STEP 4: Determine confidence score
            result.ConfidenceScore = CalculateConfidenceScore(result);
            _logger.LogInformation($"[OcrValidation] Overall confidence: {result.ConfidenceScore}/100");

            // STEP 5: Find relevant keywords for front/back detection
            result.KeywordsFound = FindKeywords(result.ExtractedText);
            _logger.LogInformation($"[OcrValidation] Keywords found: {string.Join(", ", result.KeywordsFound)}");

            // STEP 6: Generate warning if quality is low
            if (!result.IsQualityAcceptable)
            {
                result.QualityWarning = "Image quality is low. Text may not be clearly readable.";
                _logger.LogWarning($"[OcrValidation] ⚠️ Quality warning: {result.QualityWarning}");
            }
            else
            {
                _logger.LogInformation("[OcrValidation] ✓ Image quality acceptable");
            }

            return result;
        }

        /// <summary>
        /// Check if Aadhaar number pattern exists in text
        /// </summary>
        public bool ContainsAadhaarNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var match = _aadhaarPattern.Match(text);
            return match.Success;
        }

        /// <summary>
        /// Analyze text clarity based on text density and character distribution
        /// </summary>
        public int AnalyzeTextClarity(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int score = 0;

            // Check text length (good indicator of OCR quality)
            if (text.Length > 100)
                score += 25;
            else if (text.Length > 50)
                score += 15;
            else if (text.Length > 20)
                score += 5;

            // Check for digit presence (important for Aadhaar)
            if (text.Any(char.IsDigit))
                score += 25;

            // Check for letter presence
            if (text.Any(char.IsLetter))
                score += 25;

            // Check for word boundaries (indicates readable text)
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 5)
                score += 25;
            else if (words.Length > 2)
                score += 15;

            return Math.Min(score, 100);
        }

        /// <summary>
        /// Extract text from image (simulated)
        /// Production: Use Tesseract OCR or Azure Computer Vision
        /// </summary>
        private async Task<string> ExtractTextFromImageAsync(Image<Rgba32> image)
        {
            // PLACEHOLDER: Simulated OCR extraction
            // In production, this would integrate:
            // - Tesseract OCR (local)
            // - Azure Computer Vision API (cloud)
            // - Google Vision API (cloud)
            // - AWS Textract (cloud)

            _logger.LogInformation($"[OcrValidation] Simulated OCR extraction from image: {image.Width}x{image.Height}");

            // For now, return simulated extracted text
            // This would be replaced with actual OCR call
            await Task.Delay(50); // Simulate OCR processing time

            return "GOVERNMENT OF INDIA UNIQUE IDENTIFICATION AADHAAR " +
                   "Name: XXXX XXXX Gender: M Date of Birth: XX/XX/XXXX " +
                   "Aadhaar Number: 1234 5678 9012 " +
                   "Address: XXXXX, State: XX Pin Code: 123456";
        }

        /// <summary>
        /// Calculate overall confidence score based on multiple factors
        /// </summary>
        private int CalculateConfidenceScore(OcrValidationResult result)
        {
            int score = 0;

            // Text clarity contributes 50%
            score += (int)(result.TextClarity * 0.50);

            // Aadhaar number presence contributes 30%
            if (result.HasAadhaarNumber)
                score += 30;

            // Text length contributes 20%
            if (result.ExtractedText.Length > 100)
                score += 20;
            else if (result.ExtractedText.Length > 50)
                score += 10;

            return Math.Min(score, 100);
        }

        /// <summary>
        /// Find keywords in text that help identify front vs back card
        /// </summary>
        private List<string> FindKeywords(string text)
        {
            var keywords = new List<string>();

            if (string.IsNullOrEmpty(text))
                return keywords;

            string lowerText = text.ToLower();

            // Check for front keywords
            foreach (var keyword in _frontKeywords)
            {
                if (lowerText.Contains(keyword))
                    keywords.Add(keyword);
            }

            // Check for back keywords
            foreach (var keyword in _backKeywords)
            {
                if (lowerText.Contains(keyword))
                    keywords.Add(keyword);
            }

            return keywords;
        }
    }
}
