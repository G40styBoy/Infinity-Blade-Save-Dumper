using System.Runtime.CompilerServices;
using System.Reflection;

public class Util
{
    // // not needed
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static int ToInt32FromBytes(byte[] bytes)
    // {
    //     try
    //     {
    //         if (bytes.Length != sizeof(int))
    //         {
    //             throw new ArgumentException("Byte array must be 4 bytes long.");
    //         }
    //         if (!BitConverter.IsLittleEndian)
    //         {
    //             Array.Reverse(bytes);
    //         }

    //         return BitConverter.ToInt32(bytes, 0);
    //     }
    //     catch (ArgumentOutOfRangeException)
    //     {
    //         Console.WriteLine("ArgumentOutOfRangeException: Invalid byte array length.");
    //         return 0;
    //     }
    //     catch (ArgumentException ex)
    //     {
    //         Console.WriteLine(ex.Message);
    //         return 0;
    //     }
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static byte[] ToLittleEndianBytes(int value)
    // {
    //     byte[] bytes = new byte[sizeof(int)];
    //     BitConverter.GetBytes(value).CopyTo(bytes, 0);

    //     if (!BitConverter.IsLittleEndian)
    //     {
    //         Array.Reverse(bytes);
    //     }

    //     return bytes;
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static byte[] FloatToLittleEndianBytes(float value)
    // {
    //     byte[] bytes = BitConverter.GetBytes(value); 

    //     if (!BitConverter.IsLittleEndian)
    //     {
    //         Array.Reverse(bytes);
    //     }

    //     return bytes;
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReturnClampedInt(int value)
    {
        if (value > int.MaxValue || (value < 0 && value != -1))
        {
            value = int.MaxValue;
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReturnClampedByte(byte value)
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

    public static bool IsArrayDynamic(string arrayName) 
    {
        if (Enum.TryParse(arrayName, out Globals.DynamicArrayTypes.DynamicArrayList dResult) && Enum.IsDefined(typeof(Globals.DynamicArrayTypes.DynamicArrayList), dResult)) return true;
        else return false;
    }

    public static bool IsUPropertyStatic(string propertyName) 
    {
        if (Enum.TryParse(propertyName, out Globals.StaticArrayTypes.StaticArrayNameList dResult) && Enum.IsDefined(typeof(Globals.StaticArrayTypes.StaticArrayNameList), dResult)) return true;
        else return false;
    }

     public static Type GetArrayVariableType<StructName>(string name) where StructName : struct
    {
        Type? someType = typeof(StructName);
        FieldInfo fieldInfo = someType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!; 
        if (fieldInfo == null) throw new ArgumentException($"Field {name} not found in type {typeof(StructName)}!"); 
        return fieldInfo.FieldType;  
    }

    private static void InstantiateDynamicArrayClass(Type arrayReflectionType, UnrealArchive Ar, string arrayName, int arraySize, JsonParser dataParser) =>
        Activator.CreateInstance(arrayReflectionType, Ar, arrayName, arraySize, dataParser);  // create instance of class with generic type

    private static void InstantiateStaticArrayClass(Type arrayReflectionType, UnrealArchive Ar, UPropertyManager manager,string arrayName, JsonParser dataParser) =>
        Activator.CreateInstance(arrayReflectionType, Ar, manager, arrayName, dataParser);  // create instance of class with generic type

    private static Type CreateDynamicGenericType(Type genericType) =>
        typeof(UPropertyManager.UDynamicArrayManager<>).MakeGenericType(genericType);

    private static Type CreateStaticGenericType(Type genericType) =>
        typeof(UStaticPropertyManager<>).MakeGenericType(genericType);


    public static void ManageDynamicArrayDeserialization(UnrealArchive Ar, string arrayName, int arraySize, JsonParser dataParser)
    {
        Type genericType = GetArrayVariableType<Globals.DynamicArrayTypes>(arrayName);
        InstantiateDynamicArrayClass(CreateDynamicGenericType(genericType), Ar, arrayName, arraySize, dataParser);
    }

    public static void ManageStaticArrayDeserialization(UnrealArchive Ar, UPropertyManager manager, string arrayName, JsonParser dataParser)
    {
        Type genericType = GetArrayVariableType<Globals.StaticArrayTypes>(arrayName);
        InstantiateStaticArrayClass(CreateStaticGenericType(genericType), Ar, manager, arrayName, dataParser);
    }

}