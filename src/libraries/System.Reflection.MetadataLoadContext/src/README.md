## About

Provides read-only reflection on assemblies in an isolated context with support for assemblies that target different processor architectures and runtimes. Using MetadataLoadContext enables you to inspect assemblies without loading them into the main execution context. Assemblies in MetadataLoadContext are treated only as metadata, that is, you can read information about their members, but cannot execute any code contained in them.

For more information, see the documentation:

- [How to: Inspect assembly contents using MetadataLoadContext](https://docs.microsoft.com/dotnet/standard/assembly/inspect-contents-using-metadataloadcontext)
- [System.Reflection.MetadataLoadContext](https://docs.microsoft.com/dotnet/api/system.reflection.metadataloadcontext)
- [System.Reflection.MetadataAssemblyResolver](https://docs.microsoft.com/dotnet/api/system.reflection.metadataassemblyresolver)

## Example

The following example shows how to print the list of types defined in an assembly.

```cs
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        string inspectedAssembly = "Example.dll";
        var resolver = new PathAssemblyResolver(new string[] {inspectedAssembly, typeof(object).Assembly.Location});
        using var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().ToString());

        // Load assembly into MetadataLoadContext
        Assembly assembly = mlc.LoadFromAssemblyPath(inspectedAssembly);
        AssemblyName name = assembly.GetName();

        // Print types defined in assembly
        Console.WriteLine($"{name.Name} has following types: ");

        foreach (Type t in assembly.GetTypes())
        {
            Console.WriteLine(t.FullName);
        }
    }
}
```
