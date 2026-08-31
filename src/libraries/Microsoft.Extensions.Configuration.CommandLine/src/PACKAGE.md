## About

<!-- A description of the package and where one can find more documentation -->

Command line configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from the command line arguments of your application. You can use [CommandLineConfigurationExtensions.AddCommandLine](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.commandlineconfigurationextensions.addcommandline) extension method on `IConfigurationBuilder` to add the command line configuration provider to the configuration builder.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example shows how to read application configuration from the command line. You can use a command like `dotnet run --InputPath "c:\fizz" --OutputPath "c:\buzz"` to run it.

```C#
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

## Additional Documentation

<!-- Links to further documentation -->

* [Command-line configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#command-line-configuration-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.commandline)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.CommandLine is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).