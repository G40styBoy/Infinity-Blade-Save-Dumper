using SaveDumper.FArrayManager;
using SaveDumper.UnrealPackageManager;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace SaveDumper.FPropertyManager;

class FProperties
{
    private List<ArrayMetadata> gameArrayInfo = new();

    #region Core UProperty Info
    /// <summary>
    /// Used to package and pass data more neatly once its been stored.
    /// </summary>
    public record struct TagContainer
    {
        public string Name;
        public string Type;
        public int Size;
        public int ArrayIndex;
        public int ArrayEntryCount;
    }

    public abstract class UProperty
    {
        public string Name;
        public string Type;
        public int Size;
        public int ArrayIndex;

        public UProperty(TagContainer tag)
        {
            Name = tag.Name;
            Type = tag.Type;
            ArrayIndex = tag.ArrayIndex;
            Size = tag.Size;
        }
    }
    #endregion

    #region UProperties
    public class UIntProperty : UProperty
    {
        public int Value;
        public UIntProperty(UnrealPackage UPK, TagContainer tag)
            : base(tag) => Value = UPK.DeserializeInt();

        public int GetSpecificFieldInfo() => Value;
    }

    public class UFloatProperty : UProperty
    {
        public float Value;
        public UFloatProperty(UnrealPackage UPK, TagContainer tag)
            : base(tag) => Value = UPK.DeserializeFloat();

        public float GetSpecificFieldInfo() => Value;
    }

    public class UBoolProperty : UProperty
    {
        public bool Value;
        public UBoolProperty(UnrealPackage UPK, TagContainer tag)
            : base(tag) => Value = UPK.DeserializeBool();

        public bool GetSpecificFieldInfo() => Value;
    }

    public class UStringProperty : UProperty
    {
        public string Value = string.Empty;
        public UStringProperty(UnrealPackage UPK, TagContainer tag)
            : base(tag) => Value = UPK.DeserializeString();

        public string GetSpecificFieldInfo() => Value;
    }

    public abstract class UByteProperty : UProperty
    {
        public UByteProperty(TagContainer tag)
            : base(tag) { }

        public static UByteProperty Create(UnrealPackage UPK, TagContainer tag)
        {
            var identifier = UPK.ReadString();
            return tag.Size switch
            {
                1 => new USimpleByteProperty(UPK, tag),
                > 1 => new UEnumByteProperty(UPK, tag, identifier),
                _ => throw new NotSupportedException($"Unsupported byte property size: {tag.Size}")
            };
        }
    }
    public class USimpleByteProperty : UByteProperty
    {
        public byte Value;
        public USimpleByteProperty(UnrealPackage UPK, TagContainer tag)
            : base(tag) => Value = UPK.DeserializeByte();

        public byte GetSpecificFieldInfo() => Value;
    }

    public class UEnumByteProperty : UByteProperty
    {
        public string enumName = string.Empty;
        public string enumValue = string.Empty;

        public UEnumByteProperty(UnrealPackage UPK, TagContainer tag, string enumName)
            : base(tag)
        {
            this.enumName = enumName;
            enumValue = UPK.ReadString();
        }

        public KeyValuePair<string, string> GetSpecificFieldInfo() => new KeyValuePair<string, string>(enumName, enumValue);
    }

    public class UStructProperty : UProperty
    {
        public List<UProperty> elements;
        public string StructName;

        public UStructProperty(TagContainer tag, string structName, List<UProperty> elements)
            : base(tag)
        {
            StructName = structName;
            this.elements = elements;
        }

        public List<UProperty> GetSpecificFieldInfo() => elements;
    }

    public class UArrayProperty<T> : UProperty
    {
        public int ArrayEntryCount;
        public List<T> Elements;

        public UArrayProperty(TagContainer tag, List<T> elements)
            : base(tag)
        {
            ArrayEntryCount = tag.ArrayEntryCount;
            Elements = elements;
        }

        public List<T> GetSpecificFieldInfo() => Elements;
    }
    #endregion

    #region Main Deserialization

    internal List<UProperty> Deserialize(UnrealPackage UPK)
    {
        List<UProperty> _genericProperties = new();
        gameArrayInfo = UPK.RequestArrayInfo();
        while (!UPK.IsEndFile())
        {
            UProperty tag = ConstructTag(UPK);
            if (tag is not null)
                _genericProperties.Add(tag);
        }
        return _genericProperties;
    }

