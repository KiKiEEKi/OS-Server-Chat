using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace OSServerChat
{
	public class OSServerChat : BasePlugin
	{
		public override string ModuleName => "OS Server Chat";
		public override string ModuleVersion => "1.0";
		public override string ModuleAuthor => "KiKiEEKi ( DS: kikieeki | vk.com/kikieeki )";

		public static DiscordClient Client { get; set; }
		public static string Token { get; set; }
		public static ulong Channel { get; set; }

		public override void Load(bool hotReload)
		{
			HookUserMessage(118, OnTextMsg);

			Task.Run(async () =>
			{
				try
				{
					await OSStart();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[OS Server Chat] ERROR: {ex.Message}");
					Console.WriteLine(ex.StackTrace);
				}
			});
		}

		public override void Unload(bool hotReload)
		{
			UnhookUserMessage(118, OnTextMsg);
		}

		private static HookResult OnTextMsg(UserMessage userMsg)
		{
			int entityIndex = userMsg.ReadInt("entityindex");
			CCSPlayerController? player = CounterStrikeSharp.API.Utilities.GetEntityFromIndex<CCSPlayerController>(entityIndex);
		
			if (player == null || player.IsBot) return HookResult.Continue;

			string name = userMsg.ReadString("param1");
			string msg = userMsg.ReadString("param2");

			if (string.IsNullOrEmpty(msg))
			{
				Console.WriteLine("[SERVER] Пусто");
				return HookResult.Handled;
			}

			Console.WriteLine($"[SERVER] {name}: {msg}");

			Task.Run(async () =>
			{
				if (Client != null)
				{
					var channel = await Client.GetChannelAsync(Channel);
					await Client.SendMessageAsync(channel, $"[SERVER] {name}: {msg}");
				}
			});

			return HookResult.Continue;
		}

		public async Task OSStart()
		{
			Console.WriteLine("[OS Server Chat] Start");

			string config = Path.Combine(ModuleDirectory, "Config.json");

			if (!File.Exists(config)) throw new FileNotFoundException($"Файл не найден: {config}");

			await ReadJson(config);

			var dsConfig = new DiscordConfiguration()
			{
				Token = Token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.All,
				AutoReconnect = true
			};

			Client = new DiscordClient(dsConfig);
			Client.Ready += ClientOnReady;
			Client.MessageCreated += OnMessageCreated;

			Console.WriteLine("[OS Server Chat] Ожидание сообщений из чата...");

			await Client.ConnectAsync();
		}

		private static Task ClientOnReady(DiscordClient sender, ReadyEventArgs args)
		{
			return Task.CompletedTask;
		}

		public static async Task ReadJson(string config)
		{
			using StreamReader sr = new StreamReader(config);
			string json = await sr.ReadToEndAsync();
			JsonStructure jsonData = JsonConvert.DeserializeObject<JsonStructure>(json);

			Token = jsonData.Token;
			Channel = jsonData.Channel;
		}

		private static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
		{
			if (args.Author.IsBot) return;

			if (args.Channel.Id != Channel) return;

			var name = args.Author.Username;
			var msg = args.Message.Content;
			Console.WriteLine($"[DISCORD] {name}: {msg}");

			Server.NextFrame(() =>
			{
				Server.PrintToChatAll($"[DISCORD] {name}: {msg}");
			});
		}
	}
}

internal sealed class JsonStructure
{
	public required string Token { get; set; }
	public ulong Channel { get; set; }
}
