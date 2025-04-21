using System.Diagnostics;

public class Program
{
    private static bool isRunning = true;
    private static readonly object progressLock = new object();
    static int LastProgressLine => Console.WindowHeight - 1;
    
    static async Task Main(string[] args)
    {
        string? url = null;
        string? dictionaryPath = null;
        int parallelTasks = 10;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-u":
                    if (i + 1 < args.Length) url = args[++i];
                    break;
                case "-w":
                    if (i + 1 < args.Length) dictionaryPath = args[++i];
                    break;
                case "-t":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int t))
                    {
                        parallelTasks = t;
                        i++;
                    }
                    break;
                case "-h":
                    ShowHelp();
                    return;
                default:
                    Console.WriteLine($"Opción desconocida: {args[i]}");
                    ShowHelp();
                    return;
            }
        }

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(dictionaryPath))
        {
            Console.WriteLine("Faltan argumentos requeridos.");
            ShowHelp();
            return;
        }

        ShowBanner(url, parallelTasks, dictionaryPath);

        if (!File.Exists(dictionaryPath))
        {
            Console.WriteLine("No se encontró el archivo de diccionario.");
            return;
        }

        string[] keywords = (await File.ReadAllLinesAsync(dictionaryPath))
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .ToArray();
        
        string[] extensions = {
            "", ".bak", ".old", ".backup", ".orig", ".tmp", ".save",
            ".log", ".txt", ".out",
            ".zip", ".tar", ".tar.gz", ".rar", ".7z",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg",
            ".php", ".asp", ".aspx", ".jsp", ".js", ".html", ".css",
            ".sql", ".db", ".sqlite", ".sqlite3",
            ".conf", ".ini", ".env", ".yml", ".yaml", ".json", ".xml"
        };
        
        var keywordVariants = keywords
            .SelectMany(kw => extensions.Select(ext => kw + ext))
            .Distinct()
            .ToArray();
        
        int[] errorCodes = { 200, 204, 301, 302, 307, 401 };
        int total = keywordVariants.Length;
        int processed = 0;
        
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            isRunning = false;
            cts.Cancel();
        };
        
        DrawProgressBar(0, total, 30, LastProgressLine);
        
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        await Parallel.ForEachAsync(keywords, new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelTasks,
            CancellationToken = cts.Token
        },
        async (keyword, token) =>
        {
            if (!isRunning) return;
            
            string testUrl = url!.TrimEnd('/') + "/" + keyword;
            string? resultLine = null;
            
            try
            {
                HttpResponseMessage response = await client.GetAsync(testUrl, token);
                int statusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    Console.Write($"{keyword,-30} ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Status: {statusCode}");
                    Console.ResetColor();
                }
                else if (Array.IndexOf(errorCodes, statusCode) != -1 && statusCode != 404)
                {
                    resultLine = $"{keyword,-30} Status: {statusCode}";
                }
            }
            catch (OperationCanceledException) { /* ctrl+c */ }
            catch (HttpRequestException ex)
            {
                resultLine = $"Error al acceder a {testUrl}: {ex.Message}";
            }
            
            int current = Interlocked.Increment(ref processed);
            
            lock (progressLock)
            {
                if (resultLine != null)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine(resultLine);
                }
                
                DrawProgressBar(current, total, 30, LastProgressLine);
            }
        });

        stopwatch.Stop();
        
        Console.WriteLine();
        Console.WriteLine("Búsqueda completada.");
        Console.WriteLine($"Tiempo transcurrido: {stopwatch.Elapsed:hh\\:mm\\:ss}");
    }

    
    static void ShowHelp()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("  ejemplo.exe -u <url> -w <wordlist> -t <tareas>");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -u     URL objetivo para fuzzing");
        Console.WriteLine("  -w     Ruta del archivo de diccionario");
        Console.WriteLine("  -t     Número de tareas paralelas (por defecto: 10)");
        Console.WriteLine("  -h     Muestra este mensaje de ayuda");
        Console.WriteLine();
    }
    
    static void ShowBanner(string url, int threads, string wordlist)
    {
        Console.WriteLine("================================================");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("dotnet FUZZ");
        Console.ResetColor();
        Console.WriteLine("================================================");
        Console.WriteLine("v1.1\n");
        
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[+] {"URL:",-10} {url}");
        Console.WriteLine($"[+] {"Threads:",-10} {threads}");
        Console.WriteLine($"[+] {"Wordlist:",-10} {wordlist}");
        Console.WriteLine();
        Console.ResetColor();
    }
    
    static void DrawProgressBar(int current, int total, int barSize, int progressLine)
    {
        double percent = (double)current / total;
        int filled = (int)(percent * barSize);
        string bar = "[" + new string('#', filled) + new string('-', barSize - filled) + "]";
        string status = $"{bar} {percent * 100:0.0}% ({current}/{total})";

        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, progressLine);
        Console.Write(status.PadRight(Console.WindowWidth - 1));
        Console.SetCursorPosition(0, currentLine);
    }
}

