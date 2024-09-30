using System.Text.Json;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

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
    private void Flush() => writer.Flush();

    internal void ParseSaveData(string name, object value, string type, [Optional] string enumName, [Optional] string enumValue)
    {
        if (name != null && type != "NameProperty" && type != "ByteProperty" && type != "StrProperty" || !string.IsNullOrEmpty(enumName)) WritePropertyName($"{name}");
        if (type == "ByteProperty" && string.IsNullOrEmpty(enumName)) WritePropertyName($"b{name}");
        if (type == "NameProperty") WritePropertyName($"ini{name}");
        if (type == "StrProperty") WritePropertyName($"str{name}");

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
                    ProcessObj();
                    WriteString(enumName, enumValue);
                    TerminateObj();
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

