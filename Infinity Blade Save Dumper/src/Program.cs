using SaveDumper.UnrealPackageManager;
using SaveDumper.Globals.FilePaths;

namespace SaveDumper;

public class Program
{
    public static void Main()
    {


        // Initialize the UnrealPackage instance.
        using (var UPK = new UnrealPackage(FilePaths.saveLocation[0]))
        {
            var properties = UPK.DeserializeUPK();
        } 

        G40Util.Exit();
    }
}     