namespace MemGPT
{
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
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