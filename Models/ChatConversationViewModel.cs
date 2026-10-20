
namespace MiniShop.Models;

public class ChatConversationViewModel
{
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public int MessageCount { get; set; }

    public DateTime LastMessage { get; set; }
}
