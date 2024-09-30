using System.Text;

public class JsonVariableFinder
{
    internal void FindAndPrintVariables(Stream jsonStream, string variableName)
    {
        jsonStream.Seek(0, SeekOrigin.Begin); // Reset stream position

        // Read the stream into a byte array
        byte[] buffer = new byte[jsonStream.Length]; 
        int bytesRead = jsonStream.Read(buffer, 0, buffer.Length);
        string jsonString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        List<string> foundValues = new List<string>();

        FindVariableInString(jsonString, variableName, foundValues);

        PrintResults(variableName, foundValues);
    }

    private static void FindVariableInString(string jsonString, string variableName, List<string> foundValues)
    {
        string searchPattern = $"\"{variableName}\":";
        int startIndex = jsonString.IndexOf(searchPattern);

        while (startIndex >= 0)
        {
            startIndex += searchPattern.Length;

            // Skip whitespace after the colon
            while (startIndex < jsonString.Length && char.IsWhiteSpace(jsonString[startIndex]))
            {
                startIndex++;
            }

            // Find the end of the value (comma, closing brace, or end of string)
            int endIndex = jsonString.IndexOf(',', startIndex);
            int endIndexBrace = jsonString.IndexOf('}', startIndex);
            if (endIndexBrace > 0 && (endIndexBrace < endIndex || endIndex == -1)) 
            {
                endIndex = endIndexBrace;
            }

            if (endIndex == -1) 
            {
                endIndex = jsonString.Length; // Reached the end
            }

            // Extract the value
            string value = jsonString.Substring(startIndex, endIndex - startIndex).Trim();

            // Handle special cases for true, false, and null 
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                // Keep these values as they are
            }
            else
            {
                // Remove quotes from strings
                value = value.Trim('"');
            }

            foundValues.Add(value);

            startIndex = jsonString.IndexOf(searchPattern, endIndex); 
        }
    }

    private static void PrintResults(string variableName, List<string> foundValues)
    {
        // Remove "str" or "ini" prefix
        string cleanedVariableName = variableName;
        if (cleanedVariableName.StartsWith("str", StringComparison.OrdinalIgnoreCase))
        {
            cleanedVariableName = cleanedVariableName.Substring(3);
        } 
        else if (cleanedVariableName.StartsWith("ini", StringComparison.OrdinalIgnoreCase)) 
        {
            cleanedVariableName = cleanedVariableName.Substring(3);
        }

        if (foundValues.Count == 0)
        {
            Console.WriteLine($"{cleanedVariableName} not found."); // No "Variable" prefix
        }
        else if (foundValues.Count == 1)
        {
            Console.WriteLine($"{cleanedVariableName}: {foundValues[0].Trim('"')}"); // Remove quotes
        }
        else
        {
            Console.WriteLine($"Found multiple occurrences of '{cleanedVariableName}':");
            foreach (string value in foundValues)
            {
                Console.WriteLine($"- {value.Trim('"')}"); // Remove quotes
            }
        }
    }
}