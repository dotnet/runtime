## About

`Microsoft.Extensions.Primitives` contains isolated types that are used in many places within console or ASP.NET Core applications using framework extensions.

## Key Features

* IChangeToken: An interface that represents a token that can notify when a change occurs. This can be used to trigger actions or invalidate caches when something changes. For example, the configuration and file providers libraries use this interface to reload settings or files when they are modified.
* StringValues: A struct that represents a single string or an array of strings. This can be used to efficiently store and manipulate multiple values that are logically a single value. For example, the HTTP headers and query strings libraries use this struct to handle multiple values for the same key.
* StringSegment: A struct that represents a substring of another string. This can be used to avoid allocating new strings when performing operations on parts of a string. For example, the configuration and logging libraries use this struct to parse and format strings.

## How to Use

#### IChangeToken with configuration example

```C#
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Create a configuration builder
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            // appsettings.json expected to have the following contents:
            // {
            //   "SomeKey": "SomeValue"
            // }
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        // Build the configuration
        IConfiguration configuration = configurationBuilder.Build();

        // Create a change token for the configuration
        IChangeToken changeToken = configuration.GetReloadToken();

        // Attach a change callback
        IDisposable changeTokenRegistration = changeToken.RegisterChangeCallback(state =>
        {
            Console.WriteLine("Configuration changed!");
            IConfigurationRoot root = (IConfigurationRoot)state;
            var someValue = root["SomeKey"]; // Access the updated configuration value
            Console.WriteLine($"New value of SomeKey: {someValue}");
        }, configuration);

        // go and update the value of the key SomeKey in appsettings.json.
        // The change callback will be invoked when the file is saved.
        Console.WriteLine("Listening for configuration changes. Press any key to exit.");
        Console.ReadKey();

        // Clean up the change token registration when no longer needed
        changeTokenRegistration.Dispose();
    }
}
```
#### StringValues example

```C#
using System;
using Microsoft.Extensions.Primitives;

namespace StringValuesSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a StringValues object from a single string or an array of strings
            StringValues single = "Hello";
            StringValues multiple = new string[] { "Hello", "World" };

            // Use the implicit conversion to string or the ToString method to get the values
            Console.WriteLine($"Single: {single}"); // Single: Hello
            Console.WriteLine($"Multiple: {multiple}"); // Multiple: Hello,World

            // Use the indexer, the Count property, and the IsNullOrEmpty method to access the values
            Console.WriteLine($"Multiple[1]: {multiple[1]}"); // Multiple[1]: World
            Console.WriteLine($"Single.Count: {single.Count}"); // Single.Count: 1
            Console.WriteLine($"Multiple.IsNullOrEmpty: {StringValues.IsNullOrEmpty(multiple)}"); // Multiple.IsNullOrEmpty: False

            // Use the Equals method or the == operator to compare two StringValues objects
            Console.WriteLine($"single == \"Hello\": {single == "Hello"}"); // single == "Hello": True
            Console.WriteLine($"multiple == \"Hello\": {multiple == "Hello"}"); // multiple == "Hello": False
       }
    }
}
```
## Main Types

The main types provided by this library are:

* `IChangeToken`
* `StringValues`
* `StringSegment`

## Additional Documentation

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/primitives)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.primitives)

## Related Packages

* [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)

## Feedback & Contributing

Microsoft.Extensions.Primitives is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).