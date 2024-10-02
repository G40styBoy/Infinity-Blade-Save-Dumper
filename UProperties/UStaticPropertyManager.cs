class UStaticPropertyManager<T>
{
    // constructer
    private UnrealArchive Ar;
    private UPropertyManager uPropertyManager;
    private JsonParser dataParser;
    private Type constructType; 
    private string arrayName;


    //deserialization
    private string lastType = null;
    private string name = "";
    private string type = "";
    private int arrayIndex, size;
    // private DeserializerState state;


    public UStaticPropertyManager(UnrealArchive Ar, UPropertyManager uPropertyManager, string arrayName, JsonParser dParse)
    {
        this.Ar = Ar;
        this.arrayName = arrayName; //get array name
        this.uPropertyManager = uPropertyManager;
        dataParser = dParse;  // get data parser
        constructType = typeof(T);  // get type
        // state = new DeserializerState();

        DeserializeStaticArray();
    }

    private void RevertDeserialize() => Ar.ChangeStreamPosition(-4);

    private void DeserializeStaticArray()
    {
        dataParser.ProcessArray(arrayName); // Start the array with the name, all pathways need this
        if (constructType == typeof(int))
        {

        }
        else if (constructType == typeof(byte))
        {

        } 
        // something along the lines of this
        else if (constructType == typeof(Globals.StaticArrayTypes.StaticArrayNameList))
        {

        }
        else Console.WriteLine("Deserialization for type {0} is not supported.", constructType);
        dataParser.TerminateArray();
    }

    private bool DeserializeUProperty()
    {
        Ar.Deserialize(ref name);
        if (string.IsNullOrEmpty(lastType))
        {
            lastType = name;
        }
        if (name != lastType)
        {
            RevertDeserialize();
            return false;
        }

        if (name == "None")
        {
            if (uPropertyManager.state.loadingStruct) uPropertyManager.state.loadingStruct = false;
            return false;
        }
        Ar.Deserialize(ref type);
        Ar.Deserialize(ref size);
        Ar.Deserialize(ref arrayIndex);

        return uPropertyManager.ConstructPropertyValue();
    }
}
