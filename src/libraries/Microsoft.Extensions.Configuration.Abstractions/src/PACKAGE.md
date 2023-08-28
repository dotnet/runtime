## About

<!-- A description of the package and where one can find more documentation -->

Provides abstractions of key-value pair based configuration. Interfaces defined in this package are implemented by classes in [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/) and other configuration packages.

## Key Features

<!-- The key features of this package -->

*
*
*

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The example below shows a small code sample using this library and trying out the `ConfigurationKeyName` attribute available since .NET 6:

```cs
public class MyClass
{
    [ConfigurationKeyName("named_property")]
    public string NamedProperty { get; set; }
}
```

Given the simple class above, we can create a dictionary to hold the configuration data and use it as the memory source to build a configuration section:

```cs
var dic = new Dictionary<string, string>
{
    {"named_property", "value for named property"},
};

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(dic)
    .Build();

var options = config.Get<MyClass>();
Console.WriteLine(options.NamedProperty); // returns "value for named property"
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* [`Microsoft.Extensions.Configuration.IConfiguration`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)
* [`Microsoft.Extensions.Configuration.IConfigurationBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder)
* [`Microsoft.Extensions.Configuration.IConfigurationProvider`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider)
* [`Microsoft.Extensions.Configuration.IConfigurationRoot`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationroot)
* [`Microsoft.Extensions.Configuration.IConfigurationSection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationsection)

## Additional Documentation

<!-- Links to further documentation -->

* [Configuration in .NET](https://learn.microsoft.com/dotnet/core/extensions/configuration)
* [API documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration)

## Related Packages

<!-- The related packages associated with this package -->

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).