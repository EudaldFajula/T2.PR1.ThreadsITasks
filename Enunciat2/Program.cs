using Enunciat2.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAsteroids
{
    public class Program
    {
        // Configuració del joc
        private const char ShipChar = '^';
        private const char AsteroidChar = 'O';
        private static int PhysicsHz = 50;
        private static int RenderHz = 20;

        // Estat compartit
        private static List<Asteroid> asteroids = new List<Asteroid>();
        private static object lockObj = new object();
        private static int shipX;
        private static int asteroidsDodged = 0;
        private static int livesLost = 0;
        private static Stopwatch gameTimer = new Stopwatch();

        public static async Task Main()
        {
            Console.CursorVisible = false;
            shipX = Console.WindowWidth / 2;

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            // Task d'entrada de teclat
            var inputTask = Task.Run(() => ReadInputLoop(cts), token);

            // Task de física (50 Hz)
            var physicsTask = Task.Run(() => PhysicsLoop(token), token);

            // Task de render (20 Hz)
            var renderTask = Task.Run(() => RenderLoop(token), token);

            gameTimer.Start();

            // Esperem que l'usuari clicki 'Q'
            await inputTask;

            // Quan l'usuari clicka 'Q'
            cts.Cancel();
            await Task.WhenAll(physicsTask.ContinueWith(_ => { }),
                               renderTask.ContinueWith(_ => { }));

            gameTimer.Stop();
            Console.Clear();
            Console.CursorVisible = true;

            // Resum de la partida
            Console.WriteLine("=== Resum de la partida ===");
            Console.WriteLine($"Asteroides esquivats: {asteroidsDodged}");
            Console.WriteLine($"Vides perdudes: {livesLost}");
            Console.WriteLine($"Temps total: {gameTimer.Elapsed:mm\\:ss}");

            // Opcional: guardar a CSV
            SaveCsv(asteroidsDodged, livesLost, gameTimer.Elapsed);

            Console.WriteLine("\nPrem una tecla per sortir...");
            Console.ReadKey(true);
        }

        public static void ReadInputLoop(CancellationTokenSource cts)
        {
            while (!cts.IsCancellationRequested)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.A && shipX > 0)
                    shipX--;
                else if (key.Key == ConsoleKey.D && shipX < Console.WindowWidth - 1)
                    shipX++;
                else if (key.Key == ConsoleKey.Q)
                    cts.Cancel();
            }
        }

        public static async Task PhysicsLoop(CancellationToken token)
        {
            var rand = new Random();
            int delay = 1000 / PhysicsHz;

            while (!token.IsCancellationRequested)
            {
                lock (lockObj)
                {
                    // Generem un nou asteroide
                    if (rand.NextDouble() < 0.1)
                        asteroids.Add(new Asteroid { X = rand.Next(Console.WindowWidth), Y = 0 });

                    // Movem els asteroides i comprovem col·lisions/esquivats
                    for (int i = asteroids.Count - 1; i >= 0; i--)
                    {
                        var ast = asteroids[i];
                        ast.Y++;

                        if (ast.Y >= Console.WindowHeight - 1)
                        {
                            if (ast.X == shipX)
                            {
                                livesLost++;
                                // reiniciem
                                asteroids.Clear(); 
                            }
                            else
                            {
                                asteroidsDodged++;
                                asteroids.RemoveAt(i);
                            }
                        }
                    }
                }

                try { await Task.Delay(delay, token); }
                catch (TaskCanceledException) { }
            }
        }

        public static async Task RenderLoop(CancellationToken token)
        {
            int delay = 1000 / RenderHz;

            while (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, 0);

                // Pinta fons amb res
                for (int y = 0; y < Console.WindowHeight; y++)
                {
                    Console.Write(new string(' ', Console.WindowWidth));
                }

                // Dibuixa asteroides
                lock (lockObj)
                {
                    foreach (var ast in asteroids)
                    {
                        if (ast.Y >= 0 && ast.Y < Console.WindowHeight)
                        {
                            Console.SetCursorPosition(ast.X, ast.Y);
                            Console.Write(AsteroidChar);
                        }
                    }
                }

                // Dibuixa nau
                Console.SetCursorPosition(shipX, Console.WindowHeight - 1);
                Console.Write(ShipChar);

                try { await Task.Delay(delay, token); }
                catch (TaskCanceledException) { }
            }
        }

        //Guardar informacio csv
        public static void SaveCsv(int dodged, int lives, TimeSpan time)
        {
            var line = $"{DateTime.Now:O},{dodged},{lives},{time:mm\\:ss}";
            File.AppendAllLines("../../../scores.csv", new[] { line });
        }
    }
}
