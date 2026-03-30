using Microsoft.AspNetCore.Http.Features;
using QuestPDF.Infrastructure;
using JS_Printing_Service.Services; // adjust namespace if different

var builder = WebApplication.CreateBuilder(args);


// ========================================
// ☁️ CLOUD DEPLOYMENT CONFIGURATION
// ========================================

// Dynamic Port Binding - CRITICAL for cloud platforms
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// ========================================
// 🔧 SERVICES CONFIGURATION
// ========================================

// MVC
builder.Services.AddControllersWithViews();

// File upload limit (100MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
});

// ========================================
// 🔹 MINIMAL DETERMINISTIC PIPELINE (AI-ENHANCED)
// ========================================
// 4-STEP PIPELINE: Load → Enhance → Layout → Save
// No cropping. AI-based enhancement only. Fixed sizes/positions.

// Image I/O Service - Load and save images (ImageSharp)
builder.Services.AddScoped<JS_Printing_Service.Services.ImageIO.IImageIOService, JS_Printing_Service.Services.ImageIO.ImageIOService>();

// Image Enhancement Service - AI-based clarity, QR enhancement, noise reduction
builder.Services.AddScoped<JS_Printing_Service.Services.ImageEnhancement.IImageEnhancementService, JS_Printing_Service.Services.ImageEnhancement.ImageEnhancementService>();

// Simple Deterministic Composition - Hardcoded positions and sizes only
builder.Services.AddScoped<JS_Printing_Service.Services.AdvancedLayout.ISimpleDeterministicCompositionEngine, JS_Printing_Service.Services.AdvancedLayout.SimpleDeterministicCompositionEngine>();

// OCR Validation Service - Text extraction and Aadhaar validation
builder.Services.AddScoped<JS_Printing_Service.Services.OcrValidation.IOcrValidationService, JS_Printing_Service.Services.OcrValidation.OcrValidationService>();

// Front/Back Detection Service - Smart card side detection
builder.Services.AddScoped<JS_Printing_Service.Services.CardDetection.IFrontBackDetectionService, JS_Printing_Service.Services.CardDetection.FrontBackDetectionService>();

// PDF Export Service Final - 4-step pipeline coordinator with preview support
builder.Services.AddScoped<IPdfExportServiceFinal, PdfExportServiceFinal>();

// QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;


// ========================================
// 🚀 BUILD APP
// ========================================

var app = builder.Build();


// ========================================
// ⚙️ MIDDLEWARE PIPELINE
// ========================================

// Development error page
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ❌ DISABLED HTTPS (VERY IMPORTANT FOR YOU)
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


// ========================================
// 🌐 ROUTING
// ========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ========================================
// ▶️ RUN APP
// ========================================

app.Run();