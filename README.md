# RAGrimosa

RAGrimosa is a .NET 8 console application that ingests local text documents into a pgvector-enabled Postgres database and answers user questions using OpenAI chat and embedding models.

## Prerequisites
- Docker Desktop (or Docker Engine) with Docker Compose
- An OpenAI API key configured in `RAGrimosa/appsettings.json`

> **Note:** The default `appsettings.json` targets the `db` hostname that Docker Compose provisions. When running the app outside of Docker, switch that host back to `localhost` (or whatever matches your environment).

## Configure the App
1. Open `RAGrimosa/appsettings.json`.
2. Make sure the `OpenAI` section contains a valid API key and the models you want to use.
3. Adjust the `Ingestion` and `Rag` sections as needed (input file path, chunking parameters, etc.).

## Run the Interactive Console via Docker Compose
1. (Optional) Pre-build the application image:
   ```bash
   docker compose build app
   ```
2. Launch the console app with STDIN attached (Compose will auto-start Postgres on demand):
   ```bash
   docker compose run --rm --build app
   ```
3. Watch the logs until ingestion finishes. When the prompt switches to `user >`, type your question and press Enter.
4. Submit an empty line or press `Ctrl+C` to end the session. Because `--rm` is used, the app container is removed automatically when you exit.

## Clean Up
When you are finished working with the project, stop the database container and remove Compose resources:
```bash
docker compose down
```

## Repository Structure
```
Dockerfile                 # Multi-stage build for the .NET console app
RAGrimosa/                 # Application source, configuration, and sample data
  appsettings.json         # Primary configuration (OpenAI, Postgres, ingestion, RAG)
  data/source.txt          # Example document ingested on startup
  ...
docker-compose.yml         # Orchestrates Postgres (pgvector) and the app container
docker/                    # Docker assets (pgvector extension setup, etc.)
  postgres/initdb.d/
```

## Troubleshooting
- `permission denied while trying to connect to the Docker daemon socket`: run the Compose command with sufficient privileges or from a user that can access the Docker daemon.
- `OpenAI__ApiKey cannot be empty`: double-check that the `OpenAI:ApiKey` value is set in `RAGrimosa/appsettings.json` before starting the container.
