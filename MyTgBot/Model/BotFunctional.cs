using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyTgBot.Model {
    internal static class BotFunctional {
        private static readonly ITelegramBotClient BotClient = new TelegramBotClient("<TOKEN>");

        private static readonly ConcurrentDictionary<long, User> UsersInProcess = new();
        private static readonly ConcurrentDictionary<long, User> UsersWithNotifications = new();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
            if (update.Type != UpdateType.Message || update.Message?.From == null) return;

            var message = update.Message;
            var userId = message.From.Id;

            if (UsersInProcess.ContainsKey(userId)) {
                await HandleInProgressCommand(userId, message);
                return;
            }

            await HandleCommand(message);
        }

        private static async Task HandleCommand(Message message) {
            var userId = message.From!.Id;

            switch (message.Text?.ToLower()) {
                case "/start":
                    await SendWelcomeMessage(message.Chat, message.From.FirstName);
                    break;
                case "/commands":
                    await SendCommandsList(message.Chat);
                    break;
                case "/addnf":
                    await StartAddNotificationProcess(userId, message.Chat);
                    break;
                case "/removenf":
                    await StartRemoveNotificationProcess(userId, message.Chat);
                    break;
                case "/mynotifyings":
                    await ListUserNotifications(userId, message.Chat);
                    break;
                case "/cancel":
                    await SendTextMessage(message.Chat, "No command started.");
                    break;
                default:
                    await SendTextMessage(message.Chat, "Undefined command.");
                    break;
            }
        }

        private static async Task HandleInProgressCommand(long userId, Message message) {
            if (message.Text?.ToLower() == "/cancel") {
                CancelUserCommand(userId);
                await SendTextMessage(message.Chat, "Command execution canceled.");
                return;
            }

            var user = UsersInProcess[userId];
            switch (user.OperationLevel) {
                case -1:
                    await RemoveNotification(userId, message);
                    break;
                case 1:
                    await HandleDayInput(userId, message);
                    break;
                case 2:
                    await HandleMonthInput(userId, message);
                    break;
                case 3:
                    await HandleNoteInput(userId, message);
                    break;
                default:
                    await SendTextMessage(message.Chat, "Invalid operation state.");
                    break;
            }
        }

        private static async Task SendWelcomeMessage(ChatId chatId, string userName) {
            await SendTextMessage(chatId, $"Hey {userName}, your notifier is here!\nCheck the /commands button.");
            await SendAnimation(chatId, "CgACAgIAAxkBAAIIdGNAPwPHXBhd73AUTtrcBG9sVyz2AAKKEwAC235RSW2fcqrKYAGaKgQ");
        }

        private static async Task SendCommandsList(ChatId chatId) {
            var commands = await System.IO.File.ReadAllLinesAsync("commands.txt");
            var response = string.Join(Environment.NewLine, commands);
            await SendTextMessage(chatId, "All commands:\n\n" + response);
        }

        private static async Task StartAddNotificationProcess(long userId, ChatId chatId) {
            if (UsersWithNotifications.TryGetValue(userId, out var user) && user.NotifyCount >= 30) {
                await SendTextMessage(chatId, "You have reached the notification limit! (30)");
                return;
            }

            var newUser = user ?? new User();
            newUser.OperationLevel = 1;
            UsersInProcess[userId] = newUser;

            await SendTextMessage(chatId, "Enter the number of birthday (1 - 31)");
        }

        private static async Task StartRemoveNotificationProcess(long userId, ChatId chatId) {
            if (!UsersWithNotifications.ContainsKey(userId)) {
                await SendTextMessage(chatId, "You don't have any notifies yet.");
                return;
            }

            var user = new User { OperationLevel = -1 };
            UsersInProcess[userId] = user;
            await SendTextMessage(chatId, "Enter the notification note you want to delete");
        }

        private static async Task ListUserNotifications(long userId, ChatId chatId) {
            if (!UsersWithNotifications.TryGetValue(userId, out var user) || user.NotifyCount == 0) {
                await SendTextMessage(chatId, "You don't have any notifies yet.");
                return;
            }

            var notifications = user.userNotifies.Select(n => $"{n.Note} — Day: {n.Day} / Month: {n.Month}");
            var response = string.Join(Environment.NewLine, notifications);

            await SendTextMessage(chatId, "Your notifyings:\n\n" + response);
        }

        private static void CancelUserCommand(long userId) {
            UsersInProcess.TryRemove(userId, out _);
        }

        private static async Task HandleDayInput(long userId, Message message) {
            if (!int.TryParse(message.Text, out var day) || day < 1 || day > 31) {
                await SendTextMessage(message.Chat, "Enter a valid number!");
                return;
            }

            var user = UsersInProcess[userId];
            user.userNotifies.Add(new NotifierData { Day = day });
            user.OperationLevel = 2;

            await SendTextMessage(message.Chat, "Enter month of birthday (1 - 12)");
        }

        private static async Task HandleMonthInput(long userId, Message message) {
            if (!int.TryParse(message.Text, out var month) || month < 1 || month > 12) {
                await SendTextMessage(message.Chat, "Enter a valid number!");
                return;
            }

            var user = UsersInProcess[userId];
            user.userNotifies[^1].Month = month;
            user.OperationLevel = 3;

            await SendTextMessage(message.Chat, "Enter note for birthday (1 - 20 letters)");
        }

        private static async Task HandleNoteInput(long userId, Message message) {
            var note = message.Text;

            if (string.IsNullOrEmpty(note) || note.Length > 20) {
                await SendTextMessage(message.Chat, "Note must be between 1 and 20 characters long");
                return;
            }

            var user = UsersInProcess[userId];
            if (user.userNotifies.Any(n => n.Note == note)) {
                await SendTextMessage(message.Chat, "Notify with this note already exists!");
                return;
            }

            user.userNotifies[^1].Note = note;
            UsersWithNotifications[userId] = user;
            UsersInProcess.TryRemove(userId, out _);

            await SendTextMessage(message.Chat, "Notification successfully created!");
            await SendSticker(message.Chat, "CAACAgIAAxkBAAIIkGNAQGVhIQLvY-_Pw6CG0QRZGDdYAAI8CAACSBUBS2kngY0iItlJKgQ");
        }

        private static async Task RemoveNotification(long userId, Message message) {
            var note = message.Text;
            if (UsersWithNotifications.TryGetValue(userId, out var user) &&
                user.userNotifies.RemoveAll(n => n.Note == note) > 0) {
                if (user.NotifyCount == 0) {
                    UsersWithNotifications.TryRemove(userId, out _);
                }

                UsersInProcess.TryRemove(userId, out _);
                await SendTextMessage(message.Chat, "Note successfully deleted.");
                return;
            }

            await SendTextMessage(message.Chat, "Enter valid note-key.");
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
            var errorMessage = exception switch {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private static async Task SendTextMessage(ChatId chatId, string text) {
            await BotClient.SendTextMessageAsync(chatId, text);
        }

        private static async Task SendSticker(ChatId chatId, string stickerId) {
            await BotClient.SendStickerAsync(chatId, stickerId);
        }

        private static async Task SendAnimation(ChatId chatId, string animationId) {
            await BotClient.SendAnimationAsync(chatId, animationId);
        }
    }
}