using System.Diagnostics;

namespace Fuzz;

public class Fuzz
{
    private readonly FuzzOptions _options;
    private readonly object _lock = new();
    private bool _running = true;
    private int _processed = 0;
    private List<string> _results = new();

    public Fuzz(FuzzOptions options)
    {
        _options = options;
    }

    public async Task RunAsync()
    {
        FuzzUI.ShowBanner(_options);

        if (!File.Exists(_options.DictionaryPath))
        {
            Console.WriteLine("No se encontró el archivo de diccionario.");
            return;
        }

        var lines = await File.ReadAllLinesAsync(_options.DictionaryPath);
        var keywords  = lines
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
            .Select(l => l.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        
        List<string> variants;
        if (_options.Extensions != null && _options.Extensions.Any())
        {
            variants = keywords
                .SelectMany(kw => new[] { kw }.Concat(_options.Extensions.Select(ext => kw + (ext.StartsWith(".") ? ext : "." + ext))))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            variants = keywords;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _running = false;
            cts.Cancel();
        };

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var stopwatch = Stopwatch.StartNew();
        
        Console.WriteLine(); 

        await Parallel.ForEachAsync(variants, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.Threads,
            CancellationToken = cts.Token
        },
        async (word, token) =>
        {
            if (!_running) return;

            string fullUrl = _options.Url.TrimEnd('/') + "/" + word;
            int statusCode = 0;

            try
            {
                var res = await client.GetAsync(fullUrl, token);
                statusCode = (int)res.StatusCode;

                if (statusCode != 404)
                {
                    string statusText = $"Status: {statusCode}";
                    
                    lock (_lock)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($"{word,-20} ");

                        ApplyColor(statusCode);
                        Console.WriteLine($"Status: {statusCode}");
                        Console.ResetColor();
                    }

                    _results.Add($"{word} - {statusText}");
                }
            }
            catch (OperationCanceledException) { /* ctrl+c */ }
            catch (Exception ex)
            {
                if (_options.Verbose)
                {
                    lock (_lock)
                    {
                        Console.WriteLine($"Error {word}: {ex.Message}");
                    }
                }
            }

            int current = Interlocked.Increment(ref _processed);
            
            lock (_lock)
            {
                int lastLine = Console.WindowHeight - 1;
                FuzzUI.DrawProgressBar(current, variants.Count, 30, lastLine);
            }
        });

        stopwatch.Stop();
        
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        Console.WriteLine("\n\nFinalizado en " + stopwatch.Elapsed);

        if (_options.OutputFile != null)
        {
            await File.WriteAllLinesAsync(_options.OutputFile, _results);
            Console.WriteLine($"Resultados guardados en {_options.OutputFile}");
        }
    }
    
    private void ApplyColor(int status)
    {
        if (status == 200)
            Console.ForegroundColor = ConsoleColor.Green;
        else if (status >= 300 && status < 400)
            Console.ForegroundColor = ConsoleColor.Blue;
    }
}