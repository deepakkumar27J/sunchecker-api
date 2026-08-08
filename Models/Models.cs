namespace SunChecker.Api.Models;

// ─── What your API returns to React ─────────────────────────────────────────

public record SunnyHour(
    string Time,         // "09:00"
    double SunshineSecs  // seconds of sun that hour (max 3600)
);

public record DaySummary(
    string Date,         // "2026-03-26"
    string DayLabel,     // "Today" / "Tomorrow" / "Wednesday"
    bool HasSun,
    List<SunnyHour> SunnyHours
);

// ─── Mirrors the Open-Meteo JSON response ────────────────────────────────────

public class OpenMeteoResponse
{
    public HourlyData? Hourly { get; set; }
}

public class HourlyData
{
    public List<string>? Time { get; set; }
    public List<double>? Sunshine_Duration { get; set; }
    public List<int>? Weather_Code { get; set; }
}