# Microsoft.Extensions.Configuration

`Microsoft.Extensions.Configuration` is combined with a core configuration abstraction under `Microsoft.Extensions.Configuration.Abstractions` that allows for building different kinds of configuration providers to retrieve key/value pair configuration values from in the form of `IConfiguration`.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration.

## Configuration references

A value that is exactly `$ref(Key:Path)` reads as the value of the referenced key. References resolve on each read and do not expose the target's children.

Quotes protect reference-expression syntax, not the configuration hierarchy: `$ref('Key:Path')` still names the path `Key:Path`. A doubled dollar sign is not an escape; `$$ref(Key:Path)` is returned unchanged.

Each resolution permits at most 64 reference hops, 32 levels of sub-reference nesting, and 32 substitutions per expression. It also permits at most 4,096 key reads across all branches, including repeated reads and the final target lookup. Exceeding a limit throws `InvalidOperationException`.

Set the `Microsoft.Extensions.Configuration.DisableConfigurationReferences` AppContext switch to `true` to return provider values without resolving references.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
