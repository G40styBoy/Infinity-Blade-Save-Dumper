using SaveDumper.UArrayData;
using System.Text.Json;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text;

namespace SaveDumper.UPropertyData;

/// <summary>
/// Enum representation of all UProperty types discoverable inside of an Infinity Blade Save.
/// </summary>
public enum PropertyType
{
    StructProperty,
    ArrayProperty,
    IntProperty,
    StrProperty,
    NameProperty,
    FloatProperty,
    BoolProperty,
    ByteProperty
}

/// <summary>
/// Used to package and pass tag data more neatly once its been stored.
/// </summary>
public record struct TagContainer
{
    //UProperty
    public string name;
    public string type;
    public int size;
    public int arrayIndex;

    //UStruct
    public string alternateName;

    //UArray
    public int arrayEntryCount;
    public ArrayMetadata arrayInfo;

    /// <summary>
    /// lets us know if we need to keep track of a properties total size for struct and array purposes
    /// </summary>
    public bool bShouldTrackMetadataSize;
}

public class UPropertyDataHelper
{
    public const int EMPTY = 0;
    public const int NULL_TERMINATOR = sizeof(byte);
    public const int ARRAY_INDEX_SIZE = sizeof(int);
    public const int VALUE_SIZE = sizeof(int);
    public const int INT_SIZE = sizeof(int);
    public const int FLOAT_SIZE = sizeof(float);
    public const int BYTE_SIZE = sizeof(byte);
    /// <summary>
    /// Size used for booleans inside of serialized data. Boolean size should usually be a byte
    /// </summary>
    public const int BYTE_SIZE_SPECIAL = EMPTY;
    public const int BOOL_SIZE = sizeof(bool);

    internal int ReturnLitteEndianStringLength(string str)
    {
        if (str == string.Empty)
            return sizeof(int);
        return VALUE_SIZE + str.Length + NULL_TERMINATOR;  
    } 
    internal void PopulatePropertyMetadataSize(UProperty property)
    {
        if (property.uPropertyElementSize is null)
            property.uPropertyElementSize = EMPTY;
        property.uPropertyElementSize += ReturnLitteEndianStringLength(property.name); // name string size
        property.uPropertyElementSize += ReturnLitteEndianStringLength(property.type); // name type size
        property.uPropertyElementSize += VALUE_SIZE; // Little endian value size
        property.uPropertyElementSize += ARRAY_INDEX_SIZE; // little endian array index
        property.uPropertyElementSize += property.size; // actual size of value
    }

    internal string ReaderValueToString(JsonTextReader reader)
    {
        string str;
        str = reader.Value?.ToString() ?? string.Empty;
        if (reader.Value is null)
            throw new InvalidCastException($"Reader.Value is null. Expected a value, got {reader.TokenType}");
        return str;
    }

    internal T ParseReaderValue<T>(JsonTextReader reader, TryParseDelegate<T> tryParse) where T : struct
    {
        string str = reader.Value?.ToString()!;  // we account for a null result, silence warning
        if (str is null)
            throw new InvalidCastException($"Reader.Value is null for {reader.TokenType}");

        if (!tryParse(str, out T result))
            throw new ArgumentException($"Cannot convert '{str}' to {typeof(T).Name}");

        return result;
    }

    internal protected delegate bool TryParseDelegate<T>(string input, out T result);

    internal int CalculateArrayContentSize<T>(List<T> elements)
    {
        if (elements.Count is UPropertyDataHelper.EMPTY)
            return EMPTY;

        return elements[0] switch
        {
            string => GetStringArraySize(elements),
            int => elements.Count * sizeof(int),
            float => elements.Count * sizeof(float),
            bool => elements.Count * sizeof(bool),
            List<UProperty> => GetStructArraySize(elements),
            UProperty => GetStructSize(elements),
            _ => throw new NotImplementedException("Dynamic array size calculation not implemented for this type.")
        };
    }

    private protected int GetStringArraySize<T>(List<T> elements)
    {
        int totalSize = 0;
        foreach (string element in elements.OfType<string>())
            totalSize += ReturnLitteEndianStringLength(element);

        return totalSize;
    }

    private protected int GetStructSize<T>(List<T> elements)
    {
        int totalSize = 0;

        foreach (var property in elements.OfType<UProperty>())
            totalSize += property.uPropertyElementSize ?? 0;

        // Add "None" terminator size for each nested object
        totalSize += ReturnLitteEndianStringLength(UType.NONE);

        return totalSize;
    }

