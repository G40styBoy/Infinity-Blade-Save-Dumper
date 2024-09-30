class UnrealArchive
{

    internal FileStream saveStream;  // aquire private name for the filesteram
    //internal StreamWriter streamWrite;
    internal BinaryReader bReader;
    internal BinaryWriter bWriter;
    internal StreamWriter streamWriter;
    internal bool leaveOpen;
    internal byte[] fileBytes;

    public enum ArchiveState
    {
        LoadArchive,
        SaveArchive
    }
    internal ArchiveState State { get; private set; }
    internal bool ArLoading;
    internal bool ArSaving;


    private void SetFileReadAndWrite(FileInfo fileInfo, bool val) => fileInfo.IsReadOnly = !val;  // function as a true = writable false = read only instead of vise versa
    public void ChangeStreamPosition(long amount) => saveStream.Position += amount;
    public void ChangeWriterPosition(long amount) => bWriter.BaseStream.Position = amount;
    public void ChangeReadPosition(long amount) => bReader.BaseStream.Position = amount;
    private byte[] ReadBytes(int count) => bReader.ReadBytes(count);
    private void GetFileBytes(string saveFile, ref byte[] bytes) => bytes = File.ReadAllBytes(saveFile);
    public string CleanNullTerminator(string str) => str.Remove(str.Length-1);
    public void CleanNullTerminator(ref string str) => str.Remove(str.Length-1);
    
    public UnrealArchive(string fileName, FileMode mode, bool leaveOpen, Enum classState)
    {
        try
        {

            var State = (ArchiveState)classState;

            if (State == ArchiveState.LoadArchive)
            {
                ClearFileHexContents(Globals.binaryOutput);
            }

            // Ensure the file exists and is not empty.
            if (!File.Exists(fileName))
            {
                ConsoleHelper.DisplayColoredText("Error: File not found " + fileName, ConsoleHelper.ConsoleColorChoice.Red);
                return;
            }
            else if (new FileInfo(fileName).Length == 0 && State == ArchiveState.SaveArchive)  // if we are reading the file and its empty, there is nothing to read
            {
                ConsoleHelper.DisplayColoredText("Error: File empty for reading " + fileName, ConsoleHelper.ConsoleColorChoice.Red);
                return;
            }

            // fetches the bytes for the file, this is used for deserialization
            GetFileBytes(fileName, ref fileBytes);

            // Open the file stream with read/write access.
            saveStream = new FileStream(fileName, mode, FileAccess.ReadWrite);
            //streamWriter = new StreamWriter(saveStream, Encoding.UTF8);

            // Check if the file is read-only AFTER opening the stream
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                Console.WriteLine("Warning: File is read-only: " + fileName);
                return;
            }

            // Initialize reader/writer based on readWriteStatus
            if (State == ArchiveState.SaveArchive)
            {
                bReader = new BinaryReader(saveStream);
                ArSaving = true;
            }
            else if (State == ArchiveState.LoadArchive)
            {
                bWriter = new BinaryWriter(saveStream);
                ArLoading = true;
            }
            else
            {
                // type not set, throw error
                ConsoleHelper.DisplayColoredText("Error: Archive type not set.", ConsoleHelper.ConsoleColorChoice.Red);
                return;
            }

            // Store leaveOpen flag
            this.leaveOpen = leaveOpen; 
        }
        catch (UnauthorizedAccessException) 
        {
            Console.WriteLine("Error: Access to the file is denied."); 
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Error: Directory not found.");
        }
        catch (IOException)
        {
            Console.WriteLine("Error: An IO error occurred while accessing the file.");
        }
    }

    private void ClearFileHexContents(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (File.ReadAllBytes(filePath).Length <= 0)
            {
                Console.WriteLine($"{filePath} is empty.");
                return;
            }
            // Open the file in truncate mode (which clears the existing content)
            using (FileStream fileStream = new FileStream(filePath, FileMode.Truncate))

            Console.WriteLine($"File '{filePath}' contents cleared.");
        }
        else
        {
            Console.WriteLine($"Error: File '{filePath}' not found.");
        }
    }


    public byte[] utilDeserialize(ref byte[] _buffer)
    {
        byte[] bytes = ReadBytes(Globals.InfoGrab);
        return bytes;
    }

    public float DeserializeFloatArray(int _buffer)
    {
        byte[] bytes = ReadBytes(_buffer);
        return BitConverter.ToSingle(bytes);
    }

    public int Deserialize(int _buffer)
    {
        byte[] bytes = ReadBytes(_buffer);
        return Convert.ToInt32(Util.ConvertEndian(bytes));
    }

    public void Deserialize(ref byte _buffer)
    {
        byte[] bytes = ReadBytes(1);
        _buffer = bytes[0];
    }

    public void Deserialize(ref string str)
    {
        int _buffer = 0;
        byte[] _bytesBuffer = [];
        
        _buffer = Convert.ToInt32(Util.ConvertEndian(utilDeserialize(ref _bytesBuffer)));
        byte[] bytes = ReadBytes(_buffer);
        str = System.Text.Encoding.UTF8.GetString(bytes);
        str = CleanNullTerminator(str);
    }

    public void Deserialize(ref int _buffer)
    {
        byte[] bytes = ReadBytes(Globals.InfoGrab);
        _buffer =  Convert.ToInt32(Util.ConvertEndian(bytes));
    }
    public void Deserialize(ref float _buffer)
    {
        byte[] bytes = ReadBytes(Globals.InfoGrab);
        _buffer =  BitConverter.ToSingle(bytes);
    }

    public void Deserialize(ref bool _buffer)
    {
        byte[] bytes = ReadBytes(1);  //bool
        _buffer =  BitConverter.ToBoolean(bytes);

    }
}