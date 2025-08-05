namespace SWIKIWI.Models;

/// <summary>
/// Rappresenta il risultato di una ricerca standardizzato per tutte le fonti
/// </summary>
public class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
