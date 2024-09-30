class UDynamicArrayManager<T>
{
    private UnrealArchive Ar;
    private JsonParser dataParser;
    private Type constructType; 
    private int arraySize;
    private string arrayName;

    public UDynamicArrayManager(UnrealArchive ar, string arName, int arSize, JsonParser dParse)
    {
        Ar = ar;
        arraySize = arSize; // array size
        arrayName = arName; //get array name
        dataParser = dParse;  // get data parser
        constructType = typeof(T);  // get type

        DeserializeDynamicArray();
    }
    private void DeserializeDynamicArray()
    {
        dataParser.ProcessArray(arrayName); // Start the array with the name, all pathways need this
        if (constructType == typeof(string)) DeserializeStringArray();
        else if (constructType == typeof(int)) DeserializeIntArray();
        else if (constructType == typeof(float)) DeserializeFloatArray();
        else Console.WriteLine("Deserialization for type {0} is not supported.", constructType);
        dataParser.TerminateArray();
    }
    private void DeserializeStringArray()
    {
        string _Buffer = "";
        for (int i = 0; i < arraySize; i++)
        {
            Ar.Deserialize(ref _Buffer); 
            dataParser.WriteStringValue(_Buffer);
        }
    }
    private void DeserializeIntArray()
    {
        int _Buffer = 0;
        for (int i = 0; i < arraySize; i++)
        {
            Ar.Deserialize(ref _Buffer); 
            dataParser.WriteNumberValue(_Buffer);
        }
    }
    private void DeserializeFloatArray()
    {
        float _Buffer = 0;
        for (int i = 0; i < arraySize; i++)
        {
            Ar.Deserialize(ref _Buffer); 
            dataParser.WriteNumberValue(_Buffer);
        }
    }
}
