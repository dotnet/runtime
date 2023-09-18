## About

<!-- A description of the package and where one can find more documentation -->

JSON configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read your application's settings from a JSON file. You can use [JsonConfigurationExtensions.AddJsonFile](https://docs.microsoft.com/dotnet/api/microsoft.extensions.configuration.jsonconfigurationextensions.addjsonfile) extension method on `IConfigurationBuilder` to add the JSON configuration provider to the configuration builder.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

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

## Additional Documentation

<!-- Links to further documentation -->

* [JSON configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#json-configuration-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.json)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.Json is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).