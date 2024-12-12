using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using MyTgBot.Model;

namespace TelegramBotExperiments {
    class Program {
        static void Main(string[] args) {
            var bot = new TelegramBotClient("<TOKEN>");

            Console.WriteLine("Starting bot: " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions {
                AllowedUpdates = { }, // receive all update types
            };

            bot.StartReceiving(
                updateHandler: BotFunctional.HandleUpdateAsync,
                pollingErrorHandler: BotFunctional.HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            Console.WriteLine("Bot is running. Press Enter to stop...");
            Console.ReadLine();

            cts.Cancel(); // Stop receiving updates on program exit
        }
    }
}