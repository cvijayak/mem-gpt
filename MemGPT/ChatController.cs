namespace MemGPT
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Contracts.Requests;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.ChatCompletion;
    using ChatMessage = Contracts.ChatMessage;

    [ApiController]
    [Route("api/chat")]
    public class ChatController(IMemoryManager memoryManager, IMemoryDeletionWorker worker, IPromptBuilder promptBuilder, IEmbeddingService embeddingService, Func<string, Kernel> kernelFactory) : ControllerBase
    {
        [HttpDelete]
        public IActionResult DeleteMessages(string userId)
        {
            worker.Enqueue(userId);
            return Accepted();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string userId, CancellationToken cancellationToken)
        {
            var messages = await memoryManager.GetAsync(userId, cancellationToken);
            return Ok(messages);
        }

        [HttpPost]
        public async Task SendMessage([FromBody] UserChatMessageRequest message, CancellationToken cancellationToken)
        {
            await memoryManager.AddAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                Role = RoleType.User.ToString(),
                Message = message.Message,
                Timestamp = DateTimeOffset.UtcNow,
                Embedding = await embeddingService.GenerateEmbeddingAsync(message.Message, cancellationToken)
            }, cancellationToken);

            var promptWithContext = await promptBuilder.BuildPromptAsync(message, cancellationToken);

            Console.WriteLine(promptWithContext);

            var kernel = kernelFactory(message.UserId);
            var sb = new StringBuilder();

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            await foreach (var messageChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(promptWithContext, cancellationToken: cancellationToken))
            {
                var text = messageChunk.Content;
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(text);
                    await Response.WriteAsync(text, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }

            await memoryManager.AddAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                Role = RoleType.Assistant.ToString(),
                Message = sb.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                Embedding = await embeddingService.GenerateEmbeddingAsync(message.Message, cancellationToken)
            }, cancellationToken);
        }
    }
}