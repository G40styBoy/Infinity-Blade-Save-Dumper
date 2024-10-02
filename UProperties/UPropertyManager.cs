using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public class UPropertyManager
{

    // created for the purpose of optimization 
    private class PredefinedVar
    {
        internal int _int;
        internal float _float;
        internal byte _byte;
        internal bool _bool;
        internal string _str = "";
        internal string _name = "";
    }

    private string name = "";
    private string type = "";
    private int arrayIndex, size;

    private PredefinedVar pVar;
    private JsonParser dataParser;
    internal UnrealArchive Ar;
    private UPropertyManager uPropertyManager;
    internal DeserializerState state;

    private void ParseSaveData(string name, object value, string type, int arrayIndex, [Optional] string enumName, [Optional] string enumValue) => dataParser.ParseSaveData(name, value, type, arrayIndex, enumName, enumValue);

    public UPropertyManager(UnrealArchive unrealArchive)
    {
        Ar = unrealArchive;
        dataParser = new JsonParser();
        pVar = new PredefinedVar();
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
        Ar.Deserialize(ref name);

        if(Util.IsUPropertyStatic(name))
        {
            Util.ManageStaticArrayDeserialization(Ar, uPropertyManager, name, dataParser);
        }
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
        switch (type)
        {
            case "IntProperty":
                Ar.Deserialize(ref pVar._int);
                ParseSaveData(name, pVar._int, type, arrayIndex);
                return true;

            case "FloatProperty":
                Ar.Deserialize(ref pVar._float);
                ParseSaveData(name, pVar._float, type, arrayIndex);
                return true;

            case "ByteProperty":
                string enumName = "";
                Ar.Deserialize(ref enumName);
                if (enumName == "None")
                {
                    // property pulled in None
                    Ar.Deserialize(ref pVar._byte);
                    ParseSaveData(name, pVar._byte, type, arrayIndex);
                }
                else
                {
                    //property pulled in enum
                    string enumValue = "";
                    Ar.Deserialize(ref enumValue);
                    ParseSaveData(name, pVar._byte, type, arrayIndex, enumName, enumValue);
                }
                return true;

            case "BoolProperty":
                Ar.Deserialize(ref pVar._bool);
                ParseSaveData(name, pVar._bool, type, arrayIndex);
                return true;

            case "StrProperty":
                if (Ar.Deserialize(Globals.InfoGrab) == 0) // handles empty string case
                {
                    ParseSaveData(name, "", type, arrayIndex);
                    return true;
                }
                Ar.ChangeStreamPosition(-Globals.InfoGrab);
                Ar.Deserialize(ref pVar._str);
                ParseSaveData(name, pVar._str, type, arrayIndex);
                return true;

            // TODO: we need to handle these differently.
            // we could probably do something like pulling all the names from the ib ini file, locating the name, then creating an instance of a name to match
            case "NameProperty":
                Ar.Deserialize(ref pVar._name);
                ParseSaveData(name, pVar._name, type, arrayIndex);
                return true;

            case "StructProperty":
                state.loadingStruct = true;  // detected struct
                Ar.Deserialize(ref pVar._str);
                DeserializeUStruct(pVar._str);
                return true;

            case "ArrayProperty":
                int arraySize = 0;
                Ar.Deserialize(ref arraySize);  // we need the array size to know how much to read

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
}


