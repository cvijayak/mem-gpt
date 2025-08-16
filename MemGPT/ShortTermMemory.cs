namespace MemGPT
{
    using System.Collections.Generic;
    using Contracts;
    using Config;
    using Microsoft.Extensions.Options;

    public class ShortTermMemory(IOptions<MemorySettingsOptions> settings) : IShortTermMemory
    {
        private readonly MemorySettingsOptions _settings = settings.Value;
        private readonly List<ChatMessage> _chatMessages = new();

        public void Add(ChatMessage chatMessage)
        {
            _chatMessages.Add(chatMessage);
            if (_chatMessages.Count > _settings.StmCapacity)
            {
                _chatMessages.RemoveAt(0);
            }
        }

        public ChatMessage[] Get() => _chatMessages.ToArray();

        public void Delete() => _chatMessages.Clear();
    }
}