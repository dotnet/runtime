# System.Text.Json Source Package

## Overview

* To support consumption of the `System.Text.Json` library outside of .NET Core 3.0+, we have produced a source package, `Microsoft.Bcl.Json.Sources`.
  - This package is unsupported and is only meant for advanced scenarios. Ideally, the project can target .NET Core 3.0 and get the inbox library.
  - It is intentionally not easy to consume such a package since we avoided any tooling to predict the users intention. For simplicity, treat this like
    any other source within your application or library. 

## Common Known Issues and Workarounds

### Language Version

* The `System.Text.Json` sources are built using C# 7.3 and hence you will likely see the following errors:
  - `error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.`
  - `error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.`
  - `error CS8320: Feature 'extensible fixed statement' is not available in C# 7.2. Please use language version 7.3 or greater.`

* Therefore, it is recommended that you update your compiler to one that supports C# 7.3+ and add the following attribute to your project:
  - `<LangVersion>latest</LangVersion>` OR `<LangVersion>7.3</LangVersion>`

### Unsafe Code

* The `System.Text.Json` sources are built using some unsafe code and hence you will likely see the following error:
  - `error CS0227: Unsafe code may only appear if compiling with /unsafe`

* Therefore, you will need the following attribute in your project (or something equivalent):
  - `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`

### Missing Package References

* The `System.Text.Json` sources depend on the following packages:
  - `System.Memory`
  - `System.Runtime.CompilerServices.Unsafe`
  - `System.Buffers`
  - `System.Numerics.Vectors`

* You will likely see errors like the following when you don't have the required packages referenced in your application:
  - `error CS0103: The name 'Unsafe' does not exist in the current context`
  - `error CS0246: The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)`

* Therefore, you will need to add the following package references in your project:
  - `<PackageReference Include="System.Runtime.CompilerServices.Unsafe" Version="..." />`
  - `<PackageReference Include="System.Memory" Version=..." />`

### CLS Compliance

* The `System.Text.Json` sources are built as part of a CLS Compliant library. Hence, they contain the `CLSCompliant` attributes in certain places.
  You might see warnings like the following (or errors if warnings are treated as errors):
  - `warning CS3021: 'Utf8JsonWriter.WriteNumber(string, ulong, bool)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute`

* You could either mark your project as CLSCompliant, or opt out of this warning, or manually modify the sources to remove that attribute where it is used:
  - `<NoWarn>3021</NoWarn>` OR `<CLSCompliant>true</CLSCompliant>`

### Targeting .NET Core 3.0+

* The `System.Text.Json` library is built as part of .NET Core 3.0. If you reference this source package (unconditionally) and choose to target netcoreapp3.0 or higher,
  you might see lots of type confligt warnings (or errors if warnings are treated as errors):
  - `warning CS0436: The type 'JsonTokenType' in '...' conflicts with the imported type 'JsonTokenType' in 'System.Text.Json, Version=...'. Using the type defined in '...\microsoft.bcl.json.sources\...\JsonTokenType.cs'.`

* You should not reference this package if you are targeting .NET Core 3.0 or higher. If you are multi-targeting, then only conditionally include the package reference.
  Note that this applies to other package references that you include as part of the source package dependency.

### PrivateAssets

* It is recommended that you mark your package reference as private so that this dependency does not accidentally leak outside of your package
  to applications that might be consuming it (for example, visible within the deps.json).
  ```csproj
  <PackageReference Include="Microsoft.Bcl.Json.Sources" Version="...">
     <PrivateAssets>All</PrivateAssets>
  </PackageReference>
  ```

## Other Considerations

### InternalsVisibleTo

* Since the source package contains types marked as internal, please be intentional with the use of `InternalsVisibleTo`.
  It is acceptable to use it where necessary but something we wanted to highlight as an area of consideration.