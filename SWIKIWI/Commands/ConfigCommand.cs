using System.CommandLine;
using SWIKIWI.Services;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Commands;

/// <summary>
/// Comando per la gestione della configurazione
/// </summary>
public class ConfigCommand
{
    private readonly ConfigurationService _configService;
    private readonly SearchEngineService _searchEngine;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(
        ConfigurationService configService,
        SearchEngineService searchEngine,
        ILogger<ConfigCommand> logger)
    {
        _configService = configService;
        _searchEngine = searchEngine;
        _logger = logger;
    }

    public Command CreateCommand()
    {
        var configCommand = new Command("config", "Gestisce la configurazione dell'applicazione");

        // Comando show
        var showCommand = new Command("show", "Mostra la configurazione corrente");
        showCommand.SetHandler(async () => await ShowConfigurationAsync());

        // Comando enable
        var enableCommand = new Command("enable", "Abilita una fonte di ricerca");
        var enableSourceArg = new Argument<string>("source", "Nome della fonte da abilitare");
        enableCommand.Add(enableSourceArg);
        enableCommand.SetHandler(async (source) => await EnableSourceAsync(source), enableSourceArg);

        // Comando disable
        var disableCommand = new Command("disable", "Disabilita una fonte di ricerca");
        var disableSourceArg = new Argument<string>("source", "Nome della fonte da disabilitare");
        disableCommand.Add(disableSourceArg);
        disableCommand.SetHandler(async (source) => await DisableSourceAsync(source), disableSourceArg);

        // Comando status
        var statusCommand = new Command("status", "Mostra lo stato delle fonti");
        statusCommand.SetHandler(async () => await ShowSourceStatusAsync());

        // Comando set-file (nuovo)
        var setFileCommand = new Command("set-file", "Cambia il file di configurazione attivo");
        var profileNameArg = new Argument<string>("profile", "Nome del profilo di configurazione");
        setFileCommand.Add(profileNameArg);
        setFileCommand.SetHandler(async (profile) => await SetConfigFileAsync(profile), profileNameArg);

        // Comando default (nuovo)
        var defaultCommand = new Command("default", "Crea/ripristina la configurazione di default");
        defaultCommand.SetHandler(async () => await CreateDefaultConfigAsync());

        // Comando list-profiles (nuovo)
        var listProfilesCommand = new Command("list-profiles", "Lista tutti i profili di configurazione disponibili");
        listProfilesCommand.SetHandler(() => ListProfiles());

        // Comando create-profile (nuovo)
        var createProfileCommand = new Command("create-profile", "Crea un nuovo profilo di configurazione");
        var newProfileNameArg = new Argument<string>("name", "Nome del nuovo profilo");
        createProfileCommand.Add(newProfileNameArg);
        createProfileCommand.SetHandler(async (name) => await CreateProfileAsync(name), newProfileNameArg);

        configCommand.Add(showCommand);
        configCommand.Add(enableCommand);
        configCommand.Add(disableCommand);
        configCommand.Add(statusCommand);
        configCommand.Add(setFileCommand);
        configCommand.Add(defaultCommand);
        configCommand.Add(listProfilesCommand);
        configCommand.Add(createProfileCommand);

        return configCommand;
    }

    private async Task ShowConfigurationAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            var currentProfile = _configService.GetCurrentProfile();

            Console.WriteLine("⚙️  Configurazione SWIKIWI");
            Console.WriteLine("=========================");
            Console.WriteLine($"📁 Profilo attivo: {currentProfile}");
            Console.WriteLine($"📂 Directory config: {ConfigurationService.GetConfigDirectory()}");
            Console.WriteLine();

            Console.WriteLine("📊 Impostazioni generali:");
            Console.WriteLine($"   • Risultati massimi: {config.Settings.MaxResults}");
            Console.WriteLine($"   • Timeout: {config.Settings.TimeoutSeconds}s");
            Console.WriteLine($"   • Cache abilitata: {(config.Settings.EnableCaching ? "Sì" : "No")}");
            Console.WriteLine($"   • Livello log: {config.Settings.LogLevel}");
            Console.WriteLine($"   • Formato output: {config.Settings.OutputFormat}");
            Console.WriteLine();

