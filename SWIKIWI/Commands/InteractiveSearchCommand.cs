using System.CommandLine;
using SWIKIWI.Models;
using SWIKIWI.Services;
using Microsoft.Extensions.Logging;

namespace SWIKIWI.Commands;

/// <summary>
/// Comando per ricerca interattiva con selezione risultati
/// </summary>
public class InteractiveSearchCommand
{
    private readonly SearchEngineService _searchEngine;
    private readonly ILogger<InteractiveSearchCommand> _logger;

    public InteractiveSearchCommand(SearchEngineService searchEngine, ILogger<InteractiveSearchCommand> logger)
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
            description: "Fonte specifica da utilizzare");

        var command = new Command("isearch", "Ricerca interattiva con selezione dei risultati")
        {
            queryArgument,
            sourceOption
        };

        command.SetHandler(async (query, source) =>
        {
            await ExecuteInteractiveSearchAsync(query, source);
        }, queryArgument, sourceOption);

        return command;
    }

    private async Task ExecuteInteractiveSearchAsync(string query, string? source)
    {
        try
        {
            Console.WriteLine($"🔍 Ricerca interattiva per: \"{query}\"");
            Console.WriteLine();

            var results = await _searchEngine.SearchAsync(query, source, limit: 10);
            
            if (!results.Any())
            {
                Console.WriteLine("❌ Nessun risultato trovato per la query specificata.");
                return;
            }

            // Mostra risultati con numerazione
            var resultList = results.ToList();
            DisplayResultsForSelection(resultList);

            // Chiedi all'utente di selezionare
            var selectedResult = await PromptForSelectionAsync(resultList);
            
            if (selectedResult != null)
            {
                await ShowDetailedResultAsync(selectedResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca interattiva");
            Console.WriteLine($"❌ Errore durante la ricerca: {ex.Message}");
        }
    }

    private static void DisplayResultsForSelection(List<SearchResult> results)
    {
        Console.WriteLine("📋 Risultati trovati (seleziona un numero per i dettagli):");
        Console.WriteLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var summary = result.Summary.Length > 100 ? result.Summary[..97] + "..." : result.Summary;
            
            Console.WriteLine($"[{i + 1}] {result.Title}");
            Console.WriteLine($"    📍 {result.Source} ({result.Language.ToUpperInvariant()})");
            Console.WriteLine($"    📄 {summary}");
            Console.WriteLine();
        }
    }

    private static async Task<SearchResult?> PromptForSelectionAsync(List<SearchResult> results)
    {
        while (true)
        {
            Console.Write($"Seleziona un risultato (1-{results.Count}) o 'q' per uscire: ");
            
            var input = await Task.Run(() => Console.ReadLine());
            
            if (string.IsNullOrWhiteSpace(input))
                continue;
                
            if (input.ToLowerInvariant() == "q")
            {
                Console.WriteLine("👋 Uscita dalla ricerca interattiva.");
                return null;
            }

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= results.Count)
            {
                return results[selection - 1];
            }

            Console.WriteLine($"❌ Selezione non valida. Inserisci un numero tra 1 e {results.Count} o 'q' per uscire.");
            Console.WriteLine();
        }
    }

    private static async Task ShowDetailedResultAsync(SearchResult result)
    {
        Console.Clear();
        Console.WriteLine("🔍 DETTAGLI RISULTATO");
        Console.WriteLine("".PadRight(60, '='));
        Console.WriteLine();

        Console.WriteLine($"📰 TITOLO: {result.Title}");
        Console.WriteLine($"📍 FONTE: {result.Source} ({result.Language.ToUpperInvariant()})");
        Console.WriteLine($"🌐 URL: {result.Url}");
        Console.WriteLine($"⭐ RILEVANZA: {result.RelevanceScore:F2}");
        Console.WriteLine($"🕒 RECUPERATO: {result.RetrievedAt:dd/MM/yyyy HH:mm:ss}");
        Console.WriteLine();

        Console.WriteLine("📄 RIASSUNTO:");
        Console.WriteLine("".PadRight(40, '-'));
        Console.WriteLine(WrapText(result.Summary, 80));
        Console.WriteLine();

        // Mostra metadata se disponibili
        if (result.Metadata.Any())
        {
            Console.WriteLine("📊 INFORMAZIONI AGGIUNTIVE:");
            Console.WriteLine("".PadRight(40, '-'));
            
            foreach (var metadata in result.Metadata)
            {
                if (!string.IsNullOrEmpty(metadata.Value?.ToString()))
                {
                    Console.WriteLine($"• {metadata.Key}: {metadata.Value}");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("⚡ AZIONI DISPONIBILI:");
        Console.WriteLine("  [o] Apri URL nel browser");
        Console.WriteLine("  [c] Copia URL negli appunti");
        Console.WriteLine("  [s] Cerca argomenti correlati");
        Console.WriteLine("  [b] Torna ai risultati");
        Console.WriteLine("  [q] Esci");
        Console.WriteLine();

        await HandleDetailActionsAsync(result);
    }

    private static async Task HandleDetailActionsAsync(SearchResult result)
    {
        while (true)
        {
            Console.Write("Scegli un'azione: ");
            var action = await Task.Run(() => Console.ReadKey(true));
            Console.WriteLine();

            switch (action.KeyChar)
            {
                case 'o':
                case 'O':
                    await OpenUrlAsync(result.Url);
                    break;
                    
                case 'c':
                case 'C':
                    await CopyToClipboardAsync(result.Url);
                    break;
                    
                case 's':
                case 'S':
                    await SearchRelatedAsync(result);
                    return;
                    
                case 'b':
                case 'B':
                    Console.WriteLine("🔙 Tornando ai risultati...");
                    return;
                    
                case 'q':
                case 'Q':
                    Console.WriteLine("👋 Uscita dall'applicazione.");
                    Environment.Exit(0);
                    break;
                    
                default:
                    Console.WriteLine("❌ Azione non riconosciuta. Riprova.");
                    break;
            }
        }
    }

    private static async Task OpenUrlAsync(string url)
    {
        try
        {
            if (!string.IsNullOrEmpty(url))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                Console.WriteLine("🌐 URL aperto nel browser predefinito.");
            }
            else
            {
                Console.WriteLine("❌ URL non disponibile.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore nell'apertura dell'URL: {ex.Message}");
        }
        
        await Task.Delay(1000);
    }

    private static async Task CopyToClipboardAsync(string text)
    {
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Su Windows
                if (OperatingSystem.IsWindows())
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c echo {text} | clip",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    await process.WaitForExitAsync();
                    Console.WriteLine("📋 URL copiato negli appunti.");
                }
                else
                {
                    Console.WriteLine($"📋 URL: {text}");
                    Console.WriteLine("   (copia manualmente - clipboard automatico non supportato su questa piattaforma)");
                }
            }
            else
            {
                Console.WriteLine("❌ Nessun testo da copiare.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore nella copia: {ex.Message}");
        }
        
        await Task.Delay(1000);
    }

    private static async Task SearchRelatedAsync(SearchResult result)
    {
        Console.WriteLine();
        Console.WriteLine("🔍 Ricerca argomenti correlati...");
        Console.WriteLine("💡 Suggerimenti basati sul titolo:");
        
        // Estrai parole chiave dal titolo
        var keywords = result.Title
            .Split(new[] { ' ', '-', '(', ')', '[', ']', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(3);
            
        foreach (var keyword in keywords)
        {
            Console.WriteLine($"  • {keyword}");
        }
        
        Console.WriteLine();
        Console.Write("Inserisci un nuovo termine di ricerca (o Enter per usare il titolo): ");
        
        var newQuery = await Task.Run(() => Console.ReadLine());
        if (string.IsNullOrWhiteSpace(newQuery))
        {
            newQuery = result.Title;
        }
        
        Console.WriteLine($"🔄 Avvio nuova ricerca per: \"{newQuery}\"");
        await Task.Delay(1000);
        
        // Qui dovremmo rilanciare la ricerca - per ora mostriamo un messaggio
        Console.WriteLine("💡 Riavvia il comando con la nuova query per continuare la ricerca.");
    }

    private static string WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxWidth)
            return text;

        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 <= maxWidth)
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + word;
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    lines.Add(word);
                }
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
