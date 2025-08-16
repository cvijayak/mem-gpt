namespace MemGPT.Config
{
    public class LongTermMemoryConfig
    {
        public int Capacity { get; set; }
        public double SimilarityThreshold { get; set; }
        public int DeleteBatchSize { get; set; }
        public int MaxDeleteRetries { get; set; }
        public int DeleteRetryDelayBaseMs { get; set; }
        public int PageSize { get; set; }
    }
}
