using SaveDumper.JsonParser;
using SaveDumper.JsonCruncher;
using SaveDumper.Serializer;
using SaveDumper.Utilities;

namespace SaveDumper;

internal class Program
{
    private static string? inputPath;
    
    static void Main(string[] args)
    {
        Console.Title = "Infinity Blade Save Dumper Tool v1.0";
        while (true)
        {
            Console.Clear();
            PrintBanner();
            if (args.Length == 0)
            {
                Console.WriteLine("Drag and drop file here, then press Enter:");
                inputPath = Console.ReadLine()?.Trim('"')!;
                if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                {
                    Console.WriteLine("Invalid file provided.");
                    WaitAndRestart();
                    continue;
                }
                
                Console.Clear();
                PrintBanner();
                Console.WriteLine("Processing file...\n");
            }
            else
            {
                inputPath = args[0];
                args = Array.Empty<string>(); 
            }
            
            string extension = Path.GetExtension(inputPath).ToLowerInvariant();


            try
            {
                switch (extension)
                {
                    case ".json":
                        Console.WriteLine("Running Serialization...");
                        RunSerialization(PackageType.IB3);
                        break;
                    case ".bin":
                        Console.WriteLine("Running Deserialization...");
                        var upk = new UnrealPackage(inputPath, PackageType.IB3);
                        RunDeserialization(upk);
                        break;
                    default:
                        Console.WriteLine("Unsupported file type. Only .json or .bin are allowed.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred:");
                Console.WriteLine(ex.Message);
            }
            WaitAndRestart();
        }
    }
    
    private static void PrintBanner()
    {
        Global.PrintColoredLine("========================================", ConsoleColor.Cyan, true);
        Global.PrintColoredLine("       SAVE DUMPER TOOL v1.0", ConsoleColor.Cyan, true);
        Global.PrintColoredLine("========================================", ConsoleColor.Cyan, true);
        Global.PrintColoredLine(" © 2025 G40sty. All rights reserved.\n", ConsoleColor.DarkGray, true);
    }
    
    private static void WaitAndRestart()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
        Console.Clear(); 
    }
    
    static void RunSerialization(PackageType packageType)
    {
        ProgressBar.Run("Serializing", () =>
        {
            var cruncher = new JsonDataCruncher(inputPath!, packageType);
            var crunchedData = cruncher.ReadJsonFile();
            if (crunchedData is null)
                return;
            using (var serializer = new DataSerializer(crunchedData))
                serializer.SerializeAndOutputData();
        });
    }
    
    static void RunDeserialization(UnrealPackage UPK)
    {
        ProgressBar.Run("Deserializing", () =>
        {
            List<UProperty> uProperties = UPK.DeserializeUPK(true);
            if (uProperties is null)
                return;
            var JsonDataParser = new JsonDataParser(uProperties!);
            JsonDataParser.WriteDataToFile();
        });
    }
}