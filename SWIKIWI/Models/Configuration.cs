using System.Text.Json.Serialization;

namespace SWIKIWI.Models;

/// <summary>
/// Configurazione dell'applicazione
/// </summary>
public class Configuration
{
    public List<SearchSource> Sources { get; set; } = new();
    public List<CustomApiSource> CustomApiSources { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}

public class AppSettings
{
    public int MaxResults { get; set; } = 10;

    [JsonPropertyName("timeout")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("cacheEnabled")]
    public bool EnableCaching { get; set; } = true;

    public string LogLevel { get; set; } = "Information";
    public string OutputFormat { get; set; } = "table";
    public string UserAgent { get; set; } = "SWIKIWI/1.0";
}
