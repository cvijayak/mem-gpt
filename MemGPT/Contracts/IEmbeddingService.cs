namespace MemGPT.Contracts
{
    using System.Threading.Tasks;

    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}