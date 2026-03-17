using McpCodeExplainer.Models;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace McpCodeExplainer.Services
{
    public class EventPlanningOrchestrator
{
    private readonly HttpClient _httpClient;
    private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    private readonly string? _apiKey;
    private readonly ILogger<EventPlanningOrchestrator> _logger;
    private bool _fallbackUsed;

    public EventPlanningOrchestrator(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<EventPlanningOrchestrator> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        
        
        _apiKey = configuration["Gemini:ApiKey"];
        _logger = logger;
    }
    


        public async Task<EventProductionPlan> GenerateEventPlan(EventInput eventInput)
        {
            _logger.LogInformation("Starting event plan generation for {EventName} (audience: {Target})", eventInput.EventName, eventInput.TargetAudience);
            _fallbackUsed = false;

            var plan = new EventProductionPlan { EventDetails = eventInput };

            // הרצה מקבילית
            var programmingTask = GenerateProgramming(eventInput);
            var foodTask = GenerateFoodSuggestions(eventInput);
            var brandingTask = GenerateBrandingStrategy(eventInput);
            var tipsTask = GenerateSuccessTips(eventInput);

            await Task.WhenAll(programmingTask, foodTask, brandingTask, tipsTask);

            plan.Programming = await programmingTask;
            plan.Food = await foodTask;
            plan.Branding = await brandingTask;
            plan.SuccessTips = await tipsTask;

            plan.Timeline = await GenerateDetailedTimeline(eventInput, plan);
            plan.UsedFallback = _fallbackUsed;

            _logger.LogInformation("Completed event plan generation for {EventName} (fallback used: {UsedFallback})", eventInput.EventName, plan.UsedFallback);
            return plan;
        }

        private async Task<ProgrammingIdeas> GenerateProgramming(EventInput eventInput)
        {
            var prompt = $"אתה מתכנן אירועים. צור רעיונות לתוכנית עבור {eventInput.EventName} לקהל {eventInput.TargetAudience} בפורמט JSON בלבד.";
            var result = await CallGeminiAPI<ProgrammingIdeas>(prompt);

            if (result is null)
            {
                _fallbackUsed = true;
                _logger.LogWarning("Falling back to default programming ideas for {EventName}", eventInput.EventName);
                return CreateFallbackProgramming(eventInput);
            }

            return result;
        }

        private async Task<FoodSuggestions> GenerateFoodSuggestions(EventInput eventInput)
        {
            var prompt = $"צור תפריט לאירוע {eventInput.EventName} בפורמט JSON בלבד.";
            var result = await CallGeminiAPI<FoodSuggestions>(prompt);

            if (result is null)
            {
                _fallbackUsed = true;
                _logger.LogWarning("Falling back to default food suggestions for {EventName}", eventInput.EventName);
                return CreateFallbackFood(eventInput);
            }

            return result;
        }

        private async Task<BrandingStrategy> GenerateBrandingStrategy(EventInput eventInput)
        {
            var prompt = $"צור אסטרטגיית מיתוג לאירוע {eventInput.EventName} בפורמט JSON בלבד.";
            var result = await CallGeminiAPI<BrandingStrategy>(prompt);

            if (result is null)
            {
                _fallbackUsed = true;
                _logger.LogWarning("Falling back to default branding strategy for {EventName}", eventInput.EventName);
                return CreateFallbackBranding(eventInput);
            }

            return result;
        }

        private async Task<DetailedTimelineAndPresentation> GenerateDetailedTimeline(EventInput eventInput, EventProductionPlan plan)
        {
            var prompt = $"צור לו\"ז ל-{eventInput.DurationMinutes} דקות עבור {eventInput.EventName} בפורמט JSON בלבד.";
            var result = await CallGeminiAPI<DetailedTimelineAndPresentation>(prompt);

            if (result is null)
            {
                _fallbackUsed = true;
                _logger.LogWarning("Falling back to default timeline for {EventName}", eventInput.EventName);
                return CreateFallbackTimeline(eventInput, plan);
            }

            return result;
        }

        private async Task<List<string>> GenerateSuccessTips(EventInput eventInput)
        {
            var prompt = $"תן 5 טיפים להצלחת אירוע {eventInput.EventName} כמערך JSON של מחרוזות.";
            var result = await CallGeminiAPI<List<string>>(prompt);

            if (result is null)
            {
                _fallbackUsed = true;
                _logger.LogWarning("Falling back to default success tips for {EventName}", eventInput.EventName);
                return CreateFallbackTips(eventInput);
            }

            return result;
        }

        private async Task<T?> CallGeminiAPI<T>(string prompt) where T : class
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    _logger.LogWarning("Gemini ApiKey is missing; skipping external call.");
                    return null;
                }

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1000,
                        response_mime_type = "application/json"
                    }
                };

                var url = $"{GEMINI_API_URL}?key={_apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API call failed with status {StatusCode}: {ErrorDetails}", response.StatusCode, errorDetails);

                    if ((int)response.StatusCode == 418 &&
                        errorDetails.Contains("blockByNetFree", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Network filter (NetFree) is blocking access to Gemini API; consider allowing generativelanguage.googleapis.com.");
                    }

                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (jsonResponse.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var textContent = parts[0].GetProperty("text").GetString();
                    if (string.IsNullOrWhiteSpace(textContent))
                    {
                        _logger.LogWarning("Gemini response contained empty text content.");
                        return null;
                    }

                    _logger.LogDebug("Gemini Response: {Response}", textContent);

                    return JsonSerializer.Deserialize<T>(textContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
                }

                _logger.LogWarning("Gemini response did not contain expected structure.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical exception in Gemini API call");
                return null;
            }
        }

        private ProgrammingIdeas CreateFallbackProgramming(EventInput input)
        {
            return new ProgrammingIdeas
            {
                NarrativeTheme = "תחושת הערכה וחיבור",
                OpeningActivities = new List<string>
                {
                    "פתיחה קצרה עם ברכת תודה למורים",
                    "שיתוף רגע משמעותי מהשנה על ידי נציג תלמידות"
                },
                MainProgram = new List<string>
                {
                    "נאום קצר של מנהלת בית הספר",
                    "הצגת וידאו של רגעים מהשנה"
                },
                SpecialMoments = new List<string>
                {
                    "מתנה אישית לכל מורה",
                    "קטע של מסרים מעודדים מההורים"
                },
                ClosingActivities = new List<string>
                {
                    "מעגל חיבוקים וסגירת ברכה",
                    "הודיה על ההשפעה החיובית" 
                }
            };
        }

        private FoodSuggestions CreateFallbackFood(EventInput input)
        {
            return new FoodSuggestions
            {
                MenuTheme = "חמימות והערכה",
                KosherOptions = new List<string> { "כריכים ממולאים", "סלטים טריים" },
                Desserts = new List<string> { "עוגיות ביתיות", "מיני עוגות" },
                Beverages = new List<string> { "תה חם", "מיץ טבעי" },
                HealthyAlternatives = new List<string> { "פירות טריים", "אגוזים" }
            };
        }

        private BrandingStrategy CreateFallbackBranding(EventInput input)
        {
            return new BrandingStrategy
            {
                EventTitle = input.EventName,
                Tagline = "כוח ושייכות",
                Colors = new ColorPalette
                {
                    PrimaryColor = "כחול עמוק",
                    SecondaryColor = "לבן",
                    AccentColor = "זהב",
                    Meaning = "כח, טוהר והשראה"
                },
                Logo = new LogoIdea
                {
                    Concept = "לוגו עם לב וטיפוס תוכן",
                    VisualDescription = "לב במרכז מוקף בקשתות צבע",
                    DesignTips = new List<string>
                    {
                        "השתמש בפונט קריא וברור",
                        "דאג לשימוש צבעים נעימים" 
                    }
                },
                Souvenir = new Souvenir
                {
                    ItemDescription = "צמיד זיכרון פשוט",
                    PrintingIdea = "חריטה עם שם האירוע",
                    PersonalizationOptions = new List<string> { "שם המורה", "תאריך האירוע" }
                },
                OverallStyle = "חמים ומקצועי"
            };
        }

        private DetailedTimelineAndPresentation CreateFallbackTimeline(EventInput input, EventProductionPlan plan)
        {
            return new DetailedTimelineAndPresentation
            {
                Schedule = new List<TimelineItem>
                {
                    new TimelineItem { Time = "00:00", Activity = "פתיחה", Description = "ברכה קצרה והיכרות", Responsibility = "צוות מנהלה" },
                    new TimelineItem { Time = "00:20", Activity = "נאום", Description = "נאום של מנהלת בית הספר", Responsibility = "מנהלת" },
                    new TimelineItem { Time = "00:40", Activity = "הצגת וידאו", Description = "הצגת רגעים מהשנה", Responsibility = "צוות מולטימדיה" },
                    new TimelineItem { Time = "01:00", Activity = "הפסקה", Description = "הפסקה קלה עם כיבוד", Responsibility = "צוות" },
                    new TimelineItem { Time = "01:15", Activity = "סיום", Description = "סגירת האירוע והודעות", Responsibility = "מנהלה" }
                },
                Presentation = new PresentationOutline
                {
                    OpeningMessage = "ברוכים הבאים, תודה על המאמץ וההשקעה",
                    MainPoints = new List<string> { "הישגים השנה", "הערכה לתהליך", "תוכניות לעתיד" },
                    EmotionalMoments = new List<string> { "הצגת סיפורי הצלחה", "הבעת תודה אישית" },
                    ClosingMessage = "תודה רבה, המשך הצלחה ושפע ברכה",
                    SpeakingTips = new List<string> { "שמור על קצב רגוע", "השתמש בדוגמאות אישיות" }
                },
                DecorIdeas = new List<string> { "פרחים טבעיים", "שלטי תודה" },
                TechnicalRequirements = new List<string> { "מיקרופון", "מסך ומקרן" }
            };
        }

        private List<string> CreateFallbackTips(EventInput input)
        {
            return new List<string>
            {
                "מקדישים זמן לתכנון מראש",
                "מטפלים בפרטים קטנים שלוקחים זמן",
                "מטמיעים תכנית חלופית למקרה ריכוז יתר",
                "מביאים צוות תומך לארגון",
                "שומרים על אנרגיה חיובית לאורך האירוע"
            };
        }
    }
}