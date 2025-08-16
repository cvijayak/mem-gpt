# MemGPT

MemGPT is a memory-augmented AI system designed to manage and retrieve information efficiently using both short-term and long-term memory modules. It leverages Azure AI Search for vector embeddings storage and retrieval, enabling advanced conversational and knowledge management capabilities.

## What is MemGPT?
MemGPT is a C#/.NET application that provides:
- Short-term and long-term memory management for AI agents
- Embedding service integration with Azure AI Search
- Modular architecture for extensibility
- REST API for chat/message operations

## Azure AI Search Setup
Azure AI Search is used for vector storage and retrieval. You'll need to set up an Azure AI Search service in your Azure account:

1. Create an Azure AI Search service in the Azure Portal
2. Get your search service endpoint and API key
3. Update the configuration in `Program.cs` with your Azure AI Search details

## How to Run MemGPT
1. Ensure .NET 9.0 SDK is installed.
2. Configure Azure AI Search endpoint and API key in `Program.cs`.
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