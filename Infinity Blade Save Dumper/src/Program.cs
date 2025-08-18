using SaveDumper.UnrealPackageManager;
using SaveDumper.Deserializer;
using SaveDumper.JsonParser;
using SaveDumper.JsonCruncher;
using SaveDumper.Serializer;


namespace SaveDumper;

public class Program
{
    private static void Exit() => Global.Exit();

    public static void Main()
    {
        string save1 = @$"{FilePaths.IB3SAVES}\IB3 Unencrypted Dual Saves\550 Thane\{FilePaths.UNENCRYPTEDSTRING}";
        string save2 = @$"{FilePaths.IB2SAVES}\{FilePaths.UNENCRYPTEDSTRING}";
        string save3 = @$"{FilePaths.IB3SAVES}\Bu's Save.bin";
        string save4 = @$"{FilePaths.IB1SAVES}\SwordSave1.bin";
        string save5 = @$"{FilePaths.IB1SAVES}\SwordSave.bin";
        PackageType packageType = PackageType.IB3;

        using (var UPK = new UnrealPackage(save3, packageType))
        {

            Console.Write("Do you want to deserialize or serialize? [d/s]: ");
            string choice = Console.ReadLine()?.Trim().ToLower()!;

            if (choice is "s")
                RunSerialization(packageType);
            else if (choice is "d")
                RunDeserialization(UPK);
            else
                Console.WriteLine("Invalid choice. Please enter 'd' or 's'.");
        }
        Exit();
    }

    static void RunSerialization(PackageType packageType)
    {
        var cruncher = new JsonDataCruncher(packageType);
        var crunchedData = cruncher.ReadJsonFile();
        if (crunchedData is null)
            return;

        using (var serializer = new DataSerializer(crunchedData))
            serializer.SerializeAndOutputData();
    }

    static void RunDeserialization(UnrealPackage UPK)
    {
        List<UProperty> uProperties;

        uProperties = UPK.DeserializeUPK(true);
        if (uProperties is null)
            return;

        var JsonDataParser = new JsonDataParser(uProperties!);
        JsonDataParser.WriteDataToFile();
    }
}     

