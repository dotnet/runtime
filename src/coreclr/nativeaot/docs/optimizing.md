# Optimizing programs targeting Native AOT

The Native AOT compiler provides multiple switches to influence the compilation process. These switches control the code and metadata that the compiler generates and affect the runtime behavior of the compiled program.

To specify a switch, add a new property to your project file with one or more of the values below. For example, to specify the invariant globalization mode, add

```xml
  <PropertyGroup>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
```

under the `<Project>` node of your project file.

## Options related to trimming

The Native AOT compiler supports the [documented options](https://docs.microsoft.com/en-us/dotnet/core/deploying/trim-self-contained) for removing unused code (trimming). By default, the compiler tries to very conservatively remove some of the unused code.

:information_source: Native AOT difference: The documented `PublishTrimmed` property is implied to be `true` when Native AOT is active.

By default, the compiler tries to maximize compatibility with existing .NET code at the expense of compilation speed and size of the output executable. This allows people to use their existing code that worked well in a fully dynamic mode without hitting issues caused by trimming. To read more about reflection, see the [Reflection in AOT mode](reflection-in-aot-mode.md) document.

To enable more aggressive removal of unreferenced code, set the `<TrimmerDefaultAction>` property to `link`.

To aid in troubleshooting some of the most common problems related to trimming add `<IlcGenerateCompleteTypeMetadata>true</IlcGenerateCompleteTypeMetadata>` to your project. This ensures types are preserved in their entirety, but the extra members that would otherwise be trimmed cannot be used in runtime reflection. This mode can turn some spurious `NullReferenceExceptions` (caused by reflection APIs returning a null) caused by trimming into more actionable exceptions.

The Native AOT compiler can remove unused metadata more effectively than non-Native deployment models. For example, it's possible to remove names and metadata for methods while keeping the native code of the method. The higher efficiency of trimming in Native AOT can result in differences in what's visible to reflection at runtime in trimming-unfriendly code. To increase compatibility with the less efficient non-Native trimming, set the `<IlcTrimMetadata>` property to `false`. This compatibility mode is not necessary if there are no trimming warnings.

## Options related to library features

Native AOT supports enabling and disabling all [documented framework library features](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming-options#trimming-framework-library-features). For example, to remove globalization specific code and data, add a `<InvariantGlobalization>true</InvariantGlobalization>` property to your project. Disabling a framework feature (or enabling a minimal mode of the feature) can result in significant size savings.

Since `PublishTrimmed` is implied to be true with Native AOT, some framework features such as binary serialization are disabled by default.

## Options related to metadata generation

* `<IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>`: this disables generation of stack trace metadata that provides textual names in stack traces. This is for example the text string one gets by calling `Exception.ToString()` on a caught exception. With this option disabled, stack traces will still be generated, but will be based on reflection metadata alone (they might be less complete).

## Options related to code generation
* `<OptimizationPreference>Speed</OptimizationPreference>`: when generating optimized code, favor code execution speed.
* `<OptimizationPreference>Size</OptimizationPreference>`: when generating optimized code, favor smaller code size.
* `<IlcInstructionSet>`: By default, the compiler targets the minimum instruction set supported by the target OS and architecture. This option allows targeting newer instruction sets for better performance. The native binary will require the instruction sets to be supported by the hardware in order to run. For example, `<IlcInstructionSet>avx2,bmi2,fma,pclmul,popcnt,aes</IlcInstructionSet>` will produce binary that takes advantage of instruction sets that are typically present on current Intel and AMD processors. Run `ilc --help` for the full list of available instruction sets. `ilc` can be executed from the NativeAOT package in your local nuget cache e.g. `%USERPROFILE%\.nuget\packages\runtime.win-x64.microsoft.dotnet.ilcompiler\8.0.0-...\tools\ilc.exe` on Windows or `~/.nuget/packages/runtime.linux-arm64.microsoft.dotnet.ilcompiler/8.0.0-.../tools/ilc` on Linux.

