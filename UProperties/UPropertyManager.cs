using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable IDE0051
#pragma warning disable CS0169

readonly struct DynamicArrayTypes
{
    private readonly string EquippedItemNames;
    private readonly string CurrentKeyItemList;
    private readonly string EquippedItems;
    private readonly string UsedKeyItemList;
    private readonly string PurchasedPerks;
    private readonly int GameFlagList;
    private readonly string WorldItemOrderList;
    private readonly string TreasureChestOpened;
    private readonly string BossesGeneratedThisBloodline;
    private readonly string PotentialBossElementalAttacks;
    private readonly string CurrentBattleChallengeList;
    private readonly string LoggedAnalyticsAchievements;
    private readonly string McpAuthorizedServices;
    private readonly float BossElementalRandList;
    public enum DynamicArrayList
    {
        EquippedItemNames,
        EquippedItems,
        CurrentKeyItemList,
        UsedKeyItemList,
        PurchasedPerks,
        GameFlagList, 
        WorldItemOrderList,
        TreasureChestOpened,
        BossesGeneratedThisBloodline,
        PotentialBossElementalAttacks,
        CurrentBattleChallengeList,
        LoggedAnalyticsAchievements,
        McpAuthorizedServices,
        BossElementalRandList,
    }
}

#pragma warning restore CS0169
#pragma warning restore IDE0051


class UPropertyManager
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
    private readonly JsonVariableFinder jsonFinder; 

    private bool loadingStruct = false;
    private JsonParser dataParser;
    internal UnrealArchive Ar;

    private void ParseSaveData(string name, object value, string type, [Optional] string enumName, [Optional] string enumValue) => dataParser.ParseSaveData(name, value, type, enumName, enumValue);

    public UPropertyManager(UnrealArchive unrealArchive)
    {   
        Ar = unrealArchive;
        dataParser = new JsonParser();
        pVar = new PredefinedVar();
        jsonFinder = new JsonVariableFinder();
    }

    internal void DeserializeDataToJson()
    {       
        dataParser.ProcessObj();
        DeserializeFile();
        dataParser.TerminateObj();
        File.WriteAllText(Globals.outputFile,Encoding.UTF8.GetString(dataParser.jsonStream.ToArray()));

        jsonFinder.FindAndPrintVariables(dataParser.jsonStream, "strCharacterName");
        jsonFinder.FindAndPrintVariables(dataParser.jsonStream, "strSaveTag");
        jsonFinder.FindAndPrintVariables(dataParser.jsonStream, "SaveCreatedVersion");
        jsonFinder.FindAndPrintVariables(dataParser.jsonStream, "CurrentEngineVersion");

    }

    private void DeserializeFile()
    {
        ConsoleHelper.DisplayColoredText("Deserializing File!", ConsoleHelper.ConsoleColorChoice.Yellow);

        Ar.ChangeStreamPosition(Globals.packageBegin);  // skip beginning data
        while(Ar.saveStream.Position < Ar.fileBytes.Length) DeserializeUProperty();  // deserialize file

        ConsoleHelper.DisplayColoredText("File Deserialized!", ConsoleHelper.ConsoleColorChoice.Green);
    }

    public bool DeserializeUProperty()
    {
        Ar.Deserialize(ref name);
        if (name == "None")
        {
            if (loadingStruct) loadingStruct = false;
            return false;
        }
        Ar.Deserialize(ref type); 
        Ar.Deserialize(ref size);
        Ar.Deserialize(ref arrayIndex);

        return ConstructPropertyValue(type);
    }

    private bool ConstructPropertyValue(string type)
    {
        switch (type)
        {
            case "IntProperty":
                Ar.Deserialize(ref pVar._int);
                ParseSaveData(name, pVar._int, type); 
                return true;

            case "FloatProperty":
                Ar.Deserialize(ref pVar._float);
                ParseSaveData(name, pVar._float, type); 
                return true;

            case "ByteProperty":
                string enumName = "";
                Ar.Deserialize(ref enumName);
                if (enumName == "None")
                {
                    // property pulled in None
                    Ar.Deserialize(ref pVar._byte);
                    ParseSaveData(name, pVar._byte, type); 
                }
                else
                {
                    //property pulled in enum
                    string enumValue = "";
                    Ar.Deserialize(ref enumValue);
                    ParseSaveData(name, pVar._byte, type, enumName, enumValue); 
                }
                return true;

            case "BoolProperty":
                Ar.Deserialize(ref pVar._bool);
                ParseSaveData(name, pVar._bool, type);          
                return true;

            case "StrProperty":
                if (Ar.Deserialize(Globals.InfoGrab) == 0) // handles empty string case
                {
                    ParseSaveData(name, "", type);
                    return true;
                }
                Ar.ChangeStreamPosition(-Globals.InfoGrab);
                Ar.Deserialize(ref pVar._str);
                ParseSaveData(name, pVar._str, type);            
                return true;

            // TODO: we need to handle these differently.
            // we could probably do something like pulling all the names from the ib ini file, locating the name, then creating an instance of a name to match
            case "NameProperty":
                Ar.Deserialize(ref pVar._name);
                ParseSaveData(name, pVar._name, type);    
                return true;

            case "StructProperty":
                loadingStruct = true;  // detected struct
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
        if(IsArrayDynamic(arrayName))  
        {  
            Type arrayReflectionType = GetArrayFieldType(arrayName, false); 
            Activator.CreateInstance(arrayReflectionType, Ar, arrayName, arraySize, dataParser);  // create instance of class with generic type
        }
        else DeserializeStaticArray(arraySize);
    }

    public static bool IsArrayDynamic(string arrayName)  // we need to use this in other places too
    {
        if(Enum.TryParse(arrayName, out DynamicArrayTypes.DynamicArrayList dResult) && Enum.IsDefined(typeof(DynamicArrayTypes.DynamicArrayList), dResult)) return true;
        else return false;
    }

    public static Type GetArrayFieldType(string arrayName, bool serializedReturn)
    {
        FieldInfo fieldInfo = typeof(DynamicArrayTypes).GetField(arrayName, BindingFlags.NonPublic | BindingFlags.Instance)!;      // get field info of struct, then get the specific field we want info on      
        if (fieldInfo == null) throw new Exception($"Field {arrayName} not found!"); // TODO: handle error better
        Type fieldType = fieldInfo.FieldType;  // parse the field type to a var
        if (serializedReturn)
        {
            return fieldType; // for dynamic arrays when serializing the data
        }
        else
        {
            return typeof(UDynamicArrayManager<>).MakeGenericType(fieldType);   // construct a generic type based on the field type      
        }
    }

    private void DeserializeUStruct(string structName)
    {
        dataParser.ProcessObj(name);   
        dataParser.ProcessObj(structName);     
        LoopUPropertyDeserialization();
        dataParser.TerminateObj();
        dataParser.TerminateObj();
    }

    private void DeserializeStaticArray(int arraySize)
    {   dataParser.ProcessArray(name); // Start the array with the name
        for (int i = 0; i < arraySize; i++)
        {
            dataParser.ProcessObj();  
            LoopUPropertyDeserialization();
            dataParser.TerminateObj(); 
        }
        dataParser.TerminateArray();
    }

    private void LoopUPropertyDeserialization()
    {
        while (DeserializeUProperty());  
    }
}

 

