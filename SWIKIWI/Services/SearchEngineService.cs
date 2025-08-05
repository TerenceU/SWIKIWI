using SWIKIWI.Models;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Services;

/// <summary>
/// Servizio principale per orchestrare le ricerche su multiple fonti
/// </summary>
public class SearchEngineService
{
    private readonly ILogger<SearchEngineService> _logger;
    private readonly ConfigurationService _configService;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, ISearchService> _searchServices = new();

    public SearchEngineService(
        ILogger<SearchEngineService> logger,
        ConfigurationService configService,
        HttpClient httpClient)
    {
        _logger = logger;
        _configService = configService;
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        var config = await _configService.LoadConfigurationAsync();

        // Inizializza fonti standard
        foreach (var source in config.Sources.Where(s => s.Enabled))
        {
            try
            {
                ISearchService searchService = source.Type switch
                {
                    SourceType.Api when source.Name.Contains("Wikipedia", StringComparison.OrdinalIgnoreCase) =>
                        new WikipediaService(_httpClient,
                            Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                                .CreateLogger<WikipediaService>(), source),
                    _ => throw new NotSupportedException($"Tipo di fonte non supportato: {source.Type} per {source.Name}")
                };

                _searchServices[source.Name] = searchService;
                _logger.LogInformation("Servizio inizializzato: {ServiceName}", source.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'inizializzazione del servizio {ServiceName}", source.Name);
            }
        }

        // Inizializza fonti API personalizzate
        foreach (var source in config.CustomApiSources.Where(s => s.Enabled))
        {
            try
            {
                var customService = new CustomApiService(_httpClient,
                    Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                        .CreateLogger<CustomApiService>(), source);

                _searchServices[source.Name] = customService;
                _logger.LogInformation("Servizio API personalizzato inizializzato: {ServiceName}", source.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'inizializzazione del servizio personalizzato {ServiceName}", source.Name);
            }
        }
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(
        string query,
        string? sourceName = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("La query non può essere vuota", nameof(query));
        }

        var config = await _configService.LoadConfigurationAsync();
        var maxResults = limit ?? config.Settings.MaxResults;

        _logger.LogInformation("Avvio ricerca per: {Query}", query);

        var servicesToQuery = _searchServices.Values.AsEnumerable();

        // Se è specificata una fonte, filtra solo quella
        if (!string.IsNullOrEmpty(sourceName))
        {
            servicesToQuery = _searchServices.Values
                .Where(s => s.Name.Contains(sourceName, StringComparison.OrdinalIgnoreCase));
        }

        var allResults = new List<SearchResult>();
        var searchTasks = servicesToQuery
            .Where(s => s.IsEnabled)
            .Select(async service =>
            {
                try
                {
                    var results = await service.SearchAsync(query, cancellationToken);
                    _logger.LogDebug("Servizio {ServiceName} ha restituito {Count} risultati",
                        service.Name, results.Count());
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore nella ricerca per il servizio {ServiceName}", service.Name);
                    return Enumerable.Empty<SearchResult>();
                }
            });

        var searchResults = await Task.WhenAll(searchTasks);

        foreach (var results in searchResults)
        {
            allResults.AddRange(results);
        }

        // Ordina per rilevanza e applica il limite
        var sortedResults = allResults
            .OrderByDescending(r => r.RelevanceScore)
            .ThenBy(r => r.Source)
            .Take(maxResults)
            .ToList();

        _logger.LogInformation("Ricerca completata: {TotalResults} risultati trovati", sortedResults.Count);

        return sortedResults;
    }

    public IEnumerable<string> GetAvailableSources()
    {
        return _searchServices.Keys;
    }

    public async Task<Dictionary<string, bool>> GetSourceStatusAsync()
    {
        var status = new Dictionary<string, bool>();

        foreach (var kvp in _searchServices)
        {
            try
            {
                status[kvp.Key] = await kvp.Value.IsAvailableAsync();
            }
            catch
            {
                status[kvp.Key] = false;
            }
        }

        return status;
    }
}
