using System;
using System.Threading;

namespace SystemProgramming_Task4
{
    internal class Program
    {
        static Semaphore semaphore = new Semaphore(3, 3);
        static CancellationTokenSource cts = new CancellationTokenSource();
        static CancellationToken token;
        static object lockObj = new object();

        class TaskModel
        {
            public string? Id;
            public string? Name;
            public double Size;
            public string Status = "Waiting";
            public int Progress = 0;
            public double CurrentMB = 0;
        }

        static TaskModel[] taskList = new TaskModel[]
        {
            new TaskModel { Id = "#1", Name = "mp4_sample_file_25MB.mp4", Size = 25.0 },
            new TaskModel { Id = "#2", Name = "mp4_sample_file_50MB.mp4", Size = 50.0 },
            new TaskModel { Id = "#3", Name = "mp4_sample_file_100MB.mp4", Size = 99.2 },
            new TaskModel { Id = "#4", Name = "mp4_sample_file_200MB.mp4", Size = 200.0 }
        };

        static int activeCount = 0;
        static int waitingCount = 0;
        static int completedCount = 0;

        static void ProcessFile(object state)
        {
            TaskModel item = (TaskModel)state;

            if (token.IsCancellationRequested) return;

            lock (lockObj)
            {
                waitingCount++;
            }

            semaphore.WaitOne();

            if (token.IsCancellationRequested)
            {
                semaphore.Release();
                return;
            }

            lock (lockObj)
            {
                waitingCount--;
                activeCount++;
                item.Status = "Downloading";
            }

            int stepCount = 20;
            for (int i = 1; i <= stepCount; i++)
            {
                if (token.IsCancellationRequested)
                {
                    semaphore.Release();
                    return;
                }

                Thread.Sleep(200);

                lock (lockObj)
                {
                    item.Progress = (i * 100) / stepCount;
                    item.CurrentMB = Math.Round((item.Size * item.Progress) / 100.0, 1);
                }
            }

            lock (lockObj)
            {
                activeCount--;
                completedCount++;
                item.Status = "Completed";
                item.Progress = 100;
                item.CurrentMB = item.Size;
            }

            semaphore.Release();
        }

        static void DisplayUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 MULTI-THREAD VIDEO DOWNLOADER                       ║");
            Console.WriteLine("║                 Semaphore: MAXIMUM 3                                ║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════════╝");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  1-3  → Downloading");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  4+   → Waiting");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ESC  → Cancel ALL");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘\n");

            lock (lockObj)
            {
                foreach (TaskModel item in taskList)
                {
                    int totalBars = 20;
                    int filledBars = (item.Progress * totalBars) / 100;
                    string barStr = new string('█', filledBars) + new string('░', totalBars - filledBars);

                    if (item.Status == "Downloading")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"{item.Id,-4} Downloading   [{barStr}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {item.Progress,3}%  {item.CurrentMB:0.0} MB/{item.Size:0.0} MB");
                    }
                    else if (item.Status == "Waiting")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{item.Id,-4} Waiting       [{barStr}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {item.Progress,3}%");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"{item.Id,-4} Completed     [{barStr}]");
                        Console.ResetColor();
                        Console.WriteLine($"   100%  {item.Size:0.0} MB/{item.Size:0.0} MB");
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     {item.Name}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            Console.WriteLine("───────────────────────────────────────────────────────────────────────");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Aktiv: {activeCount} | Gozleyen: {waitingCount} | Tamamlanan: {completedCount}");
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            token = cts.Token;

            foreach (TaskModel item in taskList)
            {
                ThreadPool.QueueUserWorkItem(ProcessFile, item);
            }

            Thread tScreen = new Thread(() =>
            {
                while (!token.IsCancellationRequested && completedCount < taskList.Length)
                {
                    DisplayUI();
                    Thread.Sleep(100);
                }
                DisplayUI();
            });
            tScreen.Start();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }
                }

                if (completedCount == taskList.Length)
                {
                    Thread.Sleep(500);
                    break;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine("\nEmeliyyat bitdi.");
            Console.ReadLine();
        }
    }
}