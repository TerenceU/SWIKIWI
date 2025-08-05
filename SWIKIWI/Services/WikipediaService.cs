using System.Text.Json;
using System.Text.Json.Serialization;
using SWIKIWI.Models;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Services;

/// <summary>
/// Servizio per l'integrazione con l'API di Wikipedia
/// </summary>
public class WikipediaService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikipediaService> _logger;
    private readonly SearchSource _source;

    public string Name => _source.Name;
    public bool IsEnabled => _source.Enabled;

    public WikipediaService(HttpClient httpClient, ILogger<WikipediaService> logger, SearchSource source)
    {
        _httpClient = httpClient;
        _logger = logger;
        _source = source;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_source.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(_source.TimeoutSeconds);
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Servizio {ServiceName} disabilitato", Name);
            return Enumerable.Empty<SearchResult>();
        }

        try
        {
            _logger.LogDebug("Avvio ricerca Wikipedia per: {Query}", query);

            // Prima cerchiamo i risultati di ricerca
            var searchResults = await SearchArticlesAsync(query, cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogDebug("Nessun risultato trovato nella ricerca per: {Query}", query);
                return Enumerable.Empty<SearchResult>();
            }

            var results = new List<SearchResult>();

            // Per ogni risultato, otteniamo il summary
            foreach (var title in searchResults.Take(3)) // Limitiamo a 3 per evitare troppe chiamate
            {
                try
                {
                    _logger.LogDebug("Recupero summary per: {Title}", title);
                    var summary = await GetArticleSummaryAsync(title, cancellationToken);
                    if (summary != null)
                    {
                        results.Add(summary);
                        _logger.LogDebug("Summary recuperato con successo per: {Title}", title);
                    }
                    else
                    {
                        _logger.LogDebug("Nessun summary trovato per: {Title}", title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Errore nel recupero summary per {Title}", title);
                }
            }

            _logger.LogInformation("Wikipedia {Language}: trovati {Count} risultati per '{Query}'",
                _source.Language.ToUpperInvariant(), results.Count, query);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella ricerca Wikipedia per query: {Query}", query);
            return Enumerable.Empty<SearchResult>();
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<IEnumerable<string>> SearchArticlesAsync(string query, CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();
        var searchUrl = $"{baseUrl}/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(query)}&format=json&srlimit=5";

        _logger.LogDebug("Chiamata API Wikipedia: {Url}", searchUrl);

        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("Risposta API Wikipedia: {Json}", json.Length > 500 ? json[..500] + "..." : json);

        var searchResponse = JsonSerializer.Deserialize<WikipediaSearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var titles = searchResponse?.Query?.Search?.Select(s => s.Title) ?? Enumerable.Empty<string>();
        _logger.LogDebug("Titoli trovati: {Titles}", string.Join(", ", titles));

        return titles;
    }

    private async Task<SearchResult?> GetArticleSummaryAsync(string title, CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();
        var summaryUrl = $"{baseUrl}/api/rest_v1/page/summary/{Uri.EscapeDataString(title)}";

        _logger.LogDebug("Recupero summary da: {Url}", summaryUrl);

        try
        {
            var response = await _httpClient.GetAsync(summaryUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Errore HTTP {StatusCode} per summary di {Title}", response.StatusCode, title);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Risposta summary: {Json}", json.Length > 200 ? json[..200] + "..." : json);

            var summary = JsonSerializer.Deserialize<WikipediaSummaryResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (summary == null)
            {
                _logger.LogDebug("Deserializzazione summary fallita per {Title}", title);
                return null;
            }

            return new SearchResult
            {
                Title = summary.Title ?? title,
                Summary = summary.Extract ?? "Nessun riassunto disponibile",
                Url = summary.ContentUrls?.Desktop?.Page ?? $"{baseUrl}/wiki/{Uri.EscapeDataString(title)}",
                Source = Name,
                Language = _source.Language,
                RelevanceScore = 1.0,
                Metadata = new Dictionary<string, object>
                {
                    ["pageId"] = summary.PageId?.ToString() ?? "",
                    ["thumbnail"] = summary.Thumbnail?.Source ?? ""
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Errore HTTP nel recupero summary per {Title}", title);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore generico nel recupero summary per {Title}", title);
            return null;
        }
    }

    private string GetBaseUrl()
    {
        // Estrae l'URL base dalla configurazione
        var url = _source.Url;
        var apiIndex = url.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
        return apiIndex > 0 ? url[..apiIndex] : "https://it.wikipedia.org";
    }

    // Classi per la deserializzazione JSON
    private class WikipediaSearchResponse
    {
        public string? Batchcomplete { get; set; }
        public WikipediaQuery? Query { get; set; }
    }

    private class WikipediaQuery
    {
        public WikipediaSearchResult[]? Search { get; set; }
    }

    private class WikipediaSearchResult
    {
        public string Title { get; set; } = string.Empty;
        public int PageId { get; set; }
        public string Snippet { get; set; } = string.Empty;
    }

    private class WikipediaSummaryResponse
    {
        public string? Title { get; set; }
        public string? Extract { get; set; }

        [JsonPropertyName("pageid")]
        public int? PageId { get; set; }

        [JsonPropertyName("content_urls")]
        public WikipediaContentUrls? ContentUrls { get; set; }

        public WikipediaThumbnail? Thumbnail { get; set; }
    }

    private class WikipediaContentUrls
    {
        public WikipediaDesktopUrls? Desktop { get; set; }
    }

    private class WikipediaDesktopUrls
    {
        public string? Page { get; set; }
    }

    private class WikipediaThumbnail
    {
        public string? Source { get; set; }
    }
}
