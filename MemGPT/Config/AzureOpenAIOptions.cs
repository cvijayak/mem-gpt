namespace MemGPT.Config
{
    public class AzureOpenAIOptions
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string EmbeddingDeploymentName { get; set; }
        public string ChatDeploymentName { get; set; }
    }
}