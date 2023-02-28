using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace IDecisBot
{
    class IDecisBot : TelegramBotClient
    {
        private Dictionary<string, User> commandIDs = new Dictionary<string, User>()
        {
            { "2ew", new User()},
            { "2ye", new User()},
            { "2ux", new User()},
            { "2xv", new User()},
            { "2sd", new User()},
            { "2kd", new User()},
            { "2qw", new User()},
            { "2tr", new User()}
        };

        private string leaderID = "2qt";

        private User leaderChat = new User();

        private List<string> ideas = new List<string>();

        private List<string> criterias = new List<string>();

        public IDecisBot(string token)
            : base(token)
        {

        }

        private ReplyKeyboardMarkup replyMarkupUser = new(new[]
        {
                    new KeyboardButton[] { "Добавить идею"},
                    new KeyboardButton[] { "Добавить критерий"},
                    new KeyboardButton[] { "Посмотреть статистику"},
        })
        {
            ResizeKeyboard = true
        };

        private ReplyKeyboardMarkup replyMarkupLeader = new(new[]
        {
                    new KeyboardButton[] { "Добавить идею"},
                    new KeyboardButton[] { "Добавить критерий"},
                    new KeyboardButton[] { "Начать голосование"},
                    new KeyboardButton[] { "Посмотреть статистику"},
                    new KeyboardButton[] { "Пройти тест Белбина"}
                })
        {
            ResizeKeyboard = true
        };

        private ReplyKeyboardMarkup replyMarkupUnathorized = new(new[]
        {
                    new KeyboardButton[] { "Авторизация"},
        })
        {
            ResizeKeyboard = true
        };

        private ReplyKeyboardMarkup replyMarkupVoting = new(new[]
{
                    new KeyboardButton[] { "1", "2", "3", "4", "5" },
                    new KeyboardButton[] { "6", "7", "8", "9", "10" }
        })
        {
            ResizeKeyboard = true
        };

        /// <summary>
        /// Starts receiving updates.
        /// </summary>
        /// <param name="receiverOptions"></param>
        /// <param name="cancellationTokenSource"></param>
        public void StartReceiving(ReceiverOptions receiverOptions, CancellationTokenSource cancellationTokenSource)
        {
            this.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cancellationTokenSource.Token);
        }

        /// <summary>
        /// Handles chat bot updades.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;

            // Only process text messages
            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var userFirstName = update.Message.Chat.FirstName;
            var messageText = update.Message.Text;

            if(messageText == "/start")
            {
                await HandleStartCommandAsync(botClient, cancellationToken, chatId, userFirstName);
                return;
            }

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            User userChat = null;

            foreach (string login in commandIDs.Keys)
            {
                User user = commandIDs[login];
                if (user.chatID.Equals(chatId))
                {
                    userChat = user;
                }
            }

            if(userChat == null && chatId.Equals(leaderChat.chatID))
            {
                userChat = leaderChat;
                userChat.leader = true;
            }

            //

            if (messageText.Trim() == "Авторизация")
            {
                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, пришлите ваш логин");
                return;
            }

            if (messageText.Trim() == leaderID && leaderChat.chatID == -1)
            {
                leaderChat.chatID = chatId;
                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Вы авторизованы как лидер команды",
                replyMarkup: replyMarkupLeader);
                return;
            }

            if (commandIDs.ContainsKey(messageText.Trim()) && commandIDs[messageText.Trim()].chatID == -1)
            {
                // This method will send a text message (obviously).
                User user = commandIDs[messageText.Trim()];

                user.chatID = chatId;

                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Вы авторизованы как участник команды!",
                replyMarkup: replyMarkupUser);
                return;
            }

            //

            if(userChat == null)
            {
                Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Вы не авторизовались!",
                replyMarkup: replyMarkupUnathorized);
                return;
            }

            //

            if(userChat.inPool == true)
            {
                if (int.TryParse(messageText.Trim(), out int value))
                {
                    if (value >= 1 && value <= 10)
                    {
                        userChat.poolResults[userChat.poolQuestionNum][userChat.criteriaNum] = value;
                        userChat.criteriaNum++;
                    }
                    else
                    {
                        await userChat.SendMessageAsync(botClient, "Число должно быть от 1 до 10", replyMarkupVoting).ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    await userChat.SendMessageAsync(botClient, "Пожалуйста, введите число", replyMarkupVoting).ConfigureAwait(false);
                    return;
                }

                if (userChat.criteriaNum >= criterias.Count)
                {
                    userChat.poolQuestionNum++;
                    userChat.poolResults.Add(new int[criterias.Count]);
                    userChat.criteriaNum = 0;

                    if (userChat.poolQuestionNum < ideas.Count)
                    {
                        await userChat.SendMessageAsync(botClient, "Идея: " + ideas[userChat.poolQuestionNum], null).ConfigureAwait(false);
                    }

                }

                if (userChat.poolQuestionNum >= ideas.Count)
                {
                    await userChat.SendMessageAsync(botClient, "вы успешно проголосовали", userChat.leader ? replyMarkupLeader: replyMarkupUser).ConfigureAwait(false);
                    userChat.inPool = false;
                    userChat.poolQuestionNum = 0;
                    return;
                }

                await userChat.SendMessageAsync(botClient, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[userChat.criteriaNum], replyMarkupVoting);
                return;
            }

            //

            if (userChat.listenForIdea)
            {
                ideas.Add(messageText);
                userChat.listenForIdea = false;
                await userChat.SendMessageAsync(botClient, "Идея " + messageText + " добавлена", userChat.leader ? replyMarkupLeader : replyMarkupUser);
                return;
            }

            if (userChat.listenForCriteria)
            {
                criterias.Add(messageText);
                userChat.listenForCriteria = false;
                await userChat.SendMessageAsync(botClient, "Критерий добавлен", userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
                return;
            }

            if(userChat.inBelbin)
            {
                int result = userChat.belbin.ParseAnswer(messageText);
                if(result == -1)
                {
                    await userChat.SendMessageAsync(botClient, "Вы прислали ответ в неправильном формате. Используйте шаблон!", null).ConfigureAwait(false);
                }
                else if(result == -2)
                {
                    await userChat.SendMessageAsync(botClient, "В сумме должно получаться 10. Смотрите пример!", null).ConfigureAwait(false);
                }
                else if(result == 0 && !userChat.belbin.end)
                {
                    await userChat.SendMessageAsync(botClient, userChat.belbin.GetHead(), null).ConfigureAwait(false);
                    string prompts = "";
                    string[] headPrompts = userChat.belbin.GetPrompts();
                    for (int i = 0; i < headPrompts.Length; i++) prompts += headPrompts[i] + '\n';
                    await userChat.SendMessageAsync(botClient, prompts, null).ConfigureAwait(false);
                }
                else if (result == 0 && userChat.belbin.end)
                {
                    await userChat.SendMessageAsync(botClient, "Вы успешно прошли тест Белбина! Ваши результаты:", null).ConfigureAwait(false);
                    Dictionary<string, int> results = userChat.belbin.GetResults();
                    foreach (string key in results.Keys)
                    {
                        await userChat.SendMessageAsync(botClient, key + ". Результат: " + results[key].ToString(), userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
                    }
                    userChat.inBelbin = false;
                }
                return;
            }

            string command = messageText.ToLower().Trim();

            if (command == "добавить идею")
            {
                userChat.listenForIdea = true;
                await userChat.SendMessageAsync(botClient, "Пожалуйста, введите название идеи", null).ConfigureAwait(false);
                return;
            }

            if (command == "добавить критерий")
            {
                userChat.listenForCriteria = true;
                await userChat.SendMessageAsync(botClient, "Пожалуйста, введите описание критерия", null).ConfigureAwait(false);
                return;
            }

            //

            if (command == "начать голосование" && chatId.Equals(leaderChat.chatID))
            {
                await StartQuestionaryAsync(userChat, botClient).ConfigureAwait(false);
                return;
            }

            //

            if (command == "посмотреть статистику")
            {
                await SendStatisticsAsync(userChat, botClient).ConfigureAwait(false);
                return;
            }

            if (command == "пройти тест белбина")
            {
                await StartBelbinTest(userChat, botClient).ConfigureAwait(false);
                return;
            }

            await userChat.SendMessageAsync(botClient, "Я не знаю такой команды :(", userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
        }

        private async Task StartQuestionaryAsync(User userChat, ITelegramBotClient botClient)
        {
            if (ideas.Count == 0)
            {
                await userChat.SendMessageAsync(botClient, "Нет идеи, за которые можно проголосовать. Сначала добавьте идеи", userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
                return;
            }

            if (criterias.Count == 0)
            {
                await userChat.SendMessageAsync(botClient, "Нет критериев, по которым нужно проголосовать. Сначала добавьте критерии", userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
                return;
            }

            await userChat.SendMessageAsync(botClient, "Голосование начато", null).ConfigureAwait(false);
            foreach (string userLogin in commandIDs.Keys)
            {
                User user = commandIDs[userLogin];
                if (user.chatID != -1)
                {
                    user.inPool = true;
                    user.poolResults.Clear();
                    await userChat.SendMessageAsync(botClient, "Идея: " + ideas[0], null);
                    await userChat.SendMessageAsync(botClient, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[0], replyMarkupVoting);
                    user.criteriaNum = 0;
                    user.poolResults.Add(new int[criterias.Count]);
                }
            }
            leaderChat.inPool = true;
            leaderChat.poolResults.Clear();
            await userChat.SendMessageAsync(botClient, "Идея: " + ideas[0], null).ConfigureAwait(false);
            await userChat.SendMessageAsync(botClient, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[0], replyMarkupVoting).ConfigureAwait(false);
            leaderChat.criteriaNum = 0;
            leaderChat.poolResults.Add(new int[criterias.Count]);
            return;
        }

        private async Task SendStatisticsAsync(User userChat, ITelegramBotClient botClient)
        {
            for (int i = 0; i < ideas.Count; i++)
            {
                double med = 0;
                int a = 0;

                foreach (User user in commandIDs.Values)
                {
                    if (user.poolResults.Count >= i + 1)
                    {
                        double medCr = 0;
                        for (int j = 0; j < user.poolResults[i].Length; j++)
                        {
                            medCr += user.poolResults[i][j];
                        }
                        med += ((double)medCr / user.poolResults[i].Length);
                        a++;
                    }
                }

                if (leaderChat.poolResults.Count >= i + 1)
                {
                    double medCr = 0;
                    for (int j = 0; j < leaderChat.poolResults[i].Length; j++)
                    {
                        medCr += leaderChat.poolResults[i][j];
                    }
                    med += ((double)medCr / leaderChat.poolResults[i].Length);
                    a++;
                }

                string s = (Math.Round((double)med / a, 2)).ToString();

                await userChat.SendMessageAsync(botClient, "За идею " + ideas[i] + " проголосовало " + a + " человек, средний балл: " + s, userChat.leader ? replyMarkupLeader : replyMarkupUser).ConfigureAwait(false);
            }
        }

        private async Task StartBelbinTest(User userChat, ITelegramBotClient botClient)
        {
            userChat.belbin = new BelbinTest();

            await userChat.SendMessageAsync(botClient, "Вы начали проходить тест Белбина. В каждом из семи разделов распределите 10 баллов между возможными ответами согласно тому, как Вы полагаете, они лучше всего подходят Вашему собственному поведению. Эти десять пунктов могут быть распределены поровну или, возможно, все приданы одному единственному ответу.", null).ConfigureAwait(false);
            await userChat.SendMessageAsync(botClient, "Для проходения теста используйте следующий шаблон (скопируйте его, и вставляйте каждый раз ваши значения вместо нулей)", null).ConfigureAwait(false);
            await userChat.SendMessageAsync(botClient, userChat.belbin.GetPattern(), null).ConfigureAwait(false);
            await userChat.SendMessageAsync(botClient, "Пример заполнения шаблона (в сумме получается 10)", null).ConfigureAwait(false);
            await userChat.SendMessageAsync(botClient, userChat.belbin.GetExample(), null).ConfigureAwait(false);
            userChat.inBelbin = true;
            await userChat.SendMessageAsync(botClient, userChat.belbin.GetHead(), null).ConfigureAwait(false);
            string prompts = "";
            string[] headPrompts = userChat.belbin.GetPrompts();
            for (int i = 0; i < headPrompts.Length; i++) prompts += headPrompts[i] + '\n';
            await userChat.SendMessageAsync(botClient, prompts, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Hadles user's message sent to chat bot.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="chatId"></param>
        /// <param name="messageText"></param>
        /// <returns></returns>
        private async Task HandleUserInputAsync(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string messageText)
        {

        }

        /// <summary>
        /// Handles the "/start" command. 
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="chatId"></param>
        /// <param name="userFirstName"></param>
        /// <returns></returns>
        private async Task HandleStartCommandAsync(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string userFirstName)
        {
            Message sendSticker = await botClient.SendStickerAsync(
                    chatId: chatId,
                    sticker: "CAACAgIAAxkBAAEEKwliMJIttXq76fgq4G2dpIos37lixgACBQADwDZPE_lqX5qCa011IwQ",
                    cancellationToken: cancellationToken);

            Message sendGreetingMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Привет, {userFirstName}!\n\n" +
                      "Меня зовут Growther. " +
                      "Я был создан, чтобы сделать брейнштормы в твоей команде эффективнее " +
                      $"(используй меня, чтобы автоматизировать брейнштормы в твоей команде)\n\n",
                replyMarkup: replyMarkupUnathorized,
                cancellationToken: cancellationToken);

            Message sendRulesMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Чтобы начать, авторизуйся с помощью команды Авторизация",
                cancellationToken: cancellationToken);
        }
        private Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }
    }
}
