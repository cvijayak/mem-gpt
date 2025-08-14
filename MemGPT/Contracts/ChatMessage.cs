namespace MemGPT.Contracts
{
    using System;
    using Microsoft.Extensions.VectorData;

    public class ChatMessage
    {
        [VectorStoreKey(StorageName = "id")]
        public Guid Id { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "user_id")]
        public string UserId { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "role")]
        public string Role { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [VectorStoreData(IsFullTextIndexed = true, StorageName = "message")]
        public string Message { get; set; }

        [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "embedding")]
        public float[] Embedding { get; set; }
    }
}