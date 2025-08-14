namespace MemGPT
{
    using System;
    using System.Text;
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
    public class ChatController(IMemoryManager memoryManager, IPromptBuilder promptBuilder, IEmbeddingService embeddingService, Func<string, Kernel> kernelFactory) : ControllerBase
    {
        [HttpPost]
        public async Task SendMessage([FromBody] UserChatMessageRequest message)
        {
            await memoryManager.AddAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                Role = RoleType.User.ToString(),
                Message = message.Message,
                Timestamp = DateTimeOffset.UtcNow,
                Embedding = await embeddingService.GenerateEmbeddingAsync(message.Message)
            });

            var promptWithContext = await promptBuilder.BuildPromptAsync(message);

            Console.WriteLine(promptWithContext);

            var kernel = kernelFactory(message.UserId);
            var sb = new StringBuilder();

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            await foreach (var messageChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(promptWithContext))
            {
                var text = messageChunk.Content;
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(text);
                    await Response.WriteAsync(text);
                    await Response.Body.FlushAsync();
                }
            }

            await memoryManager.AddAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                Role = RoleType.Assistant.ToString(),
                Message = sb.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                Embedding = await embeddingService.GenerateEmbeddingAsync(message.Message)
            });
        }
    }
}