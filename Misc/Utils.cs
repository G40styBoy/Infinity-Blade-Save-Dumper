public class Util
{
    public static int ConvertEndian(byte[] bytes)
    {
        try{
             if (!BitConverter.IsLittleEndian)  
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes);       
        }

        catch(ArgumentOutOfRangeException)
        {
            Console.WriteLine("ArgumentOutOfRangeException");
            return 0;
        }
    }

    public static byte[] IntToLittleEndianBytes(int value)
    {
        byte[] bytes = new byte[sizeof(int)]; // Allocate on the heap
        BitConverter.TryWriteBytes(bytes, value);

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes; // Now you can safely return the Span<byte>
    }
    // public static void ChangeConsoleColor(Console.ConsoleColor)
    public static byte[] FloatToLittleEndianBytes(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value); 

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

/// <summary>
/// Checks to see if a floating point has a decimal.
/// </summary>
    public static bool Truncate(float value)
    {
        if (value.ToString().Contains(".")) return true;
        return false;
    }

}
