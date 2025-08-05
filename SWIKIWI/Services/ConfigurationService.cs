using System.Text.Json;
using System.Text.Json.Serialization;
using SWIKIWI.Models;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Services;

/// <summary>
/// Servizio per la gestione della configurazione
/// </summary>
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private string _configPath;
    private Configuration? _configuration;
    private static readonly string DefaultConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        ".config",
        "swikiwi"
    );
    private static readonly string ProfileStateFile = Path.Combine(DefaultConfigDirectory, ".active-profile");

    public ConfigurationService(ILogger<ConfigurationService> logger, string? configPath = null)
    {
        _logger = logger;
        _configPath = configPath ?? GetActiveProfilePath();

        // Assicuriamoci che la directory esista
        EnsureConfigDirectoryExists();
    }

    public async Task<Configuration> LoadConfigurationAsync()
    {
        if (_configuration != null)
            return _configuration;

        _logger.LogInformation("Tentativo di caricamento da: {ConfigPath}", _configPath);

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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Assicura che la directory di configurazione esista
    /// </summary>
    private void EnsureConfigDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Creata directory di configurazione: {Directory}", directory);
        }
    }

    /// <summary>
    /// Cambia il file di configurazione attivo
    /// </summary>
    public async Task<bool> SetConfigFileAsync(string profileName)
    {
        try
        {
            var newConfigPath = Path.Combine(DefaultConfigDirectory, $"{profileName}.json");

            // Se il file non esiste, crealo con la configurazione di default
            if (!File.Exists(newConfigPath))
            {
                var defaultConfig = CreateDefaultConfiguration();
                var json = JsonSerializer.Serialize(defaultConfig, GetJsonOptions());
                await File.WriteAllTextAsync(newConfigPath, json);
                _logger.LogInformation("Creato nuovo profilo di configurazione: {ProfileName}", profileName);
            }

            _configPath = newConfigPath;
            _configuration = null; // Reset cache

            // Salva il profilo attivo
            await SaveActiveProfileAsync(profileName);

            // Forza il reload della configurazione
            await LoadConfigurationAsync();

            _logger.LogInformation("Cambiato file di configurazione a: {ConfigPath}", _configPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel cambio del file di configurazione");
            return false;
        }
    }

    /// <summary>
    /// Crea la configurazione di default nella directory standard
    /// </summary>
    public async Task<bool> CreateDefaultConfigAsync()
    {
        try
        {
            var defaultPath = Path.Combine(DefaultConfigDirectory, "config.json");
            var defaultConfig = CreateDefaultConfiguration();

            EnsureConfigDirectoryExists();
            var json = JsonSerializer.Serialize(defaultConfig, GetJsonOptions());
            await File.WriteAllTextAsync(defaultPath, json);

            _configPath = defaultPath;
            _configuration = defaultConfig;

            _logger.LogInformation("Creata configurazione di default in: {ConfigPath}", defaultPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella creazione della configurazione di default");
            return false;
        }
    }

    /// <summary>
    /// Lista tutti i profili di configurazione disponibili
    /// </summary>
    public List<string> ListProfiles()
    {
        try
        {
            if (!Directory.Exists(DefaultConfigDirectory))
                return new List<string>();

            return Directory.GetFiles(DefaultConfigDirectory, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella lettura dei profili");
            return new List<string>();
        }
    }

    /// <summary>
    /// Crea un nuovo profilo di configurazione
    /// </summary>
    public async Task<bool> CreateProfileAsync(string profileName)
    {
        try
        {
            var profilePath = Path.Combine(DefaultConfigDirectory, $"{profileName}.json");

            if (File.Exists(profilePath))
            {
                _logger.LogWarning("Il profilo {ProfileName} esiste già", profileName);
                return false;
            }

            var defaultConfig = CreateDefaultConfiguration();
            EnsureConfigDirectoryExists();

            var json = JsonSerializer.Serialize(defaultConfig, GetJsonOptions());
            await File.WriteAllTextAsync(profilePath, json);

            _logger.LogInformation("Creato nuovo profilo: {ProfileName}", profileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella creazione del profilo");
            return false;
        }
    }

    /// <summary>
    /// Ottiene il nome del profilo corrente
    /// </summary>
    public string GetCurrentProfile()
    {
        try
        {
            if (File.Exists(ProfileStateFile))
            {
                var activeProfile = File.ReadAllText(ProfileStateFile).Trim();
                if (!string.IsNullOrEmpty(activeProfile))
                {
                    return activeProfile;
                }
            }
        }
        catch
        {
            // Ignora errori e usa fallback
        }

        return Path.GetFileNameWithoutExtension(_configPath) ?? "config";
    }

    /// <summary>
    /// Ottiene il percorso della directory di configurazione
    /// </summary>
    public static string GetConfigDirectory()
    {
        return DefaultConfigDirectory;
    }

    /// <summary>
    /// Ottiene il percorso del profilo attivo
    /// </summary>
    private string GetActiveProfilePath()
    {
        try
        {
            if (File.Exists(ProfileStateFile))
            {
                var activeProfile = File.ReadAllText(ProfileStateFile).Trim();
                if (!string.IsNullOrEmpty(activeProfile))
                {
                    var profilePath = Path.Combine(DefaultConfigDirectory, $"{activeProfile}.json");
                    if (File.Exists(profilePath))
                    {
                        _logger.LogInformation("Profilo attivo rilevato: {ActiveProfile}", activeProfile);
                        return profilePath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore nel caricamento del profilo attivo, uso default");
        }

        // Default fallback
        return Path.Combine(DefaultConfigDirectory, "config.json");
    }

    /// <summary>
    /// Salva il profilo attivo nel file di stato
    /// </summary>
    private async Task SaveActiveProfileAsync(string profileName)
    {
        try
        {
            EnsureConfigDirectoryExists();
            await File.WriteAllTextAsync(ProfileStateFile, profileName);
            _logger.LogInformation("Profilo attivo salvato: {ProfileName}", profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel salvataggio del profilo attivo");
        }
    }
}
