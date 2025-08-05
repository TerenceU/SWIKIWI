using System.CommandLine;
using SWIKIWI.Models;
using SWIKIWI.Services;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Commands;

/// <summary>
/// Comando per la ricerca
/// </summary>
public class SearchCommand
{
    private readonly SearchEngineService _searchEngine;
    private readonly ILogger<SearchCommand> _logger;

    public SearchCommand(SearchEngineService searchEngine, ILogger<SearchCommand> logger)
    {
        _searchEngine = searchEngine;
        _logger = logger;
    }

    public Command CreateCommand()
    {
        var queryArgument = new Argument<string>(
            name: "query",
            description: "Termine o frase da cercare");

        var sourceOption = new Option<string?>(
            name: "--source",
            description: "Fonte specifica da utilizzare (es. 'wikipedia', 'britannica')");

        var limitOption = new Option<int?>(
            name: "--limit",
            description: "Numero massimo di risultati da mostrare");

        var detailedOption = new Option<bool>(
            name: "--detailed",
            description: "Mostra informazioni dettagliate sui risultati");

        var formatOption = new Option<string>(
            name: "--format",
            getDefaultValue: () => "table",
            description: "Formato di output: table, json, plain");

        var command = new Command("search", "Cerca informazioni dalle fonti configurate")
        {
            queryArgument,
            sourceOption,
            limitOption,
            detailedOption,
            formatOption
        };

        command.SetHandler(async (query, source, limit, detailed, format) =>
        {
            await ExecuteSearchAsync(query, source, limit, detailed, format);
        }, queryArgument, sourceOption, limitOption, detailedOption, formatOption);

        return command;
    }

    private async Task ExecuteSearchAsync(string query, string? source, int? limit, bool detailed, string format)
    {
        try
        {
            Console.WriteLine($"🔍 Ricerca in corso per: \"{query}\"");
            
            if (!string.IsNullOrEmpty(source))
            {
                Console.WriteLine($"📚 Fonte: {source}");
            }
            
            Console.WriteLine();

            var results = await _searchEngine.SearchAsync(query, source, limit);
            
            if (!results.Any())
            {
                Console.WriteLine("❌ Nessun risultato trovato per la query specificata.");
                return;
            }

            DisplayResults(results, format, detailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca");
            Console.WriteLine($"❌ Errore durante la ricerca: {ex.Message}");
        }
    }

    private static void DisplayResults(IEnumerable<SearchResult> results, string format, bool detailed)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                DisplayAsJson(results);
                break;
            case "plain":
                DisplayAsPlain(results, detailed);
                break;
            case "table":
            default:
                DisplayAsTable(results, detailed);
                break;
        }
    }

    private static void DisplayAsTable(IEnumerable<SearchResult> results, bool detailed)
    {
        Console.WriteLine("📋 Risultati trovati:");
        Console.WriteLine();

        foreach (var (result, index) in results.Select((r, i) => (r, i + 1)))
        {
            Console.WriteLine($"🔸 [{index}] {result.Title}");
            Console.WriteLine($"   📍 Fonte: {result.Source} ({result.Language.ToUpperInvariant()})");
            
            if (detailed)
            {
                Console.WriteLine($"   🌐 URL: {result.Url}");
                Console.WriteLine($"   ⭐ Rilevanza: {result.RelevanceScore:F2}");
                Console.WriteLine($"   🕒 Recuperato: {result.RetrievedAt:HH:mm:ss}");
            }
            
            // Tronca il riassunto se troppo lungo
            var summary = result.Summary;
            if (summary.Length > 200 && !detailed)
            {
                summary = summary[..197] + "...";
            }
            
            Console.WriteLine($"   📄 {summary}");
            Console.WriteLine();
        }
    }

    private static void DisplayAsPlain(IEnumerable<SearchResult> results, bool detailed)
    {
        foreach (var result in results)
        {
            Console.WriteLine($"Titolo: {result.Title}");
            Console.WriteLine($"Fonte: {result.Source}");
            Console.WriteLine($"URL: {result.Url}");
            
            if (detailed)
            {
                Console.WriteLine($"Lingua: {result.Language}");
                Console.WriteLine($"Rilevanza: {result.RelevanceScore:F2}");
                Console.WriteLine($"Recuperato: {result.RetrievedAt}");
            }
            
            Console.WriteLine($"Riassunto: {result.Summary}");
            Console.WriteLine(new string('-', 80));
        }
    }

    private static void DisplayAsJson(IEnumerable<SearchResult> results)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        
        Console.WriteLine(json);
    }
}
