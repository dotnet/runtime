# ILLink.Tasks

ILLink.Tasks contains MSBuild tasks that run the linker for .NET Core. It runs illink.dll, built from the same [sources](../linker/) that are used to build monolinker.exe. ILLink.Tasks is shipped as part of the .NET Core 3.0 SDK.

Note: in previous versions of .NET Core, ILLink.Tasks was shipped as an external nuget package. This is no longer supported - please update to the latest 3.0 SDK and try the new experience!

## Usage

To use this tool, set `PublishTrimmed` to `true` in your project and publish a self-contained app:

```
dotnet publish -r <rid> -c Release
```

The publish output will include a subset of the framework libraries, depending on what the application code calls. For a "hello world" app, this reduces the size from ~68MB to ~28MB.

Applications or frameworks (including ASP<span />.NET Core and WPF) that use reflection or related dynamic features will often break when trimmed, because the linker does not know about this dynamic behavior, and can not determine in general which framework types will be required for reflection at runtime. To trim such apps, you will need to tell the linker about any types needed by reflection in your code, and in packages or frameworks that you depend on. Be sure to test your apps after trimming.

## How it works

The IL linker scans the IL of your application to detect which code is actually required, and trims unused framework libraries. This can significantly reduce the size of some apps. Typically small tool-like console apps benefit the most as they tend to use fairly small subsets of the framework, and are usually more amenable to trimming. Applications that use reflection may not work with this approach.

## Default behavior

By default, the linker will operate in a conservative mode that keeps
all managed assemblies that aren't part of the framework (they are
kept intact, and the linker simply copies them). This means that any reflection calls to non-framework code should continue to work. Reflection calls to code in the framework can potentially break if the target of the call is removed. Any framework assemblies that aren't predicted to be used at runtime will be removed from the publish output. Used framework assemblies will be kept entirely.

# Adding reflection roots

If your app or its dependencies use reflection, you may need to tell the linker to keep reflection targets explicitly. For example, dependency injection in ASP<span />.NET Core apps will activate
types depending on what is present at runtime, and therefore may fail
if the linker has removed assemblies that would otherwise be
present. Similarly, WPF apps may call into framework code depending on
the features used. If you know beforehand what your app will require
at runtime, you can tell the linker about this in a few ways.

For example, an app may reflect over `System.IO.File`:
```csharp
Type file = System.Type.GetType("System.IO.File,System.IO.FileSystem");
```

To ensure that this works with `PublishTrimmed=true`:

- You can include a direct reference to the required type in your code
  somewhere, for example by using `typeof(System.IO.File)`.

- You can tell the linker to explicitly keep an assembly by adding it
  to your csproj (use the assembly name *without* extension):

  ```xml
  <ItemGroup>
    <TrimmerRootAssembly Include="System.IO.FileSystem" />
  </ItemGroup>
  ```

- You can give the linker a more specific list of types/methods,
  etc. to include using an xml file, using the format described at
  http://github.com/mono/linker

  `.csproj`:
  ```xml
  <ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
  </ItemGroup>
  ```

  `TrimmerRoots.xml`:
  ```xml
  <linker>
    <assembly fullname="System.IO.FileSystem">
      <type fullname="System.IO.File" />
    </assembly>
  </linker>
  ```


# MSBuild task

The linker can be invoked as an MSBuild task, `ILLink`. We recommend not using the task directly, because the SDK has built-in logic that handles computing the right set of reference assemblies as inputs, incremental linking, and similar logic. If you would like to use the [advanced options](../linker/README.md), you can invoke the msbuild task directly and pass any extra arguments like this:

```xml
<ILLink AssemblyPaths="@(AssemblyFilesToLink)"
        RootAssemblyNames="@(LinkerRootAssemblies)"
        RootDescriptorFiles="@(LinkerRootDescriptors)"
        OutputDirectory="output"
        ExtraArgs="-t -c link -l none" />
```

For a full description of the inputs that this task supports, see the
comments in [LinkTask.cs](LinkTask.cs).


# Building

To build ILLink.Tasks:

```
linker> dotnet restore illink.sln
linker> dotnet pack illink.sln
```

To produce a package:
```
linker> ./eng/dotnet.{sh/ps1} pack illink.sln
```

In .NET Core 3.0, this package is shipped with the SDK.

# Caveats

The linker does not analyze reflection calls, so any reflection
targets outside of the kept assemblies will need to be rooted
explicitly (see above).

Sometimes an application may include multiple versions of the same
assembly. This may happen when portable apps include platform-specific
managed code, which gets placed in the `runtimes` directory of the
publish output. In such cases, the linker will pick one of the
duplicate assemblies to analyze. This means that dependencies of the
un-analyzed duplicates may not be included in the application, so you
may need to root such dependencies manually.
