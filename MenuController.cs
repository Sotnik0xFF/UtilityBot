using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace UtilityBot
{
    public delegate Task KeyboardCallback(long chatId, int messageId, string buttonId, CancellationToken cancellationToken);
    public delegate Task TextMessageHandler(long chatId, string message, CancellationToken cancellationToken);

    public class MenuController
    {
        ILogger<MenuController> _logger;
        ITelegramBotClient _client;

        ConcurrentDictionary<string, KeyboardCallback> _keyboardCallbacks;
        ConcurrentDictionary<string, TextMessageHandler> _textMessageHandlers;

        public MenuController(ITelegramBotClient client, ILogger<MenuController> logger)
        {
            _client = client;
            _logger = logger;
            _keyboardCallbacks = new ConcurrentDictionary<string, KeyboardCallback>();
            _textMessageHandlers = new ConcurrentDictionary<string, TextMessageHandler>();
        }

        async public Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Data == null || callbackQuery.Message == null)
            {
                return;
            }

            KeyboardCallback? callback;
            if (_keyboardCallbacks.TryGetValue(callbackQuery.Data, out callback))
            {
                _logger.LogInformation($"Button [{callbackQuery.Data}] pressed.");
                await callback.Invoke(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, callbackQuery.Data, cancellationToken);
                return;
            }
            _logger.LogInformation("Unregistered button pressed.");
        }

        async public Task HandleTextMessage(string menuId, Message message, CancellationToken cancellationToken)
        {
            TextMessageHandler? textMessageHandler;
            if (_textMessageHandlers.TryGetValue(menuId, out textMessageHandler))
            {
                await textMessageHandler.Invoke(message.Chat.Id, message.Text!, cancellationToken);
            }
        }

        public void RegisterMenu(string menuId, KeyboardCallback callback, TextMessageHandler? messageHandler)
        {
            _keyboardCallbacks.AddOrUpdate(menuId, callback, (_, _) => callback);
            _textMessageHandlers.AddOrUpdate(menuId, messageHandler!, (_, _) => messageHandler!);
        }

    }
}
