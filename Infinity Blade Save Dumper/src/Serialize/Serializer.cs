using System.Diagnostics;
using SaveDumper.Utilities;

namespace SaveDumper.Serializer;

/// <summary>
/// Takes crunched Json data and converts it into serialized data readable for BasicLoadObject
/// This gets written out into a binary file
/// </summary>
class DataSerializer : IDisposable
{
    private const string DEFAULT_NAME = "UnencryptedSave0";
    private const string EXTENSION = ".bin";

    private readonly List<UProperty> crunchedData;
    private BinaryWriter binWriter;
    private FileStream stream;
    private readonly UPropertyDataHelper uhelper;
    private readonly string outputPath;
    private UnrealPackage UPK;

    public DataSerializer(UnrealPackage UPK, List<UProperty> crunchedData)
    {
        this.UPK = UPK;
        this.crunchedData = crunchedData;
        string fileName;
        uhelper = new UPropertyDataHelper();

        if (UPK.packageData.isEncrypted)
            fileName = $"{UPK.packageData.packageName} - MODDED{EXTENSION}";
        else
            fileName = Util.GetNextSaveFileNameForOuput(DEFAULT_NAME, EXTENSION);

        outputPath = Path.Combine(FilePaths.OutputDir, fileName);

        stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        binWriter = new BinaryWriter(stream);
    }

    internal bool SerializeAndOutputData()
    {
        try
        {
            // write our file header contents
            binWriter.Write(UPK.packageData.saveType);
            binWriter.Write(UPK.packageData.savePadding);

            foreach (var uProperty in crunchedData)
            {
                uhelper.SerializeMetadata(ref binWriter, uProperty);
                uProperty.SerializeValue(binWriter);
            }
            uhelper.SerializeString(ref binWriter, "None");
            binWriter.Flush(); 

            if (UPK.packageData.isEncrypted)
                EncryptPackage();

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }

    private void EncryptPackage()
    {
        Dispose();

        var encryptedData = Util.EncryptDataECB(
            File.ReadAllBytes(outputPath), AESKey.IB2, UPK.packageData.saveMagic);

        // once we dispose of files' resources we are going to duplicate it but with the encrypted data
        File.WriteAllBytes(outputPath, encryptedData);        
    }

    public void Dispose()
    {
        binWriter?.Dispose();
        stream?.Dispose();
    }
}
