namespace MemGPT.Contracts
{
    using System;

    public class ChatMessage
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; set; }
        public float[] Embedding { get; set; }
    }
}