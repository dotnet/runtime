# Microsoft.Extensions.Configuration.EnvironmentVariables

Environment variables configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/).

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.EnvironmentVariables](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.

## Example
The following example shows how to read application configuration from environment variables.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        // Build a configuration object from environment variables
        IConfiguration config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        
        // Read configuration values
        Console.WriteLine($"Server: {config["Server"]}");
        Console.WriteLine($"Database: {config["Database"]}");
    }
}
```
