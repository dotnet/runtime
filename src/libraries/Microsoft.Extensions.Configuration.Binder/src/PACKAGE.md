## About

<!-- A description of the package and where one can find more documentation -->

Provides the functionality to bind an object to data in configuration providers for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to represent the configuration data as strongly-typed classes defined in the application code. To bind a configuration, use the [Microsoft.Extensions.Configuration.ConfigurationBinder.Get](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.configurationbinder.get) extension method on the `IConfiguration` object. To use this package, you also need to install a package for the [configuration provider](https://learn.microsoft.com/dotnet/core/extensions/configuration#configuration-providers), for example, [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/) for the JSON provider.

The types contained in this assembly use Reflection at runtime which is not friendly with linking or AOT.  To better support linking and AOT as well as provide more efficient strongly-typed binding methods - this package also provides a source generator.  This generator is enabled by default when a project sets `PublishAot` but can also be enabled using `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>`.

## Key Features

<!-- The key features of this package -->

* Configuring existing type instances from a configuration section (Bind)
* Constructing new configured type instances from a configuration section (Get & GetValue)
* Generating source to bind objects from a configuration section without a runtime reflection dependency.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

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

You can add the following property to enable the source generator.  This requires a .NET 8.0 SDK or later.
```xml
<PropertyGroup>
  <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
</PropertyGroup>
```

### Custom type conversion

When binding a configuration section that has a string value, the reflection-based binder honors a `TypeConverterAttribute` applied to the target property. The property-level converter takes precedence over the converter registered for the property's type.

Converters on virtual overrides are honored, while properties hidden with `new` retain their own converters. A property's converter also applies to a matching constructor parameter when the parameter and property have the same type. A default `TypeConverterAttribute`, an unresolved converter, or a converter that cannot convert from `string` preserves the binder's built-in conversion behavior.

```cs
class Settings
{
    [TypeConverter(typeof(TimeoutConverter))]
    public TimeSpan Timeout { get; set; }
}
```

Property-level converters in source-generated binding require a separate, explicit opt-in:

```xml
<PropertyGroup>
  <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
  <EnableConfigurationBindingGeneratorTypeConverters>true</EnableConfigurationBindingGeneratorTypeConverters>
</PropertyGroup>
```

`EnableConfigurationBindingGeneratorTypeConverters` defaults to `false`. Neither `PublishAot` nor `PublishTrimmed` enables it automatically. When it is unset or `false`, the generator continues to ignore property-level converter attributes, without introducing a new reflection fallback for those attributes. This preserves the previous generated binding behavior on upgrade; it does not change the reflection-based binder's behavior.

With the opt-in enabled, the generator calls statically resolved, accessible converters directly. It supports `typeof(...)` and resolvable metadata type names, and public parameterless constructors or constructors taking a `Type` argument by value. Use `typeof(...)` for closed generic converter types. If a converter has required members, the selected constructor must be annotated with `SetsRequiredMembers`. The converter itself must be compatible with trimming and Native AOT when used in those applications.

For statically known target types, both implementations apply the following rules:

* `IConfigurationSection` properties and matching constructor parameters receive the configuration section itself. Their converter is not constructed or called.
* `[ConfigurationIgnore]` on a virtual override excludes that property, including from constructor-parameter matching. A property hidden with `new` retains its own attributes.
* A getter-only override inherits its property's converter when used to bind a matching constructor parameter.
* If a property converter cannot convert from `string`, the binder tries the property's type-level converter before binding children. The generator resolves this fallback converter statically as well.
* A missing key preserves the existing property value. An explicitly present `null` resets a leaf whose converter supports `string`, without calling `ConvertFrom` with `null`. Converter construction and `CanConvertFrom` can still run. The existing special rule for appending to nonempty `byte[]` properties is unchanged.

With the opt-in enabled, generated binding uses `UnsafeAccessor` to call public `init` setters on .NET 8 or later. This supports both `Get` and `Bind` on an existing instance without discarding constructor-initialized values or bypassing setter logic. Generic declaring types require .NET 9 or later. Constructors of types with required members use a constructor accessor when needed to preserve their initial values and constructor normalization until binding supplies a replacement.

Generated conversion uses the declared property attributes, including virtual overrides. When binding through a base type or interface, it uses the metadata visible on that static type; it cannot discover different converter or ignore attributes on the runtime concrete type. It also does not honor runtime metadata changes through `TypeDescriptor` or custom type description providers. Applications that require dynamic metadata can disable configuration binding generation using `EnableConfigurationBindingGenerator=false`; doing so does not make reflection-based binding safe for trimming or Native AOT.

If an eligible converter or required accessor cannot be handled statically, a normal, non-trimmed build reports warning `SYSLIB1105` and leaves the affected binding call to the reflection-based binder. With `PublishAot=true` or `PublishTrimmed=true`, that diagnostic is an error instead. This includes inaccessible fallback converters and target frameworks without the required unsafe-accessor support. Properties excluded from binding do not trigger fallback. Converter-only target types that otherwise have no generated binding support require a section handled by their converter.

### Compatibility

These are behavioral changes for applications that already declare property-level converters or ignored overrides. Previously, an inherited getter-only converter could be skipped during constructor binding, an ignored override could still be bound, and an explicitly configured `null` could leave a property-converted reference value unchanged. Applications that depended on retaining such a default should omit the configuration key instead of explicitly setting it to `null`. Remove `[ConfigurationIgnore]` from an override if it is intended to participate in binding.

The generator's converter and accessor changes remain opt-in. Leaving `EnableConfigurationBindingGeneratorTypeConverters` unset or `false` preserves its previous converter and init-only behavior. Correct handling of `[ConfigurationIgnore]` overrides applies regardless of this switch.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Configuration.ConfigurationBinder`
* `Microsoft.Extensions.Configuration.BinderOptions`

## Additional Documentation

<!-- Links to further documentation -->

* [Configuration in .NET](https://learn.microsoft.com/dotnet/core/extensions/configuration)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration)

## Related Packages

<!-- The related packages associated with this package -->
* [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
* [Microsoft.Extensions.Configuration.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions)
* [Microsoft.Extensions.Configuration.CommandLine](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine)
* [Microsoft.Extensions.Configuration.EnvironmentVariables](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables)
* [Microsoft.Extensions.Configuration.FileExtensions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.FileExtensions)
* [Microsoft.Extensions.Configuration.Ini](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Ini)
* [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json)
* [Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets)
* [Microsoft.Extensions.Configuration.Xml](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Xml)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Configuration.Binder is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
