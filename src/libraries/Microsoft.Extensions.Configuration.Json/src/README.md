## About

JSON configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read your application's settings from a JSON file. You can use [JsonConfigurationExtensions.AddJsonFile](https://docs.microsoft.com/dotnet/api/microsoft.extensions.configuration.jsonconfigurationextensions.addjsonfile) extension method on `IConfigurationBuilder` to add the JSON configuration provider to the configuration builder.

For more information, see the documentation:

- [Configuration in .NET](https://docs.microsoft.com/dotnet/core/extensions/configuration)
- [Microsoft.Extensions.Configuration.Json namespace](https://docs.microsoft.com/dotnet/api/microsoft.extensions.configuration.json)

## Example

The following example shows how to read application settings from the JSON configuration file.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        // Build a configuration object from JSON file
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Get a configuration section
        IConfigurationSection section = config.GetSection("Settings");

        // Read simple values
        Console.WriteLine($"Server: {section["Server"]}");
        Console.WriteLine($"Database: {section["Database"]}");

        // Read a collection
        Console.WriteLine("Ports: ");
        IConfigurationSection ports = section.GetSection("Ports");

        foreach (IConfigurationSection child in ports.GetChildren())
        {
            Console.WriteLine(child.Value);
        }
    }
}
```

To run this example, include an `appsettings.json` file with the following content in your project:

```json
{
  "Settings": {
    "Server": "example.com",
    "Database": "Northwind",
    "Ports": [ 80, 81 ]
  }
}
```

You can include a configuration file using a code like this in your `.csproj` file:

```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```
