using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Taikandi.Telebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace IDecisBot
{
    class User
    {
        public bool leader;

        public long chatID;

        public bool inPool;

        public bool inBelbin;

        public bool listenForIdea;

        public bool listenForCriteria;

        public int poolQuestionNum;
        public int criteriaNum;
        public List<int[]> poolResults;

        public BelbinTest belbin;

        public User()
        {
            chatID = -1;

            inPool = false;
            leader = false;
            poolQuestionNum = 0;
            criteriaNum = 0;
            poolResults = new List<int[]>();
        }

        public async Task SendMessageAsync(ITelegramBotClient botClient, string message, ReplyKeyboardMarkup markup)
        {
            if (markup == null)
            {
                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatID,
                text: message,
                replyMarkup: new ReplyKeyboardRemove());
            }
            else
            {
                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatID,
                text: message,
                replyMarkup: markup);
            }
        }
    }
}
