using System.Text.Json;
using SWIKIWI.Models;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Services;

/// <summary>
/// Servizio generico per API personalizzate configurabili
/// </summary>
public class CustomApiService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomApiService> _logger;
    private readonly CustomApiSource _source;

    public string Name => _source.Name;
    public bool IsEnabled => _source.Enabled;

    public CustomApiService(HttpClient httpClient, ILogger<CustomApiService> logger, CustomApiSource source)
    {
        _httpClient = httpClient;
        _logger = logger;
        _source = source;
        
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_source.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(_source.TimeoutSeconds);

        // Aggiungi headers personalizzati
        foreach (var header in _source.Headers)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
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
            _logger.LogDebug("Avvio ricerca {ServiceName} per: {Query}", Name, query);
            
            var searchUrl = BuildSearchUrl(query);
            _logger.LogDebug("URL ricerca: {Url}", searchUrl);
            
            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Risposta API {ServiceName}: {Json}", Name, 
                json.Length > 500 ? json[..500] + "..." : json);
            
            var results = ParseSearchResults(json);
            
            _logger.LogInformation("{ServiceName}: trovati {Count} risultati per '{Query}'", 
                Name, results.Count(), query);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella ricerca {ServiceName} per query: {Query}", Name, query);
            return Enumerable.Empty<SearchResult>();
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = new Uri(_source.SearchEndpoint).GetLeftPart(UriPartial.Authority);
            var response = await _httpClient.GetAsync(baseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string BuildSearchUrl(string query)
    {
        var url = _source.SearchEndpoint;
        
        // Aggiungi il parametro di query principale
        var separator = url.Contains('?') ? "&" : "?";
        url += $"{separator}{_source.SearchQueryParam}={Uri.EscapeDataString(query)}";
        
        // Aggiungi parametri aggiuntivi
        foreach (var param in _source.QueryParameters)
        {
            url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
        }
        
        // Aggiungi limite risultati se configurato
        if (_source.MaxResults > 0)
        {
            url += $"&limit={_source.MaxResults}";
        }
        
        return url;
    }

    private IEnumerable<SearchResult> ParseSearchResults(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            
            // Naviga al percorso dei dati se specificato
            var dataElement = root;
            if (!string.IsNullOrEmpty(_source.ResponseDataPath))
            {
                var pathParts = _source.ResponseDataPath.Split('.');
                foreach (var part in pathParts)
                {
                    if (dataElement.TryGetProperty(part, out var nextElement))
                    {
                        dataElement = nextElement;
                    }
                    else
                    {
                        _logger.LogWarning("Percorso dati non trovato: {Path}", _source.ResponseDataPath);
                        return Enumerable.Empty<SearchResult>();
                    }
                }
            }
            
            // Se dataElement è un array, processa ogni elemento
            if (dataElement.ValueKind == JsonValueKind.Array)
            {
                return dataElement.EnumerateArray()
                    .Select(ParseSingleResult)
                    .Where(r => r != null)
                    .Cast<SearchResult>();
            }
            
            // Se è un singolo oggetto, prova a convertirlo
            var singleResult = ParseSingleResult(dataElement);
            return singleResult != null ? new[] { singleResult } : Enumerable.Empty<SearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel parsing della risposta JSON per {ServiceName}", Name);
            return Enumerable.Empty<SearchResult>();
        }
    }

    private SearchResult? ParseSingleResult(JsonElement element)
    {
        try
        {
            var mapping = _source.FieldMapping;
            
            var title = GetFieldValue(element, mapping.TitleField) ?? "Titolo non disponibile";
            var summary = GetFieldValue(element, mapping.SummaryField) ?? "Riassunto non disponibile";
            var url = GetFieldValue(element, mapping.UrlField) ?? "";
            var thumbnail = GetFieldValue(element, mapping.ThumbnailField) ?? "";
            
            var result = new SearchResult
            {
                Title = title,
                Summary = summary,
                Url = url,
                Source = Name,
                Language = _source.Language,
                RelevanceScore = 1.0,
                Metadata = new Dictionary<string, object>
                {
                    ["thumbnail"] = thumbnail
                }
            };
            
            // Aggiungi campi personalizzati ai metadata
            foreach (var customField in mapping.CustomFields)
            {
                var value = GetFieldValue(element, customField.Value);
                if (!string.IsNullOrEmpty(value))
                {
                    result.Metadata[customField.Key] = value;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore nel parsing di un singolo risultato per {ServiceName}", Name);
            return null;
        }
    }

    private string? GetFieldValue(JsonElement element, string fieldPath)
    {
        try
        {
            if (string.IsNullOrEmpty(fieldPath))
                return null;
            
            var current = element;
            var pathParts = fieldPath.Split('.');
            
            foreach (var part in pathParts)
            {
                if (current.TryGetProperty(part, out var nextElement))
                {
                    current = nextElement;
                }
                else
                {
                    return null;
                }
            }
            
            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => current.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }
}
