using Rcb.MessageReader.Service.Models;
using Rcb.MessageReader.Service.Repositories.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rcb.MessageReader.Service;

public class Worker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<Worker> _logger;
    private readonly IChannelRepository _channelRepository;

    private List<Channel> _channels;
    
    public Worker(ILogger<Worker> logger, IConfiguration configuration, IChannelRepository channelRepository)
    {
        _logger = logger;
        _channelRepository = channelRepository;
        var botToken = configuration.GetValue<string>("Telegram:BotToken")!;
        _botClient = new TelegramBotClient(botToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram Polling Service...");

        _channels = await _channelRepository.GetChannelsAsync();
        
        // Set bot commands
        await _botClient.SetMyCommands([
            new BotCommand { Command = "start", Description = "Start the bot" },
            new BotCommand { Command = "refer", Description = "Get your referral link" },
            new BotCommand { Command = "help", Description = "Get help with the bot" }
        ], cancellationToken: stoppingToken);

        // Continue with your existing logic
        var offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botClient.GetUpdates(
                    offset: offset,
                    timeout: 10,
                    allowedUpdates: new[] { UpdateType.Message, UpdateType.CallbackQuery },
                    cancellationToken: stoppingToken
                );

                foreach (var update in updates)
                {
                    await ProcessUpdate(update);
                    offset = update.Id + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessUpdate(Update update)
    {
        if (update.Message != null)
        {
            var message = update.Message;

            if (message.Text == "/start")
            {
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Welcome! Use /refer to get your referral link."
                );
            }
            else if (message.Text == "/refer")
            {
                await HandleReferCommand(message);
            }
            else if (message.Text == "/help")
            {
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Here are the available commands:\n/start - Start the bot\n/refer - Get your referral link\n/help - Get help with the bot"
                );
            }
        }
        else if (update.CallbackQuery != null)
        {
            // Call HandleCallbackQuery when a callback query is received
            await HandleCallbackQuery(update.CallbackQuery);
        }
    }

    private async Task HandleReferCommand(Message message)
    {
        var userId = message.From!.Id;
        var userName = message.From.Username ?? message.From.FirstName;

        // Message text
        string messageText = "Select a channel to refer your friend to:\n\n";

        // Inline keyboard with a button that sends callback data
        var inlineKeyboard = new InlineKeyboardMarkup(
            _channels.Select(channel => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: channel.DisplayName,
                    callbackData: $"refer_to_{channel.ChannelId}"
                )
            })
        );

        // Send the message with the inline button
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: messageText,
            replyMarkup: inlineKeyboard
        );
    }
    
    private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
    {
        // Check if the callback data starts with "refer_to_"
        if (callbackQuery.Data != null && callbackQuery.Data.StartsWith("refer_to_"))
        {
            // Extract the ChannelId from the callback data
            var channelIdString = callbackQuery.Data.Replace("refer_to_", string.Empty);

            if (int.TryParse(channelIdString, out var channelId))
            {
                // Handle the referral logic for the specific channel
                await HandleReferToChannel(callbackQuery, channelId);
            }
            else
            {
                // Log or send an error message if ChannelId is invalid
                await _botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid channel selection.",
                    showAlert: true
                );
            }
        }
        else
        {
            // Handle other callback data if applicable
            await _botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Unknown action.",
                showAlert: true
            );
        }
    }
    
    private async Task HandleReferToChannel(CallbackQuery callbackQuery, int channelId)
    {
        // Assuming you have a way to fetch channel details by ChannelId
        var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);

        if (channel != null)
        {
            var userId = callbackQuery.From.Id;
            var referralLink = $"https://t.me/{channel.TelegramChannelId}?start=ref_{userId}";

            await _botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: $"Referral link generated for {channel.DisplayName}."
            );

            await _botClient.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: $"Here is your referral link for {channel.DisplayName}: {referralLink}"
            );

            _logger.LogInformation($"Referral link generated for user {userId} for channel {channel.DisplayName} ({channelId}).");
        }
        else
        {
            // If the channel is not found
            await _botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Channel not found.",
                showAlert: true
            );

            _logger.LogError($"Channel with ID {channelId} not found.");
        }
    }
}