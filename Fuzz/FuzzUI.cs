namespace Fuzz;

public static class FuzzUI
{
    private const string Version = "v1.5";
    public static void ShowBanner(FuzzOptions options)
    {
        Console.WriteLine("=======================================");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"dotnetFUZZ {Version}");
        Console.ResetColor();
        Console.WriteLine("=======================================");
        Console.WriteLine($"[+] URL:       {options.Url}");
        Console.WriteLine($"[+] Wordlist:  {options.DictionaryPath}");
        Console.WriteLine($"[+] Threads:   {options.Threads}");
        if (options.Extensions != null)
            Console.WriteLine($"[+] Exts:  {string.Join(",", options.Extensions)}");
        Console.WriteLine($"[+] Timeout:   {options.TimeoutSeconds}s");
        if (options.Verbose)
            Console.WriteLine($"[+] Verbose:   ON");
        Console.WriteLine();
    }

    public static void ShowHelp()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("  dotnet-fuzz -u <url> -w <wordlist> -t <threads> -x <exts> [--verbose] [-o file] [--timeout 5]");
        Console.WriteLine();
    }

    public static void DrawProgressBar(int current, int total, int barSize, int line)
    {
        double percent = (double)current / total;
        int filled = (int)(percent * barSize);
        string bar = "[" + new string('#', filled) + new string('-', barSize - filled) + "]";
        string status = $"{bar} {percent * 100:0.0}% ({current}/{total})";

        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, line);
        Console.Write(status.PadRight(Console.WindowWidth - 1));
        Console.SetCursorPosition(0, currentLine);
    }
}