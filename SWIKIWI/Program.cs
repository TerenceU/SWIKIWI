using System.CommandLine;
using Microsoft.Extensions.Logging;
using SWIKIWI.Commands;
using SWIKIWI.Services;

namespace SWIKIWI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configurazione del logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Inizializzazione dei servizi
            var httpClient = new HttpClient();
            var configService = new ConfigurationService(
                loggerFactory.CreateLogger<ConfigurationService>());
            
            var searchEngine = new SearchEngineService(
                loggerFactory.CreateLogger<SearchEngineService>(),
                configService,
                httpClient);

            // Inizializzazione del motore di ricerca
            await searchEngine.InitializeAsync();

            // Creazione dei comandi
            var searchCommand = new SearchCommand(
                searchEngine,
                loggerFactory.CreateLogger<SearchCommand>());

            var configCommand = new ConfigCommand(
                configService,
                searchEngine,
                loggerFactory.CreateLogger<ConfigCommand>());
                
            var interactiveSearchCommand = new InteractiveSearchCommand(
                searchEngine,
                loggerFactory.CreateLogger<InteractiveSearchCommand>());

            // Creazione del comando root
            var rootCommand = new RootCommand("SWIKIWI - Smart Wiki Information Search Tool")
            {
                searchCommand.CreateCommand(),
                configCommand.CreateCommand(),
                interactiveSearchCommand.CreateCommand()
            };

            // Aggiunta del comando di aiuto globale
            rootCommand.Description = """
                🧠 SWIKIWI - Smart Wiki Information Search Tool
                
                Cerca informazioni da fonti affidabili come Wikipedia, Britannica e altre.
                
                Esempi di utilizzo:
                  swikiwi search "Leonardo da Vinci"
                  swikiwi isearch "Artificial Intelligence"    # Ricerca interattiva
                  swikiwi search "Roma" --source wikipedia --limit 5
                  swikiwi config show
                  swikiwi config enable "Wikipedia EN"
                """;

            // Se non ci sono argomenti, mostra l'aiuto
            if (args.Length == 0)
            {
                Console.WriteLine("🧠 SWIKIWI - Smart Wiki Information Search Tool");
                Console.WriteLine("===============================================");
                Console.WriteLine();
                Console.WriteLine("💡 Digita 'swikiwi --help' per vedere tutti i comandi disponibili");
                Console.WriteLine("💡 Esempio veloce: swikiwi search \"Leonardo da Vinci\"");
                Console.WriteLine();
                return 0;
            }

            // Esecuzione del comando
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Errore critico nell'applicazione");
            Console.WriteLine($"❌ Errore critico: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Dettaglio: {ex.InnerException.Message}");
            }
            
            return 1;
        }
    }
}
