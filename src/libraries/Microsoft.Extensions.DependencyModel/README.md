# Microsoft.Extensions.DependencyModel

`Microsoft.Extensions.DependencyModel` provides abstractions for reading .deps files. When a .NET application is compiled, the SDK generates a JSON manifest file (<ApplicationName>.deps.json) that contains information about application dependencies. You can use `Microsoft.Extensions.DependencyModel` to read information from this manifest at run time. This is useful when you want to dynamically compile code (for example, using Roslyn Emit API) referencing the same dependencies as your main application.

By default, the dependency manifest contains information about the application's target framework and runtime dependencies. Set the PreserveCompilationContext project property to true to additionally include information about reference assemblies used during compilation.

For more information, see the documentation:

- .deps.json file format
- Microsoft.Extensions.DependencyModel namespace
- Microsoft.Extensions.DependencyModel.DependencyContext

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

# Example

The following example shows how to display the list of assemblies used when compiling the current application. Include `<PreserveCompilationContext>true</PreserveCompilationContext>` in your project file to run this example.

```c#
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

## Deployment
[Microsoft.Extensions.DependencyModel](https://www.nuget.org/packages/Microsoft.Extensions.DependencyModel) is deployed as out-of-band (OOB) too and can be referenced into projects directly.