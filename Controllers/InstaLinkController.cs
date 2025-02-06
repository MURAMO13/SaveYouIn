namespace SaveYouIn.Controllers;
internal class InstaLinkController
{
    private readonly ITelegramBotClient _telegramBot;

    public InstaLinkController(ITelegramBotClient telegramBot)
    {
        _telegramBot = telegramBot;
    }

    internal async Task DownloadInstagramMediaAsync(Message message, Uri mediaUri, int oldMsgId)
    {
        try
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "instagram_video.mp4");

            // Запускаем yt-dlp
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"\"{mediaUri}\" -f bestvideo+bestaudio --merge-output-format mp4 -o \"{filePath}\" --no-warnings",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!File.Exists(filePath))
                {
                    await _telegramBot.SendMessage(message.Chat.Id, $"Ошибка скачивания видео: {error}");
                    return;
                }
            }

            // Отправка видео в Telegram
            await using (var stream = File.OpenRead(filePath))
            {
                await _telegramBot.SendVideo(message.Chat.Id, InputFile.FromStream(stream));
            }

            // Удаляем старое сообщение, если нужно
            try
            {
                await _telegramBot.DeleteMessage(message.Chat.Id, oldMsgId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось удалить сообщение: {ex.Message}");
            }

            // Удаляем файл
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            await _telegramBot.SendMessage(message.Chat.Id, $"Ошибка: {ex.Message.ToString()}");
        }
    }
}
