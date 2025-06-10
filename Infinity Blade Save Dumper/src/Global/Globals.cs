public class Globals
{
    public const string IB2AESKEY = " |FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";

    public static void Exit()
    {
        //PrintColored("\nPress any key to exit...", ConsoleColor.Green, false);
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    public static void PrintColored(string message, ConsoleColor color, bool resetColor = true)
    {
        Console.ForegroundColor = color;
        Console.Write(message);
        if (resetColor)
        {
            Console.ResetColor();
        }
    }

    // Overload for writing a line (adds newline)
    public static void PrintColoredLine(string message, ConsoleColor color, bool resetColor = true)
    {
        PrintColored(message + Environment.NewLine, color, resetColor);
    }

}

public enum PackageType : byte
{
    VOTE,
    IB1,
    IB2,
    IB3
}

public record struct FType
{
    public const string INT_PROPERTY = "IntProperty";
    public const string FLOAT_PROPERTY = "FloatProperty";
    public const string BYTE_PROPERTY = "ByteProperty";
    public const string BOOL_PROPERTY = "BoolProperty";
    public const string STR_PROPERTY = "StrProperty";
    public const string NAME_PROPERTY = "NameProperty";
    public const string STRUCT_PROPERTY = "StructProperty";
    public const string ARRAY_PROPERTY = "ArrayProperty";
    public const string NONE = "None";
}

public enum ValueType
{
    StructProperty,
    ArrayProperty,
    IntProperty,
    StrProperty,
    NameProperty,
    FloatProperty,
    BoolProperty,
    ByteProperty
}

public static class FilePaths
{
    #pragma warning disable CS0414
    private static string input = @"SAVE\input\";
    private static string output = @"SAVE\output\";
    private static string IB1 = @"IB1";
    private static string IB2 = @"IB2";
    private static string IB3 = @"IB3";
    #pragma warning restore CS0414 

    private static DirectoryInfo parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())!;
    private static string testPath = Path.Combine(parentDirectory.FullName, @"SAVE\Test");


    // public static string saveFilePath = Path.Combine(parentDirectory.FullName, input);
    // public static string outputPath = Path.Combine(parentDirectory.FullName, output);

    public static string[] saveLocation = Directory.GetFiles(testPath, "*.bin");
}
