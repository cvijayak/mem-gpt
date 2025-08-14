namespace MemGPT
{
    using System;
    using System.Threading.Tasks;
    using Contracts;

    public class MemoryManager(Func<string, IShortTermMemory> stmFactory, Func<string, ILongTermMemory> ltmFactory) : IMemoryManager
    {
        public async Task AddAsync(ChatMessage chatMessage)
        {
            stmFactory(chatMessage.UserId).Add(chatMessage);
            await ltmFactory(chatMessage.UserId).AddAsync(chatMessage);
        }
    }
}