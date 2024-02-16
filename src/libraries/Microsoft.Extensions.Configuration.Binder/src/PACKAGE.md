## About

<!-- A description of the package and where one can find more documentation -->

Provides the functionality to bind an object to data in configuration providers for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to represent the configuration data as strongly-typed classes defined in the application code. To bind a configuration, use the [Microsoft.Extensions.Configuration.ConfigurationBinder.Get](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.configurationbinder.get) extension method on the `IConfiguration` object. To use this package, you also need to install a package for the [configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration#configuration-providers), for example, [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/) for the JSON provider.

The types contained in this assembly use Reflection at runtime which is not friendly with linking or AOT.  To better support linking and AOT as well as provide more efficient strongly-typed binding methods - this package also provides a source generator.  This generator is enabled by default when a project sets `PublishAot` but can also be enabled using `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>`.

## Key Features

<!-- The key features of this package -->

* Configuring existing type instances from a configuration section (Bind)
* Constructing new configured type instances from a configuration section (Get & GetValue)
* Generating source to bind objects from a configuration section without a runtime reflection dependency.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example shows how to bind a JSON configuration section to .NET objects.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Settings
{
    public string Server { get; set; }
    public string Database { get; set; }
    public Endpoint[] Endpoints { get; set; }
}

class Endpoint
{
    public string IPAddress { get; set; }
    public int Port { get; set; }
}

class Program
{
    static void Main()
    {
        // Build a configuration object from JSON file
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Bind a configuration section to an instance of Settings class
        Settings settings = config.GetSection("Settings").Get<Settings>();

        // Read simple values
        Console.WriteLine($"Server: {settings.Server}");
        Console.WriteLine($"Database: {settings.Database}");

        // Read nested objects
        Console.WriteLine("Endpoints: ");

        foreach (Endpoint endpoint in settings.Endpoints)
        {
            Console.WriteLine($"{endpoint.IPAddress}:{endpoint.Port}");
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
    "Endpoints": [
      {
        "IPAddress": "192.168.0.1",
        "Port": "80"
      },
      {
        "IPAddress": "192.168.10.1",
        "Port": "8080"
      }
    ]
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

You can add the following property to enable the source generator.  This requires a .NET 8.0 SDK or later.
```xml
<PropertyGroup>
  <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
</PropertyGroup>
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Configuration.ConfigurationBinder`
* `Microsoft.Extensions.Configuration.BinderOptions`

## Additional Documentation

<!-- Links to further documentation -->

* [Configuration in .NET](https://learn.microsoft.com/dotnet/core/extensions/configuration)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration)

## Related Packages

<!-- The related packages associated with this package -->
* [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
* [Microsoft.Extensions.Configuration.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions)
* [Microsoft.Extensions.Configuration.CommandLine](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine)
* [Microsoft.Extensions.Configuration.EnvironmentVariables](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables)
* [Microsoft.Extensions.Configuration.FileExtensions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.FileExtensions)
* [Microsoft.Extensions.Configuration.Ini](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Ini)
* [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json)
* [Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets)
* [Microsoft.Extensions.Configuration.Xml](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Xml)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.Binder is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
