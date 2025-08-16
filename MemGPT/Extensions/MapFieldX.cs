namespace MemGPT.Extensions
{
    using System;
    using System.Linq;
    using Contracts;
    using Google.Protobuf.Collections;
    using Qdrant.Client.Grpc;

    public static class MapFieldExtensions
    {
        public static ChatMessage MapToChatMessage(MapField<string, Value> map)
        {
            var chatMessage = new ChatMessage();

            if (map.TryGetValue("id", out var idValue))
            {
                chatMessage.Id = Guid.Parse(idValue.StringValue);
            }

            if (map.TryGetValue("user_id", out var userIdValue))
            {
                chatMessage.UserId = userIdValue.StringValue;
            }

            if (map.TryGetValue("role", out var roleValue))
            {
                chatMessage.Role = roleValue.StringValue;
            }

            if (map.TryGetValue("timestamp", out var tsValue))
            {
                chatMessage.Timestamp = DateTimeOffset.Parse(tsValue.StringValue);
            }

            if (map.TryGetValue("message", out var msgValue))
            {
                chatMessage.Message = msgValue.StringValue;
            }

            if (map.TryGetValue("embedding", out var embValue) && embValue.KindCase == Value.KindOneofCase.ListValue)
            {
                chatMessage.Embedding = embValue.ListValue.Values
                    .Select(v => (float)v.DoubleValue)
                    .ToArray();
            }

            return chatMessage;
        }
    }
}
