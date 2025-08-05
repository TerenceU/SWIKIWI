namespace SWIKIWI.Models;

/// <summary>
/// Configurazione dell'applicazione
/// </summary>
public class Configuration
{
    public List<SearchSource> Sources { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}

public class AppSettings
{
    public int MaxResults { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableCaching { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public string OutputFormat { get; set; } = "table";
}
