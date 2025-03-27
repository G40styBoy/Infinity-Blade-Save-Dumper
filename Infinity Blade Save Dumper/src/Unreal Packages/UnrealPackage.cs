using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using SaveDumper.FPropertyManager;
using SaveDumper.FArrayManager;
// using SaveDumper.Globals.FilePaths;

namespace SaveDumper.UnrealPackageManager;

public class UnrealPackage : IDisposable
{
    private readonly string _filePath;
    private FileStream _fileStream;
    private BinaryReader _binaryReader;
    private BinaryWriter _binaryWriter;
    internal PackageData _packageData;
    private bool _disposed = false;

    public class PackageData
    {
        public bool IsEncrypted { get; set; }
        public byte[] EncryptedInitialBytes = Array.Empty<byte>();
        public PackageType PackageType { get; set; }
    }

    #pragma warning disable CS8618  // this warning is a pain in the ass, and i dont feel like dirtying up the code to fix it   
    public UnrealPackage(string filePath)
    {
        _filePath = filePath;
        _packageData = new PackageData(); 
        SetupFile();
        OpenFile();
        GetPackageInfo();
    }
    #pragma warning restore CS8618

    private void SetupFile()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("The specified file does not exist.", _filePath);
        }

        try
        {
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // testing access here
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("File is not accessible for read/write operations.", ex);
        }
    }

    private void OpenFile()
    {
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
    }

    // TODO: fixed values for now, need to tailor data based on file type
    private void GetPackageInfo()
    {
        _packageData.PackageType = PackageType.IB3;
        _packageData.IsEncrypted = false;
    }


    // ██╗░░░██╗████████╗██╗██╗░░░░░██╗████████╗██╗░░░██╗
    // ██║░░░██║╚══██╔══╝██║██║░░░░░██║╚══██╔══╝╚██╗░██╔╝
    // ██║░░░██║░░░██║░░░██║██║░░░░░██║░░░██║░░░░╚████╔╝░
    // ██║░░░██║░░░██║░░░██║██║░░░░░██║░░░██║░░░░░╚██╔╝░░
    // ╚██████╔╝░░░██║░░░██║███████╗██║░░░██║░░░░░░██║░░░
    // ░╚═════╝░░░░╚═╝░░░╚═╝╚══════╝╚═╝░░░╚═╝░░░░░░╚═╝░░░


    public void LogStreamPosition() => Console.WriteLine($"Stream Position: {_fileStream.Position}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetStreamPosition() => _fileStream.Position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStreamPosition(long position) => _fileStream.Position = position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetStreamLength() => _fileStream.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadBytes(int count) => _binaryReader.ReadBytes(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte[] PeekBytes(int count)
    {
        long originalPosition = _binaryReader.BaseStream.Position;

        try
        {
            return _binaryReader.ReadBytes(count);
        }
        finally
        {
            _binaryReader.BaseStream.Position = originalPosition;
        }
    }

    internal string ReadNullTerminatedString()
    {
        // int strLength = DeserializeInt();
        _fileStream.Position += 4; 

        var bytes = new List<byte>();
        byte currentByte;
        while ((currentByte = _binaryReader.ReadByte()) != 0)
        {
            bytes.Add(currentByte);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object DeserializeByteProperty()
    {
        string enumName = ReadNullTerminatedString();

        if (enumName == FType.NONE)
            return DeserializeByte();
        else
        {
            string enumValue = ReadNullTerminatedString();
            return new KeyValuePair<string, string>(enumName, enumValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int DeserializeInt() => BitConverter.ToInt32(ReadBytes(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float DeserializeFloat() => BitConverter.ToSingle(ReadBytes(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool DeserializeBool() => BitConverter.ToBoolean(ReadBytes(1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte DeserializeByte() => ReadBytes(1)[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string DeserializeString()
    {
        if (DeserializeInt() == 0)
            return "";
        DeserializeInt();
        return ReadNullTerminatedString();
    }

    internal Dictionary<string, FProperties.FProperty> DeserializeUPK()
    {
        var serializerInstance = new FProperties();
        DeserializePackageInfo();
        var returnData = serializerInstance.Deserialize(this);

        Console.WriteLine("UPK Deserialized.");
        return returnData;
    }

    private void DeserializePackageInfo() => _packageData.EncryptedInitialBytes = _binaryReader.ReadBytes(8);

    internal List<ArrayMetadata> RequestArrayInfo() => FArrayInitializer.FetchArrayInfo(this);  // get array data   

    public List<string> ReturnSerializedArrayNames()
    {
        var arrayNames = new List<string>();
        var seenNames = new HashSet<string>(); // Track seen names to detect duplicates

        DeserializePackageInfo();

        while (GetStreamPosition() < GetStreamLength())
        {
            string name = ReadNullTerminatedString();

            if (name == "None"){
                break;
            }

            string type = ReadNullTerminatedString();
            int size = DeserializeInt();

            // Check if the property is an array
            if (type == "ArrayProperty" || seenNames.Contains(name) && !arrayNames.Contains(name)){
                arrayNames.Add(name);
            }

            switch (type)
            {
                case "ArrayProperty":
                    _fileStream.Position += 4; // Skip entry count
                    _fileStream.Position += size; // Skip entry data
                    break;
                case "StructProperty":
                    for (int i = 0; i < 2; i++){
                        ReadNullTerminatedString();
                    }
                    _fileStream.Position += size; // Skip struct data
                    break;
                case "ByteProperty":
                    _fileStream.Position += 4; // Array Index
                    ReadNullTerminatedString();
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
            seenNames.Add(name);
        }

        _fileStream.Position = 0; // Reset file position
        return arrayNames;
    }

    //TODO: Handle decryption for IB1 and IB2 packages
    private void DecryptPackage()
    {
        // _encryptedPackageMetaData = new EncryptedPackageData();
        // _encryptedPackageMetaData.InitialBytes = PeekBytes(4);

        // byte[] iv = _fileStream.;


        // byte[] keyBytes = Encoding.UTF8.GetBytes(IB2AESKEY);
        // byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
        // using Aes aes = Aes.Create();
        // aes.Key = keyBytes;
        // aes.IV = ivBytes;
    }


    // ██████╗░███████╗░██████╗░█████╗░██╗░░░██╗██████╗░░█████╗░███████╗░██████╗
    // ██╔══██╗██╔════╝██╔════╝██╔══██╗██║░░░██║██╔══██╗██╔══██╗██╔════╝██╔════╝
    // ██████╔╝█████╗░░╚█████╗░██║░░██║██║░░░██║██████╔╝██║░░╚═╝█████╗░░╚█████╗░
    // ██╔══██╗██╔══╝░░░╚═══██╗██║░░██║██║░░░██║██╔══██╗██║░░██╗██╔══╝░░░╚═══██╗
    // ██║░░██║███████╗██████╔╝╚█████╔╝╚██████╔╝██║░░██║╚█████╔╝███████╗██████╔╝
    // ╚═╝░░╚═╝╚══════╝╚═════╝░░╚════╝░░╚═════╝░╚═╝░░╚═╝░╚════╝░╚══════╝╚═════╝░

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
}

