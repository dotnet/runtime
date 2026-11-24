## About

`Microsoft.Extensions.Logging.Configuration` provides configuration support for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/). It lets log level filter rules be read from an [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration) source such as `appsettings.json`, and lets logger providers bind their options from their own configuration section.

Applications built with the .NET Generic Host get this behavior automatically for the `Logging` configuration section, so the package is mainly used directly when configuring logging without the host, or when writing a custom logger provider.

## Key Features

* Load log level filter rules, globally and per logger provider, from configuration.
* Reload filter rules when the underlying configuration changes.
* Provide each logger provider with its configuration section through `ILoggerProviderConfiguration<T>`, so providers can bind provider-specific options such as console formatter settings.

## How to Use

The following example reads filter rules and console formatter options from configuration. The in-memory values correspond to this `appsettings.json` content:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Sample": "Information"
    },
    "Console": {
      "LogLevel": {
        "Sample": "Debug"
      },
      "FormatterName": "json",
      "FormatterOptions": {
        "TimestampFormat": "HH:mm:ss"
      }
    }
  }
}
```

```csharp
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

IConfiguration configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Sample"] = "Information",
        ["Logging:Console:LogLevel:Sample"] = "Debug",
        ["Logging:Console:FormatterName"] = "json",
        ["Logging:Console:FormatterOptions:TimestampFormat"] = "HH:mm:ss",
    })
    .Build();

using (ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
}))
{
    ILogger sampleLogger = loggerFactory.CreateLogger("Sample");
    sampleLogger.LogDebug("Written: the Console section allows Debug for the Sample category.");

    ILogger otherLogger = loggerFactory.CreateLogger("Other");
    otherLogger.LogInformation("Not written: the default minimum level is Warning.");
    otherLogger.LogWarning("Written: Warning meets the default minimum level.");
}
```

## Main Types

The main types provided by this library are:

* `Microsoft.Extensions.Logging.LoggingBuilderExtensions`
* `Microsoft.Extensions.Logging.Configuration.LoggingBuilderConfigurationExtensions`
* `Microsoft.Extensions.Logging.Configuration.ILoggerProviderConfiguration<T>`
* `Microsoft.Extensions.Logging.Configuration.ILoggerProviderConfigurationFactory`
* `Microsoft.Extensions.Logging.Configuration.LoggerProviderOptions`
* `Microsoft.Extensions.Logging.Configuration.LoggerProviderOptionsChangeTokenSource<TOptions, TProvider>`

## Additional Documentation

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/logging#configure-logging)
* [Implement a custom logging provider](https://learn.microsoft.com/dotnet/core/extensions/custom-logging-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.configuration)

## Related Packages

* [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
* [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
* [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
* [Microsoft.Extensions.Options.ConfigurationExtensions](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)

## Feedback & Contributing

Microsoft.Extensions.Logging.Configuration is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
