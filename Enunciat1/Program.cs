using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace DiningPhilosophers
{
    public class Program
    {
        const int PhilosopherCount = 5;
        const double ThinkMin = 0.5;
        const double ThinkMax = 2.0;
        const double EatMin = 0.5;
        const double EatMax = 1.0;
        const int MaxHungrySeconds = 15;
        const int SimulationSeconds = 30;

        static object[] Chopsticks = new object[PhilosopherCount];
        static Philosopher[] Philosophers = new Philosopher[PhilosopherCount];
        static CancellationTokenSource cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.Clear();

            // Inicialitza els bastons i els filòsofs
            for (int i = 0; i < PhilosopherCount; i++)
            {
                Chopsticks[i] = new object();
            }

            for (int i = 0; i < PhilosopherCount; i++)
            {
                int id = i;
                Philosophers[i] = new Philosopher(id);
                var thread = new Thread(Philosophers[id].Run)
                {
                    Name = $"Philosopher {id}",
                    IsBackground = true
                };
                Philosophers[id].Thread = thread;
                thread.Start();
            }

            //Inicia el monitor per comprovar la gana i el temps total
            var monitorThread = new Thread(Monitor)
            {
                IsBackground = true
            };
            monitorThread.Start();

            //Espera fins a la cancel·lació
            cts.Token.WaitHandle.WaitOne();

            //Mostra les estadístiques
            Console.ResetColor();
            Console.Clear();
            Console.CursorVisible = true;
            Console.WriteLine("=== Estadístiques Finals ===");
            Console.WriteLine("ID,MaxHungrySeconds,EatCount");
            var csvLines = new List<string>();
            foreach (var phil in Philosophers)
            {
                Console.WriteLine($"{phil.Id},{phil.MaxHungryTime.TotalSeconds:F2},{phil.EatCount}");
                csvLines.Add($"{phil.Id},{phil.MaxHungryTime.TotalSeconds:F2},{phil.EatCount}");
            }
            File.WriteAllLines("../../../philosophers_stats.csv", csvLines);
            Console.WriteLine("\nDades guardades a philosophers_stats.csv");
            Console.WriteLine("Prem una tecla per sortir...");
            Console.ReadKey(true);
        }

        static void Monitor()
        {
            var sw = Stopwatch.StartNew();
            while (!cts.Token.IsCancellationRequested)
            {
                //Comprova la gana
                foreach (var phil in Philosophers)
                {
                    var hungryTime = DateTime.Now - phil.LastEatTime;
                    if (hungryTime.TotalSeconds > MaxHungrySeconds)
                    {
                        phil.LogMessage("ha estat amb gana massa temps!", ConsoleColor.White, ConsoleColor.Red);
                        cts.Cancel();
                        return;
                    }
                }
                if (sw.Elapsed.TotalSeconds > SimulationSeconds)
                {
                    //Finalitza després del temps de simulació
                    cts.Cancel();
                    return;
                }
                Thread.Sleep(500);
            }
        }

        class Philosopher
        {
            public int Id { get; }
            public Thread Thread { get; set; }
            public int EatCount { get; set; }
            public DateTime LastEatTime { get; set; }
            public TimeSpan MaxHungryTime { get; set; }

            Random rand = new Random();

            ConsoleColor textColor;
            public Philosopher(int id)
            {
                Id = id;
                LastEatTime = DateTime.Now;
                textColor = (ConsoleColor)(id % 8 + 1);
            }

            public void Run()
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    //Pensant
                    LogMessage("pensa", ConsoleColor.Black, ConsoleColor.Cyan);
                    SleepRandom(ThinkMin, ThinkMax);

                    //Amb gana
                    var hungryStart = DateTime.Now;
                    LogMessage("té gana, intenta agafar bastons", ConsoleColor.Black, ConsoleColor.Yellow);

                    //Agafa els bastons
                    bool leftFirst = Id % 2 == 0;
                    int left = Id;
                    int right = (Id + 1) % PhilosopherCount;

                    if (!leftFirst)
                    {
                        (right, left) = (left, right);
                    }

                    lock (Chopsticks[left])
                    {
                        LogMessage($"agafa palet {(leftFirst ? "esquerre" : "dret")}", ConsoleColor.Black, ConsoleColor.Yellow);
                        lock (Chopsticks[right])
                        {
                            LogMessage($"agafa palet {(leftFirst ? "dret" : "esquerre")}", ConsoleColor.Black, ConsoleColor.Yellow);

                            // Menjant
                            var waited = DateTime.Now - hungryStart;
                            if (waited > MaxHungryTime)
                                MaxHungryTime = waited;

                            LastEatTime = DateTime.Now;
                            EatCount++;
                            LogMessage("menja", ConsoleColor.Black, ConsoleColor.Green);
                            SleepRandom(EatMin, EatMax);

                            //Deixa el bastó dret
                            LogMessage($"deixa palet {(leftFirst ? "dret" : "esquerre")}", ConsoleColor.Black, ConsoleColor.Yellow);
                        }
                        //Deixa el bastó esquerre
                        LogMessage($"deixa palet {(leftFirst ? "esquerre" : "dret")}", ConsoleColor.Black, ConsoleColor.Yellow);
                    }
                }
            }

            void SleepRandom(double minSeconds, double maxSeconds)
            {
                int ms = (int)(rand.NextDouble() * (maxSeconds - minSeconds) * 1000 + minSeconds * 1000);
                try { Thread.Sleep(ms); }
                catch (ThreadInterruptedException) { }
            }

            public void LogMessage(string message, ConsoleColor fg, ConsoleColor bg)
            {
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                lock (Console.Out)
                {
                    Console.BackgroundColor = bg;
                    Console.ForegroundColor = textColor;
                    Console.Write($"[{time}] Philosopher {Id}: ");
                    Console.ForegroundColor = fg;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
            }
        }
    }
}
