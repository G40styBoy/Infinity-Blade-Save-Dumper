// using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using SaveDumper.FPropertyManager;
using SaveDumper.FArrayManager;

namespace SaveDumper.UnrealPackageManager;

public class UnrealPackage : IDisposable
{
    private readonly string _filePath;
    private FileStream _fileStream;
    private BinaryReader _binaryReader;
    private BinaryWriter _binaryWriter;
    internal PackageData _packageData;
    private bool _disposed = false;

    #region Constructor
    public record PackageData
    {
        public bool IsEncrypted;
        public byte[] EncryptedInitialBytes = Array.Empty<byte>();
        public PackageType PackageType;
    }

    public UnrealPackage(string filePath)
    {
        _filePath = filePath;
        _packageData = new PackageData();
        if (!File.Exists(_filePath))
            throw new FileNotFoundException("The specified file does not exist.", _filePath);

        try
        {
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // test access
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("File is not accessible for read/write operations.", ex);
        }
        try
        {
            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _binaryReader = new BinaryReader(_fileStream, Encoding.UTF8, leaveOpen: true);
            _binaryWriter = new BinaryWriter(_fileStream, Encoding.UTF8, leaveOpen: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open file stream.", ex);
        }

        GetPackageInfo();
    }

    // TODO: fixed values for now, need to parse data respective to file type
    private void GetPackageInfo()
    {
        _packageData.PackageType = PackageType.IB3;
        _packageData.IsEncrypted = false;
    }
    #endregion


    #region Utility Methods
    public void LogStreamPosition() => Console.WriteLine($"Stream Position: {_fileStream.Position}");
    public long GetStreamPosition() => _fileStream.Position;
    public void SetStreamPosition(long position) => _fileStream.Position = position;
    public long GetStreamLength() => _fileStream.Length;

    // TODO: if need be, revert an specific amount back based on value type
    // for right now, this function doesnt need to be that extensive
    public void RevertStreamPosition(string value)
    {
        _fileStream.Position -= 5; // size + nt
        _fileStream.Position -= value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadBytes(int count) => _binaryReader.ReadBytes(count);

    /// <returns>the next string in the stream</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string PeekString()
    {
        string str;
        long originalPosition = _binaryReader.BaseStream.Position;

        try
        {
            str = ReadString();
        }
        finally
        {
            _binaryReader.BaseStream.Position = originalPosition;
        }

        return str;
    }

    /// <returns>a string extracted from a null terminated string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string ReadString()
    {
        var strLength = DeserializeInt();
        if (strLength <= 0)
            return string.Empty;
        strLength--;

        var bytes = new byte[strLength];
        _binaryReader.Read(bytes, 0, strLength);
        _fileStream.Position++;

        return Encoding.UTF8.GetString(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object DeserializeByteProperty()
    {
        string enumName = ReadString();

        if (enumName == FType.NONE)
            return DeserializeByte();
        else
        {
            string enumValue = ReadString();
            return new KeyValuePair<string, string>(enumName, enumValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int DeserializeInt(bool returnedSigned = false)
    {
        int readValue = BitConverter.ToInt32(ReadBytes(4));

        // if signed, clamp to int limit
        // infinity blade save values that are negative usually indicate they've gone over the limit
        if (int.IsNegative(readValue) && !returnedSigned)
            return int.MaxValue;

        return readValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float DeserializeFloat() => BitConverter.ToSingle(ReadBytes(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool DeserializeBool() => BitConverter.ToBoolean(ReadBytes(1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte DeserializeByte() => _binaryReader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string DeserializeString() => ReadString();

    /// <summary>
    /// Deserializes a UPK's contents.
    /// </summary>
    /// <returns>Returns UPK FProperty contents via a dictionary.</returns>
    internal List<FProperties.UProperty> DeserializeUPK()
    {
        Globals.PrintColoredLine("Deserializing UPK...", ConsoleColor.Yellow, true);
        var serializerInstance = new FProperties();
        DeserializePackageInfo();

        var properties = serializerInstance.Deserialize(this);
        Globals.PrintColoredLine("UPK Deserialization Successful.", ConsoleColor.Green, true);

        return properties;
    }

    /// <summary>
    /// Stores the UPK header data.
    /// </summary>
    private void DeserializePackageInfo() => _packageData.EncryptedInitialBytes = _binaryReader.ReadBytes(8);

    /// <returns>all array metadata respective to the UPK's file type.</returns>
    internal List<ArrayMetadata> RequestArrayInfo() => FArrayInitializer.PopulateArrayInfo();  // get array data   

    /// <returns>A boolean value that indicates whether the stream has reached the end of the file</returns>
    internal bool IsEndFile() => _fileStream.Position >= _fileStream.Length;

    /// <summary>
    /// Retrieves the names of all non-subset arrays in the UPK instance file.
    /// This is unfinished and will be updated in the future to support subset arrays, allowing for easy IB1,2, VOTE implementation.
    /// </summary>
    /// <returns></returns>
    public List<string> ReturnAllUPropertyNames()
    {
        var names = new List<string>();
        var seenNames = new HashSet<string>(); // Track seen names to detect duplicates

        DeserializePackageInfo();

        while (!IsEndFile())
        {
            string name = ReadString();

            if (name == FType.NONE)
                break;

            string type = ReadString();
            int size = DeserializeInt();

            switch (type)
            {
                case "ArrayProperty":
                    _fileStream.Position += 4; // Skip entry count
                    _fileStream.Position += size; // Skip entry data
                    break;
                case "StructProperty":
                    _fileStream.Position += 4; // Array Index
                    ReadString();
                    _fileStream.Position += size; // Skip struct data
                    break;
                case "ByteProperty":
                    _fileStream.Position += 4; // Array Index
                    ReadString();
                    _fileStream.Position += size; // Skip struct data
                    break;
                case "BoolProperty":
                    _fileStream.Position += 5; // Array Index
                    break;

                default:
                    _fileStream.Position += 4; // Array Index
                    _fileStream.Position += size; // value contents     
                    break;
            }

            if (!seenNames.Contains(name))
                names.Add(name);
            seenNames.Add(name);
        }

        _fileStream.Position = 0; // Reset file position
        return names;
    }

    //TODO: Handle decryption for IB1 and IB2 packages
    private void DecryptPackage() { }
    #endregion

    #region IDisposable
    public void Close()
    {
        _binaryReader?.Close();
        _binaryWriter?.Close();
        _fileStream?.Close();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~UnrealPackage()
    {
        Dispose();
    }
    #endregion
}

