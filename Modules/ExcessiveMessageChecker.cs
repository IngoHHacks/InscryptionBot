using Discord.WebSocket;

namespace InscryptionBot.Modules
{
    public class ExcessiveMessageChecker
    {
        private Dictionary<ulong, List<SocketUserMessage>> entries;
        private List<ulong> users;

        public ExcessiveMessageChecker()
        {
            entries = new Dictionary<ulong, List<SocketUserMessage>>();
            users = new List<ulong>();
        }

        public void CheckMessage(SocketUserMessage message)
        {
            if (message.Author is not SocketGuildUser user) return;
            if (user.GuildPermissions.ManageMessages) return;
            if (!entries.ContainsKey(message.Author.Id))
            {
                entries.Add(message.Author.Id, new List<SocketUserMessage>() { message });
            }
            else
            {
                entries[message.Author.Id].Add(message);
            }

            List<SocketUserMessage> messagesToCheck = entries[message.Author.Id];
            if (messagesToCheck.Count > 100)
            {
                messagesToCheck.RemoveAt(0);
            }
            int withLink = 0;
            int same = 0;
            int spam = 0;
            int diffChannel = 0;
            int ateveryone = 0;
            for (int i = messagesToCheck.Count - 1; i >= 0; i--)
            {
                if (messagesToCheck[i].Content.Contains("https://") || messagesToCheck[i].Content.Contains("http://")) withLink++;
                else withLink = 0;

                if (messagesToCheck[i].MentionedEveryone) ateveryone++;
                else ateveryone = 0;

                if (i < messagesToCheck.Count - 1)
                {
                    var msg1 = messagesToCheck[i];
                    var msg2 = messagesToCheck[i + 1];
                    var timeDiff = msg2.Timestamp - msg1.Timestamp;
                    if (timeDiff <= TimeSpan.FromSeconds(5))
                    {
                        if (msg1.Content == msg2.Content) same++;
                        else same = 0;

                        if (timeDiff <= TimeSpan.FromSeconds(2)) spam++;
                        else spam = 0;

                        if (msg1.Channel.Id != msg2.Channel.Id) diffChannel++;
                        else diffChannel = 0;
                    }
                }

                if (withLink > 0 && same >= 2 && diffChannel >= 2)
                {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromDays(1));
                        await LogTimeout(user, messagesToCheck, "1 day", "sending 3 same messages with link in different channels", 3);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
                if (withLink > 0 && same >= 4)
                {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromHours(8));
                        await LogTimeout(user, messagesToCheck, "8 hours", "sending 5 same messages with link", 5);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
                if (ateveryone >= 1 && withLink >= 1)
                {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromHours(8));
                        await LogTimeout(user, messagesToCheck, "8 hours", "sending a message mentioning everyone with a link", 1);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
                if (ateveryone >= 2)
                {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromHours(1));
                        await LogTimeout(user, messagesToCheck, "1 hour", "sending 2 messages mentioning everyone", 2);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
                if (same == 9) {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromHours(1));
                        await LogTimeout(user, messagesToCheck, "1 hour", "sending 10 same messages", 10);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
                if (spam == 10)
                {
                    var task = new Task(async () => {
                        await user.SetTimeOutAsync(TimeSpan.FromHours(1));
                        await LogTimeout(user, messagesToCheck, "1 hour", "sending 10 messages fast", 10);
                    });
                    entries[message.Author.Id] = new List<SocketUserMessage>();
                    task.Start();
                    break;
                }
            }

            if (entries.Count > 100)
            {
                while (entries.Count > 100 && entries[users[0]].Last().Timestamp > DateTimeOffset.Now.AddMinutes(1))
                {
                    entries.Remove(users[0]);
                    users.RemoveAt(0);
                }
            }
        }

        private async Task LogTimeout(SocketGuildUser user, List<SocketUserMessage> messages, string duration, string reason, int messagesToDelete)
        {
            var botchannel = user.Guild.Channels.Where(x => x.Name == "bot-testing").FirstOrDefault();
            if (botchannel != null && botchannel is ISocketMessageChannel botMessageChannel)
            {
                try
                {
                    await botMessageChannel.SendMessageAsync($"User {user.Mention} has been timed out for {duration} for the reason: {reason}");
                    await botMessageChannel.SendMessageAsync("**## Excerpt of messages ##**");

                    for (int i = messages.Count - 1; i > messages.Count - 1 - messagesToDelete; i--)
                    {
                        if (i >= 0)
                        {
                            string content = messages[i].Content;
                            if (content.Length > 1997)
                            {
                                content = content.Substring(0, 1997) + "...";
                            }
                            await botMessageChannel.SendMessageAsync(content, messageReference: messages[i].Reference,
                                                                     stickers: messages[i].Stickers.ToArray(), embeds: messages[i].Embeds.ToArray());
                            await messages[i].DeleteAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    await botMessageChannel.SendMessageAsync($"**An error has occured when logging the timeout for user {user.Mention}**: {ex.Message}");
                }
            }
        }
    }
}
