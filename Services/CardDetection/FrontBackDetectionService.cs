using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using JS_Printing_Service.Services.OcrValidation;

namespace JS_Printing_Service.Services.CardDetection
{
    /// <summary>
    /// Front/Back Card Detection Implementation
    /// 
    /// Uses OCR keyword analysis to determine card side
    /// </summary>
    public class FrontBackDetectionService : IFrontBackDetectionService
    {
        private readonly ILogger<FrontBackDetectionService> _logger;

        // Front card indicators
        private readonly List<string> _frontIndicators = new()
        {
            "government of india",
            "unique identity",
            "name",
            "date of birth",
            "dob",
            "father",
            "male",
            "female",
            "gender"
        };

        // Back card indicators
        private readonly List<string> _backIndicators = new()
        {
            "address",
            "state",
            "pin code",
            "postcode",
            "district",
            "signature",
            "issued",
            "valid",
            "till"
        };

        public FrontBackDetectionService(ILogger<FrontBackDetectionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Detect and sort images into front and back using OCR analysis
        /// </summary>
        public async Task<CardDetectionResult> DetectAndSortAsync(
            Image<Rgba32> image1, 
            Image<Rgba32> image2,
            OcrValidationResult ocr1,
            OcrValidationResult ocr2)
        {
            _logger.LogInformation("[FrontBackDetection] Starting detection...");

            var result = new CardDetectionResult
            {
                DetectionMethod = "ocr_keyword_matching"
            };

            // Analyze both images
            var analysis1 = AnalyzeCard(ocr1, image1);
            var analysis2 = AnalyzeCard(ocr2, image2);

            _logger.LogInformation($"[FrontBackDetection] Image 1 score - Front: {analysis1.FrontScore}, Back: {analysis1.BackScore}");
            _logger.LogInformation($"[FrontBackDetection] Image 2 score - Front: {analysis2.FrontScore}, Back: {analysis2.BackScore}");

            // Determine ordering
            if (analysis1.FrontScore > analysis1.BackScore && analysis1.FrontScore > analysis2.FrontScore)
            {
                // Image 1 is FRONT
                result.FrontImageIndex = 0;
                result.BackImageIndex = 1;
                result.ConfidenceScore = analysis1.FrontScore;
                result.DetectionReasons.AddRange(analysis1.FrontReasons);
            }
            else if (analysis2.FrontScore > analysis2.BackScore && analysis2.FrontScore > analysis1.FrontScore)
            {
                // Image 2 is FRONT
                result.FrontImageIndex = 1;
                result.BackImageIndex = 0;
                result.ConfidenceScore = analysis2.FrontScore;
                result.DetectionReasons.AddRange(analysis2.FrontReasons);
            }
            else if (analysis1.BackScore > analysis1.FrontScore && analysis1.BackScore > analysis2.BackScore)
            {
                // Image 1 is BACK, so Image 2 is FRONT
                result.FrontImageIndex = 1;
                result.BackImageIndex = 0;
                result.ConfidenceScore = analysis1.BackScore;
                result.DetectionReasons.AddRange(analysis1.BackReasons);
            }
            else if (analysis2.BackScore > analysis2.FrontScore && analysis2.BackScore > analysis1.BackScore)
            {
                // Image 2 is BACK, so Image 1 is FRONT
                result.FrontImageIndex = 0;
                result.BackImageIndex = 1;
                result.ConfidenceScore = analysis2.BackScore;
                result.DetectionReasons.AddRange(analysis2.BackReasons);
            }
            else
            {
                // Fallback: Keep original order if detection inconclusive
                _logger.LogWarning("[FrontBackDetection] Detection inconclusive. Using original order.");
                result.FrontImageIndex = 0;
                result.BackImageIndex = 1;
                result.ConfidenceScore = 50; // Default confidence
                result.DetectionReasons.Add("Detection inconclusive. Using original order.");
            }

            _logger.LogInformation($"[FrontBackDetection] ✓ Detection complete");
            _logger.LogInformation($"[FrontBackDetection] Front: Image {result.FrontImageIndex + 1}, Back: Image {result.BackImageIndex + 1}");
            _logger.LogInformation($"[FrontBackDetection] Confidence: {result.ConfidenceScore}%");

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Analyze a single card image to score front vs back likelihood
        /// </summary>
        private CardAnalysis AnalyzeCard(OcrValidationResult ocr, Image<Rgba32> image)
        {
            var analysis = new CardAnalysis();

            // Count matching indicators
            var textLower = ocr.ExtractedText.ToLower();

            foreach (var indicator in _frontIndicators)
            {
                if (textLower.Contains(indicator))
                {
                    analysis.FrontScore += 15;
                    analysis.FrontReasons.Add($"Found front indicator: {indicator}");
                }
            }

            foreach (var indicator in _backIndicators)
            {
                if (textLower.Contains(indicator))
                {
                    analysis.BackScore += 15;
                    analysis.BackReasons.Add($"Found back indicator: {indicator}");
                }
            }

            // Check for Aadhaar number (usually on front)
            if (ocr.HasAadhaarNumber)
            {
                analysis.FrontScore += 20;
                analysis.FrontReasons.Add("Aadhaar number detected (typically on front)");
            }

            // Cap scores at 100
            analysis.FrontScore = Math.Min(analysis.FrontScore, 100);
            analysis.BackScore = Math.Min(analysis.BackScore, 100);

            return analysis;
        }

        /// <summary>
        /// Internal class for card analysis scores
        /// </summary>
        private class CardAnalysis
        {
            public int FrontScore { get; set; } = 0;
            public int BackScore { get; set; } = 0;
            public List<string> FrontReasons { get; set; } = new();
            public List<string> BackReasons { get; set; } = new();
        }
    }
}
