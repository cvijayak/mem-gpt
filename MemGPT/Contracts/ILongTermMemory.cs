namespace MemGPT.Contracts
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ILongTermMemory
    {
        Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken);
        Task<ChatMessage[]> SearchAsync(string text, CancellationToken cancellationToken);
        Task DeleteAsync(CancellationToken cancellationToken);
        IAsyncEnumerable<ChatMessage> GetAsync(CancellationToken cancellationToken);
    }
}