using System.Runtime.CompilerServices;
using System.Text;

public class UPropertyManager
{

    private string name = "";
    private string type = "";
    private int arrayIndex, size;

    internal protected JsonParser dataParser;
    private protected OptimizeViaPredefine var;
    internal UnrealArchive Ar;
    internal DeserializerState state;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseSaveData(string name, object value, string type, int arrayIndex) => 
        dataParser.ParseSaveData(name, value, type, arrayIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseSaveData(string name, object value, string type, int arrayIndex, string enumName, string enumValue) => 
        dataParser.ParseSaveData(name, value, type, arrayIndex, enumName, enumValue);


    public UPropertyManager(UnrealArchive unrealArchive)
    {
        Ar = unrealArchive;
        var = new OptimizeViaPredefine();
        dataParser = new JsonParser();
        state = new DeserializerState();
    }

    public class DeserializerState
    {
        internal protected bool loadingStruct = false;
        internal protected bool loadingStaticArray = false;
        internal protected string lastStaticArrayName = "";
    }

    public class OptimizeViaPredefine
    {
        internal protected int _int = 0;
        internal protected float _float = 0f;
        internal protected string _str = "";
        internal protected string _name = "";
        internal protected byte _byte = 0;
        internal protected bool _bool = false;
    }

    internal void DeserializeDataToJson()
    {
        dataParser.ProcessObj();
        DeserializeFile();
        dataParser.TerminateObj();
        File.WriteAllText(Globals.outputFile, Encoding.UTF8.GetString(dataParser.jsonStream.ToArray()));
    }

    private void DeserializeFile()
    {
        ConsoleHelper.DisplayColoredText("Deserializing File!", ConsoleHelper.ConsoleColorChoice.Yellow);

        Ar.ChangeStreamPosition(Globals.packageBegin);  // skip beginning data
        while (Ar.saveStream.Position < Ar.fileBytes.Length) DeserializeUProperty();  // deserialize file

        ConsoleHelper.DisplayColoredText("File Deserialized!", ConsoleHelper.ConsoleColorChoice.Green);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DeserializeUProperty()
    {
        Ar.Deserialize(ref name);
        if (name == "None")
        {
            if (state.loadingStruct) state.loadingStruct = false;
            return false;
        }
        Ar.Deserialize(ref type);
        Ar.Deserialize(ref size);
        Ar.Deserialize(ref arrayIndex);

        return ConstructPropertyValue();
    }

    internal bool ConstructPropertyValue()
    {
        object defaultParser;
        switch (type)
        {
            case "IntProperty":
                Ar.Deserialize(ref var._int);                
                defaultParser = var._int;
                break;
            case "FloatProperty":
                Ar.Deserialize(ref var._float);        
                defaultParser = var._float;
                break;        
            case "BoolProperty":
                Ar.Deserialize(ref var._bool);        
                defaultParser = var._bool;
                break;

            case "ByteProperty":
                string enumName = "";
                Ar.Deserialize(ref enumName);
                if (enumName == "None")
                {
                    // property pulled in None
                    Ar.Deserialize(ref var._byte);
                    defaultParser = var._byte;  
                    break;
                }
                //property pulled in enum
                Ar.Deserialize(ref var._str);
                ParseSaveData(name, 0, type, arrayIndex, enumName, var._str);
                return true;

            case "StrProperty":
            case "NameProperty":
                Ar.Deserialize(ref var._int);
                if (var._int == 0) // handles empty string case
                {
                    ParseSaveData(name, "", type, arrayIndex);
                    return true;
                }
                Ar.ChangeStreamPosition(-4);
                Ar.Deserialize(ref var._str);
                ParseSaveData(name, var._str, type, arrayIndex);
                return true;

            case "StructProperty":
                string structName = "";
                Ar.Deserialize(ref structName);
                state.loadingStruct = true;  // detected struct
                DeserializeUStruct(structName);
                return true;

            case "ArrayProperty":
                int arraySize = 0;
                Ar.Deserialize(ref arraySize);

                if (arraySize == 0)
                {
                    dataParser.WriteEmptyArray(name);
                    return true;
                }
                DeserializeUArray(name, arraySize);
                return true;

            default:
                ConsoleHelper.DisplayColoredText($"Unsupported property type: {type}", ConsoleHelper.ConsoleColorChoice.Red);
                return false;
        }
        ParseSaveData(name, defaultParser, type, arrayIndex);
        return true;
    }

    private bool CheckForStaticName()
    {
        return false;
    }

    private void DeserializeUArray(string arrayName, int arraySize)
    {
        if (Util.IsArrayDynamic(arrayName))
        {
            Util.ManageDynamicArrayDeserialization(Ar, arrayName, arraySize, dataParser);
        }
        else DeserializeArray(arraySize);
    }

    private void DeserializeUStruct(string structName)
    {
        dataParser.ProcessObj(name);
        ProcessEntry(structName);
        dataParser.TerminateObj();
    }

    private void DeserializeArray(int arraySize)
    {
        dataParser.ProcessArray(name); // Start the array with the name
        for (int i = 0; i < arraySize; i++) 
            ProcessEntry();
        dataParser.TerminateArray();
    }

    private void LoopUPropertyDeserialization()
    {
        while (DeserializeUProperty());
    }

    private void ProcessEntry()
    {
        dataParser.ProcessObj();
        LoopUPropertyDeserialization();
        dataParser.TerminateObj();
    }

    private void ProcessEntry(string entryName)
    {
        dataParser.ProcessObj(entryName);
        LoopUPropertyDeserialization();
        dataParser.TerminateObj();
    }

    public class UDynamicArrayManager<T> : UPropertyManager
    {
        private string arrayName { get; set;}
        private int arraySize { get; set;}

        // Constructor that matches the base class constructor
        public UDynamicArrayManager(UnrealArchive Ar, string arName, int arSize, JsonParser dparser) : base(Ar) // Call the base constructor
        {
            this.dataParser = dparser;
            arrayName = arName;
            arraySize = arSize;
            DeserializeDynamicArray();
        }

        private void DeserializeDynamicArray()
        {
            dataParser.ProcessArray(arrayName); // Start the array with the name, all pathways need this
            Deserialize<T>();
            dataParser.TerminateArray();
        }

        public void Deserialize<T>()
        {
            Type type = typeof(T);
            for (int i = 0; i < arraySize; i++)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int32:
                        Ar.Deserialize(ref var._int);
                        dataParser.WriteNumberValue(var._int);
                        break;

                    case TypeCode.Single:
                        Ar.Deserialize(ref var._float);
                        dataParser.WriteNumberValue(var._float);
                        break;

                    case TypeCode.String:
                        Ar.Deserialize(ref var._str);
                        dataParser.WriteStringValue(var._str);
                        break;

                    default:
                        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
                }
            }
        }
    }
    class UStaticPropertyManager<T>
    {

    }

}