# Guida alle Fonti Personalizzate - SWIKIWI

## 📋 Panoramica

SWIKIWI supporta l'aggiunta di fonti API personalizzate attraverso il file `config.json`. Questa guida spiega come configurare nuove fonti e mappare i campi API ai risultati di ricerca di SWIKIWI.

## ⚙️ Configurazione di una Fonte Personalizzata

### Struttura Base

```json
{
  "customApiSources": [
    {
      "name": "Nome della Fonte",
      "searchEndpoint": "https://api.esempio.com/search",
      "enabled": true,
      "language": "it",
      "searchQueryParam": "q",
      "responseDataPath": "results",
      "fieldMapping": {
        "titleField": "nome_campo_titolo",
        "summaryField": "nome_campo_descrizione",
        "urlField": "nome_campo_url"
      }
    }
  ]
}
```

### Parametri Principali

| Campo | Descrizione | Esempio |
|-------|-------------|---------|
| `name` | Nome identificativo della fonte | `"NewsAPI"` |
| `searchEndpoint` | URL dell'endpoint di ricerca | `"https://newsapi.org/v2/everything"` |
| `enabled` | Se la fonte è attiva | `true` |
| `language` | Codice lingua dei risultati | `"it"`, `"en"` |
| `searchQueryParam` | Nome del parametro query nell'URL | `"q"`, `"search"` |
| `responseDataPath` | Percorso JSON ai risultati | `"articles"`, `"data.results"` |
| `maxResults` | Numero massimo di risultati | `5` |

### Mapping dei Campi

Il `fieldMapping` traduce i campi dell'API esterna nei campi standard di SWIKIWI:

```json
{
  "fieldMapping": {
    "titleField": "title",           // Campo titolo nell'API
    "summaryField": "description",   // Campo descrizione/riassunto
    "urlField": "url",              // Campo URL
    "thumbnailField": "image",      // Campo immagine/thumbnail
    "customFields": {               // Campi personalizzati aggiuntivi
      "author": "author",
      "date": "publishedAt",
      "category": "section"
    }
  }
}
```

### Parametri Query Aggiuntivi

```json
{
  "queryParameters": {
    "apiKey": "la-tua-api-key",
    "sortBy": "relevancy",
    "language": "it"
  }
}
```

### Headers Personalizzati

```json
{
  "headers": {
    "Authorization": "Bearer token",
    "Accept": "application/json"
  }
}
```

## 📚 Esempi Pratici

### 1. NewsAPI

```json
{
  "name": "NewsAPI",
  "searchEndpoint": "https://newsapi.org/v2/everything",
  "enabled": true,
  "language": "it",
  "searchQueryParam": "q",
  "responseDataPath": "articles",
  "maxResults": 5,
  "fieldMapping": {
    "titleField": "title",
    "summaryField": "description",
    "urlField": "url",
    "thumbnailField": "urlToImage",
    "customFields": {
      "author": "author",
      "publishedAt": "publishedAt",
      "source": "source.name"
    }
  },
  "queryParameters": {
    "apiKey": "YOUR_API_KEY",
    "sortBy": "relevancy"
  }
}
```

### 2. OpenLibrary

```json
{
  "name": "OpenLibrary",
  "searchEndpoint": "https://openlibrary.org/search.json",
  "enabled": true,
  "language": "en",
  "searchQueryParam": "q",
  "responseDataPath": "docs",
  "maxResults": 5,
  "fieldMapping": {
    "titleField": "title",
    "summaryField": "first_sentence",
    "urlField": "key",
    "customFields": {
      "author": "author_name",
      "year": "first_publish_year"
    }
  }
}
```

### 3. API con Nested Data

```json
{
  "name": "API Complessa",
  "searchEndpoint": "https://api.esempio.com/search",
  "responseDataPath": "data.results.items",
  "fieldMapping": {
    "titleField": "content.title",
    "summaryField": "content.body",
    "urlField": "links.self",
    "customFields": {
      "category": "metadata.category",
      "score": "relevance.score"
    }
  }
}
```

## 🔧 Test e Debug

### 1. Verificare la Configurazione

```bash
dotnet run -- config show
```

### 2. Testare una Fonte Specifica

```bash
dotnet run -- search "test" --source "Nome Fonte"
```

### 3. Verificare lo Stato delle Fonti

```bash
dotnet run -- config status
```

## ⚠️ Note Importanti

1. **API Keys**: Non committare mai le API key nel repository. Usa variabili d'ambiente o file di configurazione locali.

2. **Rate Limiting**: Molte API hanno limiti di richieste. Configura `timeoutSeconds` appropriatamente.

3. **CORS**: Le API pubbliche potrebbero non funzionare direttamente da browser a causa delle policy CORS.

4. **Formato Risposta**: L'API deve restituire JSON. XML e altri formati non sono supportati.

5. **Percorsi Nested**: Usa la notazione punto per accedere a campi nested: `"data.results.title"`

## 🚀 Esempi di Utilizzo

```bash
# Ricerca normale con tutte le fonti
dotnet run -- search "intelligenza artificiale"

# Ricerca interattiva
dotnet run -- isearch "machine learning"

# Ricerca solo su fonte personalizzata
dotnet run -- search "news" --source "NewsAPI"

# Abilitare una nuova fonte
dotnet run -- config enable "OpenLibrary"
```

## 📝 Template Rapido

Copia questo template per aggiungere rapidamente una nuova fonte:

```json
{
  "name": "NOME_FONTE",
  "searchEndpoint": "URL_API",
  "enabled": false,
  "language": "it",
  "searchQueryParam": "q",
  "responseDataPath": "",
  "maxResults": 5,
  "fieldMapping": {
    "titleField": "title",
    "summaryField": "description",
    "urlField": "url"
  },
  "queryParameters": {},
  "headers": {}
}
```
