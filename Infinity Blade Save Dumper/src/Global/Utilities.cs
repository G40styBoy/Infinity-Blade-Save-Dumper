namespace SaveDumper.Utilities;

public static class Util
{
    public static string GetNextSaveFileNameForOuput(string fileName, string fileExtension)
    {
        string[] saveFiles = Directory.GetFiles(FilePaths.OutputDir, $"*{fileExtension}");
        if (saveFiles.Length == 0)
            // start at 0
            return $"{fileName}0{fileExtension}"; 

        int maxIndex = -1;

        foreach (var file in saveFiles)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(file);

            var match = System.Text.RegularExpressions.Regex.Match(nameWithoutExt, @"(\d+)$");
            if (match.Success && int.TryParse(match.Value, out int index))
            {
                if (index > maxIndex)
                    maxIndex = index;
            }
        }

        int nextIndex = maxIndex + 1;
        string newFileName = System.Text.RegularExpressions.Regex.Replace(
            fileName,
            @"\d+$",
            nextIndex.ToString()
        );

        if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\d+$"))
            newFileName = $"{fileName}{nextIndex}";

        return newFileName + fileExtension;
    }
}

/// <summary>
/// utility class for the tool's GUI to make the interface cleaner
/// </summary>
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

        try
        {
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

/// <summary>
/// Helps keep file locations, names, etc. organized.
/// All file path data needed for the program is stored here
/// </summary>
public static class FilePaths
{
    public static DirectoryInfo parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())!;
    public static string OutputDir = $@"{parentDirectory}\OUTPUT";

    public static string baseLocation = $@"{parentDirectory}\SAVE STORAGE LOCATION";
    public static string IB3SAVES = Path.Combine(baseLocation, @"IB3 Backup");
    public static string IB2SAVES = Path.Combine(baseLocation, @"IB2 Backup");
    public static string IB1SAVES = Path.Combine(baseLocation, @"IB1 Backup");
    public static string VOTESAVES = Path.Combine(baseLocation, @"VOTE!!! Backup");

    public static void ValidateOutputDirectory()
    {
        if (!File.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);
    }

    public static bool DoesOutputExist() => File.Exists(OutputDir);
}