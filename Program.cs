using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace SecureBanglaAuditBot
{
    class Program
    {
        private DiscordSocketClient _client;

        // এখানে তোমার Bot Token সোজাসুজি বসাও
        private const string BotToken = "MTM4NzA1MDA0MDU1NTQ3MTAwMA.GschJw.tx0KpN0c5P9fOPXi7AKH5CBwLgTCHE7RQb5DEo";

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions
            });

            _client.Log += Log;
            _client.Ready += OnReady;
            _client.ChannelCreated += OnChannelCreated;
            _client.ChannelDestroyed += OnChannelDestroyed;
            _client.RoleCreated += OnRoleCreated;
            _client.RoleDeleted += OnRoleDeleted;
            _client.UserJoined += OnUserJoined;

            // Token এখানেই সরাসরি ব্যবহার করো
            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task OnReady()
        {
            Console.WriteLine($"✅ Bot চালু হয়েছে: {_client.CurrentUser.Username}");
        }

        private async Task SendEmbedToOwner(SocketGuild guild, string title, string description, Color color)
        {
            var owner = guild.Owner;
            var embed = new EmbedBuilder()
                .WithTitle($"🔔 {title}")
                .WithDescription(description)
                .WithColor(color)
                .WithFooter($"সার্ভার: {guild.Name}")
                .WithCurrentTimestamp()
                .Build();

            await owner.SendMessageAsync(embed: embed);
        }

        private async Task OnChannelCreated(SocketChannel channel)
        {
            if (channel is IGuildChannel gChannel)
            {
                await SendEmbedToOwner(gChannel.Guild, "নতুন চ্যানেল তৈরি হয়েছে", $"নাম: **{gChannel.Name}**", Color.Green);
            }
        }

        private async Task OnChannelDestroyed(SocketChannel channel)
        {
            if (channel is IGuildChannel gChannel)
            {
                await SendEmbedToOwner(gChannel.Guild, "একটি চ্যানেল মুছে ফেলা হয়েছে", $"নাম: **{gChannel.Name}**", Color.Red);
            }
        }

        private async Task OnRoleCreated(SocketRole role)
        {
            await SendEmbedToOwner(role.Guild, "নতুন রোল তৈরি হয়েছে", $"নাম: **{role.Name}**", Color.Purple);
        }

        private async Task OnRoleDeleted(SocketRole role)
        {
            await SendEmbedToOwner(role.Guild, "একটি রোল মুছে ফেলা হয়েছে", $"নাম: **{role.Name}**", Color.DarkRed);
        }

        private async Task OnUserJoined(SocketGuildUser user)
        {
            if (user.IsBot)
            {
                var guild = user.Guild;
                var logs = await guild.GetAuditLogsAsync(1, actionType: ActionType.BotAdded).FlattenAsync();
                var entry = logs.FirstOrDefault();

                if (entry != null && entry.User.Id != guild.OwnerId)
                {
                    await user.KickAsync("অননুমোদিত বট");
                    await SendEmbedToOwner(guild, "অননুমোদিত বট যোগ", $"বট: **{user.Username}**\nযোগকারী: **{entry.User.Username}**", Color.Orange);
                }
                else
                {
                    await SendEmbedToOwner(guild, "নতুন বট যোগ হয়েছে", $"বট: **{user.Username}**", Color.Blue);
                }
            }
        }

        // Nuke detection (manual call or add event-based call)
        private async Task MonitorNuke(SocketGuild guild)
        {
            var logs = await guild.GetAuditLogsAsync(10).FlattenAsync();
            var nukeActions = logs.Where(a =>
                a.Action == ActionType.ChannelDeleted ||
                a.Action == ActionType.RoleDeleted ||
                a.Action == ActionType.MemberKicked).ToList();

            if (nukeActions.Count >= 5)
            {
                var attacker = nukeActions.First().User;
                var attackerUser = guild.GetUser(attacker.Id);
                if (attackerUser != null)
                {
                    await attackerUser.RemoveRolesAsync(attackerUser.Roles.Where(r => !r.IsEveryone));
                    await SendEmbedToOwner(guild, "⚠️ Nuke চেষ্টা রোধ করা হয়েছে", $"ব্যবহারকারী: **{attacker.Username}** এর রোল মুছে ফেলা হয়েছে।", Color.DarkOrange);
                }
            }
        }
    }
}
