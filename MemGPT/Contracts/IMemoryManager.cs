namespace MemGPT.Contracts
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMemoryManager
    {
        Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken);
        Task<ChatMessage[]> GetAsync(string userId, CancellationToken cancellationToken);
        Task DeleteAsync(string userId, CancellationToken cancellationToken);
    }
}