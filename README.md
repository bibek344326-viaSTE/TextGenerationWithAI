# TextGenerationWithAI

A .NET 9 Web API project for generating AI text using Mistral API with caching and SQLite database support.

---

## Table of Contents

* [Features](#features)
* [Prerequisites](#prerequisites)
* [Setup](#setup)
* [Configuration](#configuration)
* [Usage](#usage)
* [API Endpoints](#api-endpoints)
* [Logging](#logging)
* [Caching](#caching)
* [Database](#database)

---

## Features

* Generate AI text using Mistral API
* Cache responses in memory for faster retrieval
* Store generated texts in SQLite database
* Retrieve history of generated texts
* Centralized logging using Serilog

---

## Prerequisites

* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [SQLite](https://www.sqlite.org/download.html)
* Mistral API key

---

## Setup

1. Clone the repository:

```bash
git clone https://github.com/bibek344326-viaSTE/TextGenerationWithAI
cd TextGenerationWithAI
```

2. Install dependencies:

```bash
dotnet restore
```

3. Update the database:

```bash
dotnet ef database update
```

---

## Configuration

Create/Update `appsettings.json` file in the root directory.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=generated_text.db"
  },
  "Mistral": {
    "ApiKey": "YOUR_MISTRAL_API_KEY",
    "BaseUrl": "https://api.mistral.ai/v1/chat/completions",
    "Model": "mistral-large-latest"
  }
}
```

* **ApiKey**: Your Mistral API key.
* **BaseUrl**: Mistral API endpoint.
* **Model**: Model name to use (default: `mistral-large-latest`).

---

## Usage

1. Run the API:

```bash
dotnet run
```

2. Access Swagger UI for API documentation at:

```
https://localhost:{PORT}/swagger
```

---

## API Endpoints

### Generate Text

* **POST** `/TextGeneration/generate`
* **Body**:

```json
{
  "model": "{selected-model}",
  "prompt": "sample text example"
}
```

* **Response**:

```json
{
  "text": "Generated AI response here..."
}
```

### Get All Generated Texts

* **GET** `/TextGeneration/history`
* **Response**:

```json
[
  {
    "id": 1,
    "prompt": "Hello, AI!",
    "response": "Generated AI response",
    "createdAt": "2025-08-21T12:34:56Z"
  }
]
```

---

## Logging

* Uses `Serilog` to log

---

## Caching

* Responses are cached in memory for 10 minutes
* Cache is automatically cleared when the service is restarted

---

## Database

* SQLite database (`generated_text.db`)
* Table: `GeneratedText`
* Columns:

  * `Id` (int, PK)
  * `Prompt` (string)
  * `Response` (string)
  * `CreatedAt` (datetime)
* Use `dotnet ef migrations add InitialCreate` and `dotnet ef database update` to create/update schema

---

## Future Updates (Possibly)
* AI Model Selection (Partially Completed)
* UI Based Application
