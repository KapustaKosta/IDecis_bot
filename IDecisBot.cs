using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taikandi.Telebot;
using Taikandi.Telebot.Types;

namespace IDecisBot
{
    class IDecisBot
    {
        private readonly Telebot _telebot = new Telebot("6195704050:AAEms2By2Bh3-Bptj1YRjrg1cxos24g1ejQ");

        private Dictionary<string, User> commandIDs = new Dictionary<string, User>()
        {
            { "2ew", new User()},
            { "2ye", new User()},
            { "2xc", new User()}
        };

        private string leaderID = "2qt";

        private User leaderChat = new User();

        private List<string> ideas = new List<string>();

        private List<string> criterias = new List<string>();

        private bool listenForIdea = false;

        private bool listenForCriteria = false;


        public async Task RunAsync()
        {
            // Used for getting only the unconfirmed updates.
            // It is recommended to stored this value between sessions. 
            // More information at https://core.telegram.org/bots/api#getting-updates
            var offset = 0L;

            while (true)
            {
                // Use this method to receive incoming updates using long polling.
                // Or use Telebot.SetWebhook() method to specify a URL to receive incoming updates.
                List<Update> updates = (await this._telebot.GetUpdatesAsync(offset).ConfigureAwait(false)).ToList();
                if (updates.Any())
                {
                    offset = updates.Max(u => u.Id) + 1;

                    foreach (Update update in updates)
                    {
                        switch (update.Type)
                        {
                            case UpdateType.Message:
                                await this.CheckMessagesAsync(update.Message).ConfigureAwait(false);
                                break;
                            case UpdateType.InlineQuery:
                                await this.CheckInlineQueryAsync(update).ConfigureAwait(false);
                                break;
                            case UpdateType.ChosenInlineResult:
                                this.CheckChosenInlineResult(update);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }

        private async Task CheckMessagesAsync(Message message)
        {
            // Assume we are doing more than echoing stuff.
            if (message == null)
                return;

            // This method will tell the user that something is happening on the bot's side.
            // It is recommended to use this method when a response from the bot 
            // will take a noticeable amount of time to arrive.
            await this._telebot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            if (listenForIdea == true)
            {
                ideas.Add(message.Text);
                listenForIdea = false;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Идея " + message.Text + " добавлена").ConfigureAwait(false);
                return;
            }

            if (listenForCriteria == true)
            {
                criterias.Add(message.Text);
                listenForCriteria = false;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Критерий добавлен").ConfigureAwait(false);
                return;
            }

            User userChat = null;

            foreach (string login in commandIDs.Keys)
            {
                User user = commandIDs[login];
                if (user.chatID.Equals(message.Chat.Id))
                {
                    userChat = user;
                }
            }

            if(userChat == null && message.Chat.Id.Equals(leaderChat.chatID))
            {
                userChat = leaderChat;
            }

            if (userChat != null && userChat.inPool == true)
            {
                if (int.TryParse(message.Text, out int value))
                {
                    if (value >= 1 && value <= 10)
                    {
                        userChat.poolResults[userChat.poolQuestionNum][userChat.criteriaNum] = value;
                        userChat.criteriaNum++;
                    }
                    else
                    {
                        await this._telebot.SendMessageAsync(message.Chat.Id, "Число должно быть от 1 до 10").ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    await this._telebot.SendMessageAsync(message.Chat.Id, "Пожалуйста, введите число").ConfigureAwait(false);
                    return;
                }

                if (userChat.criteriaNum >= criterias.Count)
                {
                    userChat.poolQuestionNum++;
                    userChat.poolResults.Add(new int[criterias.Count]);
                    userChat.criteriaNum = 0;

                    if(userChat.poolQuestionNum < ideas.Count)
                    {
                        await this._telebot.SendMessageAsync(userChat.chatID, "Идея: " + ideas[userChat.poolQuestionNum]).ConfigureAwait(false);
                    }

                }

                if (userChat.poolQuestionNum >= ideas.Count)
                {
                    await this._telebot.SendMessageAsync(message.Chat.Id, "вы успешно проголосовали").ConfigureAwait(false);
                    userChat.inPool = false;
                    userChat.poolQuestionNum = 0;
                    return;
                }

                await this._telebot.SendMessageAsync(userChat.chatID, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[userChat.criteriaNum]).ConfigureAwait(false);
            }

            //

            if (message.Text.Trim() == "Авторизация")
            {
                await this._telebot.SendMessageAsync(message.Chat.Id, "Пожалуйста, пришлите ваш логин").ConfigureAwait(false);
            }

            if (message.Text.Trim() == leaderID && leaderChat.chatID == -1)
            {
                leaderChat.chatID = message.Chat.Id;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Вы авторизованы как лидер команды").ConfigureAwait(false);
                return;
            }

            if (commandIDs.ContainsKey(message.Text.Trim()) && commandIDs[message.Text.Trim()].chatID == -1)
            {
                // This method will send a text message (obviously).
                User user = commandIDs[message.Text.Trim()];

                user.chatID = message.Chat.Id;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Вы авторизованы как участник команды").ConfigureAwait(false);
                return;
            }

            //

            if (message.Text.Trim() == "Добавить идею")
            {
                listenForIdea = true;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Пожалуйста, введите название идеи").ConfigureAwait(false);
            }

            if (message.Text.Trim() == "Добавить критерий")
            {
                listenForCriteria = true;
                await this._telebot.SendMessageAsync(message.Chat.Id, "Пожалуйста, введите описание критерия").ConfigureAwait(false);
            }

            //

            if (message.Text.Trim() == "Начать голосование" && message.Chat.Id.Equals(leaderChat.chatID))
            {
                if(ideas.Count == 0)
                {
                    await this._telebot.SendMessageAsync(message.Chat.Id, "Нет идеи, за которые можно проголосовать. Сначала добавьте идеи").ConfigureAwait(false);
                    return;
                }

                if (criterias.Count == 0)
                {
                    await this._telebot.SendMessageAsync(message.Chat.Id, "Нет критериев, по которым нужно проголосовать. Сначала добавьте критерии").ConfigureAwait(false);
                    return;
                }

                await this._telebot.SendMessageAsync(message.Chat.Id, "Голосование начато").ConfigureAwait(false);
                foreach (string userLogin in commandIDs.Keys)
                {
                    User user = commandIDs[userLogin];
                    if (user.chatID != -1)
                    {
                        user.inPool = true;
                        user.poolResults.Clear();
                        await this._telebot.SendMessageAsync(user.chatID, "Идея: " + ideas[0]).ConfigureAwait(false);
                        await this._telebot.SendMessageAsync(user.chatID, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[0]).ConfigureAwait(false);
                        user.criteriaNum = 0;
                        user.poolResults.Add(new int[criterias.Count]);
                    }
                }
                leaderChat.inPool = true;
                leaderChat.poolResults.Clear();
                await this._telebot.SendMessageAsync(leaderChat.chatID, "Идея: " + ideas[0]).ConfigureAwait(false);
                await this._telebot.SendMessageAsync(leaderChat.chatID, "Пожалуйста, проголосуйте от 0 до 10 за эту идею по критерию " + criterias[0]).ConfigureAwait(false);
                leaderChat.criteriaNum = 0;
                leaderChat.poolResults.Add(new int[criterias.Count]);
            }

            //

            if (message.Text.Trim() == "Посмотреть статистику")
            {
                for(int i = 0; i < ideas.Count; i++)
                {
                    double med = 0;
                    int a = 0;

                    foreach(User user in commandIDs.Values)
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

                    string s = (Math.Round((double) med / a, 2)).ToString();

                    await this._telebot.SendMessageAsync(leaderChat.chatID, "За идею " + ideas[i] + " проголосовало " + a + " человек, средний балл: " + s).ConfigureAwait(false);
                }
            }
        }

        private async Task CheckInlineQueryAsync(Update update)
        {
            // Telebot will support all 19 types of InlineQueryResult.
            // To see available inline query results:
            // https://core.telegram.org/bots/api#answerinlinequery
            var articleResult = new InlineQueryResultArticle
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "This is a title",
                Url = "https://core.telegram.org/bots/api#inlinequeryresultarticle"
            };

            var photoResult = new InlineQueryResultPhoto
            {
                Id = Guid.NewGuid().ToString("N"),
                Url = "https://telegram.org/file/811140636/1/hzUbyxse42w/4cd52d0464b44e1e5b",
                ThumbnailUrl = "https://telegram.org/file/811140636/1/hzUbyxse42w/4cd52d0464b44e1e5b"
            };


            var gifResult = new InlineQueryResultGif
            {
                Id = Guid.NewGuid().ToString("N"),
                Url = "http://i.giphy.com/ya4eevXU490Iw.gif",
                ThumbnailUrl = "http://i.giphy.com/ya4eevXU490Iw.gif"
            };

            var results = new InlineQueryResult[] { articleResult, photoResult, gifResult };
            await this._telebot.AnswerInlineQueryAsync(update.InlineQuery.Id, results).ConfigureAwait(false);
        }

        private void CheckChosenInlineResult(Update update)
        {
            Console.WriteLine("Received ChosenInlineResult.");
        }
    }
}
