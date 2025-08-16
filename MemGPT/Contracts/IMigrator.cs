namespace MemGPT.Contracts
{
    using System.Threading.Tasks;

    public interface IMigrator
    {
        Task MigrateAsync();
    }
}
