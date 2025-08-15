namespace MemGPT
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Microsoft.Extensions.VectorData;
    using Microsoft.SemanticKernel.Connectors.Qdrant;
    using Qdrant.Client;

    public class LongTermMemory(string userId, string collectionName, int capacity, QdrantClient qdrantClient, IEmbeddingService embeddingService) : ILongTermMemory
    {
        public async Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
        {
            using var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            using var collection = vectorStore.GetCollection<Guid, ChatMessage>(collectionName);
            await collection.UpsertAsync(chatMessage, cancellationToken);
        }

        public async Task<ChatMessage[]> SearchAsync(string text, CancellationToken cancellationToken)
        {
            using var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            using var collection = vectorStore.GetCollection<Guid, ChatMessage>(collectionName);
            var queryVector = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
            var searchResults = collection.SearchAsync(queryVector, capacity, new VectorSearchOptions<ChatMessage>
            {
                IncludeVectors = false,
                Filter = f => f.UserId == userId
            }, cancellationToken);

            var hotels = new List<ChatMessage>();
            await foreach (var searchResult in searchResults)
            {
                if (searchResult.Score >= 0.7)
                {
                    hotels.Add(searchResult.Record);
                }
            }

            return hotels.ToArray();
        }
        
        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            const int batchSize = 500;
            var toDelete = new List<Guid>(batchSize);
            bool hasMore = true;

            using var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            using var collection = vectorStore.GetCollection<Guid, ChatMessage>(collectionName);

            while (hasMore)
            {
                var messages = collection.GetAsync(f => f.UserId == userId, top: batchSize, options: new FilteredRecordRetrievalOptions<ChatMessage>
                {
                    IncludeVectors = false,
                }, cancellationToken: cancellationToken);

                toDelete.Clear();
                await foreach (var message in messages)
                {
                    toDelete.Add(message.Id);
                }

                if (toDelete.Count > 0)
                {
                    var uniqueIds = toDelete.Distinct().ToList();
                    await collection.DeleteAsync(uniqueIds, cancellationToken);
                }
                else
                {
                    hasMore = false;
                }

                await Task.Delay(100, cancellationToken);
            }
        }
    }
}