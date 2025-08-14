# MemGPT

MemGPT is a memory-augmented AI system designed to manage and retrieve information efficiently using both short-term and long-term memory modules. It leverages vector databases like Qdrant for embedding storage and retrieval, enabling advanced conversational and knowledge management capabilities.

## What is MemGPT?
MemGPT is a C#/.NET application that provides:
- Short-term and long-term memory management for AI agents
- Embedding service integration (e.g., with Qdrant)
- Modular architecture for extensibility
- REST API for chat/message operations

## How to Run Qdrant
Qdrant is an open-source vector database used for storing and searching embeddings. You can run Qdrant locally using Docker:

```powershell
# Pull the latest Qdrant image
docker pull qdrant/qdrant

# Run Qdrant container
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

Qdrant REST API will be available at `http://localhost:6333` and gRPC API at `http://localhost:6334`.

## How to Run MemGPT
1. Ensure .NET 9.0 SDK is installed.
2. Start Qdrant (see above).
3. Build and run the MemGPT project:

```powershell
cd MemGPT
# Restore dependencies
dotnet restore
# Build the project
dotnet build
# Run the application
dotnet run
```

## API Endpoints
MemGPT exposes REST endpoints for chat and memory operations. See `ChatController.cs` for details.


## Example: Executing the Chat Controller
You can interact with MemGPT's chat endpoint using tools like Postman or curl. Below is an example using Postman:

**Endpoint:**
```
POST https://localhost:4356/api/chat
Content-Type: application/json
```

**Request Body:**
```json
{
  "UserId": "b5f7cd4f-7078-4e5e-b252-639861f754a8",
  "Message": "Summurize what we discussed"
}
```

**Response:**
```
HTTP/1.1 200 OK
We discussed your request for a detailed document featuring budget-friendly beachside hotels and resorts in Goa within your budget of ₹10,000 per night. I shared a preliminary overview of several options near popular beaches, including their rates and amenities. I am currently finalizing a comprehensive document with photos and detailed descriptions, which I will share with you shortly. Thank you for your patience throughout this process.
```