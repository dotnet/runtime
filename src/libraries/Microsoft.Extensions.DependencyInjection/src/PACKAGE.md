## About
Supports the dependency injection (DI) software design pattern which is a technique for achieving Inversion of Control (IoC) between classes and their dependencies.

## Key Features
Provides an implementation of the DI interfaces found in the `Microsoft.Extensions.DependencyInjection.Abstractions` package.

## How to Use
```cs
ServiceCollection services = new ();
services.AddSingleton<IMessageWriter, MessageWriter>();
using ServiceProvider provider = services.BuildServiceProvider();

// The code below, following the IoC pattern, is typically only aware of the IMessageWriter interface, not the implementation.
IMessageWriter messageWriter = provider.GetService<IMessageWriter>()!;
messageWriter.Write("Hello");

public interface IMessageWriter
{
    void Write(string message);
}

internal class MessageWriter : IMessageWriter
{
    public void Write(string message)
    {
        Console.WriteLine($"MessageWriter.Write(message: \"{message}\")");
    }
}
```

## Main Types
The main types provided by this library are:
* `Microsoft.Extensions.DependencyInjection.DefaultServiceProviderFactory`
* `Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions`
* `Microsoft.Extensions.DependencyInjection.ServiceProvider`

## Additional Documentation
* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
* API documentation
  - [DefaultServiceProviderFactory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.defaultserviceproviderfactory)
  - [ServiceCollectionContainerBuilderExtensions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectioncontainerbuilderextensions)
  - [ServiceProvider](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovider)

## Related Packages
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Options`

## Feedback & Contributing
Microsoft.Extensions.DependencyInjection is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
