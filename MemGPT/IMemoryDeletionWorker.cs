namespace MemGPT
{
    public interface IMemoryDeletionWorker
    {
        bool Enqueue(string userId);
    }
}