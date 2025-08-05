namespace SWIKIWI.Models;

/// <summary>
/// Mapping per tradurre i campi di una API esterna nei nostri SearchResult
/// </summary>
public class ApiFieldMapping
{
    public string TitleField { get; set; } = "title";
    public string SummaryField { get; set; } = "summary";
    public string UrlField { get; set; } = "url";
    public string LanguageField { get; set; } = "language";
    public string ThumbnailField { get; set; } = "thumbnail";
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

/// <summary>
/// Configurazione avanzata per fonti API personalizzate
/// </summary>
public class CustomApiSource : SearchSource
{
    public string SearchEndpoint { get; set; } = string.Empty;
    public string DetailEndpoint { get; set; } = string.Empty;
    public ApiFieldMapping FieldMapping { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string SearchQueryParam { get; set; } = "q";
    public string ResponseDataPath { get; set; } = ""; // JSONPath per estrarre array risultati
    public int MaxResults { get; set; } = 5;
}
