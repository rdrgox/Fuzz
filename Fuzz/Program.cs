using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomFuzzer
{
    class Program
    {
        private static bool isRunning = true;

        public static string ConsoleBlue(string text)
        {
            return "\u001b[34m" + text + "\u001b[0m";
        }

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("$$$$$$$$\\ $$\\   $$\\ $$$$$$$$\\ $$$$$$$$\\");
            Console.WriteLine("$$  _____|$$ |  $$ |\\____$$  |\\____$$  |");
            Console.WriteLine("$$ |      $$ |  $$ |    $$  /     $$  /");
            Console.WriteLine("$$$$$\\    $$ |  $$ |   $$  /     $$  /");
            Console.WriteLine("$$  __|   $$ |  $$ |  $$  /     $$  /");
            Console.WriteLine("$$ |      $$ |  $$ | $$  /     $$  /");
            Console.WriteLine("$$ |      \\$$$$$$  |$$$$$$$$\\ $$$$$$$$\\");
            Console.WriteLine("\\__|       \\______/ \\________|\\________|");
            Console.ResetColor();
            Console.WriteLine("________________________________________________");
            Console.WriteLine("v0.1");
            Console.WriteLine();

            Console.Write("Ingrese la URL de prueba: ");
            string url = Console.ReadLine()!;

            Console.Write("Ingrese la ruta del archivo de diccionario: ");
            string dictionaryPath = Console.ReadLine()!;

            Console.WriteLine("________________________________________________");
            Console.WriteLine();
            Console.WriteLine("Objetivo (target): " + ConsoleBlue(url));
            Console.WriteLine("________________________________________________");
            Console.WriteLine();

            string[] keywords = await File.ReadAllLinesAsync(dictionaryPath!);

            int[] errorCodes = { 200, 204, 301, 302, 307, 401 }; // Agrega aquí los códigos de error deseados

            // Manejar el evento de interrupción Ctrl + C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Cancelar el evento de interrupción para permitir una salida ordenada
                isRunning = false; // Establecer la variable de control para detener el programa
            };

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); // Iniciar el temporizador

            foreach (string keyword in keywords)
            {
                if (!isRunning)
                {
                    break; // Detener el bucle y salir del programa
                }

                string testUrl = url + "/" + keyword;

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(testUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("{0,-30} {1}{2,-20}", keyword, "Status: ", (int)response.StatusCode);
                        Console.ResetColor();
                    }
                    else
                    {
                        int statusCode = (int)response.StatusCode;
                        if (Array.IndexOf(errorCodes, statusCode) != -1 && statusCode != 404)
                        {
                            Console.WriteLine("{0,-30} {1,-10}", keyword, $"Status: {statusCode}");
                        }
                    }
                }
            }
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine("Búsqueda completada.");

            TimeSpan elapsedTime = stopwatch.Elapsed;
            Console.WriteLine($"Tiempo transcurrido: {elapsedTime.ToString(@"hh\:mm\:ss")}");
            Console.ReadLine();
        }

    }
}
