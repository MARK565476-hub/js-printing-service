namespace JS_Printing_Service.Models
{
    /// <summary>
    /// View model for Aadhaar card upload and processing
    /// </summary>
    public class UploadViewModel
    {
        public List<IFormFile>? ImageFiles { get; set; }
        public string? PrintMode { get; set; } = "RAW"; // RAW, COLOR, or BLACK_WHITE
        public byte[]? ProcessedImageFront { get; set; }
        public byte[]? ProcessedImageBack { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsProcessed { get; set; }
        public string? PersonalInfo { get; set; }
        public string? OutputFormat { get; set; } = "JPG"; // JPG or PDF
    }
}
