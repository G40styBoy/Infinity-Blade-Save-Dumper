using SaveDumper.UArrayData;

namespace SaveDumper.Deserializer;

/// <summary>
/// Takes unencrypted serialized data and mutates it into digestable data.
/// </summary>
public class UPKDeserializer
{
    private List<ArrayMetadata> gamearrayInfo;
    private List<UProperty> _genericProperties = new();

    private const int MAX_STATIC_ARRAY_ELEMENTS = 10000; // Prevent memory exhaustion
    private static readonly int UNINITIALIZED_VALUE = -1; 

    internal protected UPKDeserializer(PackageType type) => gamearrayInfo = UArray.PopulateArrayInfo(type);
    
    internal List<UProperty> DeserializePackage(UnrealPackage UPK)
    {
        while (!UPK.IsEndFile())
        {
            try
            {
                UProperty tag = ConstructTag(UPK);
                if (tag != null)
                    _genericProperties.Add(tag);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}\nFile Position: {UPK.GetStreamPosition()}\nFile Length: {UPK.GetStreamLength()}");
                return null!;
            }
        }
        return _genericProperties;
    }

    private UProperty ConstructTag(UnrealPackage UPK, bool allowStaticArrayDetection = true)
    {
        try
        {
            if (UPK.IsEndFile())
                throw new Exception("Attempting to construct tag when file has been fully read.");

            // immediatly checking if we pull in NONE
            // signify end of construction asap
            var tag = new TagContainer();
            tag.name = UPK.DeserializeString();
            if (tag.name is UType.NONE)
                return null!;

            // We can tell the compiler to ignore the warning about tag.arrayInfo possibly being null
            // null possibility is fully accounted for, and in some cases expected for logic
            tag.arrayInfo = UArray.GetCurrentArray(gamearrayInfo, tag.name)!;

            if (allowStaticArrayDetection && tag.arrayInfo != null && tag.arrayInfo.arrayType is ArrayType.Static)
            {
                UPK.RevertStreamPosition(tag.name);
                PopulateUPropertyMetadata(ref tag, UType.ARRAY_PROPERTY, UNINITIALIZED_VALUE, UNINITIALIZED_VALUE);
                tag.type = UType.ARRAY_PROPERTY;
                tag.arrayEntryCount = UPropertyDataHelper.EMPTY;
            }
            else
            {
                PopulateUPropertyMetadata(ref tag, UPK.DeserializeString(), UPK.DeserializeInt(), UPK.DeserializeInt());
                if (tag.type is UType.ARRAY_PROPERTY)
                    tag.arrayEntryCount = UPK.DeserializeInt();
            }
            return ConstructUProperty(UPK, tag);
        }
        catch (Exception){ return null!; }
    }

    /// <summary>
    /// Populate the main metadata for a property based on what's read.
    /// </summary>
    private void PopulateUPropertyMetadata(ref TagContainer tag, string type, int size, int arrayIndex)
    {
        tag.type = type;
        tag.arrayIndex = arrayIndex;
        tag.size = size;
    }

    private UProperty ConstructUProperty(UnrealPackage UPK, TagContainer tag)
    {
        return tag.type switch
        {
            UType.INT_PROPERTY => new UIntProperty(UPK, tag),
            UType.FLOAT_PROPERTY => new UFloatProperty(UPK, tag),
            UType.BOOL_PROPERTY => new UBoolProperty(UPK, tag),
            UType.BYTE_PROPERTY => UByteProperty.InstantiateProperty(UPK, tag),
            UType.STR_PROPERTY or UType.NAME_PROPERTY => new UStringProperty(UPK, tag),
            UType.STRUCT_PROPERTY => CreateStructProperty(UPK, tag),
            UType.ARRAY_PROPERTY => CreateArrayProperty(UPK, tag),
            _ => throw new NotSupportedException($"Unsupported property type: {tag.type}")
        };
    }

    private UStructProperty CreateStructProperty(UnrealPackage UPK, TagContainer tag)
    {
        tag.alternateName = UPK.DeserializeString();  // store the alternate name for the struct somewhere. This does not get used! 
        LoopTagConstructor(UPK, out List<UProperty> elements);
        return new UStructProperty(tag, tag.alternateName, elements);
    }

    private UProperty CreateArrayProperty(UnrealPackage UPK, TagContainer tag)
    {
        if (tag.arrayInfo.arrayType is ArrayType.Static)
            return BuildArrayProperty(UPK, tag);

        return tag.arrayInfo.valueType switch
        {
            PropertyType.IntProperty => BuildArrayProperty(UPK, tag, UPK => UPK.DeserializeInt()),
            PropertyType.FloatProperty => BuildArrayProperty(UPK, tag, UPK => UPK.DeserializeFloat()),
            PropertyType.StrProperty or PropertyType.NameProperty => BuildArrayProperty(UPK, tag, UPK => UPK.DeserializeString()),
            PropertyType.StructProperty => BuildArrayProperty(UPK, tag, _ => ReadStructElement(UPK)),
            _ => throw new NotSupportedException($"Unsupported array type: {tag.arrayInfo.valueType}")
        };
    }

    private UArrayProperty<T> BuildArrayProperty<T>(UnrealPackage UPK, TagContainer tag, Func<UnrealPackage, T> reader) where T : notnull
    {
        var elements = new List<T>();

        for (int i = 0; i < tag.arrayEntryCount; i++)
            elements.Add(reader(UPK));

        return new UArrayProperty<T>(tag, elements);
    }

    private UArrayProperty<UProperty> BuildArrayProperty(UnrealPackage UPK, TagContainer tag)
    {
        var elements = new List<UProperty>();
        int loopCount = 0;
        while (true)
        {
            if (loopCount > MAX_STATIC_ARRAY_ELEMENTS)
                throw new Exception("Infinite loop detected while deserializing static array.");

            var nextname = UPK.PeekString();
            if (nextname != tag.name)
                break;

            elements.Add(ConstructTag(UPK, allowStaticArrayDetection: false));
            tag.arrayEntryCount++;
            loopCount++;
        }

        return new UArrayProperty<UProperty>(tag, elements);
    }

    private List<UProperty> ReadStructElement(UnrealPackage UPK)
    {
        LoopTagConstructor(UPK, out List<UProperty> elements);
        return elements;
    }

    private void LoopTagConstructor(UnrealPackage UPK, out List<UProperty> elements)
    {
        UProperty tag;
        elements = new List<UProperty>();

        while ((tag = ConstructTag(UPK)) != null)
            elements.Add(tag);
    }
}
