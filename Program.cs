using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taikandi.Telebot;
using Taikandi.Telebot.Types;

namespace IDecisBot
{
    class Program
    {
        public static async Task Main()
        {
            IDecisBot bot = new IDecisBot();
            await bot.RunAsync();
        }
    }
}
