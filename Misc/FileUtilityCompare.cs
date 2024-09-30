using System;
using System.IO;

public class FileComparer
{
    private string _file1Path;
    private string _file2Path;

    public FileComparer(string file1Path, string file2Path)
    {
        _file1Path = file1Path;
        _file2Path = file2Path;
    }

    public void CompareFiles()
    {
        // Check if files exist
        if (!File.Exists(_file1Path) || !File.Exists(_file2Path))
        {
            Console.WriteLine("Error: One or both files do not exist.");
            return;
        }

        // Extract file names
        string fileName1 = Path.GetFileName(_file1Path);
        string fileName2 = Path.GetFileName(_file2Path);

        // Open file streams
        using (FileStream file1Stream = File.OpenRead(_file1Path))
        using (FileStream file2Stream = File.OpenRead(_file2Path))
        {
            // Determine smallest file size
            long smallestFileSize = Math.Min(file1Stream.Length, file2Stream.Length);

            // Compare bytes
            int differentByteCount = 0;
            long offset = 0;
            while (offset < smallestFileSize)
            {
                // Check if 9 or fewer bytes are remaining
                if (smallestFileSize - offset <= 9)
                {
                    Console.WriteLine($"Comparison stopped with 9 or fewer bytes remaining at offset {offset:X}.");
                    break; // Exit the comparison loop
                }

                int byte1 = file1Stream.ReadByte();
                int byte2 = file2Stream.ReadByte();
                offset++;

                if (byte1 != byte2)
                {
                    Console.WriteLine($"Difference at offset {offset - 1:X}: {fileName1} = {byte1:X2}, {fileName2} = {byte2:X2}");
                    differentByteCount++;

                    if (differentByteCount > 100)
                    {
                        Console.WriteLine("Comparison terminated: Too many differences (over 100).");
                        return;
                    }
                }
            }

            // Output results
            if (differentByteCount == 0 && file1Stream.Length == file2Stream.Length)
            {
                Console.WriteLine("Files are identical.");
            }
            else
            {
                if (file1Stream.Length != file2Stream.Length)
                {
                    Console.WriteLine($"File sizes differ. Comparison stopped at offset {offset:X}.");
                }

                //Console.WriteLine($"Total different bytes: {differentByteCount}");
            }
        }
    }
}