using System.Text.Json;
using SWIKIWI.Models;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Services;

/// <summary>
/// Servizio per la gestione della configurazione
/// </summary>
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    private Configuration? _configuration;

    public ConfigurationService(ILogger<ConfigurationService> logger, string configPath = "config.json")
    {
        _logger = logger;
        _configPath = configPath;
    }

    public async Task<Configuration> LoadConfigurationAsync()
    {
        if (_configuration != null)
            return _configuration;

        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                _configuration = JsonSerializer.Deserialize<Configuration>(json, GetJsonOptions());
                _logger.LogInformation("Configurazione caricata da {ConfigPath}", _configPath);
            }
            else
            {
                _configuration = CreateDefaultConfiguration();
                await SaveConfigurationAsync(_configuration);
                _logger.LogInformation("Creata configurazione predefinita in {ConfigPath}", _configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento della configurazione");
            _configuration = CreateDefaultConfiguration();
        }

        return _configuration;
    }

    public async Task SaveConfigurationAsync(Configuration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, GetJsonOptions());
            await File.WriteAllTextAsync(_configPath, json);
            _configuration = configuration;
            _logger.LogInformation("Configurazione salvata in {ConfigPath}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel salvataggio della configurazione");
            throw;
        }
    }

    public async Task<bool> EnableSourceAsync(string sourceName)
    {
        var config = await LoadConfigurationAsync();
        var source = config.Sources.FirstOrDefault(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        
        if (source == null)
        {
            _logger.LogWarning("Fonte non trovata: {SourceName}", sourceName);
            return false;
        }

        source.Enabled = true;
        await SaveConfigurationAsync(config);
        _logger.LogInformation("Fonte abilitata: {SourceName}", sourceName);
        return true;
    }

    public async Task<bool> DisableSourceAsync(string sourceName)
    {
        var config = await LoadConfigurationAsync();
        var source = config.Sources.FirstOrDefault(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        
        if (source == null)
        {
            _logger.LogWarning("Fonte non trovata: {SourceName}", sourceName);
            return false;
        }

        source.Enabled = false;
        await SaveConfigurationAsync(config);
        _logger.LogInformation("Fonte disabilitata: {SourceName}", sourceName);
        return true;
    }

    private Configuration CreateDefaultConfiguration()
    {
        return new Configuration
        {
            Sources = new List<SearchSource>
            {
                new SearchSource
                {
                    Name = "Wikipedia IT",
                    Url = "https://it.wikipedia.org/api/rest_v1/page/summary/{query}",
                    Enabled = true,
                    Language = "it",
                    Type = SourceType.Api,
                    Parameters = new Dictionary<string, string>
                    {
                        ["redirect"] = "true"
                    }
                },
                new SearchSource
                {
                    Name = "Wikipedia EN",
                    Url = "https://en.wikipedia.org/api/rest_v1/page/summary/{query}",
                    Enabled = true,
                    Language = "en",
                    Type = SourceType.Api,
                    Parameters = new Dictionary<string, string>
                    {
                        ["redirect"] = "true"
                    }
                }
            },
            CustomApiSources = new List<CustomApiSource>
            {
                new CustomApiSource
                {
                    Name = "JSONPlaceholder Example",
                    SearchEndpoint = "https://jsonplaceholder.typicode.com/posts",
                    Enabled = false,
                    Language = "en",
                    Type = SourceType.Api,
                    SearchQueryParam = "title",
                    ResponseDataPath = "",
                    FieldMapping = new ApiFieldMapping
                    {
                        TitleField = "title",
                        SummaryField = "body",
                        UrlField = "id", // Convertiremo l'ID in URL
                        CustomFields = new Dictionary<string, string>
                        {
                            ["userId"] = "userId",
                            ["postId"] = "id"
                        }
                    },
                    MaxResults = 5
                }
            },
            Settings = new AppSettings()
        };
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
