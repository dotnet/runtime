# Microsoft.Extensions.Configuration

`Microsoft.Extensions.Configuration` is combined with a core configuration abstraction under `Microsoft.Extensions.Configuration.Abstractions` that allows for building different kinds of configuration providers to retrieve key/value pair configuration values from in the form of `IConfiguration`. There are a number of built-in configuration provider implementations to read from environment variables, in-memory collections, JSON, INI or XML files. Aside from the built-in variations, there are more shipped libraries shipped by community for integration with various configuration service and other data sources.

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
