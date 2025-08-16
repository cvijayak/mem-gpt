namespace MemGPT.Contracts
{
    using Azure.Search.Documents.Indexes;
    using Azure.Search.Documents.Indexes.Models;
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Searchable document model for Azure AI Search
    /// </summary>
    public class SearchableChatMessage
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }
        
        [SearchableField(IsFilterable = true)]
        public string UserId { get; set; }
        
        [SearchableField(IsFilterable = true)]
        public string Role { get; set; }
        
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset Timestamp { get; set; }
        
        [SearchableField(AnalyzerName = "en.microsoft")]
        public string Message { get; set; }
        
        [VectorSearchField(VectorSearchDimensions = 1536)]
        public float[] Embedding { get; set; }
    }
}
