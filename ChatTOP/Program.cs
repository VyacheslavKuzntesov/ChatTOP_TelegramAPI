using Newtonsoft.Json.Linq;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

string? keyOpenAIAPI = Environment.GetEnvironmentVariable("openai_api_key");
if (keyOpenAIAPI == null)
{
    throw new InvalidOperationException("Переменная окружения openai_api_key не задана!");
}
string? keyTelegram = Environment.GetEnvironmentVariable("telegram_api_key");
if (keyTelegram == null)
{
    throw new InvalidOperationException("Переменная окружения telegram_api_key не задана!");
}

var botClient = new TelegramBotClient(keyTelegram);
var api = new OpenAIAPI(new APIAuthentication(keyOpenAIAPI));

const int limitTime = 60;
const int limitQuantity = 10;
ConcurrentDictionary<long, UserMessageDataState> clientMessageData = new ConcurrentDictionary<long, UserMessageDataState>();

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    if (clientMessageData.ContainsKey(message.Chat.Id))
    {
        if (clientMessageData[message.Chat.Id].messageDateTimes.Count == limitQuantity)
        {
            int timeLeft = limitTime - (message.Date - clientMessageData[message.Chat.Id].messageDateTimes.First()).Seconds;
            if (timeLeft > 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Превышен лимит запросов подождите.",
                    cancellationToken: cancellationToken);
                return;
            }
            clientMessageData[message.Chat.Id].messageDateTimes.TryDequeue(out _);
        }
    }
    else
    {
        clientMessageData[message.Chat.Id] = new UserMessageDataState();
    };
    clientMessageData[message.Chat.Id].messageDateTimes.Enqueue(message.Date);

    var result = await api.Completions.CreateCompletionAsync(messageText, max_tokens: 10);
    ////Echo received message text
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: result.Completions[0].Text,
        cancellationToken: cancellationToken);
}
Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

























//ClassWork
//string? key = Environment.GetEnvironmentVariable("openai_api_key");

//Console.WriteLine("Что вы хотели спросить?");
//var text = Console.ReadLine();
//var api = new OpenAI_API.OpenAIAPI(key);

//var result = await api.Completions.GetCompletion(text);
//Console.WriteLine(result);

//return;

//using CancellationTokenSource cts = new();


//// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
//ReceiverOptions receiverOptions = new()
//{
//    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
//};

//botClient.StartReceiving(
//    updateHandler: HandleUpdateAsync,
//    pollingErrorHandler: HandlePollingErrorAsync,
//    receiverOptions: receiverOptions,
//    cancellationToken: cts.Token
//);

//var me = await botClient.GetMeAsync();

//Console.WriteLine($"Start listening for @{me.Username}");
//Console.ReadLine();

//// Send cancellation request to stop bot
//cts.Cancel();

//string[] mess = new string[40];

//async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//{
//    // Only process Message updates: https://core.telegram.org/bots/api#message
//    if (update.Message is not { } message)
//        return;
//    // Only process text messages
//    if (message.Text is not { } messageText)
//        return;


//    //var chatId = message.Chat.Id;

//    //var result = await api.Completions.CreateCompletionAsync(messageText,max_tokens:10);

//    ////Echo received message text
//    //await botClient.SendTextMessageAsync(
//    //    chatId: chatId,
//    //    text: result.Completions[0].Text,
//    //    cancellationToken: cancellationToken);

//}

//Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
//{
//    var ErrorMessage = exception switch
//    {
//        ApiRequestException apiRequestException
//            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
//        _ => exception.ToString()
//    };

//    Console.WriteLine(ErrorMessage);
//    return Task.CompletedTask;
//}