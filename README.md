# RAGrimosa

RAGrimosa is a lightweight RAG console app built to answer questions about Lacrimosa.  
The story can be found in `data/source.txt`.

The orchestrator ingests Lacrimosa's narrative into a vector store, retrieves the most relevant chunks for each user question, and streams answers from an OpenAI chat model.

---

## About

This project was created as a **demo for the dotnet.ge community talk**  - Building a RAG System with Microsoft.Extensions.VectorData.  
It demonstrates how to integrate OpenAI models, PostgreSQL with pgvector, and the new `Microsoft.Extensions.AI` abstractions to build a simple yet functional RAG pipeline in .NET.

---

## How it works
- Text ingestion splits `data/source.txt` into overlapping chunks and upserts them into a pgvector-backed PostgreSQL collection.
- At runtime, the app performs semantic search over the stored embeddings to assemble context for each prompt.
- A system prompt and the retrieved snippets are sent to the configured OpenAI chat model to generate replies.

---

## Tech stack
- .NET 8 console host with `Microsoft.Extensions.AI` for chat and embedding pipelines.  
- OpenAI Chat & Embeddings APIs via `Microsoft.Extensions.AI.OpenAI`.  
- PostgreSQL + pgvector using `Microsoft.SemanticKernel.Connectors.PgVector` and `Npgsql`.  
- Vector store abstractions using `Microsoft.Extensions.VectorData`.

---

## Getting started
1. Ensure PostgreSQL with the pgvector extension is available and reachable from your machine.  
2. Update `appsettings.json` with your OpenAI API key, model IDs, Postgres connection string, and ingestion settings.  
3. Ask Lacrimosa-themed questions in the terminal; press Enter on an empty line to exit.

---

## License

This project is licensed under the **MIT License** — you are free to use, modify, and distribute it with attribution.
