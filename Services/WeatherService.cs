using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SunChecker.Api.Models;

namespace SunChecker.Api.Services;

public class WeatherService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WeatherService(IHttpClientFactory httpFactory, IMemoryCache cache)
    {
        _httpFactory = httpFactory;
        _cache = cache;
    }

    public async Task<List<DaySummary>?> GetSunshineSummaryAsync(double lat, double lon)
    {
        // Round coords so nearby requests share a cache entry
        var cacheKey = $"sunshine_{lat:F2}_{lon:F2}";

        if (_cache.TryGetValue(cacheKey, out List<DaySummary>? cached))
            return cached;

        var client = _httpFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(BuildUrl(lat, lon));
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json, JsonOpts);

        if (data?.Hourly is null)
            return null;

        var summary = ProcessHourlyData(data.Hourly);

        _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(30));

        return summary;
    }

    private static string BuildUrl(double lat, double lon) =>
        $"https://api.open-meteo.com/v1/forecast" +
        $"?latitude={lat}&longitude={lon}" +
        $"&hourly=sunshine_duration,weather_code" +
        $"&timezone=auto" +
        $"&forecast_days=3";

    private static List<DaySummary> ProcessHourlyData(HourlyData hourly)
    {
        var times = hourly.Time ?? [];
        var sunshine = hourly.Sunshine_Duration ?? [];
        var today = DateTime.Today;

        var byDay = times
            .Select((t, i) => new
            {
                DateTime = DateTime.Parse(t),
                SunshineSecs = i < sunshine.Count ? sunshine[i] : 0
            })
            .GroupBy(x => x.DateTime.Date)
            .Take(3);

        var summaries = new List<DaySummary>();

        foreach (var day in byDay)
        {
            var sunnyHours = day
                .Where(x => x.SunshineSecs > 0)
                .Select(x => new SunnyHour(
                    x.DateTime.ToString("HH:mm"),
                    Math.Round(x.SunshineSecs, 0)
                ))
                .ToList();

            var dayLabel = day.Key == today ? "Today"
                : day.Key == today.AddDays(1) ? "Tomorrow"
                : day.Key.ToString("dddd");

            summaries.Add(new DaySummary(
                Date: day.Key.ToString("yyyy-MM-dd"),
                DayLabel: dayLabel,
                HasSun: sunnyHours.Count > 0,
                SunnyHours: sunnyHours
            ));
        }

        return summaries;
    }
}

