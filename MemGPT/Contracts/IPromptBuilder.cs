namespace MemGPT.Contracts
{
    using System.Threading;
    using System.Threading.Tasks;
    using Requests;

    public interface IPromptBuilder
    {
        Task<string> BuildPromptAsync(UserChatMessageRequest message, CancellationToken cancellationToken);
    }
}