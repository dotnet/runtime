# DisablePrivateReflectionAttribute: hiding the implementation details of an assembly from reflection

## Why

NativeAOT (ILC) makes a type reflection visible for many reasons that a program can not influence: constructed types get a
"with metadata" MethodTable, every type argument of such a type gets metadata, attribute classes, type map values, and so on.
A library that knows that nothing will ever reflect over its implementation details (for example the generated static helper
classes and marker types of CsWinRT 3.0) has no way to say so, so the names and the metadata of these types stay in the binary.

`[assembly: DisablePrivateReflection]` lets the author of an assembly say it: the types of the assembly that are not
externally visible, and the members that are not public, are not available to reflection, in NativeAOT and in CoreCLR, and the NativeAOT
compiler removes their metadata. The author controls what is reflectable with the visibility of the types and members: everything that must stay
reflectable is `public`, everything else is `internal` (or private).

## Shape

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class DisablePrivateReflectionAttribute : Attribute
    {
        public DisablePrivateReflectionAttribute();
    }
}
```

This is the shape of the existing attribute (it was added in .NET Framework 4.5 and obsoleted as SYSLIB0015 in .NET 6, where it had
no effect). No new API is needed: the attribute is not obsolete anymore and has an effect.

NativeAOT matches the attribute by namespace and name in any assembly, so a library that targets frameworks where the
attribute has no effect (or does not exist) can declare its own copy. CoreCLR matches the attribute by name as well.
Mono is not affected (the AOT compiler of Mono has a different use of the attribute name, on methods).

## What is blocked

A type is *externally visible* if it is public and all the types that contain it are public (`Type.IsVisible`).
In an assembly annotated with the attribute, a type that is not externally visible is blocked ("stripped"), unless it is one of the types
that keep their metadata (see "Types that stay reflectable"). The rule applies to the type itself and to every type nested in it.
A public type nested in an internal type is blocked, an internal type nested in a public type is blocked as well.

For a blocked type:

* it has no members (methods, fields, properties, events, constructors), no nested types and no custom attributes
* its name is not available: `Name`, `FullName` and `ToString()` return a placeholder (`<PrivateType 0x...>`), `Namespace` and
  `DeclaringType` return `null`, `GUID` and `AssemblyQualifiedName` throw `NotSupportedException`
* it is not found by name (`Type.GetType`, `Assembly.GetType`) and it is not listed by `Assembly.GetTypes()`
* `Activator.CreateInstance(Type)` fails (there is no constructor to find)
* `MethodBase.GetMethodFromHandle`, `FieldInfo.GetFieldFromHandle` and `Delegate.Method` fail with `NotSupportedException`
  for its members

Everything else works: the type keeps its MethodTable and all its code, so instances can be created and cast, interfaces, virtual calls
and static abstract members work, and `object.GetType()`, `typeof`, type equality, `IsClass`, `IsValueType`, `BaseType`,
`GetInterfaces()` and `IsAssignableFrom` keep working. A blocked type can be a generic argument and can be created with
`Activator.CreateInstance<T>()` (the constructor is called directly), and it can be the key or the value of a type map entry.
The diagnostics are not affected (see [Diagnostics](#diagnostics)), and neither is `[UnsafeAccessor]` (see [UnsafeAccessor](#unsafeaccessor)).

The types that the compiler generates inside a type (the closures and the cached delegates of lambdas, the state machines of
async methods and iterators) are private nested types, so they are blocked, unless they are generic (the generic methods and the
methods of generic types get generic closures and state machines, which are reflectable).
The global `<Module>` type is never blocked: the runtime uses it to identify the assembly (lazy P/Invoke binding,
`Assembly.GetExecutingAssembly()`).

## Members

Reflection of the non public members is private reflection, so the members that are not public are not available either, also the ones
of public types. For a type that is externally visible (and is not one of the exceptions below), the members that are available to reflection are the
public ones. Private, internal, protected, protected internal and private protected members are not. The protected members are part of the
contract between a type and its derived types, but not of the contract with reflection: `BindingFlags.NonPublic` is the private reflection that the attribute
turns off, and a single rule keeps the behavior simple (a rule for the protected members would depend on whether the type is sealed, and on
the accessibility of the types that are nested in it). Every member of a type that is not externally visible is
hidden (the types that are not blocked because the runtime needs their metadata, like a generic type, are in this group, and so are all
the members of a blocked type).

A member that is not available behaves like the members of a blocked type: it is not listed by `GetMethods`, `GetFields`, `GetProperties`, `GetEvents`,
`GetConstructors`, `GetMembers`, `GetNestedTypes` and `DeclaredMembers`, it is not found by name (`GetMethod(name, BindingFlags.NonPublic)`, `GetMember`,
`InvokeMember`), the same members of a base type are not listed for the derived type, handles to it can't be converted to a `MethodBase` or `FieldInfo`
(`GetMethodFromHandle`, `GetFieldFromHandle`, `Module.ResolveMethod`, and the expression trees that the C# compiler builds with them),
`Delegate.CreateDelegate` by name does not find it and `Delegate.Method` of a delegate to it fails with `NotSupportedException`. It works for the
program all the same: calls, delegates, virtual dispatch, interface dispatch and `[UnsafeAccessor]` do not use reflection.

The decisions for the members that are not obvious:

| Member | Decision |
|---|---|
| Explicit interface implementations | They are private, so hidden. They are reachable through the interface (`typeof(I).GetMethod("M").Invoke(instance)`). `GetInterfaceMap` fails with `NotSupportedException` if a target is hidden, in CoreCLR and NativeAOT |
| Constructors | Like the other methods: only a public constructor of a type that is externally visible is available. `Activator.CreateInstance(Type, nonPublic: true)` does not find a non public default constructor of a class (`MissingMethodException`). Static constructors are private, so `Type.TypeInitializer` is `null` |
| Fields | Only the public ones. The backing fields of the properties are private, so hidden. Literal fields follow the same rule, and the fields of an enum are always available |
| Properties and events | Available if at least one accessor is. A hidden accessor is not reported (`GetSetMethod(nonPublic: true)` returns `null`, the setter of a `public int P { get; private set; }` is not there). The members of an interface are available if they are public (an interface can also declare private, internal and protected members, which are hidden) |
| Enums and delegates | All their members are available, whatever their accessibility, and they are available even if the type is not externally visible: `Enum.ToString`, `Delegate.DynamicInvoke` and the runtime libraries need them |
| Attribute classes | The constructors, fields and properties of a class that derives from `Attribute` are available whatever their accessibility (the runtime creates the attributes through them, with `GetCustomAttributes`, and the arguments that name a field or a property can be of any accessibility). The other members (methods, events) follow the rule of the other types, so the private helpers of an attribute class are hidden. This applies to a class that is not blocked, of any visibility |
| Generic methods and generic virtual methods | They are hidden like any other, but the runtime type loader needs to identify them (dictionary lookups, generic virtual method dispatch). In NativeAOT their handle in the metadata is a record that is not part of the type, so reflection does not find it (see "ILC implementation") |
| Compiler generated members | They follow their accessibility (lambda caches, local functions, the backing fields, the closures' fields and the members of a record that the compiler adds: `EqualityContract`, `PrintMembers` are protected and so hidden, `Clone`, `Deconstruct` and `Equals` are public) |
| `ThreadStatic` fields, struct layout, `Marshal.SizeOf`, `StructLayout` | Do not depend on reflection in NativeAOT. `Marshal.OffsetOf` and `ValueType.Equals`/`GetHashCode` find the fields whatever their accessibility in CoreCLR (they use the runtime directly) |
| Members of generic types | The members of the generic type definition and of its instances follow the same rule |
| `<Module>` | The members of the global type are not hidden (the runtime uses them for P/Invokes and the module initializer) |

Readers of the private state of a type by reflection stop working with the members that they read (this is what the attribute is for), for
example `DataContractSerializer` and `XmlSerializer` over internal members, reflection based JSON and DI, `EventSource` types with non public members,
`ResourceManager(Type)`, and test frameworks that use `InternalsVisibleTo` and reflect over internals. Types and members that these need must be public.
The runtime libraries themselves do not depend on the private members of the user types, except for the cases that were listed in this document
(`ValueType.Equals` and `Marshal.OffsetOf` in CoreCLR, which are handled).

## Custom attributes

* The custom attributes of a blocked type are not available: `GetCustomAttributes`, `IsDefined` and `CustomAttributeData` return nothing
  (the attributes that the type inherits from a public base type are still returned). This is the same in NativeAOT and CoreCLR. It is up to
  the library to make a type public if the attributes of the type are needed at run time (for example the types that the runtime
  libraries inspect, like the ones that source generated COM interop uses).
* The attributes of a hidden member are not available (the member is not).
* The custom attributes of a type that is not blocked (a public type, or any type of an assembly without the attribute) are available even if
  they mention a blocked type, for example as a generic argument of the attribute type (`ComExposedClassAttribute<T>` and
  `IUnknownDerivedAttribute<TInterface, TImpl>` over the helper types that the COM source generators emit), as a `typeof` argument,
  or nested in either (`Attribute<List<Blocked>>`, `Blocked[]`). The blocked type is surfaced as a blocked type: it follows
  the rules above (placeholder name, no members, `GUID` fails), and it is equal to every other `Type` of the same type. The blocked type
  can be in another assembly than the attribute (`InternalsVisibleTo`). This includes the blocked generic types (a generic type nested in a
  blocked type, like the closures that the compiler generates): `Attribute<Blocked.Box<int>>` and `typeof(Blocked.Box<string>)` surface
  an instance that is a blocked type like any other, that is still reported as a generic type with its generic arguments.
* The custom attributes whose attribute type is blocked are dropped from every member that has them, in any assembly (the attribute class can be a public
  type nested in an internal type of an assembly with the attribute, and used by an assembly that sees it with `InternalsVisibleTo`). This is what
  `GetCustomAttributes`, `IsDefined` and `CustomAttributeData` do, in CoreCLR and in NativeAOT, and none of them fails.
* In NativeAOT, a blocked type that an attribute mentions gets a MethodTable (it is a reflection root), but still no metadata: the
  metadata refers to it with a placeholder reference that the runtime resolves with the type map by the hash code of the type.
  For an instance of a blocked generic type the reference is the placeholder of the generic type definition inside the usual
  instantiation: the runtime finds the MethodTable of the definition with the type map, and the one of the instance with the generic type
  tables (the lookup that `MakeGenericType` uses, without the constraint check), and the instance is the blocked type of that MethodTable. An instance
  that the runtime can neither find nor build is not available (`NotSupportedException`). What an attribute roots is what it mentions, for the targets
  whose attributes are reflectable: the attribute type and its constructor, the fields and setters of its named arguments, the metadata of the types that
  it mentions and, for a blocked type, its MethodTable (for an instance of a blocked generic type, the MethodTable of the instance, which brings the one of
  the generic type definition and the ones of its type arguments).

## Types that stay reflectable

These types are not blocked, without a diagnostic, even if they are not externally visible:

* enums and delegates: the runtime needs their metadata (`Enum.ToString`, `Delegate.DynamicInvoke`)
* top-level types that derive from `System.Attribute`: attributes are meant to be read, and the compiler embeds some in every assembly
  (`NullableAttribute`) to describe the public types. Attribute classes nested in other types are blocked with them.
* types that declare generic virtual methods: the runtime type loader identifies these methods by their metadata
* generic types (the type loader needs the metadata of the generic type definition), except for the generic types that are
  nested in a blocked type, which are blocked as well (these are the closures and the iterators that the compiler generates for the
  generic methods of that type)
* a type that contains one of the types above as a nested type (the metadata of a nested type requires the metadata of the type
  that contains it). For example `internal class Parser { private enum State { ... } }` keeps its metadata.

These types keep their definition, so they are listed (`GetNestedTypes`, `Assembly.GetTypes()`) and found by name with their real name, and
their non public members are hidden like the members of any other type. This includes the closures and state machines that the compiler generates
for the generic methods and the generic types: their names contain the names of the methods they come from.

## Diagnostics

The attribute does not affect the diagnostics: stack traces (`Exception.StackTrace` and `ToString`, `Environment.StackTrace`, the text of `StackTrace` and
`StackFrame`), the debugger (breakpoints, stepping and function evaluation; on CoreCLR the function evaluation resolves its target without the member caches) and `DiagnosticMethodInfo`
(also the diagnostic uses of `Delegate.Method` in the runtime: timer and task debugger display, assembly load tracing)
show the real names of the blocked types and of the members that are not available, like in an assembly without the attribute.

* NativeAOT: the stack trace metadata (the names that stack traces print, which is not part of the reflection metadata) keeps the names of the blocked
  types, of the types that contain them, of the methods and of the types in their signatures. ILC writes them as references with real names that
  are not shared with the placeholder references that reflection uses, and only if the stack trace support is on (`StackTraceSupport`): with the support
  off nothing is kept, no matter what the attribute hides. The type and member definitions are still not in the metadata.
* CoreCLR: the text of a stack trace uses the real name, the attributes (`StackTraceHidden`, the ones that mark the state machines of
  async methods) and the members of the types that are not available to reflection, through paths that do not use the reflection caches, so
  reflection can't get to the members that they use. `StackFrame.GetMethod()` and `Exception.TargetSite` return `null` for a method that is not
  available (like NativeAOT does for the methods without reflection data), and `Exception.Source` still finds the assembly.
* NativeAOT: the debugger display of a `Timer` or `Task` whose callback is a method that is not available still reads `Delegate.Method`, which throws for it.

The names that the runtime prints for a blocked type in other places, and that this attribute does not change: `Type.Name`, `FullName` and `ToString()` of a blocked
type are a placeholder on both runtimes, so is `object.ToString()` of an instance (it prints the type name) and so is what code prints with it. The messages that
the CoreCLR runtime formats itself (`InvalidCastException`, `TypeLoadException` and so on) have the real name, and the ones that NativeAOT formats have the placeholder. The type
name that `Exception.ToString()` and the default `Exception.Message` print is the real one on CoreCLR (it is a diagnostics name), and the placeholder on NativeAOT.
A real name is not
available to the NativeAOT runtime for a type that has no methods in the stack trace data (the stack trace data is keyed by the code address), so giving it
needs a table from the MethodTable of every blocked type to its name, which is about a name string per type that has a MethodTable (the size of the names that
the attribute removed), so it was not done: it would give back the saving for the types that the program allocates, and the choice is between the saving and
the names in the messages.

## UnsafeAccessor

`[UnsafeAccessor]` works for the members and types that are not available to reflection, for every kind (methods, static methods, fields, static fields and constructors,
generic methods, and `[UnsafeAccessorType("Name, Assembly")]` for the types that are not accessible to the code of the accessor), in CoreCLR and in NativeAOT:

* NativeAOT: ILC resolves the target by name with its type system when it compiles the accessor (and the type of `[UnsafeAccessorType]`
  by name), and it emits a direct call or field access, so the reflection metadata is not involved, and the code and fields of the target are kept as for a direct use.
  A generic method that the runtime type loader has to identify (`ldtoken`, shared generic code, generic virtual methods) has a metadata record of its own that
  is not part of the type (see below).
* CoreCLR: the runtime binds the accessor to the method with its own binder and resolves the type names with the type name parser of the runtime, and none of them use the
  reflection caches that the attribute filters.

## Fidelity: CoreCLR versus NativeAOT

The goal is that code that runs on CoreCLR during development fails the same way when it is published with NativeAOT.
This is the status for the types that are blocked. "Same" means the same observable result.

| Behavior | NativeAOT | CoreCLR |
|---|---|---|
| `Name`, `FullName`, `ToString()` | placeholder | same (the hash in the placeholder differs) |
| `Namespace`, `DeclaringType` | `null` | same |
| `GUID`, `AssemblyQualifiedName` | `NotSupportedException` | same |
| `MetadataToken`, `StructLayoutAttribute` | `InvalidOperationException` / `NotSupportedException` | real values (not checked, to keep the cost off hot paths) |
| `Assembly`, `Module` | `NotSupportedException` | real values (not checked) |
| Methods, fields, properties, events, constructors, nested types | none declared | same (members inherited from public base types such as `object` are still returned) |
| Members that are not public, of a type that is externally visible (and of the types that keep their metadata because the runtime needs it; the enums and the delegates keep all their members, and the attribute classes their constructors, fields and properties) | not in the metadata: not listed, not found by name, `GetMethodFromHandle`, `GetFieldFromHandle` and `Delegate.Method` fail with `NotSupportedException` | same (`Delegate.CreateDelegate` by name does not find them either) |
| A property or event with an accessor that is not public | only the public accessors are there, the property or event is there if one is | same |
| `GetInterfaceMap` of a type whose implementation of an interface method is not public (explicit implementations) | `NotSupportedException` | same |
| `Activator.CreateInstance(Type, nonPublic: true)` of a class whose default constructor is not public | fails | `MissingMethodException`, same |
| Custom attributes, `IsDefined` | none (the custom attributes that a blocked type inherits from a public base type are still returned) | same |
| Custom attributes of other members whose attribute type is blocked | dropped | same, also in `CustomAttributeData`, also if the member is in another assembly (`InternalsVisibleTo`) |
| Custom attributes of a visible type or member that mention a blocked type as an argument (a generic argument of the attribute type, `typeof`, an array, a nested generic argument), also an instance of a blocked generic type | available, the blocked type is a blocked type | available, the blocked type is a blocked type |
| `Type.GetType(name)`, `Assembly.GetType(name)`, `GetTypes()` | not found, not listed | same |
| `Activator.CreateInstance(Type)` | fails; a value type is zero initialized without calling a parameterless constructor it declares | `MissingMethodException`; a value type calls the parameterless constructor it declares |
| `Activator.CreateInstance<T>()` | works | works |
| `ValueType.Equals` over fields of a blocked struct | works (does not use reflection) | works |
| `GetMethodFromHandle`, `GetFieldFromHandle`, `Delegate.Method` | `NotSupportedException` | same |
| Stack traces (`Exception.StackTrace` and `ToString`, `Environment.StackTrace`, `StackTrace` and `StackFrame` text, `DiagnosticMethodInfo`) | the names of blocked types and of hidden members are there, like without the attribute (when the stack trace support is on) | same |
| `StackFrame.GetMethod()`, `Exception.TargetSite` of a hidden method | `null` (NativeAOT only has it for the methods that have reflection data) | `null` |
| `GetType()`, `typeof`, equality, base type, interfaces, `IsClass`, `IsValueType` | works | works |
| `Attributes` | the kind (class, interface, value type), `IsAbstract` and `IsSealed` are real. The visibility of a nested type is `NotPublic` (`IsNestedPrivate`, `IsNestedPublic`, ... are `false` and `IsNotPublic` is `true`, while `IsNested` is `false` as `DeclaringType` is `null`), the layout is auto (`IsLayoutSequential` and `IsExplicitLayout` are `false`), and `IsSerializable`, `IsSpecialName`, `BeforeFieldInit` and the string format are not set; `MemberType` is `TypeInfo` | real values (see [Type attributes on NativeAOT](#type-attributes-on-nativeaot)) |
| Generic type definition of a blocked generic type (`typeof(Blocked.Box<>)` in an attribute, or from `GetGenericTypeDefinition()` of an instance) | `IsGenericTypeDefinition` and `ContainsGenericParameters` work (answered from the MethodTable), but it has no generic parameters (`GetGenericArguments()` is empty), no base type, no interfaces and no members, and `MakeGenericType` fails with `NotSupportedException` | real values, no members, `MakeGenericType` fails with `NotSupportedException` |
| Instance of a blocked generic type (`Blocked.Box<int>`, the type of a closure, or the one that an attribute mentions) | a blocked type like any other, with a placeholder name per instance; `IsGenericType`, `GetGenericArguments()` and `GetGenericTypeDefinition()` work (they are answered from the MethodTable) | same |
| `typeof(object).IsAssignableFrom(Blocked.Box<>)` | `false` | `true` |
| Names in messages formatted by the runtime itself (cast failures, type load errors) | placeholder | real name |
| Names of constructed types over blocked types (`List<T>`, arrays) | placeholder for the blocked part | real name |
| `Assembly.EntryPoint` of an assembly whose entry point is in a blocked type or is not a public method | `null` | `null` |
| `MakeGenericType` over a blocked type as an argument | works (for a generic type definition as the argument, `NotSupportedException`) | same |
| Reflection over a type that mentions a blocked type in a member signature | `ParameterType`, `FieldType` and binding by `Type[]` return the blocked type if the program has a MethodTable for it, otherwise they fail with `NotSupportedException` | work (the real types are returned) |

The differences are in the direction of CoreCLR being more permissive, so that code that depends on information that is
not available passes on CoreCLR and fails on NativeAOT.

### Type attributes on NativeAOT

A blocked type has no metadata, so what reflection derives from the declaration of the type (`Type.Attributes`) comes from its MethodTable.
The MethodTable knows the kind of type, so everything that depends on it is exact (checked by the test, which runs the same assertions on
both runtimes): `IsClass`, `IsInterface`, `IsValueType`, `IsByRefLike`, `IsEnum`, `IsPrimitive`, `IsGenericType`, `IsGenericTypeDefinition`,
`ContainsGenericParameters`, arrays, pointers and by refs over a blocked type and `GetElementType()`, `BaseType`, `GetInterfaces()`,
`IsAssignableFrom`, `IsSubclassOf`, `IsInstanceOfType` and `GetTypeCode`. `IsVisible`, `IsPublic`, `IsNested` (always `false`) and `IsCOMObject` are the same on both runtimes.

`IsAbstract` and `IsSealed` are exact too, but the MethodTable did not have them: ILC sets two spare bits of the extended flags of the
MethodTable (`StrippedIsAbstractFlag` and `StrippedIsSealedFlag`) for the types that it strips and for no other type, so the MethodTables
of all other types are unchanged and no data is added (a few bytes of code read the flags). This also matters for correctness: `RuntimeHelpers.GetUninitializedObject`
uses `IsAbstract`, and an abstract class that is the base of a class that is allocated has a vtable, so without the flag an instance of it could be
allocated (CoreCLR throws `MemberAccessException`). After these two, the flags `0x0800` and `0x8000` of the extended flags are
the only ones left (`0x00200000` of the flags is the collectible bit that the GC reads).

The rest of the attributes are not in the MethodTable and are not kept, so they are not exact on NativeAOT:

* A blocked nested type reports `NotPublic` and `MemberType.TypeInfo` instead of its accessibility. CoreCLR reports `IsNestedPrivate`,
  `IsNestedAssembly`, ... and `MemberType.NestedType`, while `IsNested` is `false` there, because `DeclaringType` is `null`.
* The layout is reported as auto: `IsLayoutSequential` is `false` for a struct that C# emits as sequential and for a class with
  `[StructLayout(LayoutKind.Sequential)]`, and `IsExplicitLayout` is `false`.
* `IsSerializable`, `BeforeFieldInit` and `IsSpecialName` are `false`, and the string format is the default one (ANSI), which is the real
  value for the types that C# emits unless they say otherwise.
* `StructLayoutAttribute` throws.

A type that has no metadata for a reason other than being stripped keeps the attributes that its kind gives (and no abstract or sealed flag).
Making them exact needs data, and these are the options:

| Option | Fidelity | Cost |
|---|---|---|
| Keep it as it is (documented here) | exact kind, abstract and sealed | none |
| Sidecar table (a native format hashtable keyed by the hash code of the type, an external reference to the MethodTable and the `TypeAttributes` as an unsigned constant) for the stripped types that have a MethodTable, emitted only if an assembly has the attribute | exact `Attributes` (not `StructLayoutAttribute.Pack/Size/CharSet`, which would need more bytes) | about 12 bytes per stripped type that has a MethodTable (measured with a prototype: 55 types +640 bytes of `.rdata` in the smoke test, 1055 types +12928 bytes, about 12.3 bytes each, with no change for an app that has no stripped types), for comparison keeping the metadata of 1000 empty classes costs about 58 bytes each. It also needs the runtime lookup (a hashtable read per `Attributes`, and the definition for the instances of generic types) |
| CoreCLR reports for a blocked type what NativeAOT can report (`NotPublic`, auto layout, ...) | same on both | a check on `Attributes`, `IsNestedX` and `MemberType` of CoreCLR nested types (it would no longer report the real values) |
| Default values on NativeAOT (sequential layout for value types) | right for most types, wrong for the structs that are not sequential (auto or explicit layout) | none, but not exact |

The sidecar (binary size versus fidelity) and the change of CoreCLR (it would give up information that it has) need a decision, so none of them is implemented.

## Performance on CoreCLR

The assembly attribute is read lazily, once per module (a by-name custom attribute lookup on the assembly, as for
`DisableRuntimeMarshallingAttribute`), and cached in the managed `RuntimeModule`. Whether a type is blocked is computed when
the `RuntimeTypeCache` of the type is created (module flag check, and for the assemblies with the attribute a QCall that applies the rules above), and cached there.
The checks are placed where a result is computed or cached already:

* member lists, names, namespace and declaring type are computed on a cache miss, the check is a field read there
* custom attributes: `IsDefined` and `GetCustomAttributes` of a type, and the attribute filter, read the result that the type cache has
  already, or the module flag of the type if it has no cache yet
* `Type.GetType(string)` and `Assembly.GetType(string)`: the same check for each resolved type name
* `Activator.CreateInstance(Type)`: the result is part of the flag of the cached activator entry that is read already
* `GetMethodFromHandle` and friends: only on the cache miss path, where the `MethodBase` object is created
* `MakeGenericType`, `GetBaseDefinition` and the inherited custom attributes of a method are not cached, they read the same result for each type that
  they look at

Assemblies without the attribute pay for these module flag reads only (no allocation, no QCall after the first read of a module).
`Assembly.GetTypes()` of an assembly with the attribute evaluates the rules for every type, which scans the nested type table of the
assembly for each type that is not externally visible.

Measured on an x64 Release build, interleaved runs (4 rounds, median of the best of 7 hot runs) against a baseline build of the parent upstream commit: `Activator.CreateInstance(Type)`,
`GetCustomAttributes`, `GetMethods`, `GetMembers`, `GetMethod(name)`, `GetField(name)`, `GetNestedTypes`, `MakeGenericMethod`, `GetMethodFromHandle` and
`Name`/`FullName` are within noise (about 3% in either direction). `Assembly.GetType` and `IsDefined` are about 2.5% slower, `MakeGenericType` about 3.7% (about 7 ns on 194 ns),
`CustomAttributeData` about 4.7% (about 16 ns on 341 ns), `GetBaseDefinition` (3 levels) about 5% (about 6 ns on 125 ns) and `Type.GetType(string)` of a framework type about 5% (about 44 ns on 815 ns).
The first calls over many types show no difference, except for custom attributes (7.0 ms to 7.5 ms).

## Limits and open questions

* Dataflow does not warn when a reflection feature (`[DynamicallyAccessedMembers]`, `GetMethod`, `Activator`, ...) targets a blocked
  type; the request is dropped and fails at run time. A warning could be added.
* Exception types, `EventSource` types, types used by DI containers or serializers that are internal in an annotated assembly lose
  their names and members. The author is responsible for making them public.
* The custom attributes whose type is a blocked nested type are dropped from every member that has them, also from public ones, and an
  internal type that a library reads the `GUID` or custom attributes of at run time (proxy or provider types of an interop
  layer, type map values that are inspected) fails `GUID` and has no custom attributes. Make these types public (hidden from
  the editor with `EditorBrowsable` if needed).
* A visible method can mention a blocked type in its signature through a generic argument of a public type or in IL (C# does not allow
  it for a public member). In NativeAOT its blocked parameter type is resolved to the blocked type if the program has a
  MethodTable for it, and fails with `NotSupportedException` otherwise.
* Generic methods and generic virtual methods that are not available to reflection (hidden members, and the members of blocked types) work, also when the
  runtime type loader needs them (`MakeGenericMethod`-like instantiation inside shared generic code, generic virtual method dispatch, `[UnsafeAccessor]` generic
  accessors). In NativeAOT they have a small metadata record (name, signature, generic parameters) that is not part of the metadata of the type, so reflection does not find it.
* The names and the attributes of blocked types, and the non public members of every type, are also hidden from the runtime libraries that use reflection: `EventSource`
  (name, GUID, events that are not public), COM source generated interop (`GeneratedComInterface` and `GeneratedComClass` types must be public: the attributes of a blocked type
  are not available), `Marshal.PtrToStructure` over internal types, DI containers and serializers, and `DataContractSerializer`. Types and members that these need must be public.
* `Delegate.Method` of a lambda and of a delegate to a method that is not public fails (the lambdas of a public type are methods of a blocked closure type
  or private methods), and so does an expression tree that captures a local variable (it reads the field of the closure by its handle).
  `MethodBase.GetCurrentMethod()` fails in a method that is not available, in both runtimes.
* `Type.Assembly` fails in NativeAOT for blocked types, and `ResourceManager(Type)` uses the name of the type, so
  `new ResourceManager(typeof(InternalType))` style resource lookups with a blocked type do not work.
* `Assembly.EntryPoint` is `null` if the entry point method is not public (`Main` of a `Program` class is private in the C# top-level statements and
  internal in the `internal` types), as the method has no metadata.
* `where T : new()` and `abstract` constraints of a type argument that is a blocked type can't be checked when NativeAOT instantiates a generic
  type with the type loader (the blocked type reports no constructors), so the instantiation fails with `ArgumentException`.
* Assemblies created with `Reflection.Emit` are not subject to the attribute in CoreCLR.
* The attribute is not a security boundary: the metadata stays in the assembly file on CoreCLR (and is read by tools such as `MetadataLoadContext`), and
  the compiler generated types of generic methods and types are listed by name (see "Types that stay reflectable").
* In CoreCLR, `Delegate.CreateDelegate(type, target, name)` does not find a public virtual method that the target inherits from a type that is available
  if the type of the target overrides it with a member that is not available (NativeAOT binds the inherited method and dispatches to the override).
* Assemblies that were built for .NET Framework 4.5 or later and that carry the attribute (it had no effect since .NET 6) get the behavior
  described here when they are loaded or compiled, which is a breaking change for them.
* The type map lookup of a placeholder reference only searches the type map of the module of the metadata that contains the reference. That is
  the only module in single file NativeAOT (the framework and all the assemblies of the program are in the same module and metadata blob), and
  the compiler does not support reflection over a multi file compilation, so this has no effect. The lookup is not extended to other modules,
  because a metadata handle is only meaningful for its own metadata reader and the hash code of a type is not unique across modules.
* `AppContext` opt-out and a diagnostic that warns about the reflection requests that a hidden type or member drops are not part of this change.

## ILC implementation

* `DisablePrivateReflectionMetadataBlockingPolicy` wraps the existing blocking policy and reports `IsStripped` for the types and for the members
  described above (a method or field is stripped if the member rule hides it). `IsBlocked` is true for a stripped type, a stripped member and the members of a stripped type,
  so ILC generates no method, field, constructor, property, event or custom attribute records for them, and no reflection invoke or field mapping.
  The code, the vtables, the GC layout and the unwind and stack trace data are kept.
* `MetadataBlockingPolicy.IsStripped` / `IsHardBlocked`: hard blocking keeps the old rule (a blocked type argument blocks the
  instantiation). `MetadataManager.IsReflectionBlocked` ignores stripped arguments, array element types and function pointer
  types, and a stripped type that is only mentioned (`mentioned: true`, for example a `typeof` argument of a custom attribute)
  is not blocked either.
* Detached method metadata: the runtime type loader identifies generic methods and generic virtual methods by their metadata handle (native layout
  method entries, the generic method hashtables, the generic virtual method tables and the dispatch cells). For a method that is stripped and that
  these need, `DetachedMethodMetadataNode` (`UsageBasedMetadataManager.AddNativeLayoutMetadataDependencies`) makes `MetadataManager`
  emit a minimal `Method` record (name, signature, generic parameters, flags with a marker bit, `0x10000`, that the 16 bits of the method attributes of an assembly can't have) that is not added to the methods of its type
  and does not make a property or event available. `GetMetadataHandleForNativeLayout` returns its handle to the native layout users, and
  `GetMetadataHandleForMethod` (`ldtoken`, the reflection tables) does not. The runtime rejects a method handle that is `nil` (an `ldtoken` of a stripped method) or has
  the marker (the `ldtoken` of a generic method from shared generic code builds its handle from the native layout entry) in `ExecutionDomain.GetMethod`.
  The records are passed from the scanner to the code generation compilation with `AnalysisBasedMetadataManager`.
* `CustomAttributeBasedDependencyAlgorithm`: an attribute that mentions a stripped type keeps its metadata, and the stripped types that
  it mentions (including the instances of stripped generic types and their type arguments) get a reflected MethodTable.
* `Transform` (MetadataTransform): `IMetadataPolicy.IsStripped` makes type references use a placeholder name that has the hash code
  of the type, which the runtime uses to find the MethodTable in the type map. `HandleTypeWithRealNames` and `HandleMethodSignatureWithRealNames` make the references that
  the stack trace records use (real names, not shared with the placeholder references, only for the types that contain a stripped type).
* `MetadataManager`: the stack trace records of stripped owners have real names (they are written only if the stack trace policy asks for the method names).
* IDynamicInterfaceCastable implementation interfaces that are stripped keep a constructed MethodTable.
* `EETypeNode` and `GenericDefinitionEETypeNode` set `StrippedIsAbstractFlag` and `StrippedIsSealedFlag` for the stripped types (see
  [Type attributes on NativeAOT](#type-attributes-on-nativeaot)).
* `SubstitutedILProvider`: `x == typeof(T)` is not folded to `false` for a stripped `T` that has no constructed MethodTable. A stripped
  type has no metadata, but the program can still have a Type object for it (made from a MethodTable that is not constructed), and the unreachable code
  that the folding leaves is an infinite loop.

## CoreCLR implementation

* `Module::IsPrivateReflectionDisabled` (native) reads the attribute by name once per module. `RuntimeModule` caches it.
* `RuntimeTypeHandle_GetReflectionVisibility` (native QCall) applies the rules for one type (is the type blocked, and which members are available:
  all, the public ones, none, or for the attribute classes the constructors, fields and properties and the public ones) and `IsMethodHiddenFromReflection` applies it to a method. They must match the ILC policy.
* `RuntimeTypeCache` caches the result per type (`IsReflectionBlocked`, `MemberVisibility`) and uses it for names, member population (`PopulateMethods`,
  `PopulateConstructors`, `PopulateRtFields`, `PopulateLiteralFields`, `PopulateEvents`, `PopulateProperties`, with the accessors in `Associates`, per declaring type
  so the members that a type inherits from a type of an annotated assembly are filtered as well), nested types, and
  `MemberInfoCache.AddMethod`/`AddField` and `GetGenericMethodInfo` (handles).
* `RuntimeCustomAttributeData` skips an attribute whose constructor is not available, whatever the assembly of the attributed member. `RuntimeType.ActivatorCache`,
  `TypeNameResolver`, `RuntimeModule.GetDefinedTypes`, `GetInterfaceMap`, `Marshal.OffsetOf`, `ValueType.Equals` and `COMDelegate::BindToMethodName` have the other checks.
* Diagnostics: `StackFrame`, `StackTrace.ToString`, `StackFrame.ToString`, `Exception.Source`, the type name of `Exception.ToString` and `DiagnosticMethodInfo` use methods, names and attributes that are
  created with `RuntimeType.GetMethodBaseForDiagnostics` and the `*ForDiagnostics` members of `RuntimeType`, which do not use the member caches of the type.
