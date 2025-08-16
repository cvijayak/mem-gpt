namespace MemGPT.Contracts
{
    public interface IMemoryDeletionWorker
    {
        bool Enqueue(string userId);
    }
}