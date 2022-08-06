﻿using DSharpPlus;

using System.Collections.Concurrent;

namespace Speedbump
{
    public class XPHandler
    {
        DiscordClient Discord;
        ConcurrentQueue<(ulong guild, ulong player)> Players = new ();
        bool Closing;

        public XPHandler(DiscordManager discord, Lifetime lifetime)
        {
            Discord = discord.Client;
            lifetime.Add(cause => Closing = true);

            Discord.MessageCreated += Discord_MessageCreated;

            new Thread(HandlerThread).Start();
        }

        private Task Discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || (e.Author.IsSystem.HasValue && e.Author.IsSystem.Value)) { return Task.CompletedTask; }

            Players.Enqueue((e.Guild.Id, e.Author.Id));
            return Task.CompletedTask;
        }

        private void HandlerThread()
        {
            while (!Closing)
            {
                for (var i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000);
                    if (Closing) { break; }
                }

                var list = new List<(ulong guild, ulong user)>();
                while (!Players.IsEmpty)
                {
                    Players.TryDequeue(out var p);
                    list.Add(p);
                }

                var list2 = list.GroupBy(c => new
                {
                    c.Item1,
                    c.Item2
                }).Select(p => p.First());

                foreach (var user in list2)
                {
                    XPConnector.Increment(user.guild, user.user);
                    var level = XPConnector.GetLevel(user.guild, user.user);
                    var levels = XPConnector.GetLevels(user.guild);

                    var guild = Discord.Guilds[user.guild];
                    var member = guild.GetMemberAsync(user.user, true).GetAwaiter().GetResult();

                    foreach (var l in levels)
                    {
                        if (level >= l.Level && !member.Roles.Any(r => r.Id == l.Role))
                        {
                            var role = guild.GetRole(l.Role);
                            member.GrantRoleAsync(role);
                            try
                            {
                                member.SendMessageAsync($"You've been given the role `{role.Name}` for reaching level {level} in {guild.Name}!").GetAwaiter().GetResult();
                            } catch { }
                        }
                    }
                }
            }
        }
    }
}
