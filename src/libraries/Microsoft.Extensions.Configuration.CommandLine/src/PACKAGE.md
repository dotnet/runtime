## About

Command line configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from the command line arguments of your application. You can use [CommandLineConfigurationExtensions.AddCommandLine](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.commandlineconfigurationextensions.addcommandline) extension method on `IConfigurationBuilder` to add the command line configuration provider to the configuration builder.

For more information, see the documentation: [Command-line configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#command-line-configuration-provider).

## Example

The following example shows how to read application configuration from the command line. You can use a command like `dotnet run --InputPath "c:\fizz" --OutputPath "c:\buzz"` to run it.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        // Build a configuration object from command line
        IConfiguration config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();
        
        // Read configuration values
        Console.WriteLine($"InputPath: {config["InputPath"]}");
        Console.WriteLine($"OutputPath: {config["OutputPath"]}");
    }
}
```
