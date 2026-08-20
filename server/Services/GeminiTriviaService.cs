using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Services
{
    /// <summary>
    /// שירות האחראי על יצירת משחקי טריוויה באמצעות Google Gemini AI.
    /// השירות מייצר שאלות מותאמות אישית בהתאם למיקום, גיל המשתמש
    /// וסגנון הטיול שלו.
    /// </summary>
    public class GeminiTriviaService : IGeminiTriviaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string[] _models;

        /// <summary>
        /// אתחול השירות באמצעות HttpClientFactory
        /// וטעינת מפתח ה-API מתוך הגדרות המערכת.
        /// </summary>
        public GeminiTriviaService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = config["GeminiTrivia:ApiKey"] ?? throw new ArgumentNullException("GeminiTrivia API Key missing");

            _models = new[]
            {
    "gemini-2.0-flash",
    "gemini-2.0-flash-lite",
    "gemini-flash-latest",
    "gemini-2.5-flash"
};
        }

        /// <summary>
        /// יוצר משחק טריוויה מותאם אישית באמצעות Gemini AI.
        /// השאלות נבנות בהתאם לנקודת העניין, לגיל המשתמש
        /// ולסגנון הטיול שלו.
        /// </summary>
        /// <param name="locationName">שם נקודת העניין.</param>
        /// <param name="age">גיל המשתמש.</param>
        /// <param name="travelStyle">סגנון הטיול של המשתמש.</param>
        /// <returns>
        /// רשימת שאלות טריוויה שנוצרו על ידי Gemini,
        /// או Null במקרה שלא ניתן היה ליצור שאלות.
        /// </returns>
        public async Task<List<GeminiQuestionModel>?> GenerateQuizAsync(string locationName, int age, string travelStyle, int questionsCount)
        {
            Console.WriteLine($"[DEBUG AI] Checking API Key... Exists: {!string.IsNullOrEmpty(_apiKey)}");
            if (string.IsNullOrEmpty(_apiKey)) return null;

            string audienceDescription = travelStyle.Contains("משפחה")
                ? $"משפחות עם ילדים, ברמה קלילה וחווייתית, מותאם לגיל {age}"
                : $"מטיילים עצמאיים, ברמה מעניינת ומאתגרת, מותאם לגיל {age}";

            string prompt = $"אתה מדריך טיולים מומחה בישראל. צור משחק טריוויה קצר הכולל בדיוק {questionsCount} שאלות אמריקאיות ייחודיות ומעניינות על האתר הגיאוגרפי: {locationName}.\n" +
                         $"קהל היעד הוא: {audienceDescription}.\n" +
                         $"החזר אך ורק מערך JSON תקין בעברית, ללא סימני markdown וללא תגיות (אל תכתוב ```json).\n" +
                         $"🌟 חוק קשיח: אל תשתמש בגרשיים כפולים (\") בתוך הטקסט של השאלות, התשובות או ההסברים, אלא רק בגרש בודד (').\n" +
                         $"המבנה חייב להיות בדיוק מערך של אובייקטים המכילים את השדות הבאים באנגלית בלבד:\n" +
                         $"[{{\"QuestionText\": \"השאלה בעברית\", \"Answers\": [\"תשובה 1\", \"תשובה 2\", \"תשובה 3\", \"תשובה 4\"], \"CorrectAnswerIndex\": 0, \"ExplanatoryMessage\": \"הסבר קצר בעברית\"}}]";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.5, responseMimeType = "application/json" }
            };

            foreach (var model in _models)
            {
                try
                {
                    Console.WriteLine($"[DEBUG AI] Trying model: {model}...");
                    string rawUrl = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent?key=" + _apiKey;

                    using var client = new HttpClient();

                    using var response = await client.PostAsJsonAsync(rawUrl, requestBody);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                        string? rawJsonText = doc?.RootElement.GetProperty("candidates")[0]
                                                   .GetProperty("content")
                                                   .GetProperty("parts")[0]
                                                   .GetProperty("text")
                                                   .GetString();

                        if (string.IsNullOrEmpty(rawJsonText)) continue;

                        rawJsonText = rawJsonText.Replace("```json", "").Replace("```", "").Trim();

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var questions = JsonSerializer.Deserialize<List<GeminiQuestionModel>>(rawJsonText, options);

                        if (questions != null && questions.Count > 0)
                        {
                            Console.WriteLine($"[AI SUCCESS] Generated questions successfully using model: {model}");
                            return questions;
                        }
                    }
                    else
                    {
                        string errContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[AI MODEL ERROR] Model {model} failed. Status: {response.StatusCode}, Content: {errContent}");
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AI CRITICAL EXCEPTION] Failed with model {model}: {ex.Message}");
                }
            }

            Console.WriteLine("[AI ERROR] All models failed to generate content or returned empty arrays.");
            return null;
        }
    }
}