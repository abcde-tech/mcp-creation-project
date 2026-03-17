using McpCodeExplainer.Models;
using McpCodeExplainer.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpCodeExplainer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventPlanController : ControllerBase
    {
        private readonly EventPlanningOrchestrator _orchestrator;
        private readonly ILogger<EventPlanController> _logger;

        public EventPlanController(EventPlanningOrchestrator orchestrator, ILogger<EventPlanController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateEventPlan([FromBody] EventInput eventInput)
        {
            if (eventInput == null || !ModelState.IsValid)
            {
                return BadRequest(new { error = "נתוני הקלט אינם תקינים" });
            }

            try
            {
                _logger.LogInformation("מתחיל תכנון אירוע: {EventName}", eventInput.EventName);
                
                var plan = await _orchestrator.GenerateEventPlan(eventInput);
                
                if (plan == null)
                {
                    return StatusCode(500, new { error = "נכשלה יצירת תוכנית האירוע" });
                }

                return Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה קריטית בתהליך תכנון האירוע");
                return StatusCode(500, new { error = "אירעה שגיאה פנימית בשרת" });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "מערכת תכנון אירועים פעילה", 
                timestamp = DateTime.UtcNow,
                version = "1.1"
            });
        }
    }
}