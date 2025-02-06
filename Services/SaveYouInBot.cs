namespace SaveYouIn.Services;

internal class SaveYouInBot : BackgroundService
{
    public static event Action? TaskAdded;
    private static bool _isEventWorking = true; 

    private ITelegramBotClient _telegramBot;
    private YouTubeLinkController _youTubeLinkController;
    private InstaLinkController _instaLinkController;

    
    private static readonly ConcurrentQueue<(Message message, Uri uri,int oldMsgId)> _taskQueue = new ConcurrentQueue<(Message, Uri, int)>();

    private static readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(6); // 6 потоков в моменте.

    public SaveYouInBot(ITelegramBotClient telegramBot, YouTubeLinkController youTubeLinkController, InstaLinkController instaLinkController)
    {
        _ = new StaticTemplates();
        _telegramBot = telegramBot;
        _youTubeLinkController = youTubeLinkController;
        _instaLinkController = instaLinkController;

        TaskAdded += WorkWithQueueAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _telegramBot.StartReceiving
        (                           HandleUpdatesAsync, 
                                    HandleError, 
                                    new ReceiverOptions() { AllowedUpdates = { } }, 
                                    cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            Console.WriteLine($"Получено сообщение {update.Message.Text}");

            var userMsg = update.Message.Text;

            if (userMsg.ToLower() == "/start")
            {
                // Сообщение приветствия
                await _telegramBot.SendMessage
                (
                    update.Message.Chat.Id,
                    "Привет! Отправьте ссылку на видео или фото из YouTube или Instagram, чтобы я скачал её для вас.\n" +
                    "\nHello there! Send a link to a video or photo from YouTube or Instagram so that I can download it for you.",
                    replyParameters: update.Message.MessageId,
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Анализ ссылки на корректность и добавление в очередь
            if (Uri.TryCreate(userMsg, UriKind.Absolute, out Uri? uri))
            {
                var oldMsgId = (await _telegramBot.SendMessage
                (
                   update.Message.Chat.Id,
                   "Добавляю вашу задачу в очередь...\n" +
                   "Мы приступили к работе, в ближайшее время сообщим о результатах." +
                   "\n\n Adding your task to the queue..." +
                   "We have started working, and we will report on the results of the project in the near future",
                   replyParameters: update.Message.MessageId,
                   cancellationToken: cancellationToken
                )).MessageId;

                _taskQueue.Enqueue((update.Message, uri, oldMsgId));

                if (_isEventWorking)
                {
                    TaskAdded?.Invoke();

                    _isEventWorking = false;
                }

                return;
            }
        }

        if (update.Message != null)
        {
            await _telegramBot.SendMessage
                   (
                      update.Message.Chat.Id,
                      "Пожалуйста, отправьте правильную ссылку!\n" +
                      "Please send the correct link!",
                      replyParameters: update.Message.MessageId,
                      cancellationToken: cancellationToken
                   );
        }
    }

    private Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Задаем сообщение об ошибке в зависимости от того, какая именно ошибка произошла
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        // Выводим в консоль информацию об ошибке
        Console.WriteLine(errorMessage + "\n");

        // Задержка перед повторным подключением
        Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
        Task.Delay(10000, cancellationToken);

        return Task.CompletedTask;
    }

    private async void WorkWithQueueAsync()
    {
        await _queueSemaphore.WaitAsync();

        try
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                var (message, uri, oldMsgId) = task;
                try
                {
                    if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
                    {
                        await _youTubeLinkController.DownloadYouTubeVideoAsync(message, uri, oldMsgId);
                    }
                    else if (uri.Host.Contains("instagram.com"))
                    {
                        await _instaLinkController.DownloadInstagramMediaAsync(message, uri, oldMsgId);
                    }
                    else
                    {
                        await _telegramBot.SendMessage(message.Chat.Id, "Ссылка не поддерживается.", replyParameters: message.MessageId);
                    }
                }
                catch (Exception ex)
                {
                    await _telegramBot.SendMessage(message.Chat.Id, $"Ошибка при обработке ссылки: {ex.Message}", replyParameters: message.MessageId);
                    Console.WriteLine($"Ошибка при обработке ссылки: {ex.Message}");

                    return;
                }
            }
            
            // После обработки очереди мы снова можем вызывать событие
            _isEventWorking = true;
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Ошибка в процессе обработки очереди: {exc.Message}");
        }
        finally
        {
            _queueSemaphore.Release();
        }
    }
}
