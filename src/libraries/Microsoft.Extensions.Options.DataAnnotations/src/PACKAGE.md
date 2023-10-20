## About

<!-- A description of the package and where one can find more documentation -->

Microsoft.Extensions.Options.DataAnnotations is a library that adds extra validation functionality to configuration options using data annotations.

It allows to apply validation rules to configuration classes to ensure they are correctly configured before the application starts running.

This way, misconfiguration issues are catched early during the application startup rather than facing them later in production.

## Key Features

<!-- The key features of this package -->

* Enables validation of configuration options using data annotations.
* Early detection of misconfiguration issues during application startup.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

While configuring services, chain the `ValidateDataAnnotations()` and `ValidateOnStart()` methods to the `AddOptions` method for your configuration class.

Here is a simple example demonstrating how to validate options on application startup:

```csharp
services
    .AddOptions<MyOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

In the configuration class, use data annotations to specify the validation rules.

For instance, in the following `MyOptions` class, the  `Name` property is marked as required:

```csharp
using System.ComponentModel.DataAnnotations;

public class MyOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }
}
```

With this setup, an error indicating that the `Name` field is required will be thrown upon startup if it hasn't been configured.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Options.DataAnnotationsValidateOptions<TOptions>`
* `Microsoft.Extensions.DependencyInjection.OptionsBuilderDataAnnotationsExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/options)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.dataannotationvalidateoptions-1)

## Related Packages

<!-- The related packages associated with this package -->

Core options: [Microsoft.Extensions.Options](https://www.nuget.org/packages/Microsoft.Extensions.Options/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Options.DataAnnotations is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
