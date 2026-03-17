# McpCodeExplainer

ASP.NET Core API project for event planning using AI (Gemini).

## Description

The API provides a complete event planning service, including program, food, branding, and scheduling, using AI.

## Installation

1. Install .NET 9.0
2. Clone the repo
3. Add the Gemini API key to appsettings.json
4. Run `dotnet build`
5. Run `dotnet run`

## Usage

### Main endpoint
- `GET /api/EventPlan/health` - Health check

### Generate event plan
- `POST /api/EventPlan/generate` - Generate event plan

Example request:

```json
{
"eventName": "End of year event",
"targetAudience": "teachers",
"eventDateTime": "2026-06-15T09:00:00",
"location": "hall",
"eventObjective": "Teacher evaluation",
"durationMinutes": 120,
"budget": 5000,
"expectedAttendees": 80
}
```

## Configuration

Add to appsettings.json:

```json
{
"Gemini": {
"ApiKey": "YOUR_API_KEY"
}
}
```