    private protected int GetStructArraySize<T>(List<T> elements)
    {
        int totalSize = 0;
        foreach (var nestedArray in elements.OfType<List<UProperty>>())
        {
            foreach (var property in nestedArray)
                totalSize += property.uPropertyElementSize ?? 0;

            // Add "None" terminator size for each nested object
            totalSize += ReturnLitteEndianStringLength(UType.NONE);
        }

        return totalSize;
    }

    internal void SerializeString(ref BinaryWriter binWriter, string str)
    {
        // write the size of the empty str
        if (str == string.Empty)
        {
            binWriter.Write(EMPTY);
            return;
        }

        // instead of using binWriter.Write directly for strings, use this work-around 
            // avoids appending the string size as a byte to the beginning of the string in hex
        byte[] strBytes = Encoding.UTF8.GetBytes(str);
        binWriter.Write(str.Length + sizeof(byte)); // str + nt
        binWriter.Write(strBytes);
        binWriter.Write((byte)EMPTY);  // null term
    }

    internal void SerializeMetadata(ref BinaryWriter binWriter, UProperty property)
    {
        SerializeString(ref binWriter, property.name);
        SerializeString(ref binWriter, property.type);
        binWriter.Write(property.size);
        binWriter.Write(property.arrayIndex);
    }
}

/// <summary>
/// String representation of all UProperty types discoverable inside of an Infinity Blade Save.
/// </summary>
public record struct UType
{
    public const string INT_PROPERTY = "IntProperty";
    public const string FLOAT_PROPERTY = "FloatProperty";
    public const string BYTE_PROPERTY = "ByteProperty";
    public const string BOOL_PROPERTY = "BoolProperty";
    public const string STR_PROPERTY = "StrProperty";
    public const string NAME_PROPERTY = "NameProperty";
    public const string STRUCT_PROPERTY = "StructProperty";
    public const string ARRAY_PROPERTY = "ArrayProperty";
    public const string NONE = "None";
}

public abstract class UProperty
{
    public string name;
    public string type;
    public int size;
    public int arrayIndex;
    public int? uPropertyElementSize; // This value is seperately constructed due to its special construction requirments

    private protected UPropertyDataHelper uHelper = new UPropertyDataHelper();

    public UProperty(TagContainer tag)
    {
        name = tag.name;
        type = tag.type;
        arrayIndex = tag.arrayIndex;
        size = tag.size;
    }

    private protected bool ShouldTrackFullSize(TagContainer tag) => tag.bShouldTrackMetadataSize is true;

    public abstract void WriteValueData(Utf8JsonWriter writer);
    public abstract void WriteValueData(Utf8JsonWriter writer, string name);
    public abstract void SerializeValue(BinaryWriter writer);
}

public class UIntProperty : UProperty
{
    public int value;
    public UIntProperty(UnrealPackage UPK, TagContainer tag)
        : base(tag) => value = UPK.DeserializeInt();

    public UIntProperty(JsonTextReader reader, TagContainer tag) : base(tag)
    {
        value = uHelper.ParseReaderValue<int>(reader, int.TryParse);

        if (ShouldTrackFullSize(tag))
            uHelper.PopulatePropertyMetadataSize(this);
    }

    public override void WriteValueData(Utf8JsonWriter writer) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteValueData(Utf8JsonWriter writer, string name) => writer.WriteNumber(name, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer) => writer.Write(value); 
}

public class UFloatProperty : UProperty
{
    public float value;
    private readonly string doubleFormat = "0.0#########"; // More decimal places                              
    private readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture;
    public UFloatProperty(UnrealPackage UPK, TagContainer tag)
        : base(tag) => value = UPK.DeserializeFloat();

    public UFloatProperty(JsonTextReader reader, TagContainer tag) : base(tag)
    {
        value = uHelper.ParseReaderValue<float>(reader, float.TryParse);
        if (ShouldTrackFullSize(tag))
            uHelper.PopulatePropertyMetadataSize(this);
    }


    public override void WriteValueData(Utf8JsonWriter writer) { }
    public override void WriteValueData(Utf8JsonWriter writer, string name)
    {
        // Since json rounds up .0 values it causes an issue for us when interpreting the json data
        // with this we work around the program omitting the .0 suffix
        writer.WritePropertyName(name);
        writer.WriteRawValue(value.ToString(doubleFormat, cultureInfo));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer) => writer.Write(value); 
}

public class UBoolProperty : UProperty
{
    public bool value;
    public UBoolProperty(UnrealPackage UPK, TagContainer tag)
        : base(tag) => value = UPK.DeserializeBool();

