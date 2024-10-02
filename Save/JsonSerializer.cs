using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

class JsonSerializer
{
#region Arbitrary Data
    private string jsontext = File.ReadAllText(Globals.outputFile); // get json text
    private JsonTextReader reader;
    private BinaryWriter bWriter;
    private UnrealArchive Ar;
    private SerializePropertyState serializeState;
    private const int dataSize = 4;
    private const int none = 5;
    private const int nullTerminator = 1;
    private byte[] binaryHeader = [05, 00, 00, 00, 0xFF, 0xFF, 0xFF, 0xFF];

#endregion

#region Shorthand Functions 
    private bool Read() => reader.Read();
    private void Read(int count) 
    {
        for (int i = 0; i < count; i++) Read();
    }
    private void serialize(byte[] bytes, int index) => bWriter.Write(bytes, index, bytes.Length);
    private void serialize(byte[] value) => bWriter.Write(value, 0, value.Length);
    private bool IsDynamicArray(string name) => Util.IsArrayDynamic(name);
    private Type GetArrayFieldType(string name) => Util.GetArrayVariableType<Globals.DynamicArrayTypes>(name);

#endregion

#region Constructor
    public JsonSerializer(UnrealArchive UnrealArchive)
    {
        Ar = UnrealArchive;
        bWriter = Ar.bWriter; // get binary writer instance from the unreal archive
        reader = new JsonTextReader(new StringReader(jsontext));  // get output file text
        serializeState = new SerializePropertyState();

        ConsoleHelper.DisplayColoredText("Serializing file!", ConsoleHelper.ConsoleColorChoice.Yellow);
        SerializeJsonFile();
        ConsoleHelper.DisplayColoredText("File Serialized!", ConsoleHelper.ConsoleColorChoice.Green);
    }
#endregion

#region Main

    private class SerializePropertyState
    {
        // properties
        internal string lastPropertyName = "";
        internal int arrayIndex = 0;
        internal int structArrayIndex = 0;
        internal int arrArrayIndex = 0;
        internal int structsLoaded = 0;
        internal int arraysLoaded = 0;

        // objects & arrays
        internal string lastStructName = "";
        internal string lastArrayName = "";

        // state properties
        internal bool pendingLoadByte = false;
        internal bool pendingLoadName = false;
        internal bool pendingLoadStr = false;
        internal bool enumHandle = false;
    }

    private void SerializeJsonFile()
    {
        SerializeBinaryheader(); 
        while(Read()) 
        {
            SerializeProperty();   // we will never encounter an end object unless the file is ending
        }
        SerializeStringValue("None"); // end file
    }


    private void SerializeProperty()
    {
        // data
        string propertyName = "";
        JsonToken propertyValueType;
        object propertyValue;

        if (!GetPropertyName(ref propertyName, false)) return;
        (propertyValueType, propertyValue) = GetPropertyValueInfo();
        CleanPropertyName(ref propertyName, propertyValueType);

        long satisfy = -1;
        SerializePropertyAndVariable(propertyName, propertyValueType, propertyValue, ref satisfy);
    }

    private void SerializeProperty(ref long sizeLog, [CallerMemberName] string callerName = "")
    {
        // data
        string propertyName = "";
        JsonToken propertyValueType;
        object propertyValue;

        if (serializeState.enumHandle)
        {
            serializeState.enumHandle = false;
        }

        // get property name
        if (!GetPropertyName(ref propertyName, true)) return;
        
        // get property value info
        (propertyValueType, propertyValue) = GetPropertyValueInfo();

        // clean the string and set flags if needed
        CleanPropertyName(ref propertyName, propertyValueType);

        // serialize property
        SerializePropertyAndVariable(propertyName, propertyValueType, propertyValue, ref sizeLog, callerName);
    }

