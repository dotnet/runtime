# [ReflectionBlocked]: types without reflection metadata


## Why

ILC makes a type reflection visible for many reasons that a program can not influence: constructed types get a "with
metadata" MethodTable, every type argument of such a type gets metadata, attribute classes, type map values, and so on.
A library that knows that nothing will ever reflect over some of its types (for example the generated static helper
classes and marker types of CsWinRT 3.0) has no way to say so. This attribute lets the author say it, per type. It is
a contract of the author, not an ILC switch that applies to everything.

## Shape

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ReflectionBlockedAttribute : Attribute { }
}
```

ILC matches the attribute by namespace and name on the type definition, in any assembly. A library that targets runtimes
without the attribute can declare its own internal copy.

## What is blocked

For a type annotated with the attribute ("stripped type"), and for every type nested in it:

* no type definition, no method, field, property or event records, no custom attributes on the type or its members;
* no names in stack traces (the stack trace records of its methods are dropped; frames print as unknown);
* references to the type from other metadata (base types, signatures, generic arguments) are emitted as a placeholder
  reference without the name of the type;
* custom attributes that mention the type (a typeof or enum argument, or a generic attribute instantiated over it) are dropped, as for any other blocked type.

## What is not affected

* code generation, MethodTables, allocation, casts, interface dispatch, static abstract members, virtual calls;
* `object.GetType()`, `typeof(T)`, `Type` equality, `IsClass`, `IsValueType`, `IsInterface`, `BaseType`, `IsAssignableFrom`
  (all answered from the MethodTable);
* **types that mention the stripped type**: a generic instantiation over it, an array of it, a type derived from it,
  a method with a parameter of that type. These keep their metadata (this is the difference to types that are blocked
  for other reasons, where a blocked type argument blocks the whole instantiation). The stripped type is then present only
  as an anonymous type handle. All references to stripped types in a module share one placeholder reference, so resolving
  such a reference by name (for example `ParameterInfo.ParameterType` of a visible method whose signature mentions a stripped
  type) throws `NotSupportedException`. This includes binding by parameter types (`GetMethod(name, Type[])`, `Activator.CreateInstance(type, args)`) over an overload set where any overload mentions a stripped type.

## Runtime behavior

The runtime represents a stripped type with a `RuntimeBlockedTypeInfo`. `Name`, `FullName` and `ToString()` return a
placeholder that contains the address of the type (so code that prints types, such as the message of an
`InvalidCastException`, keeps working), `Namespace` and `DeclaringType` are null and `MetadataToken` throws
`InvalidOperationException`. Members that need the metadata (`GUID`, `Assembly`, `StructLayoutAttribute`,
`MethodBase.GetMethodFromHandle` and `FieldInfo.GetFieldFromHandle` for members of the type) throw
`NotSupportedException`. Custom attributes and members read as empty (the implemented interfaces are available, they come from the MethodTable). A constructed type over a stripped argument reports
its generic arguments (`GetGenericArguments()`), but its `FullName` throws because it has to name the assembly of the argument.
Reflecting over a generic type nested in a stripped type (a closure or an iterator of a generic method) throws `NotSupportedException`.

`RuntimeMethodHandle` and `RuntimeFieldHandle` values of members of a stripped type compare equal only if they are the same handle.

## Restrictions (checked by ILC)

* Only non generic types. A generic type with the attribute is a compile error (ILC error 1049).
  Generic types nested in a stripped type (the closures and iterators that the compiler generates, or declared ones) are stripped as well.
* Generic methods on a stripped type that the type loader needs at runtime (generic virtual methods, `MakeGenericMethod`,
  `ldtoken` of a generic method, shared generic methods that need a dictionary lookup through the type loader) are a compile
  error (ILC fails): the runtime identifies generic methods by their metadata and would otherwise confuse them with each other.
  The same applies to generic methods whose signature mentions a stripped type, because all stripped types share one
  placeholder reference and overloads that differ only by them could not be told apart.
* Enums and delegates are not valid targets (`Enum.ToString`, `DynamicInvoke` need metadata), also when nested in a stripped type.
* Exception types should not be stripped: their `Message` and `ToString` use reflection data and unhandled exception output
  would not be useful. This is not enforced.

## Not covered by the prototype

* No warnings when a reflection feature (dataflow annotations, `Activator`, `GetMethod`, ...) targets a stripped type; the
  request is dropped and fails at runtime.
* Method level and field level metadata handles used by the runtime for reflection (`RuntimeMethodHandle`, reflection
  based delegate creation) are 0 for members of stripped types. The runtime throws `NotSupportedException` when such a handle
  is converted to a `MethodBase` or `FieldInfo`.
* `--stacktracedata` records of stripped types are dropped, so stack traces through them are less useful.
* Dynamic instantiation of a stripped class that has no default constructor metadata (`Activator.CreateInstance`, a `new()`
  constraint through the type loader) and `RuntimeHelpers.GetUninitializedObject` on an abstract stripped class are not supported.
* A stripped type is still referenced by a type reference record of the metadata of any type that mentions it, which costs
  a little metadata (the shared placeholder reference).

## Interaction with type maps

A stripped type can be the key or the value of a type map entry. `TypeMapping.GetOrCreate*TypeMapping` returns a `Type`
whose name is a placeholder.

## ILC implementation

* `ReflectionBlockedAttributeMetadataBlockingPolicy` wraps the existing blocking policy and reports `IsStripped` for
  annotated types; `IsBlocked` is true for a stripped type and its members.
* `MetadataBlockingPolicy.IsStripped` / `IsHardBlocked`: hard blocking keeps the old rule (a blocked type argument blocks the
  instantiation). `MetadataManager.IsReflectionBlocked` ignores stripped arguments, array element types and function pointer
  types (`IsReflectionBlockedOrMentionsStripped` is the old, strict rule used for custom attribute arguments).
* `Transform` (MetadataTransform): `IMetadataPolicy.IsStripped` makes type references use a placeholder name.
* `MetadataManager`: stack trace records skip stripped owners; generic methods on stripped types that the type loader needs are rejected.
