// using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using SaveDumper.Deserializer;
using SaveDumper.UArrayData;

namespace SaveDumper.UnrealPackageManager;

public class UnrealPackage : IDisposable
{
    private string _filePath;
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

    public UnrealPackage(string filePath, PackageType type)
    {
        _filePath = filePath;
        _packageData = new PackageData();
        if (!File.Exists(_filePath))
            throw new FileNotFoundException("The specified file does not exist.", _filePath);

        try{
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)){
                // test access
            }
        }
        catch (Exception ex){
            throw new InvalidOperationException("File is not accessible for read/write operations.", ex);
        }
        try{
            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _binaryReader = new BinaryReader(_fileStream, Encoding.UTF8, leaveOpen: true);
            _binaryWriter = new BinaryWriter(_fileStream, Encoding.UTF8, leaveOpen: true);
        }
        catch (Exception ex){
            throw new InvalidOperationException("Could not open file stream.", ex);
        }

        GetPackageInfo(type);
    }

    // TODO: fixed values for now, need to parse data respective to file type
    private void GetPackageInfo(PackageType type)
    {
        _packageData.PackageType = type;
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
        _fileStream.Position -= sizeof(int) + sizeof(byte); // size + nt
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

        try{
            str = DeserializeString();
        }
        finally{
            _binaryReader.BaseStream.Position = originalPosition;
        }

        return str;
    }

    internal string DeserializeString()
    {
        try
        {
            var strLength = DeserializeInt();
            if (strLength <= UPropertyDataHelper.EMPTY)
                return string.Empty;
            strLength--;

            var bytes = new byte[strLength];
            _binaryReader.Read(bytes, UPropertyDataHelper.EMPTY, strLength);
            _fileStream.Position++;   
            
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception exception){
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
        catch (ArgumentOutOfRangeException){
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
        try{
            return BitConverter.ToBoolean(ReadBytes(sizeof(bool)));
        }
        catch (ArgumentOutOfRangeException){
            throw new ArgumentOutOfRangeException($"Could not convert {sizeof(bool)} to a bool.");
        }     
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte DeserializeByte()
    {
        try{
            return _binaryReader.ReadByte();
        }
        catch (Exception){
            throw new InvalidOperationException($"Could not read byte inside of Unreal Package.");
        }     
    }

    /// <summary>
    /// Deserializes a UPK's contents.
    /// </summary>
    /// <returns>Returns UPK FProperty contents via a list.</returns>
    public List<UProperty> DeserializeUPK(bool reportDiagnostics = false)
    {
        Global.PrintColoredLine("Deserializing UPK...", ConsoleColor.Yellow, true);
        var serializerInstance = new UPKDeserializer(_packageData.PackageType);
        DeserializePackageInfo();

        var properties = serializerInstance.DeserializePackage(this);

        if (properties is null)
        {
            Global.PrintColoredLine("Deserialization unsuccessful!\n", ConsoleColor.Red, true);
            return null!;
        }

        Global.PrintColoredLine("Deserialization successful!\n", ConsoleColor.Green, true);
        return properties;
    }

    /// <summary>
    /// Stores the UPK header data.
    /// </summary>
    private void DeserializePackageInfo() => _packageData.EncryptedInitialBytes = _binaryReader.ReadBytes(8);

    /// <returns>A boolean value that indicates whether the stream has reached the end of the file</returns>
    internal bool IsEndFile() => _fileStream.Position >= _fileStream.Length;

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

