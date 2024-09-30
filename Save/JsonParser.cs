using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

public class JsonParser
{
    internal MemoryStream jsonStream;
    internal Utf8JsonWriter writer;

    public  JsonParser()
    {
        jsonStream = new MemoryStream();
        writer = new Utf8JsonWriter(jsonStream, new JsonWriterOptions { Indented = true });
    }

    // create overload methods here
    private void WriteStartobj() => writer.WriteStartObject();
    private void WriteStartobj(string name) => writer.WriteStartObject(name);
    private void WriteEndObj() => writer.WriteEndObject();
    private void WriteStartArray() => writer.WriteStartArray();
    private void WriteStartArray(string name) => writer.WriteStartArray(name);
    private void WriteEndArray() => writer.WriteEndArray();

    private void WritePropertyName(string name) => writer.WritePropertyName(name);
    internal void WriteNumberValue(int value) => writer.WriteNumberValue(value); 
    internal void WriteNumberValue(float value) => writer.WriteNumberValue(value);
    internal void WriteBooleanValue(bool value) => writer.WriteBooleanValue(value);
    internal void WriteStringValue(string value) => writer.WriteStringValue(value);
    internal void WriteString(string propertyName, string value) => writer.WriteString(propertyName, value);

    internal void WriteProperty(string name) => writer.WritePropertyName(name);  
    private void WriteNonIndentedData(MemoryStream oneLineStream) => writer.WriteRawValue(Encoding.UTF8.GetString(oneLineStream.ToArray()));
    private void Flush() => writer.Flush();

    internal void ParseSaveData(string name, object value, string type, int arrayIndex, [Optional] string enumName, [Optional] string enumValue)
    {
        if (name == "NumConsumable")  // this is a special edge case that we need to handle.
        {
            ParseNumConsumable(name, value, type, arrayIndex);
            return;
        }

        if (name != null && type != "NameProperty" && type != "ByteProperty" && type != "StrProperty" || !string.IsNullOrEmpty(enumName)) WritePropertyName($"{name}");
        else if (type == "ByteProperty" && string.IsNullOrEmpty(enumName)) WritePropertyName($"b{name}");
        else if (type == "NameProperty") WritePropertyName($"ini{name}");
        else if (type == "StrProperty") WritePropertyName($"str{name}");

        switch (type)
        {
            case "IntProperty":
                WriteNumberValue((int)value);
                break;
            case "FloatProperty":
                if(!Util.Truncate((float)value)) writeFloat((float)value);
                else WriteNumberValue((float)value);  // type float
                break;
            case "ByteProperty":
                if (!string.IsNullOrEmpty(enumName) && !string.IsNullOrEmpty(enumValue))
                {
                    WriteConciseEnum(name!, enumName, enumValue);
                }
                else WriteNumberValue((byte)value);  
                break;
            case "BoolProperty":
                WriteBooleanValue((bool)value);
                break;
            case "StrProperty":
                WriteStringValue((string)value);
                break;
            case "NameProperty":
                WriteStringValue((string)value);
                break;
            default:
                ProcessObj(); 
                TerminateObj();
                break;
        }
    }

// TODO; optimize indenting code so it can be ran through one function

    private void ParseNumConsumable(string name, object value, string type, int idx)
    {
        if (type != "IntProperty")
        {
            return;
        }

        // For a one-line format, we temporarily disable indentation
        var options = new JsonWriterOptions { Indented = false }; // One-line format
        using (var oneLineStream = new MemoryStream())
        {
            using (var oneLineWriter = new Utf8JsonWriter(oneLineStream, options))
            {
                oneLineWriter.WriteStartObject();
                oneLineWriter.WriteString("Item", GetConsumableName(idx));
                oneLineWriter.WriteNumber("Count", (int)value);
                oneLineWriter.WriteEndObject();
            }

            // Write the one-line JSON to the main stream
            WritePropertyName(name);
            WriteNonIndentedData(oneLineStream);
        }
    }
    

    private void WriteConciseEnum(string name, string enumName, string enumValue)
    {
        // For a one-line format, we temporarily disable indentation
        var options = new JsonWriterOptions { Indented = false }; // One-line format
        using (var oneLineStream = new MemoryStream())
        {
            using (var oneLineWriter = new Utf8JsonWriter(oneLineStream, options))
            {    
                oneLineWriter.WriteStartObject();
                oneLineWriter.WriteString(enumName, enumValue);
                oneLineWriter.WriteEndObject();
            }
            WriteNonIndentedData(oneLineStream);
        }
    }

    private string GetConsumableName(int idx)
    {
        Globals.eTouchRewardActor rewardActor = (Globals.eTouchRewardActor)idx;
        return rewardActor.ToString();
    }

   
    internal void ProcessObj(string name)
    {
        WriteStartobj(name); 
        Flush();
    }
    internal void ProcessObj()
    {
        WriteStartobj(); 
        Flush();
    }
    internal void TerminateObj()
    {
        WriteEndObj();
        Flush();
    }
    internal void ProcessArray()
    {
        WriteStartArray();
        Flush();
    }
    internal void ProcessArray(string name) 
    {
        WritePropertyName(name); 
        WriteStartArray(); 
        Flush();
    }
    internal void TerminateArray()
    {
        WriteEndArray();
        Flush();
    }
    internal void WriteEmptyArray(string name)
    {
        ProcessArray(name);
        TerminateArray();
    }

/// <summary>
/// Takes a float, and formats it as to where it adds a decimal to the final float parsing.
/// </summary>
/// <param name="floatvalue"> float value to parse with .0</param>
/// TODO: i recognize this isnt the most optimal way to go about this, but for now it works
    private void writeFloat(float floatvalue) 
    {
        string floatStr = floatvalue.ToString("0.0################");
        JsonElement jse = JsonDocument.Parse(floatStr).RootElement;
        jse.WriteTo(writer);
    }
}

