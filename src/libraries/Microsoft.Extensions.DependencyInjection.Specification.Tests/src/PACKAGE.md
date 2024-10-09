## About

Provides a suite of xUnit.net tests designed to ensure compatibility with the `Microsoft.Extensions.DependencyInjection` framework.

This package is intended for developers implementing their own Dependency Injection (DI) containers, allowing them to verify that their implementations conform to the expected behavior of the Microsoft Dependency Injection framework.

## Key Features

<!-- The key features of this package -->

* Comprehensive suite of tests to validate DI container compatibility with the Microsoft Dependency Injection framework.
* Includes fakes and mocks (`Microsoft.Extensions.DependencyInjection.Specification.Fakes`) for testing common scenarios.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

To utilize the `Microsoft.Extensions.DependencyInjection.Specification.Tests` in a custom DI container implementation, create a test project and include this package as a dependency.
Then, run the specification tests against the DI container to validate its compatibility with the `Microsoft.Extensions.DependencyInjection` framework.

1. Start by creating a test project to run the specification tests.
2. Add the following NuGet packages to the test project:

    * Microsoft.Extensions.DependencyInjection.Specification.Tests
    * xunit

3. Add the custom DI container package (e.g., `MyCustomDI`)
4. In the test project, create a class that inherits from `DependencyInjectionSpecificationTests` provided by the `Microsoft.Extensions.DependencyInjection.Specification.Tests` package.

   Override the `CreateServiceProvider` method to return an instance of the DI container's service provider.

   ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Specification.Tests;
    using Xunit;
    using MyCustomDI;

    public class MyCustomDIContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            // Create an instance of your custom DI container and build the service provider
            return MyCustomDIContainer.BuildServiceProvider(serviceCollection);
        }
    }
    ```

5. Run the tests to validate that the custom DI container behaves according to the Microsoft Dependency Injection specifications.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.DependencyInjection.Specification.DependencyInjectionSpecificationTests`
* `Microsoft.Extensions.DependencyInjection.Specification.KeyedDependencyInjectionSpecificationTests`

<!-- ## Additional Documentation -->

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.DependencyInjection.Specification.Tests is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
