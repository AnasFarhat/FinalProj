using System;

namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג את התקדמות המשתמש במשחק הטריוויה.
    /// כולל את השאלה שנענתה, תוצאת התשובה ומועד המענה.
    /// </summary>
    public class UserTriviaProgress
    {
        public int UserId { get; set; }
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime AnsweredAt { get; set; }
    }
}