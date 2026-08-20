using System.Collections.Generic;

namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג שאלת טריוויה שנוצרה באמצעות שירות הבינה המלאכותית Gemini.
    /// כולל את נוסח השאלה, אפשרויות התשובה, התשובה הנכונה והסבר.
    /// </summary>
    public class GeminiQuestionModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Answers { get; set; } = new List<string>();
        public int CorrectAnswerIndex { get; set; }
        public string ExplanatoryMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// מייצג משחק טריוויה עבור נקודת עניין מסוימת.
    /// כולל את פרטי המיקום, רשימת השאלות ומספר הנקודות המוענק עבור כל תשובה נכונה.
    /// </summary>
    public class LocationQuizGameDto
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public List<GeminiQuestionModel> Questions { get; set; } = new List<GeminiQuestionModel>();
        public int PointsPerCorrectAnswer { get; set; } = 20;
    }
}