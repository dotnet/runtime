# Optimizing programs targeting Native AOT

See the official documentation at https://learn.microsoft.com/dotnet/core/deploying/native-aot/optimizing.

The rest of the document talks about options that exist, but their names and purpose are subject to change without a breaking change notice.

## Options related to code generation
* `<IlcInstructionSet>`: By default, the compiler targets the minimum instruction set supported by the target OS and architecture. This option allows targeting newer instruction sets for better performance. The native binary will require the instruction sets to be supported by the hardware in order to run. For example, `<IlcInstructionSet>avx2,bmi2,fma,pclmul,popcnt,aes</IlcInstructionSet>` will produce binary that takes advantage of instruction sets that are typically present on current Intel and AMD processors. `<IlcInstructionSet>native</IlcInstructionSet>` will produce a binary that uses instructions that currently running CPU supports (no cross-compilation support). Run `ilc --help` for the full list of available instruction sets. `ilc` can be executed from the NativeAOT package in your local nuget cache e.g. `%USERPROFILE%\.nuget\packages\runtime.win-x64.microsoft.dotnet.ilcompiler\8.0.0-...\tools\ilc.exe` on Windows or `~/.nuget/packages/runtime.linux-arm64.microsoft.dotnet.ilcompiler/8.0.0-.../tools/ilc` on Linux.
* `<IlcMaxVectorTBitWidth>`: By default, the compiler targets a `Vector<T>` size of `16` or `32` bytes, depending on the underlying instruction sets supported. This option allows specifying a different maximum bit width. For example, by default on x64 hardware `Vector<T>` will be 16-bytes, however, if `AVX2` is targeted then `Vector<T>` will automatically grow to be 32-bytes instead. Setting `<IlcMaxVectorTBitWidth>128</IlcMaxVectorTBitWidth>` would keep the size as 16-bytes. Alternatively, even if `AVX512F` is targeted then by default `Vector<T>` will not grow larger than 32-bytes, setting `<IlcMaxVectorTBitWidth>512</IlcMaxVectorTBitWidth>` would allow it to grow to 64-bytes.

## Options related to reflection metadata size

### Stripping the names of types and members from specific assemblies

* `<IlcStripNamesAssembly Include="MyAssembly" />`: Strips the names of all types and members of the assembly with the given simple name from the reflection metadata in the final binary. The item can be specified multiple times. It is passed to the compiler as `--stripnames:MyAssembly`. This is meant for assemblies that contain a large amount of generated code, such as interop assemblies with long generated type names, where the names are never used at runtime. The default behavior of the compiler is not affected unless the item is specified.

The option is opt-in per assembly, because it changes the observable behavior of reflection for the types and members of that assembly. Names are replaced with the empty string in the metadata, so the cost of a name is no longer paid in the binary. Names remain available in the compiler map file (`<IlcGenerateMapFile>`), in the mstat file, and in the symbol files (`.pdb`, `.dwp`, `.dSYM`) because these are produced from the compiler's own type system and not from the reflection metadata. Note that on platforms where the debug information is part of the image, the names will be there as well.

Behavior for types and members of an assembly that strips names:

| API | Result |
| --- | --- |
| `Type.Name`, `MemberInfo.Name`, `ParameterInfo.Name`, names of generic parameters | `""` (empty string, the properties are not nullable) |
| `Type.Namespace`, `Type.FullName`, `Type.AssemblyQualifiedName` | `null` |
| `Type.ToString()` | `""`, matching `Type.Name` (the method is not nullable). Also for generic type definitions |
| `FullName` and `AssemblyQualifiedName` of arrays, pointers, by-refs, nested types and constructed generic types that involve such a type (including as a type argument) | `null`, like for a generic parameter |
| `Type.GetType(string)`, `Assembly.GetType(string)`, `GetMethod(string)`, `GetField(string)`, `GetProperty(string)`, `GetEvent(string)`, `GetNestedType(string)`, `GetMember(string)` | Don't find the type or member, since nothing can match an empty name |
| Enum members, e.g. `Enum.ToString()`, `Enum.Parse` | Unchanged, the names of enum literals are kept |
| Constructors (`.ctor`, `.cctor`), `Invoke` of delegates, generic methods (the runtime looks up instantiations and implementations of generic virtual methods by name), methods, fields and properties of attribute types | Unchanged, the names are kept |
| Named arguments of custom attributes | Unchanged |
| Exception messages and `Exception.ToString()` | Use the empty name for the type |
| Stack traces | The frames of methods of the assembly don't have names. They show up as an offset in the module, which can be symbolicated with the symbol files, e.g. on a crash report server |
| `MethodInfo.GetBaseDefinition()`, `GetCustomAttributes(inherit: true)` on overridden methods, properties and events, `GetMethods()` and `GetProperties()` on derived types | Members are matched with the ones they override by name, so an override is not associated with the member of the base type. `GetBaseDefinition()` returns the method itself, inherited attributes of overridden members are not found, and both the override and the base member are listed. This also applies to a type from another assembly that overrides a member of a type that strips names |
| Compiler generated attribute types in the assembly, e.g. `NullableAttribute` | The types lose their names, so consumers that identify them by name, e.g. `NullabilityInfoContext`, don't recognize them |

Everything else is unchanged. In particular, type identity, `typeof`, `GetType()`, `Activator.CreateInstance`, `MethodInfo.Invoke`, `MakeGenericType`, `MakeGenericMethod`, custom attribute instantiation, and virtual and interface dispatch don't depend on the names.

Limitations:

* The names are removed from the reflection metadata only. Other places in the binary that contain names are not affected, e.g. the field names that are part of struct marshalling metadata, the keys of external type maps (`TypeMapping`), embedded resource names, and strings that code explicitly contains (e.g. the string passed to `[UnsafeAccessorType]`, `Type.GetType`, or the arguments of custom attributes).
* Code that looks up types or members of such an assembly by name, e.g. `Type.GetType("Namespace.Type, Assembly")`, will not find them, even if the compiler has seen the string. A reference that goes through metadata directly (e.g. a type referenced by a custom attribute argument) is not affected.
* Serializers, `EventSource`, `DataContract`, data binding, and anything else that derives behavior from type or member names don't work for these types.
* The setting is not supported for the system module or for multi-file compilation (the compiler reports an error). Assemblies that are not part of the compilation are ignored.
