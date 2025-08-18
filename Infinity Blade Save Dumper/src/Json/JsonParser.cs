using System.Diagnostics;
using System.Text.Json;

namespace SaveDumper.JsonParser;

/// <summary>
/// Accepts Deserialized Data from an UnrealPackage and writes it to a .json file
/// *generated data gets sent to \OUTPUT* 
/// </summary>
public class JsonDataParser
{
    private readonly string filePath = $@"{FilePaths.OutputDir}\Deserialized Save Data.json";
    private readonly List<UProperty> saveData;

    public JsonDataParser(List<UProperty> saveData) => this.saveData = saveData;

    // TODO: Add override for different file path?
    /// <summary>
    /// Writes out all save data neatly into a json file
    /// </summary>
    /// <param name="saveData">Data to parse</param>
    /// <returns>If the operation was successful </returns>
    internal bool WriteDataToFile()
    {
        FilePaths.ValidateOutputDirectory();

        using (FileStream fs = File.Create(filePath))
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
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
                        //TODO: handle errors more effeciently here
                        Debug.WriteLine(ex.Message);
                        return false;
                    }
                }

                writer.WriteEndObject();
            }

            return true;
        }
    }
}


