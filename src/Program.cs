using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace TelegramBot
{
	public class Program
	{
		//static void Main(string[] args)
		//{
		//	var configuration = new ConfigurationBuilder()
		//		.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
		//		.AddJsonFile($"appsettings.{Environment.}.json")
		//		.Build();
		//	var config = new TelegramBotConfig();
		//	configuration.Bind("bot", config);

		//	var Proxy = new WebProxy(config.Proxy.Host, config.Proxy.Port) { UseDefaultCredentials = true };
		//	var botClient = new TelegramBotClient(config.Token, webProxy: Proxy);
		//	var me = botClient.GetMeAsync().Result;
		//	Console.WriteLine(
		//		$"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
		//	);
		//}


		public static async Task Main(string[] args)
		{
			var builder = Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((Action<HostBuilderContext, IConfigurationBuilder>)((hostingContext, config) =>
				{
					IHostEnvironment hostingEnvironment = hostingContext.HostingEnvironment;
					config.AddJsonFile("appsettings.json", true, true).AddJsonFile("appsettings." + hostingEnvironment.EnvironmentName + ".json", true, true);
					if (hostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostingEnvironment.ApplicationName))
					{
						Assembly assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
						if (assembly != (Assembly)null)
							config.AddUserSecrets(assembly, true);
					}
					config.AddEnvironmentVariables();
					if (args == null)
						return;
					config.AddCommandLine(args);
				}))
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<TelegramBotHostedService>();
				});


			 await builder.Build().RunAsync();
		}
	}
}
