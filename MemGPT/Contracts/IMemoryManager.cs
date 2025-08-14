namespace MemGPT.Contracts
{
    using System.Threading.Tasks;

    public interface IMemoryManager
    {
        Task AddAsync(ChatMessage chatMessage);
    }
}