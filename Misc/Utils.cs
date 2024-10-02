using System.Reflection;

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
        typeof(UDynamicArrayManager<>).MakeGenericType(genericType);

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
