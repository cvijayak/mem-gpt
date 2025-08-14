namespace MemGPT
{
    using System.Threading.Tasks;
    using Contracts;
    using Microsoft.Extensions.AI;

    public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
    {
        public async Task<float[]> GenerateEmbeddingAsync(string text) 
        {
            var result = await embeddingGenerator.GenerateAsync(text);
            return result.Vector.ToArray();
        }
    }
}