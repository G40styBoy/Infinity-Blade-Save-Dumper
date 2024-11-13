using System.Runtime.CompilerServices;
using System.Text;

public class UPropertyManager
{

    private const string IntProperty = "IntProperty";
    private const string FloatProperty = "FloatProperty";
    private const string ByteProperty = "ByteProperty";
    private const string BoolProperty = "BoolProperty";
    private const string StrProperty = "StrProperty";
    private const string NameProperty = "NameProperty"; 
    private const string StructProperty = "StructProperty";
    private const string ArrayProperty = "ArrayProperty";
    private const string None = "None";

    private const int DATA_SIZE = sizeof(int);
    private const int NULL_TERMINATOR = 1;
    private const int EMPTY = 0;

    private string name = "";
    private string type = "";
    private int arrayIndex, size;

    internal protected JsonParser dataParser;
    internal protected OptimizeViaPredefine var;
    internal UnrealArchive Ar;
    internal DeserializerState state;

    public enum ArrayManagerState  // Moved enum outside for wider access if needed
    {
        None = 0,
        Dynamic = 1,
        Static = 2
    }

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

        internal protected string enumName = "";
        internal protected string enumValue = ""; 
        internal protected string structName = "";
        internal protected int arraySize = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseSaveData(string name, object value, string type, int arrayIndex) => 
        dataParser.ParseSaveData(name, value, type, arrayIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseSaveData(string name, object value, string type, int arrayIndex, string enumName, string enumValue) => 
        dataParser.ParseSaveData(name, value, type, arrayIndex, enumName, enumValue);

    private void WriteToFile(string file, byte[] bytes) => File.WriteAllText(file, Encoding.UTF8.GetString(bytes));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsUPropertyStatic(string name) => ClassManager.IsUPropertyStatic(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsArrayDynamic(string name) => ClassManager.IsArrayDynamic(name);

    private void RevertDeserialize(long amount) =>
        Ar.ChangeStreamPosition(amount);

    private void CreateArrayManager(int arraySize, ArrayManagerState state) => 
        ClassManager.CreateArrayManager(Ar, name, arraySize, dataParser, state);

    internal void DeserializeDataToJson()
    {
        dataParser.ProcessObj();
        DeserializeFile();
        dataParser.TerminateObj();

        WriteToFile(Globals.outputFile, dataParser.jsonStream.ToArray());
    }

    private void DeserializeFile()
    {
        ConsoleHelper.DisplayColoredText("Deserializing File!", ConsoleHelper.ConsoleColorChoice.Yellow);

        Ar.ChangeStreamPosition(Globals.packageBegin);  // skip beginning data
        while (Ar.saveStream.Position < Ar.fileBytes.Length) 
            DeserializeUProperty();  // deserialize file

        ConsoleHelper.DisplayColoredText("File Deserialized!", ConsoleHelper.ConsoleColorChoice.Green);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool DeserializeUProperty()
    {
        Ar.Deserialize(ref name);
        if (name == None)
        {
            if (state.loadingStruct) state.loadingStruct = false;
            return false;
        }
        if (IsUPropertyStatic(name) && !state.loadingStaticArray)
        {
            HandleStaticArray();
            return true;
        }
        Ar.Deserialize(ref type);
        Ar.Deserialize(ref size);
        Ar.Deserialize(ref arrayIndex);

        return ConstructPropertyValue();
    }

    private long GetFullStringSize(string str)
        => DATA_SIZE + str.Length + NULL_TERMINATOR;

    private void HandleStaticArray()
    {
        state.loadingStaticArray = true;
        RevertDeserialize(-GetFullStringSize(name));
        CreateArrayManager(0, ArrayManagerState.Static);
        state.loadingStaticArray = false;
    }

    protected virtual bool ConstructPropertyValue()
    {
        object defaultParser;
        switch (type)
        {
            case IntProperty:
                Ar.Deserialize(ref var._int);                
                defaultParser = var._int;
                break;
            case FloatProperty:
                Ar.Deserialize(ref var._float);        
                defaultParser = var._float;
                break;        
            case BoolProperty:
                Ar.Deserialize(ref var._bool);        
                defaultParser = var._bool;
                break;

            case ByteProperty:
                Ar.Deserialize(ref var.enumName);
                if (var.enumName == None)
                {
                    // property pulled in None
                    Ar.Deserialize(ref var._byte);
                    defaultParser = var._byte;  
                    break;
                }
                //property pulled in enum
                Ar.Deserialize(ref var.enumValue);
                ParseSaveData(name, 0, type, arrayIndex, var.enumName, var.enumValue);
                return true;

            case StrProperty:
            case NameProperty:
                Ar.Deserialize(ref var._int);
                if (var._int == EMPTY) // handles empty string case
                {
                    ParseSaveData(name, "", type, arrayIndex);
                    return true;
                }
                RevertDeserialize(-4);
                Ar.Deserialize(ref var._str);
                ParseSaveData(name, var._str, type, arrayIndex);
                return true;

            case StructProperty:
                state.loadingStruct = true;  // detected struct

                Ar.Deserialize(ref var.structName);
                DeserializeUStruct(var.structName);
                return true;

            case ArrayProperty:
                Ar.Deserialize(ref var.arraySize);

                if (var.arraySize == EMPTY)
                {
                    dataParser.WriteEmptyArray(name);
                    return true;
                }
                DeserializeUArray(name, var.arraySize);
                return true;

            default:
                ConsoleHelper.DisplayColoredText($"Unsupported property type: {type}", ConsoleHelper.ConsoleColorChoice.Red);
                return false;
        }
        ParseSaveData(name, defaultParser, type, arrayIndex);
        return true;
    }

    private void DeserializeUArray(string arrayName, int arraySize)
    {
        if (IsArrayDynamic(arrayName))
            CreateArrayManager(arraySize, ArrayManagerState.Dynamic);
        else 
            DeserializeArray(arraySize);
    }

    public virtual void DeserializeUStruct(string structName)
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

    protected virtual void LoopUPropertyDeserialization()
    {
        while (DeserializeUProperty());
    }

    private void ProcessEntry(string entryName = "")
    {
        if (string.IsNullOrWhiteSpace(entryName))
            dataParser.ProcessObj();
        else
            dataParser.ProcessObj(entryName);
        LoopUPropertyDeserialization();
        dataParser.TerminateObj();
    }

    public class UPropertyArrayManager<T> : UPropertyManager
    {
        private readonly string arrayName;
        private readonly int arraySize;
        private readonly ArrayManagerState managerState; 


        public UPropertyArrayManager(UnrealArchive Ar, string arName, int arSize, JsonParser dparser, ArrayManagerState state) : base(Ar) 
        {
            dataParser = dparser;  // might need to re-add .this (i think this was a bug of some sort)
            arrayName = arName;
            arraySize = arSize;
            managerState = state;

            switch (managerState) 
            {
                case ArrayManagerState.Dynamic:
                    ProcessDynamicArray();
                    break;
                case ArrayManagerState.Static:
                    ProcessStaticArray(); 
                    break;
            }
        }

        private bool IsStruct(Type type) =>
            type.IsValueType && !type.IsPrimitive && !type.IsEnum;

        /// <summary>
        /// Checks if the next UProperty name is a continuation of a static array.
        /// </summary>
        /// <returns></returns>
        private bool IsNextPropertyStatic()
        {
            string _buffer = "";
            Ar.Deserialize(ref _buffer);
            RevertDeserialize(-GetFullStringSize(_buffer));

            if (_buffer != arrayName)
                return false;
            else
                return true;
        }   

        protected override void LoopUPropertyDeserialization()
        {
            while (DeserializeUProperty());
        }     

        private void ProcessDynamicArray()
        {
            dataParser.ProcessArray(arrayName); // Start the array with the name, all pathways need this
            DeserializeDynamicArray();
            dataParser.TerminateArray();
        }

        private void DeserializeDynamicArray()
        {
            TypeCode typeCode = Type.GetTypeCode(typeof(T));
            for (int i = 0; i < arraySize; i++)
            {
                switch (typeCode)
                {
                    case TypeCode.String:
                        Ar.Deserialize(ref var._str);
                        dataParser.WriteStringValue(var._str);
                        break;  // continue

                    case TypeCode.Int32:
                        Ar.Deserialize(ref var._int);
                        dataParser.WriteNumberValue(var._int);
                        break;

                    case TypeCode.Single:
                        Ar.Deserialize(ref var._float);
                        dataParser.WriteNumberValue(var._float);
                        break;

                    default:
                        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
                }
            }
        }

        private void ProcessStaticArray()
        {
            dataParser.ProcessArray(arrayName);
            DeserializeStaticArray(typeof(T));
            dataParser.TerminateArray();
        }

        private void DeserializeStaticArray(Type type)
        {
            bool isStruct = IsStruct(type);

            while (IsNextPropertyStatic())
            {
                if (isStruct)
                {
                    dataParser.ProcessObj();
                    DeserializeUProperty();
                    dataParser.TerminateObj();
                }
                else
                {
                    DeserializeDynamicArray();
                }
            }
        }
        protected override bool DeserializeUProperty()
        {
            Ar.Deserialize(ref name);
            if (name == None)
            {
                if (state.loadingStruct) state.loadingStruct = false;
                return false;
            }
            Ar.Deserialize(ref type);
            Ar.Deserialize(ref size);
            Ar.Deserialize(ref arrayIndex);

            if (type == StructProperty)
            {
                state.loadingStruct = true;  // detected struct

                Ar.Deserialize(ref var.structName);
                LoopUPropertyDeserialization();
                return true;
            }
            else
                return ConstructPropertyValue();
        }      
    }
}