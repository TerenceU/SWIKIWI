# SWIKIWI - Smart Wiki Information Search Tool

## 📋 Descrizione

SWIKIWI è uno strumento CLI (Command Line Interface) sviluppato in C# che permette di cercare informazioni da fonti affidabili e configurabili su internet. Il nome deriva da "Smart Wiki" e fornisce un'interfaccia unificata per accedere a diverse fonti di conoscenza.

## ✨ Funzionalità

- **Ricerca Multi-fonte**: Cerca simultaneamente su Wikipedia (IT/EN), Britannica e altre fonti configurabili
- **Ricerca Interattiva**: Seleziona risultati per approfondimenti con comando `isearch`
- **Fonti API Personalizzate**: Aggiungi qualsiasi API REST con mapping personalizzato dei campi
- **Configurazione Flessibile**: Personalizza le fonti di ricerca tramite file JSON
- **Interfaccia CLI Intuitiva**: Comandi semplici e output formattato
- **Supporto Multilingua**: Ricerca in italiano e inglese
- **Risultati Strutturati**: Output organizzato e leggibile
- **Azioni sui Risultati**: Apri URL, copia link, cerca argomenti correlati

## 🚀 Installazione

### Prerequisiti
- .NET 6.0 o superiore
- Connessione internet attiva

### Installazione come Global Tool (Raccomandato)

SWIKIWI può essere installato come .NET Global Tool e sarà disponibile da qualsiasi cartella:

```bash
# Installa direttamente dal codice sorgente (per sviluppatori)
git clone https://github.com/TerenceU/SWIKIWI
cd SWIKIWI/SWIKIWI
dotnet pack --configuration Release
dotnet tool install --global --add-source ./bin/Release SWIKIWI

# Oppure installa dal NuGet (quando sarà pubblicato)
dotnet tool install --global SWIKIWI
```

⚠️ **Nota**: Se dovessi avere errori di sources NuGet non autorizzate, disabilita temporaneamente le sources problematiche:
```bash
dotnet nuget list source  # Visualizza tutte le sources
dotnet nuget disable source "NomeSourceProblematica"  # Disabilita se necessario
```

### Verifica Installazione

Dopo l'installazione, il comando `swikiwi` sarà disponibile globalmente:

```bash
swikiwi --help
```

### Disinstallazione

Per rimuovere il tool:

```bash
dotnet tool uninstall --global SWIKIWI
```

### Compilazione Manuale (Sviluppatori)

```bash
git clone https://github.com/TerenceU/SWIKIWI
cd SWIKIWI
dotnet build
dotnet run -- search "test"
```

## 📖 Utilizzo

### Comandi Base

```bash
# Ricerca semplice
swikiwi search "argomento da cercare"

# Ricerca interattiva (con selezione risultati)
swikiwi isearch "argomento da cercare"

# Ricerca con fonte specifica
swikiwi search "argomento" --source wikipedia

# Mostra configurazione
swikiwi config show

# Abilita/disabilita una fonte
swikiwi config enable wikipedia
swikiwi config disable britannica

# Verifica stato fonti
swikiwi config status

# Aiuto
swikiwi --help
```

### Esempi di Utilizzo

```bash
# Cerca informazioni su Leonardo da Vinci
swikiwi search "Leonardo da Vinci"

# Ricerca interattiva con selezione risultati
swikiwi isearch "Artificial Intelligence"

# Cerca solo su Wikipedia inglese
swikiwi search "Artificial Intelligence" --source "Wikipedia EN"

# Cerca con limite di risultati
swikiwi search "Roma" --limit 5

# Ricerca dettagliata con tutte le fonti
swikiwi search "Quantum Computing" --detailed
```

## ⚙️ Configurazione

Il file `config.json` permette di personalizzare:

- **Fonti di ricerca**: Aggiungi o rimuovi sorgenti
- **Parametri di ricerca**: Timeout, numero massimo risultati
- **User Agent**: Personalizza l'identificazione del client
- **Selettori CSS**: Per fonti che richiedono web scraping

### Esempio config.json
```json
{
  "sources": [
    {
      "name": "Wikipedia",
      "url": "https://it.wikipedia.org/wiki/",
      "enabled": true,
      "language": "it"
    }
  ],
  "settings": {
    "maxResults": 10,
    "timeout": 30
  }
}
```

### 🔧 Fonti API Personalizzate

SWIKIWI supporta l'aggiunta di fonti API personalizzate tramite configurazione JSON. Puoi aggiungere qualsiasi API REST che restituisca dati strutturati.

#### Configurazione di una fonte personalizzata

