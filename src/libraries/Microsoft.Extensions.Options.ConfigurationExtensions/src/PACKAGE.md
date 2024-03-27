## About

`Microsoft.Extensions.Options.ConfigurationExtensions` provides additional configuration-specific functionality related to Options.

## Key Features

* Extension methods for OptionsBuilder for configuration binding
* Extension methods for IServiceCollection for Options configuration
* ConfigurationChangeTokenSource<TOptions> for monitoring configuration changes

## How to Use

#### Options Configuration binding

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

class Program
{
    // appsettings.json contents:
    // {
    //   "MyOptions": {
    //     "Setting1": "Value1",
    //     "Setting2": "Value2"
    //   }
    // }

    static void Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        IServiceCollection services = new ServiceCollection();

        // Bind the configuration to MyOptions
        services.Configure<MyOptions>(configuration.GetSection("MyOptions"));

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Retrieve MyOptions using dependency injection
        var myOptions = serviceProvider.GetRequiredService<IOptions<MyOptions>>().Value;

        // Access the bound configuration values
        Console.WriteLine($"Setting1: {myOptions.Setting1}");
        Console.WriteLine($"Setting2: {myOptions.Setting2}");
    }
}

public class MyOptions
{
    public string Setting1 { get; set; }
    public string Setting2 { get; set; }
}

```

#### Monitoring options configuration changes

```csharp
// Assume we have a class that represents some options
public class MyOptions
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// appsettings.json contents:
// {
//   "MyOptions": {
//     "Name": "Alice",
//     "Age": 25
//   }
// }

// Assume we have a configuration object that contains some settings
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// We can use the ConfigurationChangeTokenSource to create a change token source for the options
var changeTokenSource = new ConfigurationChangeTokenSource<MyOptions>(config.GetSection("MyOptions"));

// We can register the change token source with the options monitor
services.AddOptions<MyOptions>()
    .Configure(options =>
    {
        // Configure the options with the configuration values
        config.GetSection("MyOptions").Bind(options);
    })
    .AddChangeTokenSource(changeTokenSource);

// Now we can inject the options monitor into any class that needs them
public class MyClass
{
    private readonly IOptionsMonitor<MyOptions> _optionsMonitor;

    public MyClass(IOptionsMonitor<MyOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public void DoSomething()
    {
        // Can access the current options value like this
        var options = _optionsMonitor.CurrentValue;
        var name = options.Name;
        var age = options.Age;
        // Do something with name and age

        // Can also register a callback to be notified when the options change
        _optionsMonitor.OnChange(newOptions =>
        {
            // Do something when the options change
        });
    }
}

```

## Main Types

The main types provided by this library are:

* `ConfigurationChangeTokenSource`
* `OptionsBuilderConfigurationExtensions`
* `OptionsConfigurationServiceCollectionExtensions`

## Additional Documentation

* [Conceptual documentation](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options)

## Related Packages

* [Microsoft.Extensions.Options](https://www.nuget.org/packages/Microsoft.Extensions.Options)
* [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
* [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

## Feedback & Contributing

Microsoft.Extensions.Options.ConfigurationExtensions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).