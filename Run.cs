public class Run
{
    private enum ClassState
    {
        LoadArchive,
        SaveArchive
    }

    private static int linesMade;

    public static void Main()
    {
        UnrealArchive save = new UnrealArchive(FileMode.Open, true, ClassState.SaveArchive);  // true or false for saving or loading
        Exit(save);
    } 

    // For now, we only expect one stream to be opened by the program.
    public static void Exit(UnrealArchive Ar)
    {
        if (Ar.bReader != null) Ar.bReader.Dispose();
        else if (Ar.bWriter != null) Ar.bWriter.Dispose();
        if (!Ar.leaveOpen) Ar.saveStream.Dispose();

        if (Ar.ArLoading)
        {
            ConsoleHelper.DisplayColoredText("\nContent Difference Report", ConsoleHelper.ConsoleColorChoice.Magenta);
            FileComparer comparer = new FileComparer(Globals.saveFile[0], Globals.binaryOutput);
            comparer.CompareFiles();
        }
        Finished();
    }
    
    public static void ClearConsoleLines()
    {
        var currentLineCursor = Console.CursorTop;
        var newLineCursor = currentLineCursor - linesMade;
        newLineCursor = newLineCursor < 0 ? 0 : newLineCursor;

        int _buffer = linesMade;
        Console.SetCursorPosition(0, newLineCursor);

        for (int i = 0; i < _buffer; i++)
        {
            Console.Write(new string(' ', Console.BufferWidth));
            linesMade--;
        }
        Console.SetCursorPosition(0, newLineCursor);
    }

    public static void WriteLine(string str)
    {
        Console.WriteLine(str);
        linesMade++;
    }

    public static void Finished()
    {
        WriteLine("Press any key to continue...");
        Console.ReadKey(true);
        ClearConsoleLines();
    }  
}

class ConsoleHelper
{
    public enum ConsoleColorChoice
    {
        Red,
        Green,
        Blue,
        Yellow,
        Magenta,
        Cyan,
        White,
    }

    public static void SetConsoleTextColor(ConsoleColorChoice color)
    {
        switch (color)
        {
            case ConsoleColorChoice.Red:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case ConsoleColorChoice.Green:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case ConsoleColorChoice.Blue:
                Console.ForegroundColor = ConsoleColor.Blue;
                break;
            case ConsoleColorChoice.Yellow:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case ConsoleColorChoice.Magenta:
                Console.ForegroundColor = ConsoleColor.Magenta;
                break;
            case ConsoleColorChoice.Cyan:
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case ConsoleColorChoice.White:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Gray; 
                break;
        }
    }
    public static void DisplayColoredText(string text, ConsoleHelper.ConsoleColorChoice color)
    {
        SetConsoleTextColor(color);
        Console.WriteLine(text);
        Console.ResetColor();  
    }

}




        // if (!debug){

        //     // string input;
        //     Console.WriteLine("Infinity Blade III Save Serialization/Deserialization Tool");
        //     Console.Write("Author: ");
        //     ConsoleHelper.SetConsoleTextColor(ConsoleHelper.ConsoleColorChoice.Cyan);
        //     Console.Write("G40sty\n\n");
        //     Console.ResetColor();

        //     //Console.Clear();

        //     while (true)
        //     { 
        //         // Display options 
        //         WriteLine("1. Deserialize Save File");
        //         WriteLine("2. Serialize Save File");
        //         WriteLine("3. Exit");
        //         WriteLine("Enter your choice: "); 

        //         string choice = Console.ReadLine()!;  // TODO: deal with warning here

        //         // Clear from the "Enter your choice" line downwards
        //         linesMade++;
        //         ClearConsoleLines(); 

        //         switch (choice)
        //         {
        //             case "1":
        //                 //UProperties.Start(save);
        //                 UProperties.Start(save);
        //                 break; 
        //             case "2":
        //                 Finished();
        //                 break; 
        //             case "3":
        //                 Exit(save);
        //                 return;
        //             default:
        //                 Console.WriteLine("Invalid choice. Please try again.");
        //                 Finished(); 
        //                 break; 
        //         }
        //     }
        // }