```json
{
  "customApiSources": [
    {
      "name": "Nome della tua API",
      "searchEndpoint": "https://api.example.com/search",
      "enabled": true,
      "language": "en",
      "searchQueryParam": "q",
      "responseDataPath": "results",
      "maxResults": 5,
      "fieldMapping": {
        "titleField": "title",
        "summaryField": "description", 
        "urlField": "url",
        "thumbnailField": "image",
        "customFields": {
          "author": "author",
          "date": "publishedAt"
        }
      },
      "queryParameters": {
        "apiKey": "YOUR_API_KEY",
        "format": "json"
      },
      "headers": {
        "Authorization": "Bearer YOUR_TOKEN"
      }
    }
  ]
}
```

#### Campi di configurazione:

- **searchEndpoint**: URL dell'endpoint di ricerca
- **searchQueryParam**: Nome del parametro per la query (default: "q")
- **responseDataPath**: Percorso JSONPath per i risultati (es. "data.results")
- **fieldMapping**: Mappatura tra campi API e nostri campi standard
- **queryParameters**: Parametri fissi da aggiungere alla query
- **headers**: Headers HTTP personalizzati

#### Esempi di fonti supportate:

- **NewsAPI**: Per articoli di news
- **OpenLibrary**: Per ricerca libri
- **GitHub API**: Per repository
- **JSONPlaceholder**: Per test
- Qualsiasi API REST che restituisca JSON

Vedi `config.example.json` per esempi completi di configurazione.

## 🏗️ Architettura

### Struttura del Progetto
```
SWIKIWI/
├── Program.cs              # Entry point dell'applicazione
├── Models/                 # Modelli di dati
│   ├── SearchResult.cs
│   ├── SearchSource.cs
│   └── Configuration.cs
├── Services/               # Servizi di ricerca
│   ├── WikipediaService.cs
│   ├── WebScrapingService.cs
│   └── ConfigurationService.cs
├── Commands/               # Comandi CLI
│   ├── SearchCommand.cs
│   └── ConfigCommand.cs
└── config.json            # File di configurazione
```

### Componenti Principali

1. **SearchEngine**: Motore principale di ricerca
2. **SourceManager**: Gestisce le diverse fonti di dati
3. **ResultFormatter**: Formatta l'output per la visualizzazione
4. **ConfigurationManager**: Gestisce le impostazioni

## 🔧 Fonti Supportate

### Wikipedia
- API ufficiale di Wikipedia
- Supporto multilingua (IT, EN)
- Ricerca semantica avanzata

### Britannica
- Web scraping del sito Britannica
- Contenuti di alta qualità
- Ricerca per argomenti specifici

### Fonti Personalizzate
- Aggiungi facilmente nuove fonti
- Configurazione tramite JSON
- Supporto per API REST e web scraping

## 🤝 Contributi

1. Fork del repository
2. Crea un branch per la tua feature (`git checkout -b feature/AmazingFeature`)
3. Commit delle modifiche (`git commit -m 'Add some AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri una Pull Request

## 📝 TODO / Roadmap

### ✅ Versione 1.0 (Completato)
- [x] Architettura base con Models, Services, Commands
- [x] Integrazione Wikipedia IT/EN con API ufficiali
- [x] Sistema di configurazione JSON
- [x] CLI completa con System.CommandLine
- [x] Ricerca interattiva con selezione risultati
- [x] Fonti API personalizzate configurabili
- [x] Mapping flessibile campi API → SearchResult
- [x] Azioni sui risultati (apri URL, copia, ricerca correlata)

### Versione 1.1
- [ ] Supporto per ricerca vocale
- [ ] Export risultati in PDF/HTML
- [ ] Cache distribuita
- [ ] Britannica web scraping
- [ ] Più esempi di fonti personalizzate

### Versione 1.2
- [ ] Interfaccia web opzionale
- [ ] Supporto per plugin personalizzati
- [ ] Analisi sentiment dei risultati
- [ ] Machine learning per ranking risultati
- [ ] Ricerca full-text su contenuti cached

### Versione 2.0
- [ ] Supporto per domande in linguaggio naturale
- [ ] Integrazione AI per riassunti automatici
- [ ] Modalità conversazionale
- [ ] API REST per integrazione esterna
- [ ] Dashboard web per gestione configurazioni

## 🐛 Bug Reports & Feature Requests

Utilizza la sezione Issues di GitHub per:
- Segnalare bug
- Richiedere nuove funzionalità
- Proporre miglioramenti
- Discutere l'architettura

## 📄 Licenza

Questo progetto è rilasciato sotto licenza MIT. Vedi il file `LICENSE` per maggiori dettagli.

## 👤 Autore

**Teren** - *Sviluppatore principale*

## 🙏 Ringraziamenti

- Wikipedia per l'API pubblica
- Britannica per i contenuti di qualità
- Community .NET per gli strumenti eccellenti
- Tutti i contributori del progetto

---

**SWIKIWI** - *"Knowledge at your fingertips"* 🧠✨
