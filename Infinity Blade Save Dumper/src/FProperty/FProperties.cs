using SaveDumper.FArrayManager;
using SaveDumper.UnrealPackageManager;
using System.Runtime.CompilerServices;


namespace SaveDumper.FPropertyManager;

class FProperties
{
    /// <summary>
    /// this is what we are storing all of our data in.
    /// </summary>
    private readonly Dictionary<string, FProperty> _properties = new();  

    /// <summary>
    /// This is the array metadata for the current game version.
    /// </summary>
    private List<ArrayMetadata> gameArrayInfo = new(); 

        /// <summary>
    /// metadata for a Deserialized property.
    /// </summary>
    public class FProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public int ArrayIndex { get; set; }
        public object Value { get; set; } 
    
        public FProperty()
        {
            Name = string.Empty;
            Type = string.Empty;
            Size = 0;
            ArrayIndex = 0;
            Value = null!;
        }
    }

    /// <summary>
    /// metadata for a Deserialized struct.
    /// </summary>
    public class StructTag
    {
        public List<FProperty> StructProperties { get; set; }
        public string StructType { get; set; }

        public StructTag(UnrealPackage UPK, bool isTagEmpty)
        {
            StructProperties = new List<FProperty>(); 
            StructType = isTagEmpty ? string.Empty : UPK.ReadString();
        }
    }

    /// <summary>
    /// metadata for a Deserialized array.
    /// </summary>
    public class ArrayTag
    {
        public List<object> ArrayEntries { get; set; }
        public int ArrayEntryCount { get; set; }
        public ArrayMetadata ArrayInfo { get; set; }

        public ArrayTag(UnrealPackage UPK, ArrayMetadata arrayInfo)
        {
            ArrayEntries = new List<object>();
            ArrayEntryCount = UPK.DeserializeInt();
            ArrayInfo = arrayInfo;
        }
        public ArrayTag(ArrayMetadata arrayInfo)
        {
            ArrayEntries = new List<object>();
            ArrayEntryCount = 0;
            ArrayInfo = arrayInfo;
        }
    }

    /// <returns>whether or not a tag is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsTagEmpty(FProperty tag) => tag is null || tag.Name == FType.NONE;

    /// <summary>
    /// Adds a FProperty to the _properties collection.
    /// </summary>
    private void AddTag(FProperty tag, bool isEnd)
    {
        if (IsTagEmpty(tag) && isEnd)
            return;
        if (IsTagEmpty(tag))
        {
            Console.WriteLine("ERROR: Tag is null.");
            return;
        }
        
        if (_properties.ContainsKey(tag.Name))
            _properties.Add(tag.Name + "_" + tag.ArrayIndex, tag);
        else
            _properties.Add(tag.Name, tag);    
    }

    /// <summary>
    /// Retrieves the array tag for the specified array name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>data for the array based on the ArrayTag name.</returns>
    private ArrayMetadata? GetCurrentArray(string name)
    {
        var arrayName = (ArrayName)Enum.Parse(typeof(ArrayName), name);
        var info = gameArrayInfo.FirstOrDefault(array => array.ArrayName == arrayName);

        if (info == default)
            return null;

        return info;
    }

    /// <summary>
    /// Deserializes a FProperty's value depending on its type.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="tag"></param>
    /// <returns>the value of the FProperty.</returns>
    /// <exception cref="NotSupportedException"></exception>
    private object DeserializePropertyTag(UnrealPackage UPK, FProperty tag)
    {
        return tag.Type switch
        {
            FType.INT_PROPERTY => UPK.DeserializeInt(),
            FType.FLOAT_PROPERTY => UPK.DeserializeFloat(),
            FType.BOOL_PROPERTY => UPK.DeserializeBool(),
            FType.BYTE_PROPERTY => UPK.DeserializeByteProperty(),
            FType.STR_PROPERTY => UPK.DeserializeString(),
            FType.NAME_PROPERTY => UPK.DeserializeString(),  // TODO: make a seperate functions later on down the line that actually finds the FName
            FType.STRUCT_PROPERTY => DeserializeStructTag(UPK, tag),
            FType.ARRAY_PROPERTY => DeserializeArrayTag(UPK, tag), 
            _ => throw new NotSupportedException($"Unsupported property type: {tag.Type}")
        };
    }

    /// <summary>
    /// Deserializes the main metadata for a tag.
    /// </summary>
    /// <param name="UPK"></param>
    /// <returns>data to be parsed into a new FProperty instance.</returns>
    private FProperty ReadTagData(UnrealPackage UPK)
    {
        var tag = new FProperty();

        tag.Name = UPK.ReadString();
        if (IsTagEmpty(tag)) 
            return null!;  
            
        tag.Type = UPK.ReadString();
        tag.Size = UPK.DeserializeInt();
        tag.ArrayIndex = UPK.DeserializeInt();

        return tag;
    }

    /// <summary>
    /// deserializes infinite tags and adds them to a generic collection until a criteria is met.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="addToCollection"></param>
    /// <returns>if it was successful</returns>
    private bool ProcessNextTag(UnrealPackage UPK, Action<FProperty> addToCollection)
    {
        var propertytag = ReadTagData(UPK);
        if (IsTagEmpty(propertytag))
            return false;

        propertytag.Value = DeserializePropertyTag(UPK, propertytag);
        addToCollection(propertytag); // Use the delegate to add
        return true;
    }

    /// <summary>
    /// Deserializes the main metadata for a struct.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="tag"></param>
    /// <returns>data to be parsed into a FProperty's value.</returns>
    private StructTag DeserializeStructTag(UnrealPackage UPK, FProperty tag)
    {   
        var structTag = new StructTag(UPK, IsTagEmpty(tag));

        while (ProcessNextTag(UPK, addToCollection => structTag.StructProperties.Add(addToCollection))){}

        return structTag;
    }

    /// <summary>
    /// Deserializes the main metadata for an array.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="tag"></param>
    /// <returns>data to be parsed into a FProperty's value.</returns>
    private ArrayTag DeserializeArrayTag(UnrealPackage UPK, FProperty tag)  
    {
        var arrayTag = new ArrayTag(UPK, GetCurrentArray(tag.Name) ?? throw new Exception($"ERROR: Array info for {tag.Name} could not be found."));        
        var arrayInfo = arrayTag.ArrayInfo;
        if (arrayInfo == null)
        {
            Console.WriteLine($"ERROR: Array info for {tag.Name} could not be found.");
            return null!;
        }
        if (arrayTag.ArrayEntryCount == 0)
            return null!;   

        switch (arrayInfo.ArrayType)
        {
            case ArrayType.Static:
            case ArrayType.Dynamic: DynamicHandler(UPK, arrayInfo, arrayTag);
                break;
            default:
                Console.WriteLine($"ERROR: Array type {arrayInfo.ArrayType} is not supported.");
                break;
        }
        return arrayTag;
    }

    /// <summary>
    /// Deserializes a dynamic array depending on its type.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="arrayData"></param>
    /// <param name="tag"></param>
    private void DynamicHandler(UnrealPackage UPK, ArrayMetadata arrayData, ArrayTag tag)
    {
        for (var i = 0; i < tag.ArrayEntryCount; i++)
        {
            switch (arrayData.ValueType)
            {
                case ValueType.IntProperty: tag.ArrayEntries.Add(UPK.DeserializeInt());
                    break;
                case ValueType.FloatProperty: tag.ArrayEntries.Add(UPK.DeserializeFloat());
                    break;
                case ValueType.NameProperty:
                case ValueType.StrProperty: tag.ArrayEntries.Add(UPK.DeserializeString());
                    break;
                case ValueType.StructProperty: tag.ArrayEntries.Add(DeserializeStructTag(UPK, null!));
                    break;
                default:
                    Console.WriteLine($"ERROR: Array type {arrayData.ValueType} is not supported.");
                    break;
            }
        }
    }

    /// <summary>
    /// Deserializes static arrays, completely transforming the tag passed into the function into an array.
    /// </summary>
    /// <param name="UPK"></param>
    /// <param name="tag"></param>
    /// <returns>transformed tag.</returns>
    private FProperty StaticHandler(UnrealPackage UPK, ref FProperty tag)
    {
        var arrayTag = new ArrayTag(GetCurrentArray(tag.Name) ?? throw new Exception($"ERROR: Array info for {tag.Name} could not be found."));  
        arrayTag.ArrayEntries.Add(tag); // dont forget to add the first entry

        tag.Type = FType.ARRAY_PROPERTY;
        tag.ArrayIndex = 0;
        tag.Value = arrayTag;

        while (UPK.PeekString() == tag.Name) {
            ProcessNextTag(UPK, addToCollection => arrayTag.ArrayEntries.Add(addToCollection));
        }

        tag.Size = arrayTag.ArrayEntries.Count;
        arrayTag.ArrayEntryCount = arrayTag.ArrayEntries.Count;
        return tag;
    }
    
    /// <summary>
    /// Contructs a complete tag.
    /// </summary>
    /// <param name="UPK"></param>
    /// <returns>constructed tag.</returns>
    private FProperty ConstructTag(UnrealPackage UPK)
    {
        FProperty tag = ReadTagData(UPK);

        // Account for "None" property
        if (IsTagEmpty(tag)) 
            return null!;

        tag.Value = DeserializePropertyTag(UPK, tag);

        // since we know what the full property type is at this point, it will be easier for us
        // to properly deserialize the static array
        if (tag.Type is not FType.ARRAY_PROPERTY && Enum.IsDefined(typeof(ArrayName), tag.Name))
            StaticHandler(UPK, ref tag);
        
        return tag;
    }

    internal Dictionary<string, FProperty> Deserialize(UnrealPackage UPK)
    {
        gameArrayInfo = UPK.RequestArrayInfo();
        while (!UPK.IsEndFile())  // in the future, we could just check for the "None" property to be pulled
        {
            var tag = ConstructTag(UPK);
            AddTag(tag, UPK.IsEndFile());
        }

        return _properties;
    }
}
