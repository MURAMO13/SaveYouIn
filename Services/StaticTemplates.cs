namespace SaveYouIn.Services;
internal class StaticTemplates
{
    internal static readonly string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
    internal static readonly string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

    internal static string folderPathMedia = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Media");

    internal static string folderPathYouTube = Path.Combine(folderPathMedia, "YouTube");
    internal static string folderPathInstagram = Path.Combine(folderPathMedia, "Instagram");

    public StaticTemplates()
    {
        try
        {
            Directory.CreateDirectory(folderPathMedia);
            Directory.CreateDirectory(folderPathYouTube);
            Directory.CreateDirectory(folderPathInstagram);
            Console.WriteLine("Каталоги успешно созданы.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании каталогов: {ex.Message}");
        }
    }

}
