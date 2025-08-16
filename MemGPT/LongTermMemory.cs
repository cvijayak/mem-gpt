namespace MemGPT
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Extensions;
    using Microsoft.Extensions.VectorData;
    using Microsoft.SemanticKernel.Connectors.Qdrant;
    using Qdrant.Client;
    using Qdrant.Client.Grpc;

    public class LongTermMemory(string userId, string collectionName, int capacity, QdrantClient qdrantClient, IEmbeddingService embeddingService) : ILongTermMemory
    {
        public async Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
        {
            using var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            using var collection = vectorStore.GetCollection<Guid, ChatMessage>(collectionName);
            await collection.UpsertAsync(chatMessage, cancellationToken);
        }

        public async Task<ChatMessage[]> GetAsync(CancellationToken cancellationToken)
        {
            var chatMessages = new List<ChatMessage>();
            PointId nextPageOffset = null;
            uint pageSize = 500;
            var fieldNames = typeof(ChatMessage)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(f =>
                {
                    var attr = f.GetCustomAttribute<VectorStoreDataAttribute>();
                    return attr?.StorageName ?? f.Name;
                })
                .ToList();

            var payloadSelector = new WithPayloadSelector { Include = new PayloadIncludeSelector() };
            payloadSelector.Include.Fields.AddRange(fieldNames);

            do
            {
                var response = await qdrantClient.ScrollAsync(collectionName: collectionName, 
                    limit: pageSize, 
                    offset: nextPageOffset,
                    payloadSelector: payloadSelector,
                    cancellationToken: cancellationToken);
                var messages = response.Result.Select(d => MapFieldExtensions.MapToChatMessage(d.Payload)).ToArray();
                chatMessages.AddRange(messages);

                nextPageOffset = response.NextPageOffset;
            } while (nextPageOffset != null);

            return chatMessages.ToArray();
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

            var chatMessages = new List<ChatMessage>();
            await foreach (var searchResult in searchResults)
            {
                if (searchResult.Score >= 0.7)
                {
                    chatMessages.Add(searchResult.Record);
                }
            }

            return chatMessages.ToArray();
        }
        
        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            int batchSize = 500;
            var toDelete = new List<Guid>(batchSize);
            bool hasMore = true;

            using var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            using var collection = vectorStore.GetCollection<Guid, ChatMessage>(collectionName);

            while (hasMore)
            {
                var retrievalOptions = new FilteredRecordRetrievalOptions<ChatMessage> { IncludeVectors = false };
                var messages = collection.GetAsync(f => f.UserId == userId, top: batchSize, options: retrievalOptions, cancellationToken: cancellationToken);

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