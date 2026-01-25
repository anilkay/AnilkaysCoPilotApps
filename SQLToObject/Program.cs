using GitHub.Copilot.SDK;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.ComponentModel;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


var connectionString = configuration.GetConnectionString("DefaultConnection");
var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

var databaseName = connectionStringBuilder.InitialCatalog;



var getTables = AIFunctionFactory.Create(
    async ([Description("Database Name")] string databaseName) =>
    {
        var createTableScripts = new List<string>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Change database if specified
        if (!string.IsNullOrEmpty(databaseName))
        {
            await connection.ChangeDatabaseAsync(databaseName);
        }

        // Get all table names
        var tablesQuery = @"
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        var tables = new List<(string Schema, string Name)>();
        await using (var command = new SqlCommand(tablesQuery, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        // Generate CREATE TABLE script for each table
        foreach (var (schema, tableName) in tables)
        {
            var scriptQuery = $@"
                DECLARE @SQL NVARCHAR(MAX) = '';
                
                SELECT @SQL = 'CREATE TABLE [' + s.name + '].[' + t.name + '] (' + CHAR(13) +
                    STRING_AGG(
                        '    [' + c.name + '] ' + 
                        UPPER(tp.name) + 
                        CASE 
                            WHEN tp.name IN ('varchar', 'nvarchar', 'char', 'nchar') 
                                THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR) END + ')'
                            WHEN tp.name IN ('decimal', 'numeric') 
                                THEN '(' + CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR) + ')'
                            ELSE ''
                        END +
                        CASE WHEN c.is_nullable = 0 THEN ' NOT NULL' ELSE ' NULL' END +
                        CASE WHEN c.is_identity = 1 THEN ' IDENTITY(1,1)' ELSE '' END,
                        ',' + CHAR(13)
                    ) WITHIN GROUP (ORDER BY c.column_id) + CHAR(13) + ');'
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.columns c ON t.object_id = c.object_id
                INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                WHERE s.name = @Schema AND t.name = @TableName
                GROUP BY s.name, t.name;
                
                SELECT @SQL;";

            await using var scriptCommand = new SqlCommand(scriptQuery, connection);
            scriptCommand.Parameters.AddWithValue("@Schema", schema);
            scriptCommand.Parameters.AddWithValue("@TableName", tableName);

            var script = await scriptCommand.ExecuteScalarAsync();
            if (script != null && script != DBNull.Value)
            {
                createTableScripts.Add(script.ToString()!);
            }
        }

        return string.Join("\n\n", createTableScripts);
    },
    "get_tables",
    "Get CREATE TABLE scripts for all tables in the database"
);


string prompt = $@"Analyze the database tables in '{databaseName}' database and generate C# entity classes for each table.
Return the result as a JSON array with the following structure:
[
{{
""tableName"": ""Users"",
""entityContent"": ""public class Users {{ public int Id {{ get; set; }} public string Name {{ get; set; }} }}""
}}
]

Requirements:
- Use PascalCase for class and property names
- Include all columns as properties with appropriate C# types
- Use proper formatting and naming conventions
- Return ONLY the JSON array, no additional text""";


await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5-mini",
    Streaming = false,
    Tools = [getTables]
});

var sendAndWait=await session.SendAndWaitAsync( new MessageOptions()
{
    Prompt = prompt,
});

if(sendAndWait is null)
{
    Console.WriteLine("No response from AI model.");
    return;

}

var jsonResult = sendAndWait.Data.Content;
var entities = JsonSerializer.Deserialize<List<EntityTemplate>>(jsonResult);

if (entities is null || entities.Count == 0)
{
    Console.WriteLine("No entities generated.");
    return;
}

var entitiesDir = "Entities";
Directory.CreateDirectory(entitiesDir);


foreach (var entity in entities)
{
var filePath = $"Entities/{entity.TableName}.cs";
await File.WriteAllTextAsync(filePath, entity.EntityContent);
}

class EntityTemplate
{
    [JsonPropertyName("tableName")]
    public  string? TableName { get; set; }
    [JsonPropertyName("entityContent")]
    public  string? EntityContent { get; set; }
}