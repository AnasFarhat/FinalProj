using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PartnersWebApi.Services
{
    // ============================================================
    // מחלקת התוצאה עבור ניתוח רגשות (Sentiment Analysis)
    // ============================================================
    public class SentimentResult
    {
        public string Sentiment { get; set; }   // Positive / Negative / Neutral / Urgent_Negative
        public int Score { get; set; }          // 0-100 (0=שלילי מאוד, 100=חיובי מאוד)
        public string Summary { get; set; }     // סיכום קצר של מה שהמשתמש הרגיש
    }

    // ============================================================
    // הגדרת החוזה עבור שירות ה-AI (מעודכן עם פונקציית ניתוח הרגשות)
    // ============================================================
    public interface IChatAiService
    {
        Task<string> GetAiResponseAsync(string message, string context, List<(string role, string text)> history = null);
        Task<SentimentResult> AnalyzeSentimentAsync(string text);
    }

    // ============================================================
    // המימוש של שירות ה-AI מול מודל Gemini
    // ============================================================
    public class GeminiAiService : IChatAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Constructor - הזרקת HttpClient וגישה להגדרות ה-appsettings (Configuration)
        public GeminiAiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"]; // שליפת מפתח ה-API
        }

        // --------------------------------------------------------
        // 1. פונקציית צ'אט בוט
        // --------------------------------------------------------
        public async Task<string> GetAiResponseAsync(string message, string context, List<(string role, string text)> history = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "שגיאה: מפתח API לא מוגדר";

            var models = new[]
            {
                "gemini-2.5-flash",
                "gemini-2.5-flash-lite",
                "gemini-2.0-flash",
                "gemini-3-flash-preview"
            };

            var systemInstruction = $@"אתה 'פארטנר' - עוזר וירטואלי חברותי ומקצועי של אתר טיולים בטבע בישראל.

חוקים:
1. ענה תמיד בעברית בלבד.
2. תגובות קצרות וממוקדות - עד 3 משפטים.
3. השתמש באימוג'ים רלוונטיים בצורה מתונה.
4. אל תבקש TripId — זהה טיול אוטומטית מההקשר.
5. זכור את כל השיחה הקודמת והתייחס אליה.
6. כשמשתמש אומר 'הטיול הזה' — התכוון לטיול שדיברתם עליו לאחרונה.

מידע על הטיולים של המשתמש:
{context}";

            var contents = new List<object>();

            // System instruction
            contents.Add(new { role = "user", parts = new[] { new { text = systemInstruction } } });
            contents.Add(new { role = "model", parts = new[] { new { text = "הבנתי! אני מוכן לעזור." } } });

            // היסטוריית השיחה
            if (history != null)
            {
                foreach (var (role, text) in history)
                {
                    contents.Add(new { role, parts = new[] { new { text } } });
                }
            }

            // ההודעה הנוכחית
            contents.Add(new { role = "user", parts = new[] { new { text = message } } });

            var requestBody = new
            {
                contents = contents.ToArray(),
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 2048,
                    topP = 0.8,
                    thinkingConfig = new { thinkingBudget = 0 }
                }
            };

            foreach (var model in models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    Console.WriteLine($"[AI Service] מנסה מודל: {model}");

                    var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                        var root = doc.RootElement;

                        if (root.TryGetProperty("candidates", out var candidates) &&
                            candidates.ValueKind == JsonValueKind.Array &&
                            candidates.GetArrayLength() > 0)
                        {
                            var candidate = candidates[0];

                            string finishReason = candidate.TryGetProperty("finishReason", out var fr)
                                ? fr.GetString()
                                : "UNKNOWN";

                            if (candidate.TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.ValueKind == JsonValueKind.Array &&
                                parts.GetArrayLength() > 0)
                            {
                                var sb = new StringBuilder();
                                foreach (var part in parts.EnumerateArray())
                                {
                                    if (part.TryGetProperty("text", out var textProp))
                                        sb.Append(textProp.GetString());
                                }

                                var resultText = sb.ToString().Trim();
                                if (!string.IsNullOrEmpty(resultText))
                                {
                                    Console.WriteLine($"[AI Service] ✅ הצליח עם מודל: {model} (finishReason={finishReason})");
                                    return resultText;
                                }
                            }

                            Console.WriteLine($"[AI Service] ⚠️ מודל {model} החזיר תשובה ריקה (finishReason={finishReason}). עובר למודל הבא.");
                            continue;
                        }
                    }

                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AI Service] ❌ מודל {model} נכשל: {response.StatusCode} - {errorDetails}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return "שגיאת הרשאה - בדוק את מפתח ה-API שלך בשרת.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AI Service] ❌ Exception במודל {model}: {ex.Message}");
                }
            }

            return "מצטער, השירות עמוס כרגע 😅 נסה שוב בעוד כמה דקות.";
        }

        // --------------------------------------------------------
        // 2. פונקציית ניתוח רגשות (החדשה)
        // --------------------------------------------------------
        public async Task<SentimentResult> AnalyzeSentimentAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new SentimentResult { Sentiment = "Neutral", Score = 50, Summary = "" };

            if (string.IsNullOrEmpty(_apiKey))
                return new SentimentResult { Sentiment = "Neutral", Score = 50, Summary = "" };

            var prompt = $@"
אתה מנתח רגשות (Sentiment Analysis) של משובים על טיולים בטבע בישראל.
נתח את המשוב הבא והחזר אך ורק אובייקט JSON תקין (ללא טקסט נוסף, ללא markdown).

המשוב: ""{text}""

החזר JSON במבנה המדויק הזה:
{{
  ""sentiment"": ""<Positive או Negative או Neutral או Urgent_Negative>"",
  ""score"": <מספר שלם בין 0 ל-100, כאשר 0=שלילי מאוד ו-100=חיובי מאוד>,
  ""summary"": ""<סיכום קצר מאוד בעברית, עד 6 מילים, של מה שהמטייל הרגיש>""
}}

כללים:
- אם יש אזכור של סכנה, פציעה, מחדל בטיחותי או בעיה חמורה - sentiment חייב להיות Urgent_Negative.
- היה מדויק: ""היה טיול מהמם"" = Positive עם score גבוה (85-95).
- אם המשוב מעורב או עובדתי בלבד - Neutral עם score סביב 50.";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 256,
                    thinkingConfig = new { thinkingBudget = 0 }
                }
            };

            var models = new[] { "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-2.0-flash" };

            foreach (var model in models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                        var root = doc.RootElement;

                        if (root.TryGetProperty("candidates", out var candidates) &&
                            candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                        {
                            var candidate = candidates[0];
                            if (candidate.TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                            {
                                var sb = new StringBuilder();
                                foreach (var part in parts.EnumerateArray())
                                    if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());

                                var raw = sb.ToString().Trim();
                                raw = raw.Replace("```json", "").Replace("```", "").Trim();

                                using var resultDoc = JsonDocument.Parse(raw);
                                var r = resultDoc.RootElement;

                                return new SentimentResult
                                {
                                    Sentiment = r.TryGetProperty("sentiment", out var s) ? s.GetString() : "Neutral",
                                    Score = r.TryGetProperty("score", out var sc) ? sc.GetInt32() : 50,
                                    Summary = r.TryGetProperty("summary", out var sm) ? sm.GetString() : ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Sentiment] model {model} failed: {ex.Message}");
                }
            }

            return new SentimentResult { Sentiment = "Neutral", Score = 50, Summary = "" };
        }
    }
}