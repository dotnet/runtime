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

Asking for the name of such a type fails with a `NotSupportedException` right away, instead of returning an empty or made up name, so that a missing name is not mistaken for a bug of the runtime and can be diagnosed from the stack trace. The message of the exception says that the name was stripped and mentions `IlcStripNamesAssembly`. Apps that hit the exception need to remove the assembly from the item.

Only the names of types are stripped. The compiler only emits metadata for a member if it is a reflection target (e.g. it is kept by a `DynamicallyAccessedMembers` annotation, a `DynamicDependency` attribute or a root descriptor), so the names of members are kept to keep lookups by name working.

Behavior for types of an assembly that strips names:

| API | Result |
| --- | --- |
| `Type.Name`, `Type.Namespace`, `Type.FullName`, `Type.AssemblyQualifiedName`, `Type.ToString()` | Throw `NotSupportedException`. This includes `Object.ToString()` and `ValueType.ToString()`, and exception messages that print the name of the type (the exception thrown while building the message is `NotSupportedException` instead) |
| The same members for arrays, pointers, by-refs and constructed generic types that involve such a type (including as a type argument) | Throw `NotSupportedException` too, e.g. for `List<T>` where `T` is such a type |
| `Type.GetType(string)`, `Assembly.GetType(string)`, `GetNestedType(string)`, `GetInterface(string)`, `GetMember(string)` for nested types | Don't find the type, since the name is not in the metadata. Interfaces that don't have a name are skipped by `GetInterface(string)` |
| Names of members (methods, fields, properties, events, parameters and generic parameters), `GetMethod(string)` and the other lookups of members by name | Unchanged, including for members of a stripped type |
| Named arguments of custom attributes, enum members, overrides and generic virtual methods | Unchanged |
| Stack traces | The frames of methods of the assembly don't have names. They show up as an offset in the module, which can be symbolicated with the symbol files, e.g. on a crash report server. This also applies to methods that are reflectable. `DiagnosticMethodInfo.Create` returns `null` for methods of the assembly. Frames of methods from other assemblies that have a type of the assembly in their signature show that type with an empty name, because stack traces are produced without going through reflection |
| Compiler generated attribute types in the assembly, e.g. `NullableAttribute` | Consumers that identify them by name, e.g. `NullabilityInfoContext`, fail on them |

Everything else is unchanged. In particular, type identity, `typeof`, `GetType()`, `Activator.CreateInstance`, `MethodInfo.Invoke`, `MakeGenericType`, `MakeGenericMethod`, custom attribute instantiation, and virtual and interface dispatch don't depend on the names.

Limitations:

* The names are removed from the reflection metadata only. Other places in the binary that contain names are not affected, e.g. the field names that are part of struct marshalling metadata, the keys of external type maps (`TypeMapping`), embedded resource names, and strings that code explicitly contains (e.g. the string passed to `[UnsafeAccessorType]`, `Type.GetType`, or the arguments of custom attributes).
* Code that looks up types of such an assembly by name, e.g. `Type.GetType("Namespace.Type, Assembly")`, will not find them, even if the compiler has seen the string.
* Serializers, `EventSource`, `DataContract`, data binding, logging that uses the name of the type (e.g. `ILogger<T>`), and anything else that derives behavior from type names throw for these types. This includes the default `Exception.Message` and `Exception.ToString()` of exception types of the assembly, and code that goes through all the custom attributes of a member and compares the names of their types (e.g. `NullabilityInfoContext`), if any of them is an attribute type of the assembly.
* Error messages that are produced by the type loader from the metadata and not through reflection (e.g. when a generic type can't be created) can still print an empty name for the type.
* An unhandled exception of a type of such an assembly can't print its type name, so the crash report has the message of the `NotSupportedException` and the stack trace of the original exception instead.
* The setting is not supported for the system module or for multi-file compilation (the compiler reports an error). Assemblies that are not part of the compilation are ignored.
