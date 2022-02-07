# Optimizing programs targeting Native AOT

The Native AOT compiler provides multiple switches to influence the compilation process. These switches control the code and metadata that the compiler generates and affect the runtime behavior of the compiled program.

To specify a switch, add a new property to your project file with one or more of the values below. For example, to specify the invariant globalization mode, add

```xml
  <PropertyGroup>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
```

under the `<Project>` node of your project file.

## Options related to library features

Native AOT supports enabling and disabling all [documented framework library features](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming-options#trimming-framework-library-features). For example, to remove globalization specific code and data, add a `<InvariantGlobalization>true</InvariantGlobalization>` property to your project. Disabling a framework feature (or enabling a minimal mode of the feature) can result in significant size savings.

🛈 Native AOT difference: The `EnableUnsafeBinaryFormatterSerialization` framework switch is already set to the optimal value of `false` (removing the support for [obsolete](https://github.com/dotnet/designs/blob/21b274dbc21e4ae54b7e4c5dbd5ef31e439e78db/accepted/2020/better-obsoletion/binaryformatter-obsoletion.md) binary serialization).

## Options related to trimming

The Native AOT compiler supports the [documented options](https://docs.microsoft.com/en-us/dotnet/core/deploying/trim-self-contained) for removing unused code (trimming). By default, the compiler tries to very conservatively remove some of the unused code.

🛈 Native AOT difference: The documented `PublishTrimmed` property is implied to be `true` when Native AOT is active.

By default, the compiler tries to maximize compatibility with existing .NET code at the expense of compilation speed and size of the output executable. This allows people to use their existing code that worked well in a fully dynamic mode without hitting issues caused by trimming. To read more about reflection, see the [Reflection in AOT mode](reflection-in-aot-mode.md) document.

🛈 Native AOT difference: the `TrimMode` of framework assemblies is set to `link` by default. To compile entire framework assemblies, use `TrimmerRootAssembly` to root the selected assemblies. It's not recommended to root the entire framework.

To enable more aggressive removal of unreferenced code, set the `<TrimMode>` property to `link`.

To aid in troubleshooting some of the most common problems related to trimming add `<IlcGenerateCompleteTypeMetadata>true</IlcGenerateCompleteTypeMetadata>` to your project. This ensures types are preserved in their entirety, but the extra members that would otherwise be trimmed cannot be used in runtime reflection. This mode can turn some spurious `NullReferenceExceptions` (caused by reflection APIs returning a null) caused by trimming into more actionable exceptions.

## Options related to metadata generation

* `<IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>`: this disables generation of stack trace metadata that provides textual names in stack traces. This is for example the text string one gets by calling `Exception.ToString()` on a caught exception. With this option disabled, stack traces will still be generated, but will be based on reflection metadata alone (they might be less complete).
* `<IlcTrimMetadata>true</IlcTrimMetadata>`: allows the compiler to remove reflection metadata from things that were not visible targets of reflection. By default, the compiler keeps metadata for everything that was compiled. With this option turned on, reflection metadata (and therefore reflection) will only be available for visible targets of reflection. Visible targets of reflection are things like assemblies rooted from the project file, RD.XML, ILLinkTrim descriptors, DynamicallyAccessedMembers annotations or DynamicDependency annotations.
* `<IlcDisableReflection>true</IlcDisableReflection>`: this completely disables the reflection metadata generation. Very basic reflection will still work (you can still use `typeof`, call `Object.GetType()`, compare the results, and query for basic properties such as `Type.IsValueType` or `Type.BaseType`), but most of the reflection stack will no longer work (no way to query/access methods and fields on types, or get names of types). This mode is experimental - more details in the [Reflection free mode](reflection-free-mode.md) document.

## Options related to code generation
* `<IlcOptimizationPreference>Speed</IlcOptimizationPreference>`: when generating optimized code, favor code execution speed.
* `<IlcOptimizationPreference>Size</IlcOptimizationPreference>`: when generating optimized code, favor smaller code size.
* `<IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>`: folds method bodies with identical bytes (method body deduplication). This makes your app smaller, but the stack traces might sometimes look nonsensical (unexpected methods might show up in the stack trace because the expected method had the same bytes as the unexpected method). Note: the current implementation of deduplication doesn't attempt to make the folding unobservable to managed code: delegates pointing to two logically different methods that ended up being folded together will compare equal.

## Special considerations for Linux/macOS

Debugging symbols (data about your program required for debugging) is by default part of native executable files on Unix-like operating systems. To minimize the size of your CoreRT-compiled executable, you can run the `strip` tool to remove the debugging symbols.

No action is needed on Windows since the platform convention is to generate debug information into a separate file (`*.pdb`).

## Advanced options 
* `<IlcSystemModule>classlibmodule</IlcSystemModule>`: Name of the module which contains basic classes. When specified, disable automatic referencing of the `System.Private.CoreLib` and other libraries. See https://github.com/MichalStrehovsky/zerosharp for example of usage.
