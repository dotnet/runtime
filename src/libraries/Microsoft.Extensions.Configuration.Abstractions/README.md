# Microsoft.Extensions.Configuration.Abstractions

Provides abstractions of key-value pair based configuration.

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
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
