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

        configCommand.Add(showCommand);
        configCommand.Add(enableCommand);
        configCommand.Add(disableCommand);
        configCommand.Add(statusCommand);

        return configCommand;
    }

    private async Task ShowConfigurationAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();

            Console.WriteLine("⚙️  Configurazione SWIKIWI");
            Console.WriteLine("=========================");
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
