using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Rcb.Internal.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MainController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ITelegramBotClient _botClient;
    
    public MainController(IConfiguration configuration)
    {
        _configuration = configuration;
        
        var botToken = _configuration.GetValue<string>("Telegram:BotToken")!;
        _botClient = new TelegramBotClient(botToken);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessageAsync([FromBody] MessageRequest request)
    {
        var channelId = _configuration["Telegram:ChannelId"]!;
        if (string.IsNullOrWhiteSpace(channelId))
            return BadRequest("Channel ID is not configured.");
        
        try
        {
            await _botClient.SendMessage(channelId, request.Message, ParseMode.Html);

            return Ok(new { Success = true, Message = "Message sent successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }
}

public record MessageRequest(string Message);