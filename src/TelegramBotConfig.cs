using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramBot
{
	public class TelegramBotConfig
	{
		public string Token { get; set; }
		public ProxyConfig Proxy { get; set; }
	}

	public class ProxyConfig
	{
		public string Host { get; set; }
		public int Port { get; set; }
	}
}
