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
    private const string CUSTOM_NAME = "Serialized Save Data.bin";

    private readonly List<UProperty> crunchedData;
    private BinaryWriter binWriter;
    private readonly UPropertyDataHelper uhelper;

    public DataSerializer(List<UProperty> crunchedData)
    {
        this.crunchedData = crunchedData;
        uhelper = new UPropertyDataHelper();

        string outputPath = Path.Combine(FilePaths.OutputDir, Util.GetNextSaveFileNameForOuput(DEFAULT_NAME, EXTENSION));

        // setup stream + writer here
        FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        binWriter = new BinaryWriter(fs);
    }

    internal bool SerializeAndOutputData()
    {
        int saveType = 5;
        uint saveMagic = 4294967295;
        try
        {
            // placeholder for now of the package header contents
            // this will not support unencrypted
            binWriter.Write(saveType);
            binWriter.Write(saveMagic);

            foreach (var uProperty in crunchedData)
            {
                uhelper.SerializeMetadata(ref binWriter, uProperty);
                uProperty.SerializeValue(binWriter);
            }

            binWriter.Flush(); // make sure all data is written out
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        binWriter?.Dispose();
    }
}
