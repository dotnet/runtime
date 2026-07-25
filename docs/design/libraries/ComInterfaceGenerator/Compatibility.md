# Semantic Compatibility

Documentation on compatibility guidance and the current state. The version headings act as a rolling delta between the previous version.

## .NET 8

### Interface base types

IUnknown-derived interfaces are supported. IDispatch-based interfaces are disallowed. The default is IUnknown-derived (in comparison to the built-in support's default of IDispatch-derived).

### Marshalling rules

The marshalling rules are identical to LibraryImportGenerator's support.

### Interface inheritance

Interface inheritance is supported for up to one COM-based interface type. Unlike the built-in COM interop system, base interface methods do **NOT** need to be redefined. The source generator discovers the members from the base interface and generates the derived interface members at appropriate offsets.

The generator also generates shadow members in the derived interface for each base interface member. The shadow members have default implementations that call the base interface member, but the emitted code for the "COM Object Wrapper" implementation will override the shadow members with a call to the underlying COM interface member on the current interface. This shadow member support helps reduce `QueryInterface` overhead in interface inheritance scenarios.

### Interop with `ComImport`

Source-generated COM will provide limited opt-in interop with `ComImport`-based COM interop. In particular, the following scenarios are supported:

- Casting a "Com Object Wrapper" created using `StrategyBasedComWrappers` to a `ComImport`-based interface type.

This support is achieved through some internal interfaces and reflection-emit to shim a `DynamicInterfaceCastableImplementation` of a `ComImport` interface to use the built-in runtime interop marshalling support. The core of this experience is implemented by the `System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy` class.

## .NET 11

### Properties

A `[GeneratedComInterface]`-attributed interface may now declare ordinary C# properties (`T Name { get; set; }`, `{ get; }`, `{ set; }`). Each accessor maps to a vtable slot — getter first, then setter, in source order — matching the layout the built-in CLR produces for a `[ComVisible(true)]` managed interface. Inherited properties follow the same shadowing rules as inherited methods.

`[MarshalUsing]` may be applied directly to a property and is propagated to both accessor stubs. The `new` modifier may be used to shadow an inherited property. The `init` accessor is **not** supported on a `[GeneratedComInterface]` property — `init`-vs-`set` has no representation in the COM vtable, so the generator rejects it (`SYSLIB1091`).

### Indexers

C# indexers are supported alongside properties starting with .NET 11. Each accessor maps to its own vtable slot in source order, just like a property; the `[IndexerName]` attribute is honored and propagated through derived-interface shadows; and indexer overloads distinguished by index-parameter type are allowed. See the [Indexers](./Properties.md#indexers) section of Properties.md for the full design.

### Default-implemented members (DIM)

A method or property accessor with a user-supplied body on a `[GeneratedComInterface]` interface is now treated as a default-implemented member: it ships as managed-only sugar and is **not** assigned a vtable slot. This is the supported way to wrap a pair of ABI methods with a managed property abstraction, or to add helper methods that the wire ABI does not need to know about.

A property must have all accessors abstract or all accessors bodied — mixing the two is an error (`SYSLIB1091`). `[MarshalUsing]` and `[MarshalAs]` on a default-implemented member emit a warning (`SYSLIB1091`) because they have no ABI effect.

### `new` modifier on members

In earlier releases, `[GeneratedComInterface]` disallowed declaring any methods with the `new` modifier. This is no longer the case. Both methods and properties may now be declared with `new` to explicitly shadow an inherited base member; the shadowing member receives a fresh vtable slot appended after the base interface's slots, and the base member's slot continues to dispatch to its original target.
