using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Taikandi.Telebot;
using Taikandi.Telebot.Types;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace IDecisBot
{
    class Program
    {

        public static async Task Main()
        {
            var token = "6195704050:AAEms2By2Bh3-Bptj1YRjrg1cxos24g1ejQ";
            var botClient = new IDecisBot(token);
            using var cancellationTokenSource = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            botClient.StartReceiving(receiverOptions, cancellationTokenSource);

            var me = await botClient.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cancellationTokenSource.Cancel();
        }
    }
}
