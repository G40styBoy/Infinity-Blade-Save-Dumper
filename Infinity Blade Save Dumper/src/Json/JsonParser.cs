using System.Diagnostics;
using System.Text.Json;
using SaveDumper.Utilities;

namespace SaveDumper.JsonParser;

/// <summary>
/// Accepts Deserialized Data from an UnrealPackage and writes it to a .json file
/// *generated data gets sent to \OUTPUT* 
/// </summary>
public class JsonDataParser : IDisposable
{
    private readonly string filePath = $@"{FilePaths.OutputDir}\Deserialized Save Data.json";
    private readonly List<UProperty> saveData;
    private readonly FileStream fs;
    private readonly Utf8JsonWriter writer;

    public JsonDataParser(List<UProperty> saveData)
    {
        this.saveData = saveData;

        fs = File.Create(filePath);
        writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
    }

    /// <summary>
    /// Writes out all save data neatly into a json file
    /// </summary>
    internal bool WriteDataToFile()
    {
        try
        {
            writer.WriteStartObject();
            foreach (var uProperty in saveData)
            {
                try
                {
                    uProperty.WriteValueData(writer, uProperty.name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            writer.WriteEndObject();
            writer.Flush();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        writer?.Dispose();
        fs?.Dispose();
    }
}