    private void SerializePropertyAndVariable(string propertyName, JsonToken propertyValueType, object propertyValue, ref long sizeLog, [Optional] string callerName)
    {
        bool logSize;
        if (sizeLog != -1) logSize = true;
        else logSize = false;

        SerializeStringValue(propertyName);  // name
        if (logSize)
        {
            sizeLog += dataSize + propertyName.Length + nullTerminator;
        }
        switch (propertyValueType) // type
        {
            case JsonToken.Integer:
                if (serializeState.pendingLoadByte)
                {
                    SerializeStringValue("ByteProperty");
                    SerializeIntValue(1); // size
                    SerializeIntValue(serializeState.arrayIndex); // Array Index
                    SerializeStringValue("None");
                    SerializeByteValue(Convert.ToByte(propertyValue));     
                    if (logSize)
                    {
                        sizeLog += dataSize + "ByteProperty_".Length;
                        sizeLog += dataSize + dataSize + dataSize + "None_".Length + 1;
                    }
                    serializeState.pendingLoadByte = false;
                }
                else
                {
                    SerializeStringValue("IntProperty");
                    SerializeIntValue(4); // size
                    SerializeIntValue(serializeState.arrayIndex); // Array Index
                    SerializeIntValue(Convert.ToInt32(propertyValue)); // value     
                    if (logSize)
                    {
                        sizeLog += dataSize + "IntProperty_".Length;
                        sizeLog += dataSize + dataSize + dataSize;
                    } 
                }
          
                break;

            case JsonToken.Float:
                SerializeStringValue("FloatProperty");
                SerializeIntValue(4); // size
                SerializeIntValue(serializeState.arrayIndex); // Array Index
                SerializeFloatValue(Convert.ToSingle(propertyValue));  // value
                if (logSize)
                {
                    sizeLog += dataSize + "FloatProperty_".Length;
                    sizeLog += dataSize + dataSize + dataSize;
                } 
                break;

            case JsonToken.Boolean:
                SerializeStringValue("BoolProperty");
                SerializeIntValue(0);
                SerializeIntValue(serializeState.arrayIndex);
                SerializeBooleanValue(Convert.ToBoolean(propertyValue));
                if (logSize)
                {
                    sizeLog += dataSize + "ByteProperty_".Length;
                    sizeLog += dataSize + dataSize + 1;
                } 
                break;

            case JsonToken.String:
                string serializeValue = "";
                if(serializeState.pendingLoadName)
                {
                    serializeValue = "NameProperty";
                    serializeState.pendingLoadName = false;
                    
                }
                else if(serializeState.pendingLoadStr)
                {
                    serializeValue = "StrProperty";
                    serializeState.pendingLoadStr = false;
                }

                if (string.IsNullOrEmpty(Convert.ToString(propertyValue)))
                {
                    SerializeStringValue(serializeValue);
                    SerializeIntValue(4); // acount array index as well as data size
                    SerializeIntValue(serializeState.arrayIndex);
                    SerializeIntValue(0);
                    if (logSize)
                    {
                        sizeLog += dataSize + serializeValue.Length + nullTerminator;
                        sizeLog += dataSize * 3;
                    } 
                    break;
                }

                SerializeStringValue(serializeValue);
                SerializeIntValue(propertyValue.ToString()!.Length + 5); // acount array index as well as data size
                SerializeIntValue(serializeState.arrayIndex);
                SerializeStringValue(Convert.ToString(propertyValue)!);
                if (logSize)
                {
                    sizeLog += dataSize + serializeValue.Length + nullTerminator;
                    sizeLog += dataSize + dataSize + dataSize + propertyValue.ToString()!.Length + 1; // size, array index, value size
                } 
                break;

            case JsonToken.StartObject:
                //string parentStruct = "";
                string objectPropertyName = "";
                long structSizeBuffer;

                ReadOver(ref objectPropertyName!);
                (propertyValueType, propertyValue) = GetPropertyValueInfo(); 
                if (propertyValueType == JsonToken.StartObject) 
                {
                    // if (IsParentStruct())
                    // {
                    //     parentStruct = propertyName;
                    // }
                    structSizeBuffer = SerializeStruct(objectPropertyName, false);
                    if (logSize)
                    {
                        if (callerName == "SerializeStruct" || callerName == "SerializeArray" || callerName == "SerializeArrayEntry")
                        {
                            // if we are serializing a struct, then we need to add on the array property size
                            sizeLog += dataSize + "StructProperty_".Length + dataSize * 2;
                            sizeLog += dataSize + objectPropertyName.Length + nullTerminator;
                        }
                        sizeLog += structSizeBuffer;
                    }
                }
                else if (propertyValueType == JsonToken.String)
                {
                    if(objectPropertyName == "Item")
                    {
                        SerializeEdgeCaseConsumable((string)propertyValue, ref sizeLog);
                    }
                    else 
                    {
                       SerializeEnum(objectPropertyName, Convert.ToString(propertyValue)!, ref sizeLog);
                    }
                }
                break;

            case JsonToken.StartArray:
                long arraySizeBuffer;
                arraySizeBuffer = SerializeArray(propertyName, false);
                if (logSize)
                {
                    if (callerName == "SerializeStruct" || callerName == "SerializeArray" || callerName == "SerializeArrayEntry")
                    {
                        // if we are serializing a struct, then we need to add on the array property size
                        sizeLog += dataSize + "ArrayProperty_".Length + dataSize * 2;
                    }
                    sizeLog += arraySizeBuffer;
                }
                break;

            default:
                Console.WriteLine($"{propertyValue} is not handled!");
                break;
        }
    }

