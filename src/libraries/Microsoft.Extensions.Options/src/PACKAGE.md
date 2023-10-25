## About
`Microsoft.Extensions.Options` provides a strongly typed way of specifying and accessing settings using dependency injection and acts as a bridge between configuration, DI, and higher level libraries. This library is the glue for how an app developer uses DI to configure the behavior of a library like HttpClient Factory. This also enables user to get a strongly-typed view of their configuration.

Within this package, you'll find an options validation source generator that generates exceptionally efficient and optimized code for validating options.

## Key Features

* Offer the IValidateOptions interface for the validation of options, along with several generic ValidateOptions classes that implement this interface.
* OptionsBuilder to configure options.
* Provide extension methods for service collections and options builder to register options and validate options.
* Supply a set of generic ConfigureNamedOptions classes that implement the IConfigureNamedOptions interface for configuring named options.
* Provide a source generator that generates validation code for options.
* Options caching, managing and monitoring.

## How to Use

#### Options validation example

```C#
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Load the configuration and validate it
builder.Services.AddOptions<MyConfigOptions>()
            .Bind(builder.Configuration.GetSection(MyConfigOptions.MyConfig))
            .ValidateDataAnnotations();
var app = builder.Build();


// Declare the option class to validate
public class MyConfigOptions
{
    public const string MyConfig = "MyConfig";

    [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$")]
    public string Key1 { get; set; }
    [Range(0, 1000,
        ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Key2 { get; set; }
    public int Key3 { get; set; }
}
```

#### Using IValidateOptions to validate options

```C#
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configuration to validate
builder.Services.Configure<MyConfigOptions>(builder.Configuration.GetSection(
                                        MyConfigOptions.MyConfig));

// OPtions validation through the DI container
builder.Services.AddSingleton<IValidateOptions
                              <MyConfigOptions>, MyConfigValidation>();

var app = builder.Build();

public class MyConfigValidation : IValidateOptions<MyConfigOptions>
{
    public MyConfigOptions _config { get; private set; }

    public  MyConfigValidation(IConfiguration config)
    {
        _config = config.GetSection(MyConfigOptions.MyConfig)
            .Get<MyConfigOptions>();
    }

    public ValidateOptionsResult Validate(string name, MyConfigOptions options)
    {
        string? vor = null;
        var rx = new Regex(@"^[a-zA-Z''-'\s]{1,40}$");
        var match = rx.Match(options.Key1!);

        if (string.IsNullOrEmpty(match.Value))
        {
            vor = $"{options.Key1} doesn't match RegEx \n";
        }

        if ( options.Key2 < 0 || options.Key2 > 1000)
        {
            vor = $"{options.Key2} doesn't match Range 0 - 1000 \n";
        }

        if (_config.Key2 != default)
        {
            if(_config.Key3 <= _config.Key2)
            {
                vor +=  "Key3 must be > than Key2.";
            }
        }

        if (vor != null)
        {
            return ValidateOptionsResult.Fail(vor);
        }

        return ValidateOptionsResult.Success;
    }
}

```

#### Options Validation Source Generator Example

```C#
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

public class MyConfigOptions
{
    [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$")]
    public string Key1 { get; set; }

    [Range(0, 1000,
        ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Key2 { get; set; }
    public int Key3 { get; set; }
}

[OptionsValidator]
public partial class MyConfigValidation : IValidateOptions<MyConfigOptions>
{
    // Source generator will automatically provide the implementation of IValidateOptions
    // Then you can add the validation to the DI Container using the following code:
    //
    // builder.Services.AddSingleton<IValidateOptions
    //                          <MyConfigOptions>, MyConfigValidation>();
    // builder.Services.AddOptions<MyConfigOptions>()
    //        .Bind(builder.Configuration.GetSection(MyConfigOptions.MyConfig))
    //        .ValidateDataAnnotations();
}

```

## Main Types

The main types provided by this library are:

* `IOptions`, `IOptionsFactory`, and `IOptionsMonitor`
* `IValidateOptions` and `ValidateOptions`
* `OptionsBuilder`, `OptionsFactory`, `OptionsMonitor`, and `OptionsManager`
* `OptionsServiceCollectionExtensions`
* `OptionsValidatorAttribute`

## Additional Documentation

* [Conceptual documentation](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options)

## Related Packages

[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
[Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)

## Feedback & Contributing

Microsoft.Extensions.Options is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).