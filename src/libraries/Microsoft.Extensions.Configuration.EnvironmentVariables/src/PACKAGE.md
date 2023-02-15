## About

Environment variables configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from environment variables. You can use [EnvironmentVariablesExtensions.AddEnvironmentVariables](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.environmentvariablesextensions.addenvironmentvariables) extension method on `IConfigurationBuilder` to add the environment variables configuration provider to the configuration builder.

For more information, see the documentation: [Environment variable configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider).

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
