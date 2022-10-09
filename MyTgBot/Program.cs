using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using MyTgBot.Model;

namespace TelegramBotExperiments {

    class Program {

        static void Main(string[] args) {

            var bot = BotFunctional.bot;

            Dictionary<string, object> data = new();

            Console.WriteLine("Starting bot " + bot.GetMeAsync().Result.FirstName);
#pragma warning disable
            BotFunctional.DateTracking();
#pragma warning restore
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                BotFunctional.HandleUpdateAsync,
                BotFunctional.HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
    }
}