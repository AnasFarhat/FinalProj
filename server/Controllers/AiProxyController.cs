using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// אובייקט בקשה ליצירת הודעת Push חכמה עבור טיול.
    /// מכיל את פרטי הטיול ואת מטרת ההודעה המבוקשת.
    /// </summary>
    public record SmartPushRequest(
        string? TripTitle, string? Category, string? TripDate,
        string? Location, string? Difficulty, string? About,
        string GoalPrompt
    );

    /// <summary>
    /// אובייקט תגובה המכיל את כותרת ותוכן הודעת ה-Push שנוצרה על ידי ה-AI.
    /// </summary>
    public record SmartPushResponse(string Title, string Message);

    /// <summary>
    /// מייצג דיווח בודד על תוכן שפורסם בקהילה.
    /// </summary>
    public record ReportDetailItem(string ReporterName, string ReasonCategory, string? CustomReason);

    /// <summary>
    /// אובייקט בקשה ליצירת סיכום אוטומטי של דיווחים על פוסט.
    /// </summary>
    public record ReportSummaryRequest(
        string PostContent, string PostAuthor,
        int TotalReports, List<ReportDetailItem> ReportDetails
    );

    /// <summary>
    /// אובייקט תגובה המכיל את סיכום הדיווחים שנוצר על ידי ה-AI.
    /// </summary>
    public record ReportSummaryResponse(string Summary);

    /// <summary>
    /// אובייקט בקשה לבדיקת רמת רעילות של טקסט.
    /// </summary>
    public record ToxicityRequest(string Text);

    /// <summary>
    /// אובייקט תגובה המכיל את ציון הרעילות והסיווג של הטקסט.
    /// </summary>
    public record ToxicityResponse(double Score, string Label);

    /// <summary>
    /// בקר AI המשמש כמתווך בין מערכת הניהול לבין שירותי בינה מלאכותית חיצוניים.
    /// הבקר אחראי על יצירת הודעות Push, ניתוח תוכן, סיכום דיווחים
    /// ובדיקת תקינות החיבור לשירותי ה-AI.
    /// הגישה לבקר מוגבלת למנהלי מערכת בלבד.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AiProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AiProxyController> _logger;

        /// <summary>
        /// רשימת מודלי Gemini הזמינים לשימוש.
        /// במקרה של כשל באחד המודלים, המערכת מנסה אוטומטית
        /// להשתמש במודל הבא ברשימה.
        /// </summary>
        private static readonly string[] GeminiModels = new[]
        {
            "gemini-2.5-flash",
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite",
            "gemini-2.5-flash-lite",
            "gemini-3-flash-preview",
        };

        /// <summary>
        /// אתחול הבקר באמצעות HttpClientFactory,
        /// הגדרות המערכת ומנגנון הלוגים.
        /// </summary>
        public AiProxyController(
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<AiProxyController> logger)
        {
            _httpFactory = httpFactory;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// בודק את תקינות הגדרות שירותי ה-AI ואת זמינות מודלי Gemini.
        /// הפעולה מחזירה את מצב מפתחות ה-API, רשימת המודלים הזמינים
        /// ותוצאות בדיקת החיבור לכל מודל.
        /// </summary>
        /// <returns>
        /// מידע על תקינות שירותי ה-AI והמודל הזמין לשימוש.
        /// </returns>
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var geminiKey = _config["AI:GeminiApiKey"];
            var hfToken = _config["AI:HuggingFaceToken"];

            var configStatus = new
            {
                GeminiKeyConfigured = !string.IsNullOrWhiteSpace(geminiKey),
                GeminiKeyPrefix = string.IsNullOrWhiteSpace(geminiKey) ? "MISSING" : geminiKey[..Math.Min(8, geminiKey.Length)] + "...",
                HuggingFaceConfigured = !string.IsNullOrWhiteSpace(hfToken),
                HuggingFaceTokenPrefix = string.IsNullOrWhiteSpace(hfToken) ? "MISSING" : hfToken[..Math.Min(5, hfToken.Length)] + "...",
            };

   
            string availableModels;
            try
            {
                var client = _httpFactory.CreateClient("gemini");
                var listRes = await client.GetAsync(
                    "https://generativelanguage.googleapis.com/v1beta/models?key=" + geminiKey);
                var listRaw = await listRes.Content.ReadAsStringAsync();
                using var listDoc = JsonDocument.Parse(listRaw);
                var names = listDoc.RootElement
                    .GetProperty("models")
                    .EnumerateArray()
                    .Select(m => m.GetProperty("name").GetString())
                    .Where(n => n != null && n.Contains("gemini"))
                    .ToList();
                availableModels = string.Join(", ", names!);
            }
            catch (Exception ex)
            {
                availableModels = "Could not list: " + ex.Message;
            }


            var modelTests = new List<object>();
            var client2 = _httpFactory.CreateClient("gemini");
            foreach (var model in GeminiModels)
            {
                var url = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent?key=" + geminiKey;
                var testBody = JsonSerializer.Serialize(new
                {
                    contents = new[] { new { parts = new[] { new { text = "Hi" } } } },
                    generationConfig = new { maxOutputTokens = 5 }
                });
                try
                {
                    var res = await client2.PostAsync(
                        url, new StringContent(testBody, Encoding.UTF8, "application/json"));

                    if (res.IsSuccessStatusCode)
                    {
                        modelTests.Add(new { Model = model, Status = "✅ OK" });
                    }
                    else
                    {
                        var errRaw = await res.Content.ReadAsStringAsync();
                        var errCode = (int)res.StatusCode;
                        var reason = errCode == 429 ? "⚠️ QUOTA EXHAUSTED" :
                                      errCode == 404 ? "❌ NOT FOUND" :
                                      errCode == 503 ? "⏳ LOADING" : "❌ ERROR " + errCode;
                        modelTests.Add(new { Model = model, Status = reason });
                    }
                }
                catch (Exception ex)
                {
                    modelTests.Add(new { Model = model, Status = "❌ Exception: " + ex.Message });
                }
            }

 
            string chainTest;
            string? modelUsed = null;
            try
            {
                (chainTest, modelUsed) = await CallGeminiWithFallback("Say hello in Hebrew in 3 words.", forceJson: false);
            }
            catch (Exception ex)
            {
                chainTest = "ALL MODELS FAILED: " + ex.Message;
            }

            return Ok(new
            {
                Config = configStatus,
                AvailableModels = availableModels,
                FallbackChain = modelTests,
                ChainTestResult = chainTest,
                ModelUsed = modelUsed ?? "none"
            });
        }
        /// <summary>
        /// יוצר הודעת Push חכמה באמצעות Gemini AI.
        /// ההודעה נוצרת בהתאם לפרטי הטיול ולמטרה שהוגדרה על ידי מנהל המערכת.
        /// </summary>
        /// <param name="req">
        /// אובייקט המכיל את פרטי הטיול ואת מטרת יצירת ההודעה.
        /// </param>
        /// <returns>
        /// הודעת Push הכוללת כותרת ותוכן שנוצרו על ידי מודל הבינה המלאכותית.
        /// </returns>
        [HttpPost("push/generate")]
        public async Task<IActionResult> GeneratePushNotification([FromBody] SmartPushRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.GoalPrompt))
                return BadRequest("GoalPrompt is required.");

            var tripContext = BuildTripContext(req);

            var prompt =
                "You are a push notification copywriter for a nature hiking app whose users are Hebrew-speaking Israelis.\n" +
                "Write short, punchy, warm push notifications.\n" +
                "Respond with ONLY a JSON object in this exact shape: {\"title\": \"...\", \"message\": \"...\"}\n" +
                "Rules:\n" +
                "- title: max 50 characters, in Hebrew\n" +
                "- message: max 120 characters, in Hebrew\n" +
                "- Tone: friendly, energetic, community-feel\n" +
                "- Use 1-2 relevant emojis naturally\n\n" +
                "Trip details:\n" + tripContext + "\n\n" +
                "Goal: " + req.GoalPrompt;

            try
            {
                var (raw, usedModel) = await CallGeminiWithFallback(prompt, forceJson: true);
                _logger.LogInformation("[Push] Model used: {Model} | Raw: {Raw}", usedModel, raw);

                var clean = ExtractJson(raw);
                _logger.LogInformation("[Push] After ExtractJson: {Clean}", clean);

                var parsed = JsonSerializer.Deserialize<SmartPushResponse>(
                    clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (parsed?.Title == null || parsed?.Message == null)
                {
                    _logger.LogError("[Push] Bad shape. Clean was: {Clean}", clean);
                    return StatusCode(502, "AI returned an unexpected response shape.");
                }

                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Push] Exception");
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        /// <summary>
        /// יוצר סיכום אוטומטי של דיווחים על פוסט באמצעות Gemini AI.
        /// הסיכום מרכז את סיבת הדיווחים ואת רמת החומרה
        /// כדי לסייע למנהל המערכת בקבלת החלטות מהירה.
        /// </summary>
        /// <param name="req">
        /// אובייקט המכיל את תוכן הפוסט, פרטי המחבר
        /// ורשימת הדיווחים שהתקבלו.
        /// </param>
        /// <returns>
        /// אובייקט המכיל סיכום קצר של הדיווחים שנוצר על ידי ה-AI.
        /// </returns>

        [HttpPost("reports/summarize")]
        public async Task<IActionResult> SummarizeReport([FromBody] ReportSummaryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.PostContent))
                return BadRequest("PostContent is required.");

            var preview = req.PostContent.Length > 20
                ? req.PostContent[..20]
                : req.PostContent;

            var reasonLines = (req.ReportDetails ?? new List<ReportDetailItem>()).Select((d, i) =>
            {
                var parts = new List<string> { (i + 1) + ". קטגוריה: " + d.ReasonCategory };
                if (!string.IsNullOrWhiteSpace(d.CustomReason))
                    parts.Add("סיבה: \"" + d.CustomReason + "\"");
                if (!string.IsNullOrWhiteSpace(d.ReporterName))
                    parts.Add("מדווח: " + d.ReporterName);
                return string.Join(" | ", parts);
            });

            var prompt =
                "You are a content moderation assistant for a Hebrew-speaking hiking community app.\n" +
                "Produce ONE concise Hebrew sentence (max 100 characters) summarizing:\n" +
                "1. The nature of the complaints\n" +
                "2. The severity for the admin\n" +
                "Output the sentence only — no quotes, no JSON, no explanation.\n\n" +
                "Post: \"" + req.PostContent + "\"\n" +
                "Author: " + req.PostAuthor + "\n" +
                "Reports (" + req.TotalReports + " total):\n" +
                string.Join("\n", reasonLines);

            try
            {
                var (summary, usedModel) = await CallGeminiWithFallback(prompt, forceJson: false);
                _logger.LogInformation("[Summary] Model: {Model} | Raw: {Raw}", usedModel, summary);
                return Ok(new ReportSummaryResponse(summary.Trim()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Summary] Exception for post '{Preview}'", preview);
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        /// <summary>
        /// מבצע ניתוח רעילות לטקסט באמצעות מודל HuggingFace.
        /// הפעולה מחשבת את ציון הרעילות ומחזירה את רמת הסיכון
        /// בהתאם לתוכן שהתקבל מהמשתמש.
        /// </summary>
        /// <param name="req">
        /// אובייקט המכיל את הטקסט המיועד לניתוח.
        /// </param>
        /// <returns>
        /// אובייקט המכיל את ציון הרעילות ואת רמת הסיווג
        /// (Low, Medium או High).
        /// </returns>
        [HttpPost("toxicity/score")]
        public async Task<IActionResult> ScoreToxicity([FromBody] ToxicityRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return Ok(new ToxicityResponse(0, "low"));

            var prompt =
                "You are a content moderation system for a Hebrew hiking community app.\n" +
                "Rate the toxicity of this text on a scale from 0.0 to 1.0.\n" +
                "Consider: hate speech, harassment, violence, inappropriate content.\n" +
                "Respond with ONLY a JSON object: {\"score\": 0.0, \"label\": \"low\"}\n" +
                "Labels: low (0-0.4), medium (0.4-0.75), high (0.75-1.0)\n\n" +
                "Text to analyze: \"" + req.Text + "\"";

            try
            {
                var (raw, usedModel) = await CallGeminiWithFallback(prompt, forceJson: true);
                _logger.LogInformation("[Toxicity] Model: {Model} | Raw: {Raw}", usedModel, raw);

                var clean = ExtractJson(raw);
                using var doc = JsonDocument.Parse(clean);
                var root = doc.RootElement;

                double score = root.TryGetProperty("score", out var s) ? s.GetDouble() : 0;
                var label = score >= 0.75 ? "high" : score >= 0.4 ? "medium" : "low";

                return Ok(new ToxicityResponse(score, label));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Toxicity] Exception");
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────


        private async Task<(string Text, string Model)> CallGeminiWithFallback(string prompt, bool forceJson)
        {
            var key = _config["AI:GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("GeminiApiKey is not configured. Check appsettings.json key AI:GeminiApiKey.");

            var client = _httpFactory.CreateClient("gemini");
            var errors = new List<string>();

            foreach (var model in GeminiModels)
            {
                var url = "https://generativelanguage.googleapis.com/v1beta/models/"
                          + model + ":generateContent?key=" + key;

                string payloadJson;
                if (forceJson)
                {
                    payloadJson = JsonSerializer.Serialize(new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 512,
                            responseMimeType = "application/json"
                        }
                    });
                }
                else
                {
                    payloadJson = JsonSerializer.Serialize(new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 256
                        }
                    });
                }

                try
                {
                    var response = await client.PostAsync(
                        url, new StringContent(payloadJson, Encoding.UTF8, "application/json"));

             
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var errBody = await response.Content.ReadAsStringAsync();
                        var reason = (int)response.StatusCode == 429 ? "quota exhausted" :
                                      (int)response.StatusCode == 503 ? "unavailable" : "not found";
                        _logger.LogWarning("[Gemini] {Model} skipped ({Reason})", model, reason);
                        errors.Add(model + ": " + reason);
                        continue; 
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("[Gemini] {Model} failed {Code}: {Body}", model, response.StatusCode, errBody);
                        errors.Add(model + ": HTTP " + (int)response.StatusCode);
                        continue;
                    }

                    var raw = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                 
                    if (root.TryGetProperty("promptFeedback", out var feedback) &&
                        feedback.TryGetProperty("blockReason", out var blockReason))
                    {
                        _logger.LogWarning("[Gemini] {Model} blocked: {Reason}", model, blockReason.GetString());
                        errors.Add(model + ": blocked (" + blockReason.GetString() + ")");
                        continue;
                    }

                    var candidate = root.GetProperty("candidates")[0];

          
                    if (candidate.TryGetProperty("finishReason", out var finishReason) &&
                        finishReason.GetString() != "STOP")
                    {
                        _logger.LogWarning("[Gemini] {Model} finishReason: {Reason}", model, finishReason.GetString());
                        errors.Add(model + ": finishReason=" + finishReason.GetString());
                        continue;
                    }

                    var text = candidate
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? string.Empty;

                    _logger.LogInformation("[Gemini] {Model} succeeded.", model);
                    return (text, model);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Gemini] {Model} threw exception", model);
                    errors.Add(model + ": " + ex.Message);
           
                }
            }

            throw new InvalidOperationException(
                "All Gemini models failed. Details: " + string.Join(" | ", errors));
        }

        private static string ExtractJson(string raw)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("{")) return trimmed;

            var fenceMatch = Regex.Match(raw, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
            if (fenceMatch.Success) return fenceMatch.Groups[1].Value.Trim();

            var braceMatch = Regex.Match(raw, @"\{.*\}", RegexOptions.Singleline);
            if (braceMatch.Success) return braceMatch.Value.Trim();

            return trimmed;
        }

        private static string BuildTripContext(SmartPushRequest req)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.TripTitle)) lines.Add("Title: " + req.TripTitle);
            if (!string.IsNullOrWhiteSpace(req.Category)) lines.Add("Category: " + req.Category);
            if (!string.IsNullOrWhiteSpace(req.TripDate)) lines.Add("Date: " + req.TripDate);
            if (!string.IsNullOrWhiteSpace(req.Location)) lines.Add("Location: " + req.Location);
            if (!string.IsNullOrWhiteSpace(req.Difficulty)) lines.Add("Difficulty: " + req.Difficulty);
            if (!string.IsNullOrWhiteSpace(req.About)) lines.Add("Description: " + req.About);
            return lines.Count > 0 ? string.Join("\n", lines) : "General hiking trip";
        }
    }
}