using Microsoft.Extensions.Configuration;

namespace CopilotApps.Shared;

public static class AppConfiguration
{
    public static IConfiguration LoadAppSettings(string fileName = "appsettings.json", bool optional = false)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(fileName, optional: optional, reloadOnChange: true)
            .Build();
    }
}
