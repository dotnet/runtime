## About

<!-- A description of the package and where one can find more documentation -->

`Microsoft.Extensions.Configuration` is combined with a core configuration abstraction under `Microsoft.Extensions.Configuration.Abstractions` that allows for building different kinds of configuration providers to retrieve key/value pair configuration values from in the form of `IConfiguration`. There are a number of built-in configuration provider implementations to read from environment variables, in-memory collections, JSON, INI or XML files. Aside from the built-in variations, there are more shipped libraries shipped by community for integration with various configuration service and other data sources.

## Key Features

<!-- The key features of this package -->

* In-memory configuration provider
* Chained configuration provider for chaining multiple confiugration providers together.
* Base types that implement configuration abstraction interfaces that can be used when implementing other configuration providers.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
using Microsoft.Extensions.Configuration;

var configurationBuilder = new ConfigurationBuilder();

configurationBuilder.AddInMemoryCollection(
    new Dictionary<string, string?>
    {
        ["Setting1"] = "value",
        ["MyOptions:Enabled"] = bool.TrueString,
    });

configurationBuilder.AddInMemoryCollection(
    new Dictionary<string, string?>
    {
        ["Setting2"] = "value2",
        ["MyOptions:Enabled"] = bool.FalseString,
    });

var config = configurationBuilder.Build();

// note case-insensitive
Console.WriteLine(config["setting1"]);
Console.WriteLine(config["setting2"]);

// note last in wins
Console.WriteLine(config["MyOptions:Enabled"]);
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Configuration.ConfigurationBuilder`
* `Microsoft.Extensions.Configuration.ConfigurationManager`
* `Microsoft.Extensions.Configuration.ConfigurationRoot`
* `Microsoft.Extensions.Configuration.ConfigurationSection`

## Additional Documentation

<!-- Links to further documentation -->

- [Configuration in .NET](https://learn.microsoft.com/dotnet/core/extensions/configuration)
- [Microsoft.Extensions.Configuration namespace](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration)

## Related Packages

<!-- The related packages associated with this package -->
* [Microsoft.Extensions.Configuration.Binder](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder)
* [Microsoft.Extensions.Configuration.CommandLine](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine)
* [Microsoft.Extensions.Configuration.EnvironmentVariables](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables)
* [Microsoft.Extensions.Configuration.FileExtensions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.FileExtensions)
* [Microsoft.Extensions.Configuration.Ini](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Ini)
* [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json)
* [Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets)
* [Microsoft.Extensions.Configuration.Xml](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Xml)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
