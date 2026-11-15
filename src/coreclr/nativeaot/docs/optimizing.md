# Optimizing programs targeting Native AOT

See the official documentation at https://learn.microsoft.com/dotnet/core/deploying/native-aot/optimizing.

The rest of the document talks about options that exist, but their names and purpose are subject to change without a breaking change notice.

## Options related to code generation
* `<IlcInstructionSet>`: By default, the compiler targets the minimum instruction set supported by the target OS and architecture. This option allows targeting newer instruction sets for better performance. The native binary will require the instruction sets to be supported by the hardware in order to run. For example, `<IlcInstructionSet>avx2,bmi2,fma,pclmul,popcnt,aes</IlcInstructionSet>` will produce binary that takes advantage of instruction sets that are typically present on current Intel and AMD processors. `<IlcInstructionSet>native</IlcInstructionSet>` will produce a binary that uses instructions that currently running CPU supports (no cross-compilation support). Run `ilc --help` for the full list of available instruction sets. `ilc` can be executed from the NativeAOT package in your local nuget cache e.g. `%USERPROFILE%\.nuget\packages\runtime.win-x64.microsoft.dotnet.ilcompiler\8.0.0-...\tools\ilc.exe` on Windows or `~/.nuget/packages/runtime.linux-arm64.microsoft.dotnet.ilcompiler/8.0.0-.../tools/ilc` on Linux.
* `<IlcMaxVectorTBitWidth>`: By default, the compiler targets a `Vector<T>` size of `16` or `32` bytes, depending on the underlying instruction sets supported. This option allows specifying a different maximum bit width. For example, by default on x64 hardware `Vector<T>` will be 16-bytes, however, if `AVX2` is targeted then `Vector<T>` will automatically grow to be 32-bytes instead. Setting `<IlcMaxVectorTBitWidth>128</IlcMaxVectorTBitWidth>` would keep the size as 16-bytes. Alternatively, even if `AVX512F` is targeted then by default `Vector<T>` will not grow larger than 32-bytes, setting `<IlcMaxVectorTBitWidth>512</IlcMaxVectorTBitWidth>` would allow it to grow to 64-bytes.

## Options related to reflection metadata size

### Stripping the names of types from specific assemblies

* `<IlcStripNamesAssembly Include="MyAssembly" />`: Strips the names of all types of the assembly with the given simple name from the reflection metadata in the final binary. The item can be specified multiple times. It is passed to the compiler as `--stripnames:MyAssembly`. This is meant for assemblies that contain a large amount of generated code, such as interop assemblies with long generated type names, where the names are never used at runtime. The default behavior of the compiler is not affected unless the item is specified.

The option is opt-in per assembly, because it changes the observable behavior of reflection for the types of that assembly. Names are replaced with the empty string in the metadata, so the cost of a name is no longer paid in the binary. Names remain available in the compiler map file (`<IlcGenerateMapFile>`), in the mstat file, and in the symbol files (`.pdb`, `.dwp`, `.dSYM`) because these are produced from the compiler's own type system and not from the reflection metadata. Note that on platforms where the debug information is part of the image, the names will be there as well.

Only the names of types are stripped. The compiler only emits metadata for a member if it is a reflection target (e.g. it is kept by a `DynamicallyAccessedMembers` annotation, a `DynamicDependency` attribute or a root descriptor), so the names of members are kept to keep lookups by name working.

Behavior for types of an assembly that strips names:

| API | Result |
| --- | --- |
| `Type.Name` | `""` (empty string, the property is not nullable) |
| `Type.Namespace`, `Type.FullName`, `Type.AssemblyQualifiedName` | `null` |
| `Type.ToString()` | `""`, matching `Type.Name` (the method is not nullable). Also for generic type definitions |
| `FullName` and `AssemblyQualifiedName` of arrays, pointers, by-refs, nested types and constructed generic types that involve such a type (including as a type argument) | `null`, like for a generic parameter |
| `Type.GetType(string)`, `Assembly.GetType(string)`, `GetNestedType(string)`, `GetInterface(string)`, `GetMember(string)` for nested types | Don't find the type, since nothing can match an empty name |
| Names of members (methods, fields, properties, events, parameters and generic parameters), `GetMethod(string)` and the other lookups of members by name | Unchanged, including for members of a stripped type |
| Named arguments of custom attributes, enum members, overrides and generic virtual methods | Unchanged |
| `MethodInfo.ToString()` and the like | The types in the signature are printed with an empty name, so `Void Foo()` can stand for `Foo(Widget)`. The output of `ToString()` can't be used as a key |
| Exception messages and `Exception.ToString()` | Use the empty name for the type |
| Stack traces | The frames of methods of the assembly don't have names. They show up as an offset in the module, which can be symbolicated with the symbol files, e.g. on a crash report server. This also applies to methods that are reflectable. `DiagnosticMethodInfo.Create` returns `null` for methods of the assembly. Frames of methods from other assemblies that have a type of the assembly in their signature show that type with an empty name |
| Compiler generated attribute types in the assembly, e.g. `NullableAttribute` | The types lose their names, so consumers that identify them by name, e.g. `NullabilityInfoContext`, don't recognize them |

Everything else is unchanged. In particular, type identity, `typeof`, `GetType()`, `Activator.CreateInstance`, `MethodInfo.Invoke`, `MakeGenericType`, `MakeGenericMethod`, custom attribute instantiation, and virtual and interface dispatch don't depend on the names.

Limitations:

* The names are removed from the reflection metadata only. Other places in the binary that contain names are not affected, e.g. the field names that are part of struct marshalling metadata, the keys of external type maps (`TypeMapping`), embedded resource names, and strings that code explicitly contains (e.g. the string passed to `[UnsafeAccessorType]`, `Type.GetType`, or the arguments of custom attributes).
* Code that looks up types of such an assembly by name, e.g. `Type.GetType("Namespace.Type, Assembly")`, will not find them, even if the compiler has seen the string. A reference that goes through metadata directly (e.g. a type referenced by a custom attribute argument) is not affected.
* Serializers, `EventSource`, `DataContract`, data binding, and anything else that derives behavior from type names don't work for these types.
* Code that assumes a named type has a non-null `FullName` (e.g. `type.FullName!`) can throw for these types. For example, `ILogger<T>` and `ILoggerFactory.CreateLogger(typeof(T))` throw a `NullReferenceException` when `T` is a stripped type, because the category name is derived from `FullName`. The default `Object.ToString()` and `ValueType.ToString()` return an empty string for these types.
* The setting is not supported for the system module or for multi-file compilation (the compiler reports an error). Assemblies that are not part of the compilation are ignored.