            Console.WriteLine("📚 Fonti configurate:");
            foreach (var source in config.Sources)
            {
                var status = source.Enabled ? "✅ Abilitata" : "❌ Disabilitata";
                Console.WriteLine($"   • {source.Name} ({source.Language.ToUpperInvariant()}) - {status}");
                Console.WriteLine($"     URL: {source.Url}");
                Console.WriteLine($"     Tipo: {source.Type}");
                Console.WriteLine($"     Timeout: {source.TimeoutSeconds}s");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella visualizzazione della configurazione");
            Console.WriteLine($"❌ Errore nella lettura della configurazione: {ex.Message}");
        }
    }

    private async Task EnableSourceAsync(string sourceName)
    {
        try
        {
            var success = await _configService.EnableSourceAsync(sourceName);

            if (success)
            {
                Console.WriteLine($"✅ Fonte '{sourceName}' abilitata con successo");
                Console.WriteLine("💡 Riavvia l'applicazione per applicare le modifiche");
            }
            else
            {
                Console.WriteLine($"❌ Fonte '{sourceName}' non trovata");
                await ShowAvailableSourcesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'abilitazione della fonte {SourceName}", sourceName);
            Console.WriteLine($"❌ Errore nell'abilitazione della fonte: {ex.Message}");
        }
    }

    private async Task DisableSourceAsync(string sourceName)
    {
        try
        {
            var success = await _configService.DisableSourceAsync(sourceName);

            if (success)
            {
                Console.WriteLine($"✅ Fonte '{sourceName}' disabilitata con successo");
                Console.WriteLine("💡 Riavvia l'applicazione per applicare le modifiche");
            }
            else
            {
                Console.WriteLine($"❌ Fonte '{sourceName}' non trovata");
                await ShowAvailableSourcesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella disabilitazione della fonte {SourceName}", sourceName);
            Console.WriteLine($"❌ Errore nella disabilitazione della fonte: {ex.Message}");
        }
    }

    private async Task ShowSourceStatusAsync()
    {
        try
        {
            Console.WriteLine("🔍 Verifica stato fonti...");
            Console.WriteLine();

            var status = await _searchEngine.GetSourceStatusAsync();

            Console.WriteLine("📊 Stato delle fonti:");
            Console.WriteLine();

            foreach (var kvp in status)
            {
                var statusIcon = kvp.Value ? "🟢" : "🔴";
                var statusText = kvp.Value ? "Online" : "Offline";
                Console.WriteLine($"   {statusIcon} {kvp.Key}: {statusText}");
            }

            Console.WriteLine();
            var onlineCount = status.Count(kvp => kvp.Value);
            var totalCount = status.Count;
            Console.WriteLine($"📈 Fonti online: {onlineCount}/{totalCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella verifica dello stato delle fonti");
            Console.WriteLine($"❌ Errore nella verifica dello stato: {ex.Message}");
        }
    }

    private async Task SetConfigFileAsync(string profileName)
    {
        try
        {
            var success = await _configService.SetConfigFileAsync(profileName);

            if (success)
            {
                Console.WriteLine($"✅ Configurazione cambiata al profilo: {profileName}");
                Console.WriteLine($"📁 Percorso: {ConfigurationService.GetConfigDirectory()}\\{profileName}.json");
            }
            else
            {
                Console.WriteLine($"❌ Errore nel cambio del profilo: {profileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel cambio del file di configurazione");
            Console.WriteLine($"❌ Errore: {ex.Message}");
        }
    }

    private async Task CreateDefaultConfigAsync()
    {
        try
        {
            var success = await _configService.CreateDefaultConfigAsync();

            if (success)
            {
                Console.WriteLine("✅ Configurazione di default creata con successo");
                Console.WriteLine($"📁 Percorso: {ConfigurationService.GetConfigDirectory()}\\config.json");
                Console.WriteLine();
                Console.WriteLine("📋 La configurazione include:");
                Console.WriteLine("   • Wikipedia IT e EN abilitati");
                Console.WriteLine("   • Esempio di API personalizzata (JSONPlaceholder)");
                Console.WriteLine("   • Impostazioni di default ottimizzate");
            }
            else
            {
                Console.WriteLine("❌ Errore nella creazione della configurazione di default");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella creazione della configurazione di default");
            Console.WriteLine($"❌ Errore: {ex.Message}");
        }
    }

    private void ListProfiles()
    {
        try
        {
            var profiles = _configService.ListProfiles();
            var currentProfile = _configService.GetCurrentProfile();

            Console.WriteLine("📂 Profili di configurazione disponibili:");
            Console.WriteLine($"📁 Directory: {ConfigurationService.GetConfigDirectory()}");
            Console.WriteLine();

            if (profiles.Count == 0)
            {
                Console.WriteLine("   ℹ️  Nessun profilo trovato. Usa 'swikiwi config default' per creare il profilo di base.");
                return;
            }

            foreach (var profile in profiles.OrderBy(p => p))
            {
                var indicator = profile == currentProfile ? "👉" : "  ";
                var status = profile == currentProfile ? " (attivo)" : "";
                Console.WriteLine($"{indicator} {profile}{status}");
            }

            Console.WriteLine();
            Console.WriteLine($"📌 Profilo corrente: {currentProfile}");
            Console.WriteLine("💡 Usa 'swikiwi config set-file <nome>' per cambiare profilo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella lista dei profili");
            Console.WriteLine($"❌ Errore: {ex.Message}");
        }
    }

    private async Task CreateProfileAsync(string profileName)
    {
        try
        {
            // Validazione nome profilo
            if (string.IsNullOrWhiteSpace(profileName))
            {
                Console.WriteLine("❌ Il nome del profilo non può essere vuoto");
                return;
            }

            if (profileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Console.WriteLine("❌ Il nome del profilo contiene caratteri non validi");
                return;
            }

            var success = await _configService.CreateProfileAsync(profileName);

            if (success)
            {
                Console.WriteLine($"✅ Profilo '{profileName}' creato con successo");
                Console.WriteLine($"📁 Percorso: {ConfigurationService.GetConfigDirectory()}\\{profileName}.json");
                Console.WriteLine();
                Console.WriteLine("💡 Comandi utili:");
                Console.WriteLine($"   • swikiwi config set-file {profileName}  # Attiva questo profilo");
                Console.WriteLine($"   • swikiwi config show                    # Mostra configurazione attuale");
                Console.WriteLine($"   • swikiwi config list-profiles           # Lista tutti i profili");
            }
            else
            {
                Console.WriteLine($"❌ Il profilo '{profileName}' esiste già o si è verificato un errore");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella creazione del profilo");
            Console.WriteLine($"❌ Errore: {ex.Message}");
        }
    }

    private async Task ShowAvailableSourcesAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            Console.WriteLine();
            Console.WriteLine("📚 Fonti disponibili:");

            foreach (var source in config.Sources)
            {
                var status = source.Enabled ? "(abilitata)" : "(disabilitata)";
                Console.WriteLine($"   • {source.Name} {status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella visualizzazione delle fonti disponibili");
        }
    }
}
