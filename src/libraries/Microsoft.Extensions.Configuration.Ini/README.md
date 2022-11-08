# Microsoft.Extensions.Configuration.Ini

INI configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from [INI files](https://en.wikipedia.org/wiki/INI_file). You can use [IniConfigurationExtensions.AddIniFile](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iniconfigurationextensions.addinifile) extension method on `IConfigurationBuilder` to add INI configuration provider to the configuration builder.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#ini-configuration-provider

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.Ini](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Ini/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.

## Example
The following example shows how to read the application configuration from INI file.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        // Build a configuration object from INI file
        IConfiguration config = new ConfigurationBuilder()
            .AddIniFile("appsettings.ini")
            .Build();

        // Get a configuration section
        IConfigurationSection section = config.GetSection("Settings");

        // Read configuration values
        Console.WriteLine($"Server: {section["Server"]}");
        Console.WriteLine($"Database: {section["Database"]}");
    }
}
```

To run this example, include an `appsettings.ini` file with the following content in your project:

```
[Settings]
Server=example.com
Database=Northwind
```

You can include a configuration file using a code like this in your `.csproj` file:

```xml
<ItemGroup>
  <Content Include="appsettings.ini">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```
