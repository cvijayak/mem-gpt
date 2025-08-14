namespace MemGPT.Contracts
{
    public interface IShortTermMemory
    {
        void Add(ChatMessage chatMessage);
        ChatMessage[] Get();
    }
}