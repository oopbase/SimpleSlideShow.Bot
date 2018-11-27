using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace SimpleSlideShow.Bot.Services
{
    internal class TelegramService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _saveToPath;

        public TelegramService(string apiToken, string saveToPath)
        {
            _botClient = new TelegramBotClient(apiToken);
            _botClient.OnMessage += BotOnMessageReceived;
            _saveToPath = saveToPath;
        }

        private void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;
                if (message != null)
                {
                    if (IsStartCommand(message))
                    {
                        _botClient.SendTextMessageAsync(message.Chat.Id, Messages.GetWelcomeMessage(message.From.LanguageCode));
                    }
                    else if (IsHelpCommand(message))
                    {
                        _botClient.SendTextMessageAsync(message.Chat.Id, Messages.GetHelpMessage(message.From.LanguageCode));
                    }
                    else if (HasPhoto(message))
                    {
                        DownloadPhoto(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Error while processing message '{messageEventArgs.Message?.Text}'");
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
            }

        }

        internal void StopBot()
        {
            _botClient.StopReceiving();
        }

        internal void StartBot()
        {
            _botClient.StartReceiving(new[] { UpdateType.Message });
        }

        private void DownloadPhoto(Telegram.Bot.Types.Message message)
        {
            var photo = message.Photo.OrderByDescending(p => p.FileSize).First();
            var file = _botClient.GetFileAsync(photo.FileId).Result;
            if (file != null)
            {
                DownloadFile(file, message.From);
                SendRandomThankYouMessage(message);
            }
        }

        private void SendRandomThankYouMessage(Telegram.Bot.Types.Message message)
        {
            _botClient.SendTextMessageAsync(message.Chat.Id, Messages.GetThankYou(message.From.LanguageCode));
        }

        private void DownloadFile(Telegram.Bot.Types.File file, Telegram.Bot.Types.User from)
        {
            var username = GetUsername(from); ;

            var originalPath = file.FilePath;
            string pathToFile = BuildPathToFile(username, originalPath);
            using (var outputFile = File.Create(pathToFile))
            {
                Console.WriteLine($"Writing {pathToFile}...");
                var fileStream = _botClient.DownloadFileAsync(originalPath, outputFile, new System.Threading.CancellationToken());
                Task.WaitAll(fileStream);
            }
        }

        private string BuildPathToFile(string username, string originalPath)
        {
            var filename = originalPath.Substring(originalPath.LastIndexOf("/") + 1);
            var identifier = filename.Substring(0, filename.LastIndexOf("."));
            var extension = filename.Substring(filename.LastIndexOf("."));
            var pathToFile = Path.Combine(_saveToPath, string.Join("-", identifier, username).Trim('-') + extension);
            return pathToFile;
        }

        private static string GetUsername(Telegram.Bot.Types.User from)
        {
            var username = "";

            if (!string.IsNullOrEmpty(from.FirstName))
            {
                username = from.FirstName;
            }

            if (!string.IsNullOrEmpty(from.LastName))
            {
                username = string.Join("_", username, from.LastName).Trim('_');
            }

            if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(from.Username))
            {
                username = from.Username;
            }

            return username;
        }

        private static bool HasPhoto(Telegram.Bot.Types.Message message)
        {
            return message.Photo != null && message.Photo.Length > 0;
        }

        private static bool IsHelpCommand(Telegram.Bot.Types.Message message)
        {
            return "/help".Equals(message.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsStartCommand(Telegram.Bot.Types.Message message)
        {
            return "/start".Equals(message.Text, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}