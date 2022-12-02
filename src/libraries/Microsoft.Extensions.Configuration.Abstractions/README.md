# Microsoft.Extensions.Configuration.Abstractions

Provides abstractions of key-value pair based configuration. Interfaces defined in this package are implemented by classes in [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/) and other configuration packages.

Commonly used types:

- [Microsoft.Extensions.Configuration.IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)
- [Microsoft.Extensions.Configuration.IConfigurationBuilder](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder)
- [Microsoft.Extensions.Configuration.IConfigurationProvider](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider)
- [Microsoft.Extensions.Configuration.IConfigurationRoot](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationroot)
- [Microsoft.Extensions.Configuration.IConfigurationSection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfigurationsection)

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration

## Example

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

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
