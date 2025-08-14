namespace MemGPT.Contracts.Requests
{
    public class UserChatMessageRequest
    {
        public string UserId { get; set; }
        public string Message { get; set; }
    }
}