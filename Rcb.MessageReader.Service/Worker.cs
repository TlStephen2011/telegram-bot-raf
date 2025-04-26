using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Rcb.MessageReader.Service;

public class Worker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<Worker> _logger;
    private readonly string channelId;
    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        var botToken = configuration.GetValue<string>("Telegram:BotToken")!;
        channelId = configuration.GetValue<string>("Telegram:ChannelId")!;
        _botClient = new TelegramBotClient(botToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram Polling Service...");

        var offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Fetch updates from Telegram
                var updates = await _botClient.GetUpdates(
                    offset: offset,
                    timeout: 10, // Long-polling timeout (in seconds)
                    allowedUpdates: [UpdateType.Message, UpdateType.ChatMember, UpdateType.ChannelPost],
                    cancellationToken: stoppingToken
                );

                foreach (var update in updates)
                {
                    // Process each update (e.g., messages from the channel)
                    ProcessUpdate(update);

                    // Update the offset to avoid re-processing the same update
                    offset = update.Id + 1;
                }
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError($"Telegram API Error: {ex.Message}");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected Error: {ex.Message}");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }
    }

    private void ProcessUpdate(Update update)
    {
        if (update.ChatMember != null)
        {
            var chatMember = update.ChatMember;

            // Check if the user joined the channel
            if (chatMember.NewChatMember.Status == ChatMemberStatus.Member)
            {
                var userId = chatMember.NewChatMember.User.Id;
                var userName = chatMember.NewChatMember.User.Username ?? chatMember.NewChatMember.User.FirstName;
                // Generate referral link
                var referralLink = $"https://t.me/YourBotUsername?start=ref_{userId}";

                // Send a private message to the user
                SendPrivateMessage(userId, referralLink, userName);
            }
        }
        
        if (update.ChannelPost != null)
        {
            var message = update.ChannelPost.Text;
            var channelId = update.ChannelPost.Chat.Id;

            // Log or process the message
            _logger.LogInformation($"Message received in channel {channelId}: {message}");
        }
    }
    
    private async Task SendPrivateMessage(long userId, string referralLink, string userName)
    {
        try
        {
            await _botClient.SendMessage(
                chatId: userId,
                text: $"Hi {userName}, welcome to the channel! Here is your referral link: {referralLink}"
            );
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("bot can't initiate conversation"))
        {
            // Notify the user in the channel if the bot can't send a private message
            await _botClient.SendMessage(
                chatId: channelId, // Replace with your channel ID
                text: $"Hi {userName}, please start the bot to receive your referral link: https://t.me/RcbTestBot"
            );
        }
    }
}