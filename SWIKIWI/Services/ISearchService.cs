using SWIKIWI.Models;

namespace SWIKIWI.Services;

/// <summary>
/// Interfaccia per tutti i servizi di ricerca
/// </summary>
public interface ISearchService
{
    string Name { get; }
    bool IsEnabled { get; }
    Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
