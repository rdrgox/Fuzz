using System.Diagnostics;

class Program
{
    private static bool isRunning = true;
    private static readonly object progressLock = new object();
    
    public static string ConsoleBlue(string text) => "\u001b[34m" + text + "\u001b[0m";
    public static string ConsoleGreen(string text) => "\u001b[32m" + text + "\u001b[0m";
    
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
                case "-d":
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

        string[] keywords = await File.ReadAllLinesAsync(dictionaryPath);
        int[] errorCodes = { 200, 204, 301, 302, 307, 401 };

        int total = keywords.Length;
        int processed = 0;
        int progressBarLine = Console.CursorTop + 1;
        
        Console.WriteLine();
        
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            isRunning = false;
            cts.Cancel();
        };

        using HttpClient client = new HttpClient();

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
            try
            {
                HttpResponseMessage response = await client.GetAsync(testUrl, token);
                int statusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    lock (progressLock)
                    {
                        Console.WriteLine();
                        Console.WriteLine("{0,-30} {1}{2,-20}", keyword, "Status: ", ConsoleGreen(statusCode.ToString()));
                        Console.WriteLine();
                    }
                }
                else if (Array.IndexOf(errorCodes, statusCode) != -1 && statusCode != 404)
                {
                    lock (progressLock)
                    {
                        Console.WriteLine();
                        Console.WriteLine("{0,-30} {1,-10}", keyword, $"Status: {statusCode}");
                        Console.WriteLine();
                    }
                }
            }
            catch (OperationCanceledException) { /* ctrl+c */ }
            catch (HttpRequestException ex)
            {
                lock (progressLock)
                {
                    Console.WriteLine($"Error al acceder a {testUrl}: {ex.Message}");
                }
            }
            
            int current = Interlocked.Increment(ref processed);
            lock (progressLock)
            {
                DrawProgressBar(current, total, 30);
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
        Console.WriteLine("  ejemplo.exe -u <url> -d <diccionario> [-t <tareas>]");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -u     URL objetivo para fuzzing");
        Console.WriteLine("  -d     Ruta del archivo de diccionario");
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
        Console.WriteLine("v0.2\n");
        
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[+] URL:      {url}");
        Console.WriteLine($"[+] Threads:  {threads}");
        Console.WriteLine($"[+] Wordlist: {wordlist}");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    static void DrawProgressBar(int current, int total, int barSize)
    {
        double percent = (double)current / total;
        int filled = (int)(percent * barSize);
        string bar = "[" + new string('#', filled) + new string('-', barSize - filled) + "]";
        
        Console.Write($"\r{bar} {percent * 100:0.0}% ({current}/{total})     ");
    }
}

