using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
	public class TelegramBotHostedService : IHostedService
	{
		private readonly IConfiguration _configuration;
		private TelegramBotClient _bot;

		public TelegramBotHostedService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var config = new TelegramBotConfig();
			_configuration.Bind("bot", config);

			var proxy = new WebProxy(config.Proxy.Host, config.Proxy.Port) { UseDefaultCredentials = true };
			_bot = new TelegramBotClient(config.Token, webProxy: proxy);
			var me = await _bot.GetMeAsync(cancellationToken);
			Console.WriteLine(
				$"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
			);

			await _bot.SetMyCommandsAsync(new BotCommand[] {new BotCommand { Command = "/help", Description = "Usage help"},});
			_bot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cancellationToken: cancellationToken);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_bot.StopReceiving();
			return Task.CompletedTask;
		}

		public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
		{
			var handler = update.Type switch
			{
				UpdateType.Message => BotOnMessageReceived(update.Message),
				UpdateType.EditedMessage => BotOnMessageReceived(update.Message),
				UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery),
				UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery),
				UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult),
				// UpdateType.Unknown:
				// UpdateType.ChannelPost:
				// UpdateType.EditedChannelPost:
				// UpdateType.ShippingQuery:
				// UpdateType.PreCheckoutQuery:
				// UpdateType.Poll:
				_ => UnknownUpdateHandlerAsync(update)
			};

			try
			{
				await handler;
			}
			catch (Exception exception)
			{
				await HandleErrorAsync(exception, cancellationToken);
			}
		}

		private async Task BotOnMessageReceived(Message message)
		{
			Console.WriteLine($"Receive message type: {message.Type}");
			if (message.Type != MessageType.Text)
				return;

			var action = (message.Text.Split(' ').First()) switch
			{
				"/inline" => SendInlineKeyboard(message),
				"/keyboard" => SendReplyKeyboard(message),
				"/photo" => SendFile(message),
				"/request" => RequestContactAndLocation(message),
				"/help" => Usage(message),
				_ => Nope(message)
			};
			await action;

			// Send inline keyboard
			// You can process responses in BotOnCallbackQueryReceived handler
			async Task SendInlineKeyboard(Message message)
			{
				await _bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

				// Simulate longer running task
				await Task.Delay(500);

				var inlineKeyboard = new InlineKeyboardMarkup(new[]
				{
                    // first row
                    new []
					{
						InlineKeyboardButton.WithCallbackData("1.1", "11"),
						InlineKeyboardButton.WithCallbackData("1.2", "12"),
					},
                    // second row
                    new []
					{
						InlineKeyboardButton.WithCallbackData("2.1", "21"),
						InlineKeyboardButton.WithCallbackData("2.2", "22"),
					}
				});
				await _bot.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: "Choose",
					replyMarkup: inlineKeyboard
				);
			}

			async Task SendReplyKeyboard(Message message)
			{
				var replyKeyboardMarkup = new ReplyKeyboardMarkup(
					new KeyboardButton[][]
					{
						new KeyboardButton[] { "1.1", "1.2" },
						new KeyboardButton[] { "2.1", "2.2" },
					},
					resizeKeyboard: true
				);

				await _bot.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: "Choose",
					replyMarkup: replyKeyboardMarkup

				);
			}

			async Task SendFile(Message message)
			{
				await _bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

				const string filePath = @"Files/tux.png";
				using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
				await _bot.SendPhotoAsync(
					chatId: message.Chat.Id,
					photo: new InputOnlineFile(fileStream, fileName),
					caption: "Nice Picture"
				);
			}

			async Task RequestContactAndLocation(Message message)
			{
				var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
				{
					KeyboardButton.WithRequestLocation("Location"),
					KeyboardButton.WithRequestContact("Contact"),
				});
				await _bot.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: "Who or Where are you?",
					replyMarkup: RequestReplyKeyboard
				);
			}

			async Task Usage(Message message)
			{
				const string usage = "Usage:\n" +
										"/inline   - send inline keyboard\n" +
										"/keyboard - send custom keyboard\n" +
										"/photo    - send a photo\n" +
										"/request  - request location or contact";
				await _bot.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: usage,
					replyMarkup: new ReplyKeyboardRemove()
				);
			}

			Task Nope(Message message)
			{
				return Task.CompletedTask;
			}
		}

		// Process Inline Keyboard callback data
		private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
		{
			await _bot.AnswerCallbackQueryAsync(
				callbackQuery.Id,
				$"Received {callbackQuery.Data}"
			);

			await _bot.SendTextMessageAsync(
				callbackQuery.Message.Chat.Id,
				$"Received {callbackQuery.Data}"
			);
		}

		#region Inline Mode

		private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
		{
			Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

			InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
					id: "3",
					title: "TgBots",
					inputMessageContent: new InputTextMessageContent(
						"hello"
					)
				)
			};

			await _bot.AnswerInlineQueryAsync(
				inlineQuery.Id,
				results,
				isPersonal: true,
				cacheTime: 0
			);
		}

		private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
		{
			Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
		}

		#endregion

		private Task UnknownUpdateHandlerAsync(Update update)
		{
			Console.WriteLine($"Unknown update type: {update.Type}");
			return Task.CompletedTask;
		}

		public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
		{
			var ErrorMessage = exception switch
			{
				ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => exception.ToString()
			};

			Console.WriteLine(ErrorMessage);
		}
	}
}
