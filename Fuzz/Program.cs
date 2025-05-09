using System.Diagnostics;

namespace Fuzz;

public class Program
{
    ///const string Version = "v1.4";
    public static async Task Main(string[] args)
    {
        var options = FuzzOptions.Parse(args);
        if (options == null) return;

        var executor = new Fuzz(options);
        await executor.RunAsync();
    }
}