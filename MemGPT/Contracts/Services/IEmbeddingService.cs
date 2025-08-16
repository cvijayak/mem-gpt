namespace MemGPT.Contracts.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
    }
}