namespace MemGPT.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using MemGPT.Contracts.Services;
    using Microsoft.Extensions.AI;

    public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
    {
        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken) 
        {
            var result = await embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
            return result.Vector.ToArray();
        }
    }
}