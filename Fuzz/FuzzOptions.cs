namespace Fuzz;

public class FuzzOptions
{
    public string Url { get; set; } = "";
    public string DictionaryPath { get; set; } = "";
    public int Threads { get; set; } = 10;
    public string[]? Extensions { get; set; }
    public bool Verbose { get; set; } = false;
    public string? OutputFile { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
    
    public static FuzzOptions? Parse(string[] args)
    {
        var options = new FuzzOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-u":
                    options.Url = args[++i];
                    break;
                case "-w":
                    options.DictionaryPath = args[++i];
                    break;
                case "-t":
                    options.Threads = int.Parse(args[++i]);
                    break;
                case "-x":
                    options.Extensions = args[++i].Split(',').Select(e => e.StartsWith('.') ? e : "." + e).ToArray();
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "-o":
                    options.OutputFile = args[++i];
                    break;
                case "--timeout":
                    options.TimeoutSeconds = int.Parse(args[++i]);
                    break;
                case "-h":
                    FuzzUI.ShowHelp();
                    return null;
                default:
                    Console.WriteLine($"Opción desconocida: {args[i]}");
                    FuzzUI.ShowHelp();
                    return null;
            }
        }

        if (string.IsNullOrEmpty(options.Url) || string.IsNullOrEmpty(options.DictionaryPath))
        {
            Console.WriteLine("Faltan argumentos requeridos.");
            FuzzUI.ShowHelp();
            return null;
        }

        return options;
    }
}