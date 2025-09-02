using System.Security.Cryptography;
namespace SaveDumper.Utilities;

public static class Util
{
    public static string GetNextSaveFileNameForOuput(string fileName, string fileExtension)
    {
        string[] saveFiles = Directory.GetFiles(FilePaths.OutputDir, $"*{fileExtension}");
        if (saveFiles.Length == 0)
            return $"{fileName}0{fileExtension}";

        int maxIndex = -1;

        foreach (var file in saveFiles)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(file);

            var match = System.Text.RegularExpressions.Regex.Match(nameWithoutExt, @"(\d+)$");
            if (match.Success && int.TryParse(match.Value, out int index))
            {
                if (index > maxIndex)
                    maxIndex = index;
            }
        }

        int nextIndex = maxIndex + 1;
        string newFileName = System.Text.RegularExpressions.Regex.Replace(
            fileName,
            @"\d+$",
            nextIndex.ToString()
        );

        if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\d+$"))
            newFileName = $"{fileName}{nextIndex}";

        return newFileName + fileExtension;
    }

    public static byte[] DecryptDataECB(byte[] fileBytes, string aesKey)
    {
        // Omit the first 4 bytes since they are our save magic 
        byte[] encryptedData = new byte[fileBytes.Length - 4];
        Buffer.BlockCopy(fileBytes, 4, encryptedData, 0, encryptedData.Length);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(aesKey);

            if (aes.Key.Length != 16 && aes.Key.Length != 24 && aes.Key.Length != 32)
                throw new ArgumentException("AES key must be 16, 24, or 32 bytes long.");

            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
                return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
    }

    public static byte[] EncryptDataECB(byte[] dataToEncrypt, string aesKey, byte[] magicHeader = null!)
    {
        if (magicHeader == null)
            magicHeader = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        if (magicHeader.Length != 4)
            throw new ArgumentException("Magic header must be exactly 4 bytes");

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(aesKey);
            if (aes.Key.Length != 16 && aes.Key.Length != 24 && aes.Key.Length != 32)
                throw new ArgumentException("AES key must be 16, 24, or 32 bytes long.");

            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            byte[] paddedData = PadToBlockSize(dataToEncrypt, 16);

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] encryptedData = encryptor.TransformFinalBlock(paddedData, 0, paddedData.Length);
                byte[] result = new byte[4 + encryptedData.Length];
                Buffer.BlockCopy(magicHeader, 0, result, 0, 4);
                Buffer.BlockCopy(encryptedData, 0, result, 4, encryptedData.Length);

                return result;
            }
        }
    }
    
    private static byte[] PadToBlockSize(byte[] data, int blockSize)
    {
        int remainder = data.Length % blockSize;
        // already a multiple of 16: return
        if (remainder is 0)
            return data; 
        
        int paddingSize = blockSize - remainder;
        byte[] paddedData = new byte[data.Length + paddingSize];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);
        
        return paddedData;
    }

}

/// <summary>
/// utility class for the tool's GUI to make the interface cleaner
/// </summary>
public static class ProgressBar
{
    public static void Run(string label, Action work)
    {
        int barWidth = 50;
        bool running = true;
        Exception? caughtException = null;
        
        Thread progressThread = new Thread(() =>
        {
            int progress = 0;
            int currentTop = Console.CursorTop;
            
            while (running)
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, currentTop);
                Global.PrintColored($"{label}: [", ConsoleColor.White, false);
                Global.PrintColored(new string('=', progress), ConsoleColor.Yellow, false);
                Console.Write(new string(' ', barWidth - progress));
                Global.PrintColored("]", ConsoleColor.White, false);
                progress = (progress + 1) % (barWidth + 1);
                Thread.Sleep(50);
            }
        });
        
        progressThread.Start();
        
        try
        {
            work();
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        running = false;
        progressThread.Join();
        
        Console.SetCursorPosition(0, Console.CursorTop);
        Global.PrintColored($"{label}: [", ConsoleColor.White, false);
        
        if (caughtException == null)
        {
            Global.PrintColored(new string('=', barWidth), ConsoleColor.Green, false);
            Global.PrintColored("] 100%\n", ConsoleColor.White);
        }
        else
        {
            Global.PrintColored(new string('=', barWidth), ConsoleColor.Red, false);
            Global.PrintColored("] FAILED\n", ConsoleColor.White);
            Global.PrintColored($"Error: {caughtException.Message}\n", ConsoleColor.Red);
        }
        
        Console.CursorVisible = true;
    }
}


/// <summary>
/// Helps keep file locations, names, etc. organized.
/// All file path data needed for the program is stored here
/// </summary>
public static class FilePaths
{
    public static DirectoryInfo parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())!;
    public static string OutputDir = $@"{parentDirectory}\OUTPUT";

    public static string baseLocation = $@"{parentDirectory}\SAVE STORAGE LOCATION";
    public static string IB3SAVES = Path.Combine(baseLocation, @"IB3 Backup");
    public static string IB2SAVES = Path.Combine(baseLocation, @"IB2 Backup");
    public static string IB1SAVES = Path.Combine(baseLocation, @"IB1 Backup");
    public static string VOTESAVES = Path.Combine(baseLocation, @"VOTE!!! Backup");

    public static void ValidateOutputDirectory()
    {
        if (!File.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);
    }

    public static bool DoesOutputExist() => File.Exists(OutputDir);
};
