using System.Diagnostics;

public class Global
{
    public const string IB2AESKEY = " |FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";

    public static void Exit()
    {
        //PrintColored("\nPress any key to exit...", ConsoleColor.Green, false);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    public static void PrintColored(string message, ConsoleColor color, bool resetColor = true)
    {
        Console.ForegroundColor = color;
        Console.Write(message);
        if (resetColor)
            Console.ResetColor();
    }

    // Overload for writing a line (adds newline)
    public static void PrintColoredLine(string message, ConsoleColor color, bool resetColor = true) =>
        PrintColored(message + Environment.NewLine, color, resetColor);

    [DebuggerHidden]
    [Conditional("DEBUG")]
    public static void DebugBreak()
    {
        if (Debugger.IsAttached)
            Debugger.Break();
    }
}

public enum PackageType : byte
{
    VOTE,
    IB1,
    IB2,
    IB3
}


public static class FilePaths
{
#pragma warning disable CS0414
    private static string IB1 = @"IB1";
    private static string IB2 = @"IB2";
    private static string IB3 = @"IB3";

    public const string UNENCRYPTEDSTRING = "UnencryptedSave0.bin";
#pragma warning restore CS0414

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

}

