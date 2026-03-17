# McpCodeExplainer

פרויקט ASP.NET Core API לתכנון אירועים באמצעות בינה מלאכותית (Gemini).

## תיאור

ה-API מספק שירות לתכנון אירועים מלא, כולל תוכנית, מזון, ברנדינג ותוכנית זמנים, באמצעות AI.

## התקנה

1. התקן .NET 9.0
2. שכפל את הריפו
3. הוסף את מפתח ה-API של Gemini ל-appsettings.json
4. הרץ `dotnet build`
5. הרץ `dotnet run`

## שימוש

### נקודת קצה ראשית
- `GET /api/EventPlan/health` - בדיקת תקינות

### יצירת תוכנית אירוע
- `POST /api/EventPlan/generate` - יצירת תוכנית אירוע

דוגמה לבקשה:

```json
{
  "eventName": "אירוע סוף שנה",
  "targetAudience": "מורים",
  "eventDateTime": "2026-06-15T09:00:00",
  "location": "אולם",
  "eventObjective": "הערכה למורים",
  "durationMinutes": 120,
  "budget": 5000,
  "expectedAttendees": 80
}
```

## תצורה

הוסף ל-appsettings.json:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_API_KEY"
  }
}
```