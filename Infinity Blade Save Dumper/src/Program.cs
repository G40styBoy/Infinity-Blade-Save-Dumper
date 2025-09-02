using SaveDumper.JsonParser;
using SaveDumper.JsonCruncher;
using SaveDumper.Serializer;
using SaveDumper.Utilities;

namespace SaveDumper;

internal class Program
{
    private static string? inputPath;
    private static bool debug = false;

    public static void Main(string[] args)
    {
        if (debug)
        {
            DebugMain();
            return;
        }

        Console.Title = "IBSaveDumper";

        while (true)
        {
            Console.Clear();
            PrintBanner();

            inputPath = PromptForFile(
                "Drag and drop a .bin save file: ",
                ".bin",
                "Invalid file. Please drag a valid .bin file: ",
                "Only .bin files are accepted. Please try again: "
            );

            Console.Clear();
            PrintBanner();
            Console.WriteLine("Processing save package...\n");

            FilePaths.ValidateOutputDirectory();
            UnrealPackage UPK;
            try
            {
                UPK = new UnrealPackage(inputPath, true);
                RunDeserialization(UPK);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                WaitAndRestart();
                continue;
            }

            string jsonPath = PromptForFile(
                "Now drag and drop the modified .json file to repackage: ",
                ".json",
                "Invalid file. Please drag a valid .json file: ",
                "Only .json files are accepted. Please try again: "
            );

            Console.Clear();
            PrintBanner();
            Console.WriteLine("Processing save data...\n");

            try
            {
                RunSerialization(UPK, jsonPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            WaitAndRestart();
        }
    }

private static string PromptForFile(string prompt, string requiredExtension, string invalidMessage, string wrongExtensionMessage)
{
    string? path;
    bool showingError = false;
    
    while (true)
    {
        int promptLine = Console.CursorTop;
        
        if (!showingError)
            Console.Write(prompt);
        
        path = Console.ReadLine()?.Trim('"');
        
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            ClearAndShowError(promptLine, invalidMessage);
            showingError = true;
            continue;
        }
        
        if (Path.GetExtension(path).ToLowerInvariant() != requiredExtension)
        {
            ClearAndShowError(promptLine, wrongExtensionMessage);
            showingError = true;
            continue;
        }
        
        break;
    }
    return path!;
}

private static void ClearAndShowError(int startLine, string message)
{
    // Clear from the start line downward (3 lines should be enough)
    for (int i = 0; i < 3; i++)
    {
        Console.SetCursorPosition(0, startLine + i);
        Console.Write(new string(' ', Console.WindowWidth - 1));
    }
    
    Console.SetCursorPosition(0, startLine);
    Console.Write(message);
}

    private static void PrintBanner()
    {
        Global.PrintColoredLine("========================================", ConsoleColor.Cyan, true);
        Global.PrintColoredLine("         SAVE DUMPER TOOL v0.2          ", ConsoleColor.Cyan, true);
        Global.PrintColoredLine("========================================", ConsoleColor.Cyan, true);
        Global.PrintColoredLine(" © 2025 G40sty. All rights reserved.\n", ConsoleColor.DarkGray, true);
    }

    private static void WaitAndRestart()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
        Console.Clear();
    }

    private static void RunSerialization(UnrealPackage UPK, string jsonPath)
    {
        ProgressBar.Run("Serializing", () =>
        {
            var cruncher = new JsonDataCruncher(jsonPath, UPK.packageData.PackageType);
            var crunchedData = cruncher.ReadJsonFile();
            if (crunchedData is null)
                throw new InvalidOperationException("Serialization process failed!");

            using (var serializer = new DataSerializer(UPK, crunchedData))
                serializer.SerializeAndOutputData();
        });
    }

    private static void RunDeserialization(UnrealPackage UPK)
    {
        ProgressBar.Run("Deserializing", () =>
        {
            List<UProperty> uProperties = UPK.DeserializeUPK();
            if (uProperties is null)
                throw new InvalidOperationException("Deserialization process failed!");

            using (var JsonDataParser = new JsonDataParser(uProperties))
                JsonDataParser.WriteDataToFile();

            UPK.Dispose();
        });
    }

    /// <summary>
    /// For development testing
    /// </summary>
    private static void DebugMain()
    {   
        Console.ReadKey();
    }
}
