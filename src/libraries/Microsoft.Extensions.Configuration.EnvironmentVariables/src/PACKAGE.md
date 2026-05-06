## About

<!-- A description of the package and where one can find more documentation -->

Environment variables configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to read configuration parameters from environment variables. You can use [EnvironmentVariablesExtensions.AddEnvironmentVariables](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.environmentvariablesextensions.addenvironmentvariables) extension method on `IConfigurationBuilder` to add the environment variables configuration provider to the configuration builder.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

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

## Additional Documentation

<!-- Links to further documentation -->

* [Environment variable configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.environmentvariables)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.EnvironmentVariables is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).