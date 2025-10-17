# RAGrimosa

RAGrimosa is a .NET 8 console application that ingests local text documents into a pgvector-backed Postgres database and answers user questions using OpenAI chat and embedding models.

## Prerequisites
- Docker and Docker Compose (v2) installed locally
- An OpenAI API key configured in `RAGrimosa/appsettings.json`

The default configuration in `RAGrimosa/appsettings.json` targets the `db` hostname that Docker Compose provisions. If you run the app outside Docker, switch that host back to `localhost`.

## Run with Docker Compose
1. Build the application image and start an interactive session:
   ```bash
   docker compose run --rm --build app
   ```
   Compose will automatically provision the pgvector-enabled Postgres service and run the console app.

2. Wait for ingestion to complete. When you see `user >`, type your question and press Enter.

3. To exit, press Enter on an empty line or press `Ctrl+C`. Because `--rm` is used, the container is removed automatically once the session ends.

4. When you no longer need the database container, stop and remove Compose resources:
   ```bash
   docker compose down
   ```

## Repository Structure
```
Dockerfile                 # Multi-stage build for the .NET console app
RAGrimosa/                 # Application source, configuration, and data
  appsettings.json         # Primary configuration (OpenAI, Postgres, ingestion, RAG)
  data/source.txt          # Sample document ingested on startup
  ...
docker-compose.yml         # Orchestrates Postgres (pgvector) and the app container
docker/                    # Docker-related assets
  postgres/initdb.d/       # SQL scripts, e.g., pgvector extension creation
```

## Notes
- `docker compose run` keeps STDIN attached so you can interact with the console application.
- Modify `RAGrimosa/appsettings.json` to point at your own data, change chunking parameters, or adjust RAG behavior.
