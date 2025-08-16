namespace MemGPT
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Search.Documents;
    using Azure.Search.Documents.Models;
    using Config;
    using Contracts;
    using MemGPT.Contracts.Services;
    using Microsoft.Extensions.Options;

    public class LongTermMemory(string userId, SearchClient searchClient, IEmbeddingService embeddingService, IOptions<MemorySettingsOptions> settings) : ILongTermMemory
    {
        private readonly MemorySettingsOptions _settings = settings.Value;

        public async Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
        {
            if (chatMessage.Embedding == null || chatMessage.Embedding.Length == 0)
            {
                chatMessage.Embedding = await embeddingService.GenerateEmbeddingAsync(chatMessage.Message, cancellationToken);
            }

            var searchableMessage = new SearchableChatMessage
            {
                Id = chatMessage.Id.ToString(),
                UserId = chatMessage.UserId,
                Role = chatMessage.Role,
                Message = chatMessage.Message,
                Timestamp = chatMessage.Timestamp,
                Embedding = chatMessage.Embedding
            };
            
            var batch = IndexDocumentsBatch.Upload([ searchableMessage ]);
            await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        }

        public async IAsyncEnumerable<ChatMessage> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var options = new SearchOptions
            {
                Filter = $"UserId eq '{userId}'",
                IncludeTotalCount = true,
                Size = _settings.LongTermMemory.PageSize
            };

            options.OrderBy.Add("Timestamp");

            async Task<Response<SearchResults<SearchableChatMessage>>> GetSearchResponse(int skip)
            {
                try
                {
                    options.Skip = skip;
                    return await searchClient.SearchAsync<SearchableChatMessage>("*", options, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Query cancelled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving messages: {ex.Message}");
                }

                return null;
            }
            
            int currentSkip = 0;
            bool hasMoreResults = true;
            
            while (hasMoreResults && !cancellationToken.IsCancellationRequested)
            {
                var response = await GetSearchResponse(currentSkip);
                if (response == null)
                {
                    yield break;
                }

                var results = response.Value.GetResults().ToList();

                var resultCount = results.Count;
                foreach (var result in results)
                {
                    var doc = result.Document;
                    yield return new ChatMessage
                    {
                        Id = Guid.Parse(doc.Id),
                        UserId = doc.UserId,
                        Role = doc.Role,
                        Message = doc.Message,
                        Timestamp = doc.Timestamp,
                        Embedding = doc.Embedding
                    };
                }

                if (resultCount > 0)
                {
                    currentSkip += resultCount;
                    hasMoreResults = currentSkip < response.Value.TotalCount;
                }
                else
                {
                    hasMoreResults = false;
                }
            }
        }

        public async Task<ChatMessage[]> SearchAsync(string text, CancellationToken cancellationToken)
        {
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
            var vectorQuery = new VectorizedQuery(queryEmbedding)
            {
                KNearestNeighborsCount = _settings.LongTermMemory.Capacity,
                Fields = { "Embedding" }
            };
            var vectorSearchOptions = new VectorSearchOptions
            {
                Queries = { vectorQuery }
            };
            var options = new SearchOptions
            {
                Filter = $"UserId eq '{userId}'",
                Size = _settings.LongTermMemory.Capacity,
                VectorSearch = vectorSearchOptions
            };
            
            var chatMessages = new List<ChatMessage>();
            var response = await searchClient.SearchAsync<SearchableChatMessage>("*", options, cancellationToken: cancellationToken);
                
            foreach (var result in response.Value.GetResults())
            {
                if (result.Score >= _settings.LongTermMemory.SimilarityThreshold)
                {
                    var doc = result.Document;
                    var chatMessage = new ChatMessage
                    {
                        Id = Guid.Parse(doc.Id),
                        UserId = doc.UserId,
                        Role = doc.Role,
                        Message = doc.Message,
                        Timestamp = doc.Timestamp,
                        Embedding = doc.Embedding
                    };
                    
                    chatMessages.Add(chatMessage);
                }
            }
            
            return chatMessages.ToArray();
        }
        
        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            int batchSize = _settings.LongTermMemory.DeleteBatchSize;
            bool hasMore = true;
            int retryCount = 0;
            
            while (hasMore && retryCount < _settings.LongTermMemory.MaxDeleteRetries)
            {
                try
                {
                    var options = new SearchOptions
                    {
                        Filter = $"UserId eq '{userId}'",
                        Size = batchSize
                    };

                    var response = await searchClient.SearchAsync<SearchableChatMessage>("*", options, cancellationToken);
                    var toDelete = new List<string>();
                    
                    foreach (var result in response.Value.GetResults())
                    {
                        toDelete.Add(result.Document.Id);
                    }
                    
                    if (toDelete.Count > 0)
                    {
                        await searchClient.DeleteDocumentsAsync("id", toDelete, cancellationToken: cancellationToken);
                        await Task.Delay(50, cancellationToken);
                    }
                    else
                    {
                        hasMore = false;
                    }
                    
                    retryCount = 0;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    retryCount++;
                    await Task.Delay(_settings.LongTermMemory.DeleteRetryDelayBaseMs * (1 << retryCount), cancellationToken);
                    Console.WriteLine($"Delete retry {retryCount} after error: {ex.Message}");
                }
            }
        }
    }
}
