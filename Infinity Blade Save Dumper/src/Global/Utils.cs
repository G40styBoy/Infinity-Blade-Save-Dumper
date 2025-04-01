using System.Runtime.CompilerServices;

public static class G40Util
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value)
    {
        if (value > int.MaxValue || (value < 0 && value != -1))
        {
            value = int.MaxValue;
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clamp(byte value)
    {
        if (value < 0)
        {
            value = 0; // Clamp negative values to 0
        }
        else if (value > 255)
        {
            value = 255; // Clamp values greater than 255 to 255
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Truncate(float value)
    {
        if (value.ToString().Contains(".")) return true;
        return false;
    }
}
