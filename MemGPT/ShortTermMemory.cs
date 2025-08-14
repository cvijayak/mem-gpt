namespace MemGPT
{
    using System.Collections.Generic;
    using Contracts;

    public class ShortTermMemory(int capacity = 10) : IShortTermMemory
    {
        private readonly List<ChatMessage> _chatMessages = new();

        public void Add(ChatMessage chatMessage)
        {
            _chatMessages.Add(new ChatMessage
            {
                Id = chatMessage.Id,
                UserId = chatMessage.UserId,
                Role = chatMessage.Role,
                Message = chatMessage.Message,
                Timestamp = chatMessage.Timestamp
            });
            if (_chatMessages.Count > capacity)
            {
                _chatMessages.RemoveAt(0);
            }
        }

        public ChatMessage[] Get() => _chatMessages.ToArray();
    }
}