    private bool GetPropertyName(ref string propertyName, bool loading)
    {
        if (loading) Read();
        if (reader.TokenType == JsonToken.PropertyName) propertyName = reader.Value!.ToString()!;
        else
        {
            if (reader.TokenType == JsonToken.EndObject && serializeState.structsLoaded == 1) 
            {
                serializeState.structsLoaded = 0; // nothing additional found inside struct, start clean up of master struct 
            }
            else if (reader.TokenType != JsonToken.EndObject)
            {
                Console.WriteLine($"{reader.TokenType} is expected to be a PropertyName.");           
            }
            return false;
        }
        UpdateArrayIndex(propertyName);

        return true;
    }

    private long SerializeStruct(string structName, bool recursiveCall)
    {
        long structSize = 0;
        long storeStructDataPos;
        serializeState.structsLoaded++;

        SerializeStringValue("StructProperty");
        storeStructDataPos = bWriter.BaseStream.Position; 
        SerializePlaceholders(2);
        SerializeStringValue(structName);  
        
        while (reader.TokenType != JsonToken.EndObject || serializeState.enumHandle == true || serializeState.structsLoaded == 1) 
        {
            SerializeProperty(ref structSize);
        }

        if (IsParentStruct())
        {
            UpdateArrayIndexStruct(structName);
        }
        SerializeNone(ref structSize); // end struct
        WriteHeaderInfo(storeStructDataPos, (int)structSize, false);

        if (!IsParentStruct())
        {
            serializeState.structsLoaded--;
            Read();
        }
        else
        {
            if (IsParentArray())
            {
                serializeState.enumHandle = true;
                Read();
            }
        }
        return structSize;
    }

    private long SerializeArray(string arrayName, bool recursiveCall)
    {
        long arraySize = 0;
        int itemCount = 0;
        bool isDynamic = IsDynamicArray(arrayName);  // reduce function calls needed
        serializeState.arraysLoaded++;

        if (recursiveCall)
        {
            arraySize += dataSize + "ArrayProperty_".Length + dataSize * 2;
        }

        SerializeStringValue("ArrayProperty");
        long storeArrayDataPos = bWriter.BaseStream.Position;
        SerializePlaceholders(3);  // size, arrayindex, array item count
        arraySize += dataSize;  // array entry count
        Read();
        
        if (isDynamic) SerializeDynamicArray(ref itemCount, ref arraySize, GetArrayFieldType(arrayName));
        else SerializeArrayEntry(ref itemCount, ref arraySize); // change this
        WriteHeaderInfo(storeArrayDataPos, (int)arraySize, true, itemCount);

        serializeState.arraysLoaded--;

        return arraySize;
    }

