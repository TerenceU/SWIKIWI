# Guida alle Fonti API Personalizzate

Questa guida spiega come aggiungere fonti API personalizzate a SWIKIWI per estendere le capacità di ricerca oltre Wikipedia.

## Panoramica

SWIKIWI può interfacciarsi con qualsiasi API REST che restituisca dati in formato JSON. Il sistema di mapping flessibile permette di tradurre i campi specifici dell'API nei campi standard di SWIKIWI.

## Struttura di Configurazione

```json
{
  "customApiSources": [
    {
      "name": "Nome Fonte",
      "searchEndpoint": "URL_ENDPOINT",
      "enabled": true,
      "language": "it|en",
      "searchQueryParam": "q",
      "responseDataPath": "path.to.results",
      "maxResults": 5,
      "fieldMapping": { /* mappatura campi */ },
      "queryParameters": { /* parametri fissi */ },
      "headers": { /* headers HTTP */ }
    }
  ]
}
```

## Esempi Pratici

### 1. NewsAPI - Articoli di News

```json
{
  "name": "NewsAPI",
  "searchEndpoint": "https://newsapi.org/v2/everything",
  "enabled": true,
  "language": "en",
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
      "sourceName": "source.name"
    }
  },
  "queryParameters": {
    "apiKey": "YOUR_API_KEY",
    "sortBy": "relevancy",
    "language": "en"
  }
}
```

### 2. OpenLibrary - Ricerca Libri

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
      "publishYear": "first_publish_year",
      "isbn": "isbn"
    }
  },
  "queryParameters": {
    "limit": "10"
  }
}
```

### 3. GitHub API - Repository

```json
{
  "name": "GitHub Repositories",
  "searchEndpoint": "https://api.github.com/search/repositories",
  "enabled": false,
  "language": "en",
  "searchQueryParam": "q",
  "responseDataPath": "items",
  "maxResults": 5,
  "fieldMapping": {
    "titleField": "name",
    "summaryField": "description",
    "urlField": "html_url",
    "customFields": {
      "owner": "owner.login",
      "stars": "stargazers_count",
      "language": "language",
      "updatedAt": "updated_at"
    }
  },
  "queryParameters": {
    "sort": "stars",
    "order": "desc"
  },
  "headers": {
    "Accept": "application/vnd.github.v3+json",
    "User-Agent": "SWIKIWI/1.0"
  }
}
```

## Mapping dei Campi

### Campi Standard

- **titleField**: Campo che contiene il titolo del risultato
- **summaryField**: Campo con la descrizione/riassunto
- **urlField**: Campo con l'URL della risorsa
- **thumbnailField**: Campo con l'URL dell'immagine di anteprima
- **languageField**: Campo che specifica la lingua (opzionale)

### Campi Personalizzati

I `customFields` permettono di mappare campi specifici dell'API che verranno aggiunti ai metadata del risultato.

```json
"customFields": {
  "nomeMetadata": "campo.api.path",
  "autore": "author.name",
  "data": "publishedAt"
}
```

## Percorsi JSON Annidati

Per API con strutture complesse, usa la notazione punto per navigare:

```json
// Per questa risposta API:
{
  "data": {
    "results": [
      {
        "article": {
          "title": "Titolo",
          "content": "Contenuto"
        }
      }
    ]
  }
}

// Usa questa configurazione:
"responseDataPath": "data.results",
"fieldMapping": {
  "titleField": "article.title",
  "summaryField": "article.content"
}
```

## Autenticazione

### API Key nei Query Parameters

```json
"queryParameters": {
  "apiKey": "YOUR_API_KEY",
  "api_key": "YOUR_API_KEY"
}
```

### Token nei Headers

```json
"headers": {
  "Authorization": "Bearer YOUR_TOKEN",
  "X-API-Key": "YOUR_API_KEY"
}
```

## Test e Debug

1. **Abilita la fonte**: Imposta `"enabled": true`
2. **Testa con ricerca semplice**: `swikiwi search "test" --source "Nome Fonte"`
3. **Verifica lo stato**: `swikiwi config status`
4. **Controlla i log**: I messaggi di debug mostrano le chiamate API

## Limitazioni e Considerazioni

- **Rate Limiting**: Rispetta i limiti delle API esterne
- **Timeout**: Configura timeout appropriati (default: 30s)
- **Error Handling**: SWIKIWI gestisce automaticamente errori HTTP
- **JSON Path**: Supporta solo percorsi semplici con notazione punto
- **Sicurezza**: Non salvare API key sensibili nel config.json in production

## Risoluzione Problemi

### La fonte non restituisce risultati

1. Verifica che `responseDataPath` punti al array corretto
2. Controlla che `fieldMapping` corrisponda alla struttura JSON
3. Verifica autenticazione e rate limits

### Campi vuoti o mancanti

1. Ispeziona la risposta JSON dell'API
2. Verifica i percorsi dei campi nel mapping
3. Usa `customFields` per campi opzionali

### Errori di timeout

1. Aumenta `timeoutSeconds`
2. Verifica la velocità dell'API esterna
3. Riduci `maxResults` per chiamate più veloci

## Contribuire con Nuove Fonti

Se crei configurazioni per fonti popolari, considera di condividerle creando un issue o pull request nel repository GitHub con esempi di configurazione pronti all'uso.
