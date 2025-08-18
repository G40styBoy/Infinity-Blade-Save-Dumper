using System.Text;
using System.Diagnostics;


namespace SaveDumper.Serializer;

/// <summary>
/// Takes crunched Json data and converts it into serialized data readable for BasicLoadObject
/// This gets written out into a binary file
/// </summary>
class DataSerializer : IDisposable
{
    private const string DEFAULT_NAME = "UnencryptedSave0.bin";
    private const string CUSTOM_NAME = "Serialized Save Data.bin";

    private readonly List<UProperty> crunchedData;
    private BinaryWriter binWriter;
    private UPropertyDataHelper uhelper;


    public DataSerializer(List<UProperty> crunchedData)
    {
        this.crunchedData = crunchedData;

        string outputPath = Path.Combine(FilePaths.OutputDir, DEFAULT_NAME);
        FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        binWriter = new BinaryWriter(fs);

        uhelper = new UPropertyDataHelper();
    }

    internal bool SerializeAndOutputData()
    {
        try
        {
            // placeholder for now of the package header contents
            binWriter.Write(5);
            binWriter.Write(4294967295);
            ;
            foreach (var uProperty in crunchedData)
            {
                uhelper.SerializeMetadata(ref binWriter, uProperty);
                uProperty.SerializeValue(binWriter);
            }

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