    private UProperty ConstructTag(UnrealPackage UPK, bool allowStaticArrayDetection = true)
    {
        var tag = new TagContainer();
        UProperty uProperty;

        tag.Name = UPK.ReadString();
        if (tag.Name is FType.NONE)
            return null!;

        var arrayMeta = GetCurrentArray(tag.Name);
        if (allowStaticArrayDetection && arrayMeta?.ArrayType is ArrayType.Static)
        {
            UPK.RevertStreamPosition(tag.Name);
            tag.Type = FType.ARRAY_PROPERTY;
            tag.Size = -1;
            tag.ArrayIndex = -1;
            tag.ArrayEntryCount = -1;
        }
        else
        {
            tag.Type = UPK.ReadString();
            tag.Size = UPK.DeserializeInt();
            tag.ArrayIndex = UPK.DeserializeInt();
            if (tag.Type is FType.ARRAY_PROPERTY)
                tag.ArrayEntryCount = UPK.DeserializeInt();
        }

        uProperty = ConstructUProperty(UPK, tag);
        return uProperty;
    }

    private UProperty ConstructUProperty(UnrealPackage UPK, TagContainer tag)
    {
        return tag.Type switch
        {
            FType.INT_PROPERTY => new UIntProperty(UPK, tag),
            FType.FLOAT_PROPERTY => new UFloatProperty(UPK, tag),
            FType.BOOL_PROPERTY => new UBoolProperty(UPK, tag),
            FType.BYTE_PROPERTY => UByteProperty.Create(UPK, tag),
            FType.STR_PROPERTY or FType.NAME_PROPERTY => new UStringProperty(UPK, tag),
            FType.STRUCT_PROPERTY => CreateStructProperty(UPK, tag),
            FType.ARRAY_PROPERTY => CreateArrayProperty(UPK, tag),
            _ => throw new NotSupportedException($"Unsupported property type: {tag.Type}")
        };
    }
    #endregion

    #region Utility Methods
    private UStructProperty CreateStructProperty(UnrealPackage UPK, TagContainer tag)
    {
        string structName = UPK.ReadString();
        List<UProperty> elements;
        LoopTagConstructor(UPK, out elements);
        return new UStructProperty(tag, structName, elements);
    }

    private UProperty CreateArrayProperty(UnrealPackage UPK, TagContainer tag)
    {
        var arrayInfo = GetCurrentArray(tag.Name);
        var elementType = arrayInfo!.ValueType;

        if (arrayInfo.ArrayType is ArrayType.Static)
            return BuildArrayProperty(UPK, tag);

        return elementType switch
        {
            ValueType.IntProperty => BuildArrayProperty<int>(UPK, tag, UPK => UPK.DeserializeInt()),
            ValueType.FloatProperty => BuildArrayProperty<float>(UPK, tag, UPK => UPK.DeserializeFloat()),
            ValueType.StrProperty or ValueType.NameProperty => BuildArrayProperty<string>(UPK, tag, UPK => UPK.DeserializeString()),
            ValueType.StructProperty => BuildArrayProperty<List<UProperty>>(UPK, tag, _ => ReadStructElement(UPK)),
            _ => throw new NotSupportedException($"Unsupported array type: {elementType}")
        };
    }

    private UArrayProperty<T> BuildArrayProperty<T>(UnrealPackage UPK, TagContainer tag, Func<UnrealPackage, T> reader)
    {
        var elements = new List<T>();

        for (int i = 0; i < tag.ArrayEntryCount; i++)
        {
            elements.Add(reader(UPK));
        }

        return new UArrayProperty<T>(tag, elements);
    }

    private UArrayProperty<UProperty> BuildArrayProperty(UnrealPackage UPK, TagContainer tag)
    {
        var elements = new List<UProperty>();

        while (true)
        {
            var nextName = UPK.PeekString();
            if (nextName != tag.Name)
                break;

            elements.Add(ConstructTag(UPK, allowStaticArrayDetection: false));
            tag.ArrayEntryCount++;
        }

        return new UArrayProperty<UProperty>(tag, elements);
    }

    private List<UProperty> ReadStructElement(UnrealPackage UPK)
    {
        List<UProperty> elements;
        LoopTagConstructor(UPK, out elements);
        return elements;
    }

    private ArrayMetadata? GetCurrentArray(string name)
    {
        if (!Enum.IsDefined(typeof(ArrayName), name))
            return null!;

        var arrayName = (ArrayName)Enum.Parse(typeof(ArrayName), name);
        var info = gameArrayInfo.FirstOrDefault(array => array.ArrayName == arrayName);

        if (info == default)
            return null;

        return info;
    }

    private void LoopTagConstructor(UnrealPackage UPK, out List<UProperty> elements)
    {
        UProperty tag;
        elements = new List<UProperty>();

        while ((tag = ConstructTag(UPK)) != null)
        {
            elements.Add(tag);
        }
    }
    #endregion
}

