# SWIKIWI - Smart Wiki Information Search Tool

## 📋 Descrizione

SWIKIWI è uno strumento CLI (Command Line Interface) sviluppato in C# che permette di cercare informazioni da fonti affidabili e configurabili su internet. Il nome deriva da "Smart Wiki" e fornisce un'interfaccia unificata per accedere a diverse fonti di conoscenza.

## ✨ Funzionalità

- **Ricerca Multi-fonte**: Cerca simultaneamente su Wikipedia (IT/EN), Britannica e altre fonti configurabili
- **Configurazione Flessibile**: Personalizza le fonti di ricerca tramite file JSON
- **Interfaccia CLI Intuitiva**: Comandi semplici e output formattato
- **Supporto Multilingua**: Ricerca in italiano e inglese
- **Risultati Strutturati**: Output organizzato e leggibile
- **Caching Intelligente**: Memorizza i risultati per ricerche più veloci

## 🚀 Installazione

### Prerequisiti
- .NET 6.0 o superiore
- Connessione internet attiva

### Compilazione
```bash
git clone <repository-url>
cd SWIKIWI
dotnet build
dotnet run
```

## 📖 Utilizzo

### Comandi Base

```bash
# Ricerca semplice
swikiwi search "argomento da cercare"

# Ricerca con fonte specifica
swikiwi search "argomento" --source wikipedia

# Mostra configurazione
swikiwi config show

# Abilita/disabilita una fonte
swikiwi config enable wikipedia
swikiwi config disable britannica

# Aiuto
swikiwi --help
```

### Esempi di Utilizzo

```bash
# Cerca informazioni su Leonardo da Vinci
swikiwi search "Leonardo da Vinci"

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

### Versione 1.1
- [ ] Supporto per ricerca vocale
- [ ] Export risultati in PDF/HTML
- [ ] Integrazione con più API di knowledge base
- [ ] Cache distribuita

### Versione 1.2
- [ ] Interfaccia web opzionale
- [ ] Supporto per plugin personalizzati
- [ ] Analisi sentiment dei risultati
- [ ] Machine learning per ranking risultati

### Versione 2.0
- [ ] Supporto per domande in linguaggio naturale
- [ ] Integrazione AI per riassunti automatici
- [ ] Modalità conversazionale
- [ ] API REST per integrazione esterna

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
