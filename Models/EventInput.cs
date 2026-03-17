using System.ComponentModel.DataAnnotations;

namespace McpCodeExplainer.Models
{
    /// <summary>
    /// מודל קלט לאירוע שהמשתמש יכניס
    /// </summary>
    public class EventInput
    {
        /// <summary>
        /// שם האירוע
        /// </summary>
        [Required(ErrorMessage = "שם האירוע הוא שדה חובה")]
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// תיאור הקהל היעד (מורים, תלמידות, הורים וכו')
        /// </summary>
        [Required(ErrorMessage = "תיאור הקהל היעד הוא שדה חובה")]
        public string TargetAudience { get; set; } = string.Empty;

        /// <summary>
        /// תאריך ושעת האירוע
        /// </summary>
        [Required(ErrorMessage = "תאריך ושעת האירוע הם שדות חובה")]
        public DateTime EventDateTime { get; set; }

        /// <summary>
        /// מיקום האירוע
        /// </summary>
        [Required(ErrorMessage = "מיקום האירוע הוא שדה חובה")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// מטרת האירוע - מה חשוב להעביר
        /// </summary>
        [Required(ErrorMessage = "מטרת האירוע היא שדה חובה")]
        public string EventObjective { get; set; } = string.Empty;

        /// <summary>
        /// משך האירוע בדקות
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "משך האירוע חייב להיות חיובי")]
        public int DurationMinutes { get; set; } = 120;

        /// <summary>
        /// תקציב משוער (אם קיים)
        /// </summary>
        public decimal? Budget { get; set; }

        /// <summary>
        /// מספר משוער של משתתפים
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "מספר המשתתפים חייב להיות חיובי")]
        public int ExpectedAttendees { get; set; } = 50;
    }
}
