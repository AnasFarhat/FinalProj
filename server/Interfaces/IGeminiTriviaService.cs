using System.Collections.Generic;
using System.Threading.Tasks;
using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק לשירות יצירת שאלות טריוויה באמצעות Gemini.
    /// היה חסר בקבצים של חבר הצוות — בלעדיו ה-Backend לא מתקמפל
    /// (מאחר ו-Program.cs, TriviaController ו-GeminiTriviaService כולם תלויים בו).
    /// </summary>
    public interface IGeminiTriviaService
    {
        /// <summary>
        /// יוצר משחק טריוויה (כ-3 שאלות) לאתר נתון, מותאם לגיל ולסגנון הטיול.
        /// מחזיר null אם כל המודלים נכשלו.
        /// </summary>
        Task<List<GeminiQuestionModel>?> GenerateQuizAsync(string locationName,int age,string travelStyle,int questionsCount);
    }
}
