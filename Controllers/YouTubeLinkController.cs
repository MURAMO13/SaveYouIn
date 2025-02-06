namespace SaveYouIn.Controllers;
internal class YouTubeLinkController
{
    private readonly ITelegramBotClient _telegramBot;
    public YouTubeLinkController(ITelegramBotClient botClient)
    {
        _telegramBot = botClient;
    }

    internal async Task DownloadYouTubeVideoAsync(Message message, Uri videoUri, int oldMsgId)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = StaticTemplates.ytDlpPath,
            Arguments = $"\"{videoUri}\" -f bestvideo+bestaudio --merge-output-format mp4 -o -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process? process = Process.Start(psi))
        {
            var downloadedFilePath = string.Empty;

            if (process != null)
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"Ошибка yt-dlp: {e.Data}");
                    }
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        downloadedFilePath = e.Data.Trim();
                        Console.WriteLine($"Файл загружен: {downloadedFilePath}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Видео скачано!");

                    var titleVideo = Path.GetFileNameWithoutExtension(downloadedFilePath);
                    using var stream = File.OpenRead(downloadedFilePath);
                    await _telegramBot.SendVideo(message.Chat.Id, stream, caption: titleVideo, replyParameters: message.MessageId);

                    File.Delete(downloadedFilePath);

                    try
                    {
                        await _telegramBot.DeleteMessage(message.Chat.Id, oldMsgId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не удалось удалить сообщение: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Произошла ошибка при скачивании видео. Код ошибки: " + process.ExitCode);
                }
            }
            else
            {
                Console.WriteLine("Не удалось запустить процесс.");
            }
        }
    }
}
