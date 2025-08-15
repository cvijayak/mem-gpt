namespace MemGPT
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Contracts.Requests;
    using Microsoft.SemanticKernel;

    public class PromptBuilder(Func<string, Kernel> kernelFactory, Func<string, IShortTermMemory> stmFactory, Func<string, ILongTermMemory> ltmFactory) : IPromptBuilder
    {
        private async Task<string> SummarizeAsync(string userId, ChatMessage[] messages, string label, CancellationToken cancellationToken)
        {
            if (messages == null || messages.Any() == false)
            {
                return "(No content)";
            }

            string summarizePrompt = "Summarize the following {{$label}} into a short, concise paragraph:\n{{$text}}";

            var kernel = kernelFactory(userId);
            var summarize = kernel.CreateFunctionFromPrompt(summarizePrompt);

            var result = await kernel.InvokeAsync(summarize, new()
            {
                ["label"] = label,
                ["text"] = JsonSerializer.Serialize(messages)
            }, cancellationToken);

            return result.GetValue<string>();
        }

        public async Task<string> BuildPromptAsync(UserChatMessageRequest message, CancellationToken cancellationToken)
        {
            var userId = message.UserId;
            var userQuery = message.Message;
            var shortTermMemory = stmFactory(userId).Get();
            var longTermMemory = await ltmFactory(userId).SearchAsync(userQuery, cancellationToken);

            var stmSummary = await SummarizeAsync(userId, shortTermMemory, "Short-term memory context", cancellationToken);
            var ltmSummary = await SummarizeAsync(userId, longTermMemory, "Long-term memory context", cancellationToken);

            return new StringBuilder()
                .AppendLine("You are an AI assistant that determines the user’s intention.")
                .AppendLine()
                .AppendLine("Context from short-term memory:")
                .AppendLine(stmSummary)
                .AppendLine()
                .AppendLine("Context from long-term memory:")
                .AppendLine(ltmSummary)
                .AppendLine()
                .AppendLine("User Query:")
                .AppendLine(userQuery)
                .AppendLine()
                .AppendLine("Based on the above, determine the user’s intention and respond accordingly. Just return the response alone. No need to share the user intention as part of the response")
                .ToString();
        }
    }
}