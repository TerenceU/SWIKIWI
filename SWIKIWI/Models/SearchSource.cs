using System.Text.Json.Serialization;

namespace SWIKIWI.Models;

/// <summary>
/// Configurazione per una fonte di ricerca
/// </summary>
public class SearchSource
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Language { get; set; } = "it";

    [JsonPropertyName("timeout")]
    public int TimeoutSeconds { get; set; } = 30;

    public string UserAgent { get; set; } = "SWIKIWI/1.0";
    public SourceType Type { get; set; } = SourceType.Api;
    public Dictionary<string, string> Parameters { get; set; } = new();

    [JsonPropertyName("selectors")]
    public Dictionary<string, string> CssSelectors { get; set; } = new();
}

public enum SourceType
{
    Api,
    WebScraping
}
