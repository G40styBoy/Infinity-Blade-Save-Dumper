using System.Diagnostics;

public class Global
{
    public const string IB2AESKEY = " |FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";

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

