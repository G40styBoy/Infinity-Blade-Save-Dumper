using SaveDumper.FArrayManager;
using SaveDumper.UnrealPackageManager;

namespace SaveDumper.FPropertyManager;

class FProperties
{
    private readonly Dictionary<string, FProperty> _properties = new();
    private List<ArrayMetadata> gameArrayInfo = new(); 

    /// <summary>
    /// Adds a property to the collection.
    /// </summary>
    private void AddTag(FProperty tag)
    {
        if (tag == null){
            Console.WriteLine($"Property is null, skipping.");
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
    /// <returns></returns>
    private ArrayMetadata? GetCurrentArray(string name)
    {
        var arrayName = (ArrayName)Enum.Parse(typeof(ArrayName), name);
        var info = gameArrayInfo.FirstOrDefault(array => array.ArrayName == arrayName);

        if (info == default)
            return null;

        return info;
    }

    private bool IsTagEmpty(FProperty tag) => tag is null;

    #pragma warning disable CS8618  // this warning is a pain in the ass, and i dont feel like dirtying up the code to fix it
    public class FProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public int ArrayIndex { get; set; }
        public object Value { get; set; } // For primitive types, arrays, or structs
    }

    public class StructTag
    {
        public List<FProperty> StructProperties { get; set; }
        public string StructType { get; set; }

        public StructTag(UnrealPackage UPK, bool isTagEmpty)
        {
            StructProperties = new List<FProperty>(); // Ensures initialization
            StructType = isTagEmpty ? string.Empty : UPK.ReadNullTerminatedString();
        }
    }

    public class ArrayTag
    {
        public List<object> ArrayEntries { get; set; }
        public int ArrayEntryCount { get; set; }
        public ArrayMetadata ArrayInfo { get; set; }

        // null reference fix in the future
        public ArrayTag(UnrealPackage UPK, ArrayMetadata? arrayInfo)
        {
            ArrayEntries = new List<object>();
            ArrayEntryCount = UPK.DeserializeInt();
            ArrayInfo = arrayInfo!;
        }
    }
    #pragma warning restore

    private object DeserializePropertyValue(UnrealPackage UPK, FProperty tag)
    {
        return tag.Type switch
        {
            FType.INT_PROPERTY => UPK.DeserializeInt(),
            FType.FLOAT_PROPERTY => UPK.DeserializeFloat(),
            FType.BOOL_PROPERTY => UPK.DeserializeBool(),
            FType.BYTE_PROPERTY => UPK.DeserializeByteProperty(),
            FType.STR_PROPERTY => UPK.DeserializeString(),
            FType.NAME_PROPERTY => UPK.DeserializeString(),  // TODO: make a seperate functions later on down the line that actually finds the FName
            FType.STRUCT_PROPERTY => DeserializeStruct(UPK, tag),
            FType.ARRAY_PROPERTY => DeserializeArray(UPK, tag), 
            _ => throw new NotSupportedException($"Unsupported property type: {tag.Type}")
        };
    }

    private FProperty ReadTagData(UnrealPackage UPK)
    {
        var tag = new FProperty();

        tag.Name = UPK.ReadNullTerminatedString();
        if (tag.Name == FType.NONE) 
            return null!;  

        tag.Type = UPK.ReadNullTerminatedString();

        // Read property size and array index
        tag.Size = UPK.DeserializeInt();
        tag.ArrayIndex = UPK.DeserializeInt();

        return tag;
    }

    private StructTag DeserializeStruct(UnrealPackage UPK, FProperty tag)
    {   
        var structTag = new StructTag(UPK, IsTagEmpty(tag));

        while (true)
        {
            var propertytag = ReadTagData(UPK);

            if (propertytag == null)
                break;

            propertytag.Value = DeserializePropertyValue(UPK, propertytag);

            // Add the property to the struct
            structTag.StructProperties.Add(propertytag);
        }
        return structTag;
    }


    // TODO: Deserialize Different types of arrays
    // this is fucked when it comes to nested shtuff
    private ArrayTag DeserializeArray(UnrealPackage UPK, FProperty tag)  
    {
        var arrayTag = new ArrayTag(UPK, GetCurrentArray(tag.Name));
        var arrayInfo = arrayTag.ArrayInfo;

        if (arrayTag.ArrayEntryCount == 0)
            return null!;   

        if (arrayInfo == null)
        {
            Console.WriteLine($"ERROR: Array info for {tag.Name} could not be found.");
            return null!;
        }

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
                case ValueType.StrProperty: tag.ArrayEntries.Add(UPK.ReadNullTerminatedString());
                    break;
                case ValueType.StructProperty: tag.ArrayEntries.Add(DeserializeStruct(UPK, null!));
                    break;
                default:
                    Console.WriteLine($"ERROR: Array type {arrayData.ValueType} is not supported.");
                    break;
            }
        }
    }

    // TODO: Add support for static arrays
    private void StaticHandler(UnrealPackage UPK, ArrayMetadata arrayData, ArrayTag tag)
    {

    }
    
    internal Dictionary<string, FProperty> Deserialize(UnrealPackage UPK)
    {
        gameArrayInfo = UPK.RequestArrayInfo();
        FProperty tag;
        while (UPK.GetStreamPosition() < UPK.GetStreamLength())  // in the future, we could just check for the "None" property to be pulled
        {
            tag = ReadTagData(UPK);
            if (tag == null) // Account for "None" property
                break;       

            tag.Value = DeserializePropertyValue(UPK, tag);

            // Add(tag);
            AddTag(tag);
        }

        return _properties;
    }
}
