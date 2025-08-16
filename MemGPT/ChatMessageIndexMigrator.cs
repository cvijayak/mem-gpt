namespace MemGPT
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Search.Documents.Indexes;
    using Contracts;

    public class ChatMessageIndexMigrator(SearchIndexClient indexClient, string indexName, string endpoint, string apiKey) : IMigrator
    {
        public async Task MigrateAsync()
        {
            try
            {
                await indexClient.GetIndexAsync(indexName);
                Console.WriteLine($"Index '{indexName}' already exists.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine($"Index '{indexName}' does not exist. Creating now...");
                await CreateSearchIndexAsync();
            }
        }

        private async Task CreateSearchIndexAsync()
        {
            var indexDefinition = new 
            {
                name = indexName,
                fields = new object[]
                {
                    new { name = "Id", type = "Edm.String", key = true, filterable = true },
                    new { name = "UserId", type = "Edm.String", filterable = true, searchable = true },
                    new { name = "Role", type = "Edm.String", filterable = true, searchable = true },
                    new { name = "Timestamp", type = "Edm.DateTimeOffset", filterable = true, sortable = true },
                    new { name = "Message", type = "Edm.String", searchable = true, analyzer = "en.microsoft" },
                    new { name = "Embedding", type = "Collection(Edm.Single)", dimensions = 1536, vectorSearchProfile = "default" }
                },
                vectorSearch = new
                {
                    algorithms = new object[]
                    {
                        new
                        {
                            name = "hnsw-algo",
                            kind = "hnsw",
                            hnswParameters = new { m = 4, efConstruction = 400, efSearch = 500, metric = "cosine" }
                        }
                    },
                    profiles = new object[] { new { name = "default", algorithm = "hnsw-algo" } }
                }
            };

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
                
                var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                var jsonDefinition = JsonSerializer.Serialize(indexDefinition, jsonOptions);
                
                var content = new StringContent(jsonDefinition, Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync($"{endpoint}/indexes/{indexName}?api-version=2023-10-01-Preview", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully created index '{indexName}'.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create index: {response.StatusCode}");
                    Console.WriteLine(errorContent);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error creating index: {exception.Message}");
            }
        }
    }
}
