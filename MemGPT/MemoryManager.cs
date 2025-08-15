namespace MemGPT
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;

    public class MemoryManager(Func<string, IShortTermMemory> stmFactory, Func<string, ILongTermMemory> ltmFactory) : IMemoryManager
    {
        public async Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
        {
            stmFactory(chatMessage.UserId).Add(chatMessage);
            await ltmFactory(chatMessage.UserId).AddAsync(chatMessage, cancellationToken);
        }

        public async Task DeleteAsync(string userId, CancellationToken cancellationToken)
        {
            stmFactory(userId).Delete();
            await ltmFactory(userId).DeleteAsync(cancellationToken);
        }
    }
}