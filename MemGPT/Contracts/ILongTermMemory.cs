namespace MemGPT.Contracts
{
    using System.Threading.Tasks;

    public interface ILongTermMemory
    {
        Task AddAsync(ChatMessage chatMessage);
        Task AddAsync(ChatMessage[] chatMessages);
        Task<ChatMessage[]> SearchAsync(string text);
    }
}