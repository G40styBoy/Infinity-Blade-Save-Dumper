using SaveDumper.UnrealPackageManager;
using SaveDumper.FPropertyManager;
using SaveDumper.JsonDumper;


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