    private void SerializeArrayEntry(ref int itemCount, ref long arraySize)
    {

        while (reader.TokenType != JsonToken.EndArray)
        {
            itemCount++;
            while (reader.TokenType != JsonToken.EndObject || serializeState.enumHandle == true) SerializeProperty(ref arraySize);
            Read();
            SerializeNone(ref arraySize);
        }
    }

#endregion

#region Util

    private (JsonToken, object) GetPropertyValueInfo()
    {
        Read();
        return (reader.TokenType, reader.Value!);
    }

    private void CleanPropertyName(ref string propName, JsonToken propertyValueType)
    {
        if (!propName.StartsWith("b") && !propName.StartsWith("ini") && !propName.StartsWith("str")) return;   
        else if (propertyValueType == JsonToken.Boolean) return;
        else if (propName == "bWasEncrypted") return;  // edge case, for some reason the devs used a boolean type prefix for an int?

        if (propName.StartsWith("b"))
        {
            serializeState.pendingLoadByte = true;
            propName = propName.Remove(0, 1);
            return;
        }
        else if (propName.StartsWith("ini"))
        {
            serializeState.pendingLoadName = true;
            propName = propName.Remove(0, 3);
            return; 
        }
        else if (propName.StartsWith("str"))
        {
            serializeState.pendingLoadStr = true;
            propName = propName.Remove(0, 3);
            return;    
        }

        Console.WriteLine($"{propName} was not changed");
    }

    private void UpdateArrayIndex(string propertyName)
    {
        if (propertyName == serializeState.lastPropertyName) serializeState.arrayIndex++;
        else
        {
            serializeState.arrayIndex = 0;
            serializeState.lastPropertyName = propertyName;
        }
    }

    private void UpdateArrayIndexStruct(string structName)
    {
        if (structName == serializeState.lastStructName) serializeState.structArrayIndex++;
        else
        {
            serializeState.structArrayIndex = 0;
            serializeState.lastStructName = structName;
        }
    }

    private void SerializeDynamicArray(ref int itemCount, ref long arraySize, Type arrayType)
    {
        if (arrayType == typeof(string)) SerializeStrArray(ref itemCount, ref arraySize);
        else if (arrayType == typeof(int)) SerializeIntArray(ref itemCount, ref arraySize);
        else if (arrayType == typeof(float)) SerializeFloatArray(ref itemCount, ref arraySize);
        else Console.WriteLine($"{arrayType} not supported!");
    }

    private void SerializeStrArray(ref int itemCount, ref long arraySize)
    {
        while (reader.TokenType != JsonToken.EndArray) 
        {
            itemCount++;
            SerializeStringValue(reader.Value!.ToString()!);   
            arraySize += dataSize + reader.Value!.ToString()!.Length + nullTerminator;
            Read();
        }  
    }

    private void SerializeIntArray(ref int itemCount, ref long arraySize)
    {
        while (reader.TokenType != JsonToken.EndArray) 
        {
            itemCount++;
            SerializeIntValue(Convert.ToInt32(reader.Value));   
            Read();
        }  
        arraySize += itemCount * 4;  
    }

    private void SerializeFloatArray(ref int itemCount, ref long arraySize)
    {
        while (reader.TokenType != JsonToken.EndArray) 
        {
            itemCount++;
            SerializeFloatValue(Convert.ToSingle(reader.Value));   
            Read();
        }  
        arraySize += itemCount * 4;
    }

    private void SerializeNone() => SerializeStringValue("None");

    private void SerializeNone(ref long size) 
    {
        SerializeStringValue("None");
        size += dataSize + none;
    }

