using System.Runtime.InteropServices;
using System.Text;

public class UPropertyManager
{

    private string name = "";
    private string type = "";
    private int arrayIndex, size;

    // private PredefinedVar pVar;
    protected JsonParser dataParser;
    internal UnrealArchive Ar;
    private UPropertyManager uPropertyManager;
    internal DeserializerState state;

    private void ParseSaveData(string name, object value, string type, int arrayIndex, [Optional] string enumName, [Optional] string enumValue) => dataParser.ParseSaveData(name, value, type, arrayIndex, enumName, enumValue);

    public UPropertyManager(UnrealArchive unrealArchive)
    {
        Ar = unrealArchive;
        dataParser = new JsonParser();
        state = new DeserializerState();
        uPropertyManager = this;
    }

    public class DeserializerState
    {
        internal bool loadingStruct = false;
        internal bool loadingStaticArray = false;
        internal string lastStaticArrayName = "";
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

    public bool DeserializeUProperty()
    {
        name = Ar.Deserialize<string>();

        if (name == "None")
        {
            if (state.loadingStruct) state.loadingStruct = false;
            return false;
        }
        type = Ar.Deserialize<string>();
        size = Ar.Deserialize<int>();
        arrayIndex = Ar.Deserialize<int>();

        return ConstructPropertyValue();
    }

    internal bool ConstructPropertyValue()
    {
        object defaultParse;
        switch (type)
        {
            // easy cases
            case "IntProperty":
                defaultParse = Ar.Deserialize<int>();             
                break;
            case "FloatProperty":
                defaultParse = Ar.Deserialize<float>();             
                break;
            case "BoolProperty":
                defaultParse = Ar.Deserialize<bool>();           
                break;

            case "ByteProperty":
                string enumName = Ar.Deserialize<string>();
                if (enumName == "None")
                {
                    // property pulled in None
                    ParseSaveData(name, Ar.Deserialize<byte>(), type, arrayIndex);
                }
                else
                {
                    //property pulled in enum
                    ParseSaveData(name, 0, type, arrayIndex, enumName, Ar.Deserialize<string>());
                }
                return true;

            case "StrProperty":
            case "NameProperty":
                if (Ar.Deserialize<int>() == 0) // handles empty string case
                {
                    ParseSaveData(name, "", type, arrayIndex);
                    return true;
                }
                Ar.ChangeStreamPosition(-Globals.InfoGrab);
                ParseSaveData(name, Ar.Deserialize<string>(), type, arrayIndex);
                return true;

            case "StructProperty":
                state.loadingStruct = true;  // detected struct
                DeserializeUStruct(Ar.Deserialize<string>());
                return true;

            case "ArrayProperty":
                int arraySize = Ar.Deserialize<int>();

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
        ParseSaveData(name, defaultParse, type, arrayIndex);   
        return true;
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
        for (int i = 0; i < arraySize; i++) ProcessEntry();
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
        private Type constructType { get; set;}
        private int arraySize { get; set;}
        private string arrayName { get; set;}

        // Constructor that matches the base class constructor
        public UDynamicArrayManager(UnrealArchive Ar, string arName, int arSize, JsonParser dparser) : base(Ar) // Call the base constructor
        {
            this.dataParser = dparser;
            arrayName = arName;
            arraySize = arSize;
            constructType = typeof(T); // Set the type for later use
            DeserializeDynamicArray();
        }

        private void DeserializeDynamicArray()
        {
            dataParser.ProcessArray(arrayName); // Start the array with the name, all pathways need this
            if (constructType == typeof(string))
                DeserializeString();
            else
                DeserializeNumber();
            dataParser.TerminateArray();
        }

        private void DeserializeString()
        {
            for (int i = 0; i < arraySize; i++)
                dataParser.WriteStringValue((dynamic)Ar.Deserialize<T>()!);
        }

        private void DeserializeNumber()
        {
            for (int i = 0; i < arraySize; i++)
                dataParser.WriteNumberValue((dynamic)Ar.Deserialize<T>()!);
        }
    }

    // class UStaticPropertyManager<T>
    // {
    //     public UStaticPropertyManager(UnrealArchive Ar, UPropertyManager uPropertyManager, string arrayName, JsonParser dParse)
    //     {
    //     }
    // }

}