# Microsoft.Extensions.Configuration.Binder

Provides the functionality to bind an object to data in configuration providers for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/).

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.Binder](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.

## Example
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
