namespace MemGPT
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contracts;
    using Microsoft.Extensions.VectorData;
    using Microsoft.SemanticKernel.Connectors.Qdrant;
    using Qdrant.Client;

    public class LongTermMemory : ILongTermMemory
    {
        private readonly VectorStoreCollection<Guid, ChatMessage> _collection;
        private readonly VectorStore _vectorStore;
        private readonly string _userId;
        private readonly int _capacity;
        private readonly IEmbeddingService _embeddingService;

        public LongTermMemory(string userId, string collectionName, int capacity, QdrantClient qdrantClient, IEmbeddingService embeddingService)
        {
            _userId = userId;
            _capacity = capacity;
            _embeddingService = embeddingService;
            _vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);
            _collection = _vectorStore.GetCollection<Guid, ChatMessage>(collectionName);
        }

        public async Task AddAsync(ChatMessage chatMessage) => await _collection.UpsertAsync(chatMessage);
        public async Task AddAsync(ChatMessage[] chatMessages) => await _collection.UpsertAsync(chatMessages);
        public async Task<ChatMessage[]> SearchAsync(string text)
        {
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(text);
            var searchResults = _collection.SearchAsync(queryVector, _capacity, new VectorSearchOptions<ChatMessage>
            {
                IncludeVectors = false,
                Filter = f => f.UserId == _userId
            });

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
    }
}