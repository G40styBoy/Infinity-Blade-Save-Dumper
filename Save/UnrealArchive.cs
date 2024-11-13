using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class UnrealArchive
{
    internal FileStream saveStream;  
    internal BinaryReader bReader;
    internal BinaryWriter bWriter;
    internal bool leaveOpen 
    {
        get { return false; }
        private set {}
    }
    internal byte[] fileBytes;

    public enum ArchiveState
    {
        LoadArchive,
        SaveArchive
    }
    internal ArchiveState serializationState;
    internal bool ArLoading;
    internal bool ArSaving;

    private void SetFileReadAndWrite(FileInfo fileInfo, bool val) => fileInfo.IsReadOnly = !val;  // function as a true = writable false = read only instead of vise versa
    public void ChangeStreamPosition(long amount) => saveStream.Position += amount;
    public void ChangeWriterPosition(long amount) => bWriter.BaseStream.Position = amount;
    public void ChangeReadPosition(long amount) => bReader.BaseStream.Position += amount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadBytes(int count) => 
        bReader.ReadBytes(count);

   
    public UnrealArchive(FileMode mode, bool leaveOpen, Enum classState)
    {
        // TODO; Need to adjust the scope of the try block
        try
        {
            // pre-req task's 
            serializationState = (ArchiveState)classState;
            string fileName = SetFilePath();
            if (serializationState == ArchiveState.LoadArchive)
            {
                ClearFileHexContents(Globals.binaryOutput);
            }

            CheckFileValidity(fileName); // check file status
            GetFileBytes(fileName, ref fileBytes!); // fetches the bytes for the file, this is used for deserialization

            saveStream = new FileStream(fileName, mode, FileAccess.ReadWrite);  
            if (IsReadOnly(fileName))  // check perms
                Console.WriteLine("Warning: File is read-only: " + fileName);

            InitializeReadWrite();
            InitializeFileHandler();

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

    private void InitializeFileHandler()
    {
        if (ArSaving) 
        {
            UPropertyManager uManager = new UPropertyManager(this);
            uManager.DeserializeDataToJson();  //deserialize file
        }
        else if(ArLoading) 
        {
            new JsonSerializer(this);
        }
    }

    private string SetFilePath()
    {
        if (serializationState == ArchiveState.SaveArchive) return Globals.saveFile[0];
        else if (serializationState == ArchiveState.LoadArchive) return Globals.binaryOutput;
        return "";
    }

    private bool InitializeReadWrite()
    {
        // Initialize reader/writer based on readWriteStatus
        if (serializationState == ArchiveState.SaveArchive)
        {
            bReader = new BinaryReader(saveStream);
            ArSaving = true;
            return true;
        }
        else if (serializationState == ArchiveState.LoadArchive)
        {
            bWriter = new BinaryWriter(saveStream);
            ArLoading = true;
            return true;
        }
        else
        {
            ConsoleHelper.DisplayColoredText("Error: Archive type not set.", ConsoleHelper.ConsoleColorChoice.Red);
            return false;
        }
    }

    private bool CheckFileValidity(string fileName)
    {
        if (!File.Exists(fileName))
        {
            ConsoleHelper.DisplayColoredText("Error: File not found " + fileName, ConsoleHelper.ConsoleColorChoice.Red);
            return false;
        }
        if (new FileInfo(fileName).Length == 0 && serializationState == ArchiveState.SaveArchive)  // if we are reading the file and its empty, there is nothing to read
        {
            ConsoleHelper.DisplayColoredText("Error: File empty for reading " + fileName, ConsoleHelper.ConsoleColorChoice.Red);
            return false;
        }

        return true;
    }

    private bool IsReadOnly(string fileName)
    {
        if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            Console.WriteLine("Warning: File is read-only: " + fileName);
            return true;
        }        
        return false;
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

    private void GetFileBytes(string saveFile, ref byte[] fileBytes)
    { 
        if (serializationState == ArchiveState.SaveArchive)
          fileBytes = File.ReadAllBytes(saveFile);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref int value) => 
        value = Util.Clamp(BitConverter.ToInt32( ReadBytes(4) ));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref float value) => value = BitConverter.ToSingle( ReadBytes(4) );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref bool value) => value = BitConverter.ToBoolean( ReadBytes(1) );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref byte value) => 
        value = Util.Clamp(ReadBytes(1)[0]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref string value) 
    {
        ReadOnlySpan<byte> bytes = ReadBytes( BitConverter.ToInt32( ReadBytes(4) ) );
        value = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0'); 
    }
}