    private void SerializeEnum(string enumName, string enumValue, ref long sizeLog)
    {
        serializeState.enumHandle = true;
        SerializeStringValue("ByteProperty");
        SerializeIntValue(enumValue!.Length + dataSize + nullTerminator);
        SerializeIntValue(serializeState.arrayIndex);
        SerializeStringValue(enumName);
        SerializeStringValue(enumValue);
        Read();

        if (sizeLog != -1)
        {
            sizeLog += dataSize + "ByteProperty_".Length;
            sizeLog += dataSize + dataSize + dataSize + enumName.Length + nullTerminator + dataSize + enumValue.Length + nullTerminator;
        }
    }

    private void SerializeEdgeCaseConsumable(string propertyValue, ref long sizeLog)
    {
        JsonToken type;
        object value;
        int arrayIndexValue = 0;

        serializeState.enumHandle = true;

        // Globals.eTouchRewardActor intType = 
        if (Enum.TryParse(propertyValue, out Globals.eTouchRewardActor rewardActor))
        {
            arrayIndexValue = (int)rewardActor;
        }
        else
        {
            Console.WriteLine("Enum not found.");
            return;
        }

        Read();
        (type, value) = GetPropertyValueInfo(); 

        SerializeStringValue("IntProperty");
        SerializeIntValue(4); // size
        SerializeIntValue(arrayIndexValue); // Array Index
        SerializeIntValue(Convert.ToInt32(value)); // value     

        Read();

        if (sizeLog != -1)
        {
            sizeLog += dataSize + "IntProperty_".Length;
            sizeLog += dataSize + dataSize + dataSize;
        }
    }

    private void WriteHeaderInfo(long storePos, int size, bool isArray, [Optional] int itemCount)
    {
        Ar.ChangeWriterPosition(storePos);             
        SerializeIntValue(size);
        if (isArray)
        {
            SerializeIntValue(serializeState.arrArrayIndex);  //TODO: make this actually functional
            SerializeIntValue(itemCount);
        }
        else 
        {
            //TODO: be inclusive of all struct types
            // if its not a main struct, dont update size. 
            if (IsParentStruct())
            {
                SerializeIntValue(serializeState.structArrayIndex);
            }
            else
            {
                SerializeIntValue(0);  
            }
        }  
        Ar.ChangeWriterPosition(bWriter.BaseStream.Length); 
    }

    private void SerializeStringValue(string str)
    {
        str = str + char.MinValue;  // create null terminator
        byte[] _buffer = Util.IntToLittleEndianBytes(str.Length);
        serialize(_buffer);
        _buffer = System.Text.Encoding.UTF8.GetBytes(str);
        serialize(_buffer);
    }

    private void SerializeIntValue(int value)
    {
        byte[] _buffer = Util.IntToLittleEndianBytes(value);
        serialize(_buffer);
    }

    private void SerializeFloatValue(float value)
    {
        byte[] _buffer = Util.FloatToLittleEndianBytes(value);
        serialize(_buffer);
    }

    private void SerializeBooleanValue(bool value) => SerializeByteValue(Convert.ToByte(value));


    public void SerializeByteValue(byte byteValue)
    {
        byte[] bArray = [];
        byte byteD = byteValue;
        byte[] newArray = new byte[bArray.Length + 1];
        bArray.CopyTo(newArray, 1);
        newArray[0] = byteD;
        serialize(newArray);
    }

    private void SerializeBinaryheader()
    {
        serialize(binaryHeader);
        Read();
    }

    private void SerializePlaceholders(int placeHolderCount)
    {
        for (int i = 0; i < placeHolderCount; i++) SerializeIntValue(0); 
    }

    private void ReadOver(ref string? str)
    {
        Read();
        str = reader.Value!.ToString()!;
    }
    private void ReadOver(ref string? str, int count)
    {
        Read(count);
        if (reader.TokenType == JsonToken.PropertyName) str = reader.Value!.ToString()!;
        else return;
    }

    private bool IsParentStruct()
    {
        if (serializeState.structsLoaded > 0)
        {
            return false;
        }
        return true;
    }

    private bool IsParentArray()
    {
        if (serializeState.arraysLoaded > 1)
        {
            return false;
        }
        return true;
    }

#endregion

}


