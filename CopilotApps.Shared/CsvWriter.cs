using System.Text;
using System.Text.Json;

namespace CopilotApps.Shared;

public static class CsvWriter
{
    public static async Task<string?> WriteDictionaryCsvFromJsonAsync(string? jsonContent, IReadOnlyList<string> headers, string fileNamePrefix)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return null;
        }

        var records = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonContent);
        if (records is null || records.Count == 0)
        {
            return null;
        }

        var csvFileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        await using (var writer = new StreamWriter(csvFileName, false, Encoding.UTF8))
        {
            writer.WriteLine(string.Join(',', headers));

            foreach (var record in records)
            {
                var values = headers
                    .Select(header => record.TryGetValue(header, out var value) ? EscapeCsv(value) : "");

                writer.WriteLine(string.Join(',', values));
            }
        }

        return csvFileName;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
