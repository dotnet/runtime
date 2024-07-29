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
