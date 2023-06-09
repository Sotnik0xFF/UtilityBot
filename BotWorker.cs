using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UtilityBot
{
    public class BotWorker : BackgroundService
    {
        private readonly ILogger<BotWorker> _logger;
        private readonly ITelegramBotClient _client;
        private readonly ISessionStorage _sessionStorage;
        private readonly MenuController _menuController;

        private readonly InlineKeyboardMarkup mainInlineKeyboard;
        private readonly InlineKeyboardMarkup backToMainInlineKeyboard;

        #region Constatnts
        private static class Constants
        {
            public const string MainMenuID = "Main";
            public const string GetLengthMenuID = "GetLength";
            public const string GetSumMenuID = "GetSum";
        }
        #endregion

        public BotWorker(
            ILogger<BotWorker> logger,
            ITelegramBotClient client,
            ISessionStorage storage,
            MenuController menuController)
        {
            _logger = logger;
            _client = client;
            _sessionStorage = storage;
            _menuController = menuController;

            mainInlineKeyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("📝 Определить длину сообщения", Constants.GetLengthMenuID) },
                new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("💻 Посчитать сумму чисел", Constants.GetSumMenuID) }
            });
            _menuController.RegisterMenu(Constants.GetLengthMenuID, OnGetLengthClickedAsync, OnGetLengthTextMessageHandler);
            _menuController.RegisterMenu(Constants.GetSumMenuID, OnGetSumClickedAsync, OnGetSumTextMessageHandler);

            backToMainInlineKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Назад", Constants.MainMenuID));
            _menuController.RegisterMenu(Constants.MainMenuID, OnBackToMainClickedAsync, null);
        }

        async private Task OnGetSumTextMessageHandler(long chatId, string message, CancellationToken cancellationToken)
        {
            double sum = 0;
            string[] numbers = message.Split(' ');

            try
            {
                foreach (string number in numbers)
                {
                    sum += double.Parse(number);
                }
                await _client.SendTextMessageAsync(
                    chatId,
                    $"Ок! Принято сообщение.\nТекст: {message}\nСумма чисел: {sum}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.Message);
                await _client.SendTextMessageAsync(
                    chatId,
                    $"Ошибка: {e.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        async private Task OnGetLengthTextMessageHandler(long chatId, string message, CancellationToken cancellationToken)
        {
            await _client.SendTextMessageAsync(
                chatId,
                $"Ок! Принято сообщение.\nТекст: {message}\nКол-во символов: {message.Length}",
                cancellationToken: cancellationToken);
        }

        async private Task OnBackToMainClickedAsync(long chatId, int messageId, string buttonId, CancellationToken cancellationToken)
        {
            await ShowMainMenuAsync(chatId, messageId, cancellationToken);
        }

        async private Task OnGetSumClickedAsync(long chatId, int messageId, string buttonId, CancellationToken cancellationToken)
        {
            Session session = _sessionStorage.GetSession(chatId);
            session.OperationId = buttonId;
            await _client.EditMessageTextAsync(
                chatId,
                messageId, $"Введите числа через пробел.\nНапример: {12.5} 10 -50 {3.14}",
                replyMarkup: backToMainInlineKeyboard,
                cancellationToken: cancellationToken);
        }

        async private Task OnGetLengthClickedAsync(long chatId, int messageId, string buttonId, CancellationToken cancellationToken)
        {
            Session session = _sessionStorage.GetSession(chatId);
            session.OperationId = buttonId;
            await _client.EditMessageTextAsync(
                chatId,
                messageId, "Отправьте мне сообщение. Я определю его длину.",
                replyMarkup: backToMainInlineKeyboard,
                cancellationToken: cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: new ReceiverOptions() { AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } },
                cancellationToken: stoppingToken);
            return Task.CompletedTask;
        }

        private async Task ShowMainMenuAsync(long chatId, int? messageId, CancellationToken cancellationToken)
        {
            string message = "Выберите операцию:\n";

            Session session = _sessionStorage.GetSession(chatId);
            session.OperationId = null;

            if (messageId.HasValue)
            {
                await _client.EditMessageTextAsync(
                    chatId,
                    messageId.Value,
                    message,
                    replyMarkup: mainInlineKeyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _client.SendTextMessageAsync(
                    chatId,
                    message,
                    replyMarkup: mainInlineKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                _logger.LogInformation($"{DateTime.UtcNow} - Message from {update.Message.Chat.FirstName}: {update.Message.Text}");
                if (update.Message.Text != null)
                {
                    if (update.Message.Text == "/start")
                    {
                        await ShowMainMenuAsync(update.Message.Chat.Id, null, cancellationToken);
                    }
                    else
                    {
                        Session session = _sessionStorage.GetSession(update.Message.Chat.Id);
                        if (session.OperationId != null)
                        {
                            await _menuController.HandleTextMessage(session.OperationId, update.Message, cancellationToken);
                        }
                    }
                }
            }

            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await _menuController.HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                return;
            }
        }

        Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
        {
            string errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"{DateTime.UtcNow} - Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }

    }
}