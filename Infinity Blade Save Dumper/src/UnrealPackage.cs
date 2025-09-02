using SaveDumper.Utilities;
using SaveDumper.Deserializer;

namespace SaveDumper.UnrealPackageManager;

public class UnrealPackage : IDisposable
{
    private string filePath;
    private byte[] fileBytes;
    private FileStream fileStream;
    private BinaryReader binaryReader;
    private BinaryWriter binaryWriter;
    internal PackageData packageData;
    private bool disposed = false;

    private const int SAVE_FILE_VERSION = 5;
    private const int SAVE_FILE_VERSION_INTERNAL = 3;
    private const int SAVE_FILE_FIXUP_VERSION = 9;
    private const int SAVE_FILE_VERSION_IB1 = 4;
    private const int SAVE_FILE_FIXUP_VERSION_IB1 = 1;

    #region Constructor
    public record struct PackageData
    {
        public string packageName;
        public bool isEncrypted;
        public byte[] saveMagic;
        public int saveType;
        public uint savePadding;
        public PackageType PackageType;
    }

    public UnrealPackage(string filePath, bool ouputDecryptedSave = false)
    {
        this.filePath = filePath;

        // for now we are using this for potential decryption
        // it is however in general handy that we store our files bytes somewhere just in case
        fileBytes = File.ReadAllBytes(filePath);
        packageData = new PackageData();
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file does not exist.", filePath);

        packageData.packageName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            // test access
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) { }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("File is not accessible for read/write operations.", ex);
        }
        try
        {
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            binaryReader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: true);
            binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open file stream.", ex);
        }

        PopulatePackageInfo();
    }

    // todo: support identification for package types
    private void PopulatePackageInfo()
    {
        try
        {
            ReadPackageHeaderInfo();

            if (!IsSaveTypeValid())
            {
                Global.PrintColoredLine("Encrypted file detected, attempting to decrypt...", ConsoleColor.Yellow);
                packageData.isEncrypted = true;

                // TODO: update this to support ib1 as well
                packageData.PackageType = PackageType.IB2;
                TransformEncryptedFile();
                Global.PrintColoredLine("Decryption successful!", ConsoleColor.Green);
            }
            else
                packageData.PackageType = PackageType.IB3;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Package Data population failed. Reason: {exception.Message}");
        }
    }
    #endregion

    private bool IsSaveTypeValid() =>
        packageData.saveType is SAVE_FILE_VERSION or SAVE_FILE_FIXUP_VERSION or SAVE_FILE_VERSION_INTERNAL or SAVE_FILE_VERSION_IB1 or SAVE_FILE_FIXUP_VERSION_IB1;

    #region Utility Methods
    public void LogStreamPosition() => Console.WriteLine($"Stream Position: {fileStream.Position}");
    public long GetStreamPosition() => fileStream.Position;
    public void SetStreamPosition(long position) => fileStream.Position = position;
    public long GetStreamLength() => fileStream.Length;
    public void ResetStreamPosition() => fileStream.Position = 0;
    /// <summary>
    /// Helper function used so we can overwrite file data while a stream is open
    /// </summary>
    /// <param name="data">bytes to write to the current upk</param>
    internal void WriteAllBytes(byte[] data)
    {
        fileStream.SetLength(0);
        ResetStreamPosition();
        binaryWriter.Write(data);
        binaryWriter.Flush();
    }

    internal void RevertFileToOriginalState() => WriteAllBytes(fileBytes); 

    /// <summary>
    /// Reverts the stream position during deserialization given a value and its type
    /// </summary>
    public void RevertStreamPosition(string value)
    {
        // Right now this only supports strings, but for now we don't need to revert any other type
        fileStream.Position -= sizeof(int) + sizeof(byte); // size + nt
        fileStream.Position -= value.Length;
    }

    /// <summary>
    /// Stores the UPK header data.
    /// </summary>
    /// stores the initial bytes inside of our package data
    private void ReadPackageHeaderInfo()
    {
        packageData.saveType = binaryReader.ReadInt32();
        packageData.savePadding = binaryReader.ReadUInt32();
    }

    /// <returns>A boolean value that indicates whether the stream has reached the end of the file</returns>
    internal bool IsEndFile() => fileStream.Position >= fileStream.Length;

    internal void TransformEncryptedFile(bool outputDecryptedData = false)
    {
        ResetStreamPosition();
        byte[] fileBytes = new byte[fileStream.Length];
        fileStream.Read(fileBytes, 0, fileBytes.Length);

        try
        {
            // before we omit the save magic, store it for later use when Encrypting
            ResetStreamPosition();
            packageData.saveMagic = binaryReader.ReadBytes(sizeof(int));
            byte[] decryptedData = Util.DecryptDataECB(fileBytes, AESKey.IB2);         
            WriteAllBytes(decryptedData);

            if (outputDecryptedData)
                File.WriteAllBytes($"{FilePaths.OutputDir}/Decrypted Save.bin", decryptedData);

            ResetStreamPosition();
            ReadPackageHeaderInfo();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to decrypt package. {exception.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadBytes(int count) => binaryReader.ReadBytes(count);

    /// <returns>the next string in the stream</returns>
    internal string PeekString()
    {
        string str;
        long originalPosition = binaryReader.BaseStream.Position;

        try
        {
            str = DeserializeString();
        }
        finally
        {
            binaryReader.BaseStream.Position = originalPosition;
        }

        return str;
    }

    internal string DeserializeString()
    {
        try
        {
            var strLength = DeserializeInt();
            if (strLength <= UDefinitions.Empty)
                return string.Empty;
            strLength--;

            var bytes = new byte[strLength];
            binaryReader.Read(bytes, UDefinitions.Empty, strLength);
            fileStream.Position++;

            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failure deserializing string inside an Unreal Package. {exception}");
        }
    }

    internal object DeserializeByteProperty()
    {
        string enumName = DeserializeString();

        if (enumName == UType.NONE)
            return DeserializeByte();
        else
        {
            string enumValue = DeserializeString();
            return new KeyValuePair<string, string>(enumName, enumValue);
        }
    }

    internal int DeserializeInt(bool returnedSigned = false)
    {
        try
        {
            int readValue = BitConverter.ToInt32(ReadBytes(sizeof(int)));

            // infinity blade save values that are negative usually indicate they've gone over the limit
            if (int.IsNegative(readValue) && !returnedSigned && readValue != -1)
                return int.MaxValue;

            return readValue;
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException($"Could not convert {sizeof(int)} bytes to an integer.");
        }
    }

    internal float DeserializeFloat()
    {
        try
        {
            float floatRead = BitConverter.ToSingle(ReadBytes(sizeof(float)));

            if (float.IsNaN(floatRead) || float.IsInfinity(floatRead))
                throw new InvalidDataException($"Invalid float value: {floatRead}");

            return floatRead;
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException($"Could not convert {sizeof(float)} byte to a float.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool DeserializeBool()
    {
        try
        {
            return BitConverter.ToBoolean(ReadBytes(sizeof(bool)));
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException($"Could not convert {sizeof(bool)} to a bool.");
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte DeserializeByte()
    {
        try
        {
            return binaryReader.ReadByte();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Could not read byte inside of Unreal Package.");
        }
    }

    /// <summary>
    /// Deserializes a UPK's contents.
    /// </summary>
    public List<UProperty> DeserializeUPK()
    {
        try
        {
            var serializerInstance = new UPKDeserializer(packageData.PackageType);
            // at this point we assume we have already taken care of the files header data
            // silently correct
            if (fileStream.Position is not 8)
                SetStreamPosition(8);
            
            var properties = serializerInstance.DeserializePackage(this);

            // once deserialization is finished we need to restore our original packages data
            if (packageData.isEncrypted)
                RevertFileToOriginalState();

            // set the package type for our enumerators because our deserialization was successful 
            IBEnum.packageType = packageData.PackageType;

            return properties;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(exception.Message);
        }
    }

    #endregion

    #region IDisposable
    private void Close()
    {
        binaryReader?.Close();
        binaryWriter?.Close();
        fileStream?.Close();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Close();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~UnrealPackage()
    {
        Dispose();
    }
    #endregion
}