    public UBoolProperty(JsonTextReader reader, TagContainer tag) : base(tag)
    {
        value = uHelper.ParseReaderValue<bool>(reader, bool.TryParse);

        if (ShouldTrackFullSize(tag))
        {
            // with bools, they're value size is serialized as 0
            // we need to account for this when calculating its metadata size   
            uPropertyElementSize = sizeof(bool);
            uHelper.PopulatePropertyMetadataSize(this);
        }
    }
    public override void WriteValueData(Utf8JsonWriter writer) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteValueData(Utf8JsonWriter writer, string name) => writer.WriteBoolean(name, value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer) => writer.Write(value); 
}

//TODO: eventually add proper support for fName's
public class UStringProperty : UProperty
{
    private const string FNAME_PREFIX = "ini_";
    private bool isName = false;
    public string value = string.Empty;

    public UStringProperty(UnrealPackage UPK, TagContainer tag) : base(tag)
    {
        value = UPK.DeserializeString();
        CheckForFName(tag);
    }

    public UStringProperty(JsonTextReader reader, TagContainer tag) : base(tag)
    {      
        value = uHelper.ReaderValueToString(reader);
        size = uHelper.ReturnLitteEndianStringLength(value);
        CheckForFName(tag);
        if (ShouldTrackFullSize(tag))
            uHelper.PopulatePropertyMetadataSize(this);
    }

    private void CheckForFName(TagContainer tag)
    {
        if (tag.type is UType.NAME_PROPERTY)
            isName = true;
    }

    public override void WriteValueData(Utf8JsonWriter writer) => writer.WriteStringValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteValueData(Utf8JsonWriter writer, string name)
    {
        if (isName)
            name = $"{FNAME_PREFIX}{name}";
        writer.WriteString(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer) => uHelper.SerializeString(ref writer, value); 
}

public abstract class UByteProperty : UProperty
{
    public UByteProperty(TagContainer tag) : base(tag) {}

    public static UByteProperty InstantiateProperty(UnrealPackage UPK, TagContainer tag)
    {
        var identifier = UPK.DeserializeString();
        return tag.size switch
        {
            sizeof(byte) => new USimpleByteProperty(UPK, tag),
            > sizeof(byte) => new UEnumByteProperty(UPK, tag, identifier),
            _ => throw new NotSupportedException($"Unsupported byte property size: {tag.size}")
        };
    }

    public static UByteProperty InstantiateProperty(ref JsonTextReader reader, TagContainer tag)
    {
        return tag.size switch
        {
            sizeof(byte) => new USimpleByteProperty(reader, tag),
            UPropertyDataHelper.EMPTY => new UEnumByteProperty(ref reader, tag),
            _ => throw new NotSupportedException($"Unsupported byte property size: {tag.size}")
        };
    }
}

public class USimpleByteProperty : UByteProperty
{
    public byte value;
    public USimpleByteProperty(UnrealPackage UPK, TagContainer tag)
        : base(tag) => value = UPK.DeserializeByte();

    public USimpleByteProperty(JsonTextReader reader, TagContainer tag) : base(tag)
    {
        value = uHelper.ParseReaderValue<byte>(reader, byte.TryParse);

        if (ShouldTrackFullSize(tag))
        {
            // since there is no enumerator name, we need to serialize none in its place
            uPropertyElementSize = uHelper.ReturnLitteEndianStringLength(UType.NONE);
            uHelper.PopulatePropertyMetadataSize(this);
        }
    }

    public override void WriteValueData(Utf8JsonWriter writer) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteValueData(Utf8JsonWriter writer, string name) => writer.WriteNumber($"b{name}", value);
    public override void SerializeValue(BinaryWriter writer)
    {
        uHelper.SerializeString(ref writer, "None");
        writer.Write(value);
    }
}

public class UEnumByteProperty : UByteProperty
{
    public string enumName = string.Empty;
    public string enumValue = string.Empty;
    private const string ENUM_PREFIX = "e";
    private const string EDGECASE_PLAYERTYPE = "eCurrentPlayerType";
    private const string ENUM_NAME = "Enum";
    private const string ENUM_VALUE = "Enum Value";


    public UEnumByteProperty(UnrealPackage UPK, TagContainer tag, string enumName) : base(tag)
    {
        this.enumName = enumName;
        enumValue = UPK.DeserializeString();
    }

    public UEnumByteProperty(ref JsonTextReader reader, TagContainer tag) : base(tag)
    {
        if (CheckPropertyName(ref reader, ENUM_NAME))
            enumName = uHelper.ReaderValueToString(reader);

        if (CheckPropertyName(ref reader, ENUM_VALUE))
            enumValue = uHelper.ReaderValueToString(reader);

        size = uHelper.ReturnLitteEndianStringLength(enumValue);
        // read past the "}" closing statement so our logic doesnt run into issues
        reader.Read();

        if (ShouldTrackFullSize(tag))
        {
            uPropertyElementSize = uHelper.ReturnLitteEndianStringLength(enumName);
            uHelper.PopulatePropertyMetadataSize(this);
        }
    }

    private bool CheckPropertyName(ref JsonTextReader reader, string stringExpected)
    {
        reader.Read();
        if (uHelper.ReaderValueToString(reader) != stringExpected)
            throw new InvalidDataException($"Expected {stringExpected} as a property name.");
        reader.Read();
        return true;
    }

    public override void WriteValueData(Utf8JsonWriter writer) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteValueData(Utf8JsonWriter writer, string name)
    {
        // Edge case since "eCurrentPlayerType" is the only enum that is prefixed with e
        // this is the same name that gets loaded into the loaders .obj, so we cant change it
        if (name is not EDGECASE_PLAYERTYPE)
            name = $"{ENUM_PREFIX}{name}";
        writer.WriteStartObject(name);
        writer.WriteString(ENUM_NAME, enumName);
        writer.WriteString(ENUM_VALUE, enumValue);
        writer.WriteEndObject();
    }

    public override void SerializeValue(BinaryWriter writer)
    {
        uHelper.SerializeString(ref writer, enumName);
        uHelper.SerializeString(ref writer, enumValue);
    }
}

public class UStructProperty : UProperty
{
    public List<UProperty> elements;
    public string structName;

    public UStructProperty(TagContainer tag, string structName, List<UProperty> elements) : base(tag)
    {
        this.structName = structName;
        this.elements = elements;
    }

    public UStructProperty(JsonTextReader reader, TagContainer tag, List<UProperty> elements, string structName) : base(tag)
    {
        this.structName = structName;
        this.elements = elements;
        size += uHelper.CalculateArrayContentSize(elements);

        if (this.structName == string.Empty)
            this.structName = AttemptResolveAltName();

        uPropertyElementSize = uHelper.ReturnLitteEndianStringLength(this.structName);
        uHelper.PopulatePropertyMetadataSize(this);
    }

    /// <summary>
    /// used for structs with alternames embedded in static arrays
    /// <summary/>
    private string AttemptResolveAltName()
    {
        return name switch
        {
            "Data" => "ItemEnhanceData",
            "ForcedMapVariation" => "BossMapDefinition",
            "CurrentTotalTrackingStats" => "BattleTrackingStats",
            "GameOptions" => "PersistGameOptions",
            _ => string.Empty
        };
    }

    private void LoopJsonParsing(Utf8JsonWriter writer)
    {
        foreach (var element in elements)
            element.WriteValueData(writer, element.name);
    }
    

    public override void WriteValueData(Utf8JsonWriter writer) => LoopJsonParsing(writer);
    public override void WriteValueData(Utf8JsonWriter writer, string name)
    {
        writer.WriteStartObject(name);
        LoopJsonParsing(writer);
        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer)
    {
        if (structName != string.Empty)
                uHelper.SerializeString(ref writer, structName);

        foreach (var element in elements)
        {
            uHelper.SerializeMetadata(ref writer, element);
            element.SerializeValue(writer);
        }

        uHelper.SerializeString(ref writer, "None");
    }
}

public class UArrayProperty<T> : UProperty
{
    public ArrayMetadata arrayInfo;
    public int arrayEntryCount;
    public List<T> elements;
    private const string EDGECASE_CHEEVO = "SavedCheevo";

    public UArrayProperty(TagContainer tag, List<T> elements) : base(tag)
    {
        arrayEntryCount = tag.arrayEntryCount;
        this.elements = elements;
        arrayInfo = tag.arrayInfo;
    }

    public UArrayProperty(JsonTextReader reader, TagContainer tag, List<T> elements) : base(tag)
    {
        arrayEntryCount = elements.Count;
        this.elements = elements;
        arrayInfo = tag.arrayInfo;
        size += uHelper.CalculateArrayContentSize(elements);
        uHelper.PopulatePropertyMetadataSize(this);
    }

    private void LoopJsonParsing(Utf8JsonWriter writer)
    {
        if (arrayInfo.arrayType is ArrayType.Dynamic)
        {
            WriteDynamicElements(writer);
            return;
        }

        // dealing with only static arrays now
        // static arrays with the type "Array" arent supported
        if (arrayInfo.valueType is PropertyType.StructProperty)
        {
            if (name is EDGECASE_CHEEVO)
            {
                writer.WriteStartObject();
                foreach (UStructProperty element in elements.OfType<UStructProperty>())
                    element.WriteValueData(writer, IBEnums.GetEnumEntryFromIndex(name, element.arrayIndex));

                writer.WriteEndObject();
                return;
            }

            foreach (UStructProperty element in elements.OfType<UStructProperty>())
            {
                writer.WriteStartObject();
                element.WriteValueData(writer);
                writer.WriteEndObject();
            }
            return;
        }

        if (arrayInfo.valueType is PropertyType.IntProperty)
        {
            writer.WriteStartObject();
            LoopFunctionOverVariablesOfElementType<UProperty>(element => element.WriteValueData(writer, IBEnums.GetEnumEntryFromIndex(name, element.arrayIndex)));
            writer.WriteEndObject();
            return;
        }
        else
        {
            LoopFunctionOverVariablesOfElementType<UProperty>(element => element.WriteValueData(writer));
            return;
        }

        throw new NotImplementedException($"Unsupported array type: {arrayInfo.valueType}");
    }

    private void WriteDynamicElements(Utf8JsonWriter writer)
    {
        if (arrayEntryCount is UPropertyDataHelper.EMPTY)
            return;

        switch (elements[0])
        {
            case string:
                LoopFunctionOverVariablesOfElementType<string>(writer.WriteStringValue);
                break;
            case int:
                LoopFunctionOverVariablesOfElementType<int>(writer.WriteNumberValue);
                break;
            case float:
                LoopFunctionOverVariablesOfElementType<float>(writer.WriteNumberValue);
                break;
            case bool:
                LoopFunctionOverVariablesOfElementType<bool>(writer.WriteBooleanValue);
                break;
            case List<UProperty>:
                ReadDynamicStruct(writer);
                break;
            default:
                throw new NotImplementedException("Dynamic array type not implemented.");
        }
    }

    private void ReadDynamicStruct(Utf8JsonWriter writer)
    {
        foreach (var element in elements.OfType<List<UProperty>>())
        {
            writer.WriteStartObject();
            // nested array(s)
            foreach (var dynamicElement in element)
                dynamicElement.WriteValueData(writer, dynamicElement.name);
            writer.WriteEndObject();
        }
    }

    private void SerializeDynamicStruct(BinaryWriter writer)
    {
        foreach (var element in elements.OfType<List<UProperty>>())
        {
            // nested array(s)
            foreach (var dynamicElement in element)
            {
                uHelper.SerializeMetadata(ref writer, dynamicElement);
                dynamicElement.SerializeValue(writer);
            }

            uHelper.SerializeString(ref writer, "None");
        }
    }

    private void LoopFunctionOverVariablesOfElementType<Type>(Action<Type> func) where Type : notnull
    {
        var castedElements = elements.OfType<Type>();
        foreach (Type element in castedElements)
            func(element);
    }

    public override void WriteValueData(Utf8JsonWriter writer) => LoopJsonParsing(writer);
    public override void WriteValueData(Utf8JsonWriter writer, string name)
    {
        writer.WriteStartArray(name);
        LoopJsonParsing(writer);
        writer.WriteEndArray();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SerializeValue(BinaryWriter writer)
    {
        writer.Write(arrayEntryCount);

        if (arrayEntryCount is UPropertyDataHelper.EMPTY)
            return;

        // we need a hack here because the writer.Write method for strings does not function how i want
        // custom function needs to be executed here
        if (elements[0] is string)
        {
            var castedElements = elements.OfType<string>();
            foreach (var element in castedElements)
                uHelper.SerializeString(ref writer, element);

            return;
        }            

        switch (elements[0])
        {
            case int:
                LoopFunctionOverVariablesOfElementType<int>(writer.Write);
                break;
            case float:
                LoopFunctionOverVariablesOfElementType<float>(writer.Write);
                break;
            case bool:
                LoopFunctionOverVariablesOfElementType<bool>(writer.Write);
                break;
            case List<UProperty>:
                SerializeDynamicStruct(writer);
                break;
            default:
                throw new NotImplementedException("Dynamic array type not implemented.");
        }
    }
}