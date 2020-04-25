using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace TelegramBot
{
	public class Program
	{
		static void Main(string[] args)
		{
			var configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.Build();
			var config = new TelegramBotConfig();
			configuration.Bind("bot", config);

			var Proxy = new WebProxy(config.Proxy.Host, config.Proxy.Port) { UseDefaultCredentials = true };
			var botClient = new TelegramBotClient(config.Token, webProxy: Proxy);
			var me = botClient.GetMeAsync().Result;
			Console.WriteLine(
				$"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
			);
		}
	}
}
