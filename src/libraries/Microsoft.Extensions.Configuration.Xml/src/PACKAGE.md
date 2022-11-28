## About

XML configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from XML files. You can use [XmlConfigurationExtensions.AddXmlFile](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.xmlconfigurationextensions.addxmlfile) extension method on `IConfigurationBuilder` to add XML configuration provider to the configuration builder.

For more information, see the documentation: [XML configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#xml-configuration-provider).

## Example
The following example shows how to read the application configuration from XML file.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        // Build a configuration object from XML file
        IConfiguration config = new ConfigurationBuilder()
            .AddXmlFile("appsettings.xml")
            .Build();

        // Get a configuration section
        IConfigurationSection section = config.GetSection("Settings");

        // Read simple values
        Console.WriteLine($"Server: {section["Server"]}");
        Console.WriteLine($"Database: {section["Database"]}");

        // Read nested values
        Console.WriteLine($"IPAddress: {section["Endpoint:IPAddress"]}");
        Console.WriteLine($"Port: {section["Endpoint:Port"]}");
    }
}
```

To run this example, include an `appsettings.xml` file with the following content in your project:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <Settings>
    <Server>example.com</Server>
    <Database>Northwind</Database>
    <Endpoint>
      <IPAddress>192.168.0.10</IPAddress>
      <Port>80</Port>
    </Endpoint>
  </Settings>  
</configuration>
```

You can include a configuration file using a code like this in your `.csproj` file:

```xml
<ItemGroup>
  <Content Include="appsettings.xml">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```
