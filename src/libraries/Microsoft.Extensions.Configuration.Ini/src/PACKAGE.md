## About

<!-- A description of the package and where one can find more documentation -->

INI configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from [INI files](https://en.wikipedia.org/wiki/INI_file). You can use [IniConfigurationExtensions.AddIniFile](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iniconfigurationextensions.addinifile) extension method on `IConfigurationBuilder` to add INI configuration provider to the configuration builder.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
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

## Additional Documentation

<!-- Links to further documentation -->

* [INI configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#ini-configuration-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.ini)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.Ini is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).