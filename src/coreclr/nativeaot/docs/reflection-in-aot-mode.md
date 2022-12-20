# Reflection in AOT mode #

When .NET code is compiled ahead of time, a typical problem the ahead of time compiler faces is deciding what code to compile and what data structures to generate.

For static languages such as C or C++, the problem of deciding what to include in the final executable is quite simple: one starts with including `main()` and establishing what other methods and data structures `main()` references. One then includes those references, the references of the references and so on, until there's no reference left to include. This concept is easy to understand and works great for languages like C or C++. Nice side effect of this approach is that the generated program is small. If the code doesn't call into e.g. the `printf` function, the `printf` function is not generated in the final executable.

Problems with such approach start to show up on platforms that allow unconstrained reflection. Reflection is a mechanism .NET provides that allows developers to inspect the structure of the program at runtime and access/invoke types and their members. With unconstrained reflection, the definition of "program" includes "everything that one would have access to at the time of compiling the program".

As a motivating example, consider this program:

```csharp
class Program
{
    public static void Main()
    {
        Console.Write("Name of type: ");
        string typeName = Console.ReadLine();

        // Allow to exit the program peacefully
        if (String.IsNullOrEmpty(typeName))
            return;

        Console.Write("Name of method: ");
        string methodName = Console.ReadLine();

        Type.GetType(typeName).GetMethod(methodName).Invoke(null, null);
    }

    public static void SayHello()
    {
        Console.WriteLine("Hello!");
    }
}
```

The above program lets the user invoke any parameterless public static method on any type. For the naive compilation algorithm above, this program would work great for input strings `Program` and `Main` because the algorithm included method `Main` in the final executable. The program wouldn't work so great for inputs `Program` and `SayHello`†, because method `SayHello` wasn't called from anywhere. For the naive algorithm, the only way to fix the program for inputs `Program` and `SayHello` is to add a call to `SayHello` in `Main`.

> † The behavior for `Type.GetMethod` on type `Program` and method `SayHello` would be to return `null` if `SayHello` wasn't compiled. The reason for this is that `Type.GetMethod` is documented to return `null` if there's no method with a given name, and for the purposes of the program, `SayHello` doesn't exist. The compiler could remember there used to be such method and write the information in the executable, but that would raise additional questions about whether the uncompiled method should be included in the list of methods returned from a `Type.GetMethods` call.

While the example program above is not practical in reality, similar patterns exist in e.g. reflection based serialization and deserialization libraries that access members based on their names that could be literally downloaded from the internet.

The dynamic nature of reflection doesn't pose a problem just for fully AOT .NET Runtimes. It's also a problem when tools such as [IL linker](https://github.com/dotnet/core/blob/master/samples/linker-instructions.md) are used to remove unnecessary code. The desire to remove unused code is stronger in fully AOT mode, since native code comes with a greater multiplicative factor (IL instructions are more compact than native instructions).

## Solving reflection in full AOT mode ##

The solution to reflection is about establishing what parts of the program can be reached dynamically and making sure their metadata and code is available at runtime.

### Assume everything is accessed dynamically ###

The compiler can simply assume that everything can be accessed dynamically. This means that everything will be compiled and available at runtime. This is the safest possible option, but results in big executables and long compilation times. "Everything" includes all of .NET Core framework code, including things like support for FTP or WCF. An app is unlikely to be relying on all of that.

### Assume non-framework code is accessed dynamically ###

The compiler can make an assumption that everything that is not part of .NET framework can be accessed dynamically. Unused parts of the framework will not be available for reflection, but real world programs rarely reflect on them. This option still produces executables that are pretty big (especially with many NuGet packages referenced), but their size is more practical.

### Assume statically reachable code is accessed dynamically ###

This is the algorithm we discussed above - only things that are reachable through the static callgraph will be available for reflection.

### Assume code computed by static analysis is accessed dynamically ###

The compiler can build insights into how reflection is used by analyzing the use of reflection APIs within the compiled program and using data flow analysis to see what elements are reflected on. This is effective for a lot of patterns (such as `typeof(Foo).GetMethod("Bar")`), but can also miss a lot of reflection use in practice.

### Assume nothing is accessed dynamically ###

In NativeAOT, reflection metadata (names of types, list of their methods, fields, signatures, etc.) is _optional_. The NativeAOT runtime has its own minimal version of the metadata that represents the minimum required to execute managed code (think: base type and list of interfaces, offsets to GC pointers within an instance of the type, pointer to the finalizer, etc.). The metadata used by the reflection subsystem within the base class libraries is only used by the reflection stack and is not necessary to execute non-reflection code. For a .NET app that doesn't use reflection, the compiler can skip generating the reflection metadata completely. People who would like to totally minimize the size of their applications or obfuscate their code could be interested in this option, although not much existing real world code would be expected to work with this (including a lot of the framework code).

## Providing hints to the compiler externally ##

If compiler cannot detect types used by the aplication, an rd.xml file can be supplemented to help ILCompiler find types that should be analyzed.
For that, file `rd.xml` should be created and following lines added to project file
```xml
<ItemGroup>
  <RdXmlFile Include="rd.xml" />
</ItemGroup>
```

Format of the file described [here](rd-xml-format.md)

## Shimming

Native AOT libraries have configuration settings (shims) that enable replacing some of frequently used reflection patterns that are incompatible with Native AOT with compatible equivalents that approximate their functionality, without changing the source code. The shim settings documented in this section are meant to be used as temporary unreliable workarounds until the permanent source code fix can be made. They are not guaranteed to make the application work correctly.

### Simulated calling assembly

`Assembly.GetCallingAssembly` is not supported in Native AOT and throws `PlatformNotSupportedException` by default.

`Assembly.GetCallingAssembly` can be in certain situations simulated by `Assembly.GetEntryAssembly`.

To enable simulated `Assembly.GetCallingAssembly`, you will need:

```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Assembly.SimulatedCallingAssembly" Value="true" />
  </ItemGroup>
```

## Experimental Reflection Free Mode

Reflection-free mode is a an experimental mode of the NativeAOT compiler and runtime that greatly reduces the functionality of the reflection APIs and demonstrates how far reflection trimming can get. See [Reflection Free Mode](reflection-free-mode.md) for mode details.
