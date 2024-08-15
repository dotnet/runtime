## About

<!-- A description of the package and where one can find more documentation -->

Provides abstractions for reading `.deps` files. When a .NET application is compiled, the SDK generates a JSON manifest file (`<ApplicationName>.deps.json`) that contains information about application dependencies. You can use `Microsoft.Extensions.DependencyModel` to read information from this manifest at run time. This is useful when you want to dynamically compile code (for example, using Roslyn Emit API) referencing the same dependencies as your main application.

By default, the dependency manifest contains information about the application's target framework and runtime dependencies. Set the [PreserveCompilationContext](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#preservecompilationcontext) project property to `true` to additionally include information about reference assemblies used during compilation.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example shows how to display the list of assemblies used when compiling the current application. Include `<PreserveCompilationContext>true</PreserveCompilationContext>` in your project file to run this example.

```cs
using System;
using Microsoft.Extensions.DependencyModel;

class Program
{
    static void Main()
    {
        Console.WriteLine("Compilation libraries:");
        Console.WriteLine();

        foreach (CompilationLibrary lib in DependencyContext.Default.CompileLibraries)
        {
            foreach (string path in lib.ResolveReferencePaths())
            {
                Console.WriteLine(path);
            }
        }
    }
}
```

## Additional Documentation

<!-- Links to further documentation -->

* [.deps.json file format](https://github.com/dotnet/sdk/blob/main/documentation/specs/runtime-configuration-file.md#appnamedepsjson)
* [Microsoft.Extensions.DependencyModel namespace](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel)
* [Microsoft.Extensions.DependencyModel.DependencyContext](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel.dependencycontext)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.DependencyModel is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).