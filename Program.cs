using SunChecker.Api.Models;
using SunChecker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<WeatherService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",           // local dev
                "https://sunchecker.netlify.app"  // production — update if your Netlify URL differs
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowReactApp");

app.MapGet("/", () => "SunChecker API is running.");

app.MapGet("/api/sunshine", async (
    double lat,
    double lon,
    WeatherService weatherService) =>
{
    var result = await weatherService.GetSunshineSummaryAsync(lat, lon);

    if (result is null)
        return Results.Problem("Failed to fetch weather data.");

    return Results.Ok(result);
});

app.Run();
