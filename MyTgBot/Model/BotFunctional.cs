using System.Text;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using ParseMode = Telegram.Bot.Types.Enums.ParseMode;


namespace MyTgBot.Model {
    internal class BotFunctional {
        /// <summary>
        /// BOT token
        /// </summary>
        public static ITelegramBotClient bot = new TelegramBotClient(""); // token(from Botfather)

        // When user enters here, bot will ignore other commands until the current one is completed.
        private static Dictionary<long, User> usersInProcess = new();
        
        private static Dictionary<long, User> usersWithNotifies = new();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message) {
                var message = update.Message;
                // If command is already in progress returns true
                if (!usersInProcess.ContainsKey(message!.From!.Id)) {
                    switch (message?.Text?.ToLower()) {
                        case "/start":
                            await botClient.SendTextMessageAsync(message.Chat, $"<b>👋🏼 Hey {message.From!.FirstName}, your notifier is here!</b> {Environment.NewLine}Check the /commands button.", ParseMode.Html);
                            await botClient.SendAnimationAsync(message.Chat, "CgACAgIAAxkBAAIIdGNAPwPHXBhd73AUTtrcBG9sVyz2AAKKEwAC235RSW2fcqrKYAGaKgQ");
                            return;
                        case "/commands":
                            string[] commandsList = await System.IO.File.ReadAllLinesAsync(""); // path to the commands list
                            StringBuilder commandsResult = new();
                            foreach (var command in commandsList) {
                                commandsResult.AppendLine(command);
                            }
                            await botClient.SendTextMessageAsync(message.Chat, $"<b>💾 All commands:</b>" +
                                $"{Environment.NewLine}{Environment.NewLine}" + commandsResult, ParseMode.Html);
                            return;
                        case "/addnf":
                            if (usersWithNotifies.ContainsKey(message.From.Id)) {
                                if(usersWithNotifies[message.From.Id].userNotifies.Count is 30) {
                                    await botClient.SendTextMessageAsync(message.Chat, "You have reached the notification <b>limit!</b> (30)", ParseMode.Html);
                                    return;
                                }
                            }
                            await botClient.SendTextMessageAsync(message.Chat, "Fine! I need some information to create a notify...");
                            if (usersWithNotifies.ContainsKey(message.From!.Id)) {
                                usersInProcess.Add(message.From!.Id, usersWithNotifies[message.From!.Id]);
                                usersWithNotifies[message.From.Id].OperationLevel = 1;
                            }
                            else {
                                usersInProcess.Add(message.From!.Id, new User() { OperationLevel = 1 });
                            }

                            await botClient.SendTextMessageAsync(message?.Chat!, "Enter the <b>number</b> of birthday (1 - 31)", ParseMode.Html);
                            return;
                        case "/removenf":
                            if (usersWithNotifies.ContainsKey(message!.From!.Id)) {
                                await botClient.SendTextMessageAsync(message.Chat, "Enter the notification note you want <b>to delete</b>", ParseMode.Html);
                                usersInProcess.Add(message.From!.Id, new User() { OperationLevel = -1 });
                                return;
                            }
                            await botClient.SendTextMessageAsync(message.Chat, "You don't have any notifies yet 🤧");
                            usersInProcess.Remove(message.From!.Id);
                            return;
                        case "/mynotifyings":
                            if (usersWithNotifies.ContainsKey(message!.From!.Id)) {
                                StringBuilder notifiesResult = new();
                                foreach (var notify in usersWithNotifies[message!.From!.Id].userNotifies.ToList()) {
                                    notifiesResult.Append(notify.Note + " — ");
                                    notifiesResult.AppendLine($"<b>Day:</b> {notify.Day} / <b>Month:</b> {notify.Month}");
                                }
                                await botClient.SendTextMessageAsync(message.Chat, $"<b>🗓 Your notifyings: </b>" +
                                    $"{Environment.NewLine}{Environment.NewLine}" + notifiesResult, ParseMode.Html);
                                return;
                            }
                            await botClient.SendTextMessageAsync(message.Chat, "You don't have any notifies yet 🤧");
                            return;
                        case "/cancel":
                            await bot.SendTextMessageAsync(message.Chat, $"☹️ No command started.");
                            return;
                        default:
                            await botClient.SendTextMessageAsync(message?.Chat!, "Undefined command :<");
                            return;
                    }
                }
                else {
                    if (message?.Text?.ToLower() is "/cancel") {
                        CommandCancelling(message.From.Id);
                        await botClient.SendTextMessageAsync(message?.Chat!, "❌ Command execution canceled.");
                        return;
                    }
                    try {  
                        switch (usersInProcess[message!.From!.Id].OperationLevel) {
                            case -1:
                                if (usersWithNotifies[message!.From!.Id].userNotifies.Any(x => x.Note == message.Text)) {
                                    var toRemove = usersWithNotifies[message!.From!.Id].userNotifies.Where(x => x.Note == message.Text).First();
                                    usersWithNotifies[message!.From!.Id].userNotifies.Remove(toRemove);
                                    if(usersWithNotifies[message!.From!.Id].userNotifies.Count is 0) {
                                        usersWithNotifies.Remove(message!.From!.Id);
                                    }
                                    usersInProcess[message!.From!.Id].OperationLevel *= 0;
                                    usersInProcess.Remove(message.From!.Id);
                                    await botClient.SendTextMessageAsync(message.Chat, "Note <b>successfully</b> deleted.", ParseMode.Html);
                                    return;
                                }
                                await botClient.SendTextMessageAsync(message.Chat, "Enter <b>valid note-key</b>.", ParseMode.Html);
                                return;
                            case 1:
                                await DataInputChecking(1, 31, message);

                                await botClient.SendTextMessageAsync(message?.Chat!, "Enter <b>month</b> of birthday (1 - 12)", ParseMode.Html);
                                return;
                            case 2:
                                await DataInputChecking(1, 12, message);

                                await botClient.SendTextMessageAsync(message?.Chat!, "Enter <b>note</b> for birthday (1 - 20 letters)", ParseMode.Html);
                                return;
                            case 3:
                                string note = message?.Text!;

                                if (note.Length < 1 || note.Length > 20) {
                                    await botClient.SendTextMessageAsync(message?.Chat!, "Note <b>must be between 1 and 20</b> characters long", ParseMode.Html);
                                    return;
                                }
                                
                                
                                if (usersInProcess[message!.From!.Id].userNotifies.Any(x => x.Note == note)) {
                                    await botClient.SendTextMessageAsync(message?.Chat!, "Notify with this note <b>already exists!</b>", ParseMode.Html);
                                    return;
                                }
                                usersInProcess[message!.From!.Id].userNotifies[usersInProcess[message!.From!.Id].userNotifies.Count - 1].Note = note;
                                if (!usersWithNotifies.ContainsKey(message!.From!.Id)) {
                                    usersWithNotifies.Add(message!.From!.Id, usersInProcess[message!.From!.Id]);
                                }
                                
                                usersInProcess[message!.From!.Id].OperationLevel *= 0;
                                usersInProcess.Remove(message!.From!.Id);
                                await botClient.SendTextMessageAsync(message?.Chat!, "✍🏼 Notification <b>successfully created!</b>", ParseMode.Html);
                                await botClient.SendStickerAsync(message?.Chat!, "CAACAgIAAxkBAAIIkGNAQGVhIQLvY-_Pw6CG0QRZGDdYAAI8CAACSBUBS2kngY0iItlJKgQ");
                                return;
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                        await botClient.SendTextMessageAsync(message?.Chat!, "Enter data in <b>numerical</b> format!", ParseMode.Html);
                        return;
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Checks the current date against the user notification dates every day and notifies the user if the dates match.
        /// </summary>
        /// <returns></returns>
        public static async Task DateTracking() {
            await Task.Run(() => {
                while(true) {
                    if (usersWithNotifies.Count > 0) {
                        foreach (var user in usersWithNotifies) {
                            if (user.Value.userNotifies.Any(d => d.Day == DateTime.UtcNow.Day && d.Month == DateTime.UtcNow.Month)) {
                                List<NotifierData> usersToNotify = user.Value.userNotifies.Where(nf => nf.Day == DateTime.UtcNow.Day && nf.Month == DateTime.UtcNow.Month).ToList();
                                StringBuilder notesResult = new();
                                foreach (var notify in user.Value.userNotifies) {
                                    if (notify != user.Value.userNotifies.Last()) {
                                        notesResult.Append(notify.Note + ", ");
                                    } else {
                                        notesResult.Append(notify.Note + ".");
                                      }

                                }
                                bot.SendTextMessageAsync(new ChatId(user.Key), $"🔥 Time for <b>\"HappyBirthday!\"</b> to: <b>{notesResult}</b>", ParseMode.Html);
                                Thread.Sleep(100);
                                bot.SendAnimationAsync(new ChatId(user.Key), "CgACAgIAAxkBAAIHHmM94xCrPzChHapYDpgvFcDLqVmrAAJaGQACPqdgSaBzPaDz7RslKgQ");
                            }
                        }
                        Thread.Sleep(86400000);
                    }
                }
            });
        }
        private static void CommandCancelling(long ID) {
            if (ID is not default(long)) {
                if (usersInProcess.ContainsKey(ID)) {
                    usersInProcess[ID].OperationLevel = 0;
                    usersInProcess.Remove(ID);
                    return;
                }

                return;
            }
        }

        // Simple model, not designed for the fact that some months don't have 30 and 31 numbers.
        private async static Task DataInputChecking(int min, int max, Message? message) {
            if(message is not null) {
                if (Convert.ToInt32(message?.Text) < min || Convert.ToInt32(message?.Text) > max) {
                    await bot.SendTextMessageAsync(message?.Chat!, "Enter <b>valid number!</b>", ParseMode.Html);
                    return;
                }

                if (usersInProcess[message!.From!.Id].OperationLevel is 1) {
                    int day = Convert.ToInt32(message?.Text);
                    usersInProcess[message!.From!.Id].userNotifies.Add(new NotifierData() { Day = day });
                    usersInProcess[message!.From!.Id].OperationLevel++;
                } else if (usersInProcess[message!.From!.Id].OperationLevel is 2) {
                    int month = Convert.ToInt32(message?.Text);
                    usersInProcess[message!.From!.Id].userNotifies[usersInProcess[message!.From!.Id].userNotifies.Count - 1].Month = month;
                    usersInProcess[message!.From!.Id].OperationLevel++;
                }
                return;
            }
        }

#pragma warning disable
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
#pragma warning restore
    }
}
