namespace SaveDumper.Utilities;

public static class ProgressBar
{
    public static void Run(string label, Action work)
    {
        int barWidth = 50;
        bool running = true;
        bool success = true;

        Thread progressThread = new Thread(() =>
        {
            int progress = 0;
            while (running)
            {
                progress = (progress + 1) % (barWidth + 1);

                Console.CursorVisible = false;
                Console.SetCursorPosition(0, Console.CursorTop);

                Global.PrintColored($"{label}: [", ConsoleColor.White, false);
                Global.PrintColored(new string('=', progress), ConsoleColor.Yellow, false);
                Console.Write(new string(' ', barWidth - progress));
                Global.PrintColored("]", ConsoleColor.White, false);

                Thread.Sleep(50);
            }
        });

        progressThread.Start();

        try{
            work(); 
        }
        catch (Exception ex)
        {
            success = false;
            running = false;
            progressThread.Join();

            Console.WriteLine();
            Global.PrintColored($"Error: {ex.Message}\n", ConsoleColor.Red);
            return;
        }

        finally
        {
            running = false;
            progressThread.Join();

            Console.SetCursorPosition(0, Console.CursorTop);
            Global.PrintColored($"{label}: [", ConsoleColor.White, false);

            if (success)
                Global.PrintColored(new string('=', barWidth), ConsoleColor.Green, false);
            else
                Global.PrintColored(new string('=', barWidth), ConsoleColor.Red, false);

            Global.PrintColored("] 100%\n", ConsoleColor.White);
            Console.CursorVisible = true;
        }
    }
}
