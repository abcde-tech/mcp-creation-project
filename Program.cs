using McpCodeExplainer.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Event Planner API",
        Version = "v1",
        Description = "API להפקת תכניות אירועים בעזרת בינה מלאכותית"
    });
});

builder.Services.AddHttpClient("NetFreeClient").ConfigurePrimaryHttpMessageHandler(() => {
    return new HttpClientHandler {
       
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };
});


builder.Services.AddHttpClient();


builder.Services.AddScoped<EventPlanningOrchestrator>();

var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.InjectStylesheet("/swagger/custom.css");
        c.DocumentTitle = "MCP Event Planner";
        c.RoutePrefix = string.Empty; // serve swagger at root
    });
}

app.UseHttpsRedirection(); 

app.UseAuthorization();
app.MapControllers();

app.Run();