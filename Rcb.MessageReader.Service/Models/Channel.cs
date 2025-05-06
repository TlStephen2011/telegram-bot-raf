namespace Rcb.MessageReader.Service.Models;

public class Channel
{
    public int ChannelId { get; set; }
    public string TelegramChannelId { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Active { get; set; }
    public DateTime InsertedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string LanguageCode { get; set; } = "en";
}