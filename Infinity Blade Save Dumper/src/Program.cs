using SaveDumper.UnrealPackageManager;


namespace SaveDumper;

public class Program
{
    public static void Main()
    {
        using (var UPK = new UnrealPackage(FilePaths.saveLocation[0]))
        {
            var properties = UPK.DeserializeUPK();
        } 

        Globals.Exit();
    }
}     