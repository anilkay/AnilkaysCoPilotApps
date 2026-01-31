namespace CopilotApps.Shared;

public static class EntityFileWriter
{
    public static async Task WriteEntitiesAsync(IEnumerable<EntityTemplate> entities, string directoryName = "Entities")
    {
        if (entities is null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        Directory.CreateDirectory(directoryName);

        foreach (var entity in entities)
        {
            if (string.IsNullOrWhiteSpace(entity.TableName) || string.IsNullOrWhiteSpace(entity.EntityContent))
            {
                continue;
            }

            var filePath = Path.Combine(directoryName, $"{entity.TableName}.cs");
            await File.WriteAllTextAsync(filePath, entity.EntityContent);
        }
    }
}
