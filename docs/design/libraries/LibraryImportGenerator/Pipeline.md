# P/Invoke Generation Pipeline

The P/Invoke source generator is responsible for finding all methods marked with `LibraryImportAttribute` and generating code for their implementations (stubs) and corresponding P/Invokes that will be called by the stubs. For every method, the steps are:

1. [Process the symbols and metadata](#symbols-and-metadata-processing) for the method, its parameters, and its return type.
1. [Determine the marshalling generators](#marshalling-generators) that will be responsible for generating the stub code for each parameter and return
1. [Generate the stub code](#stub-code-generation)
1. [Generate the corresponding P/Invoke](#pinvoke)
1. Add the generated source to the compilation.

The pipeline uses the Roslyn [Syntax APIs](https://docs.microsoft.com/dotnet/api/microsoft.codeanalysis.csharp.syntax) to create the generated code. This imposes some structure for the marshalling generators and allows for easier inspection or modification (if desired) of the generated code.

## Symbol and metadata processing

The generator processes the method's `LibraryImportAttribute` data, the method's parameter and return types, and the metadata on them (e.g. [`LCIDConversionAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute), [`MarshalAsAttribute`][MarshalAsAttribute], [struct marshalling attributes](StructMarshalling.md)). This information is used to determine the corresponding native type for each managed parameter/return type and how they will be marshalled.

A [`TypePositionInfo`][src-TypePositionInfo] is created for each type that needs to be marshalled. For each parameter and return type, this captures the managed type, managed and native positions (return or index in parameter list), and marshalling information.

The marshalling information is represented by various subclasses of [`MarshallingInfo`][src-MarshallingAttributeInfo] and represents all user-defined marshalling information for the specific parameter or return type. These classes are intended to simply capture any specified marshalling information, not interpret what that information means in terms of marshalling behaviour; that is handled when determining the [marshalling generator](#marshalling-generators) for each `TypePositionInfo`.

The processing step also includes handling any implicit parameter/return types that are required for the P/Invoke, but not part of the managed method signature; for example, a method with [`PreserveSig=false`][PreserveSig] requires an HRESULT return type and potentially an out parameter matching the managed method's return type.

### `PreserveSig=false`

The below signature indicates that the native function returns an HRESULT, but has no other return value (out parameter).

```C#
[LibraryImport("Lib", PreserveSig = false)]
static partial void Method();
```
Processing the above signature would create a `TypePositionInfo` for the HRESULT return type for native call, with properties indicating that it is in the native return position and has no managed position. The actual P/Invoke would be:

```C#
[DllImport("Lib", EntryPoint = "Method")]
static partial int Method__PInvoke__();
```

The below signature indicates that the native function returns an HRESULT and also has an out parameter to be used as the managed return value.

```C#
[LibraryImport("Lib", PreserveSig = false)]
[return: MarshalAs(UnmanagedType.U1)]
static partial bool MethodWithReturn();
```

Processing the above signature would create a `TypePositionInfo` for the HRESULT return type for native call, with properties indicating that it is in the native return position and has no managed position. The `TypePositionInfo` representing the `bool` return on the managed method would have properties indicating it is the last parameter for the native call and is in the managed return position. The actual P/Invoke would be:

```C#
[DllImport("Lib", EntryPoint = "MethodWithReturn")]
static partial int MethodWithReturn__PInvoke__(byte* retVal);
```

## Marshalling generators

Each parameter and return for the method is handled by an [`IMarshallingGenerator`][src-MarshallingGenerator] instance. The processed information for each parameter and return type is used to determine the appropriate marshalling generator for handling that type. Support for different types can be added in the form of new implementations of `IMarshallingGenerator`.

The marshalling generators are responsible for generating the code for each [stage](#stages) of the stub. They are intended to be stateless, such that they are given all the data ([`TypePositionInfo`][src_TypePositionInfo]) for which they need to generate marshalling code and the context ([`StubCodeContext`][src-StubCodeContext]) under which that code should be generated.

## Stub code generation

Generation of the stub code happens in stages. The marshalling generator for each parameter and return is called to generate code for each stage of the stub. The statements and syntax provided by each marshalling generator for each stage combine to form the full stub implementation.

The stub code generator itself will handle some initial setup and variable declarations:
- Assign `out` parameters to `default`
- Declare variable for managed representation of return value
- Declare variables for native representation of parameters and return value (if necessary)

### Stages

1. `Setup`: initialization that happens before marshalling any data
    - If the method has a non-void return, call `Generate` on the marshalling generator for the return
    - Call `Generate` on the marshalling generator for every parameter
1. `Marshal`: conversion of managed to native data
    - Call `Generate` on the marshalling generator for every parameter
1. `Pin`: data pinning in preparation for calling the generated P/Invoke
    - Call `Generate` on the marshalling generator for every parameter
    - Ignore any statements that are not `fixed` statements
1. `Invoke`: call to the generated P/Invoke
    - Call `AsArgument` on the marshalling generator for every parameter
    - Create invocation statement that calls the generated P/Invoke
1. `KeepAlive`: keep alive any objects who's native representation won't keep them alive across the call.
    - Call `Generate` on the marshalling generator for every parameter.
1. `Unmarshal`: conversion of native to managed data
    - If the method has a non-void return, call `Generate` on the marshalling generator for the return
    - Call `Generate` on the marshalling generator for every parameter
1. `GuaranteedUnmarshal`: conversion of native to managed data even when an exception is thrown
    - Call `Generate` on the marshalling generator for every parameter.
1. `Cleanup`: free any allocated resources
    - Call `Generate` on the marshalling generator for every parameter

Generated P/Invoke structure (if no code is generated for `GuaranteedUnmarshal` and `Cleanup`, the `try-finally` is omitted):
```C#
<< Variable Declarations >>
<< Setup >>
try
{
    << Marshal >>
    << Pin >> (fixed)
    {
        << Invoke >>
    }
    << Keep Alive >>
    << Unmarshal >>
}
finally
{
    << GuaranteedUnmarshal >>
    << Cleanup >>
}
```

### Stub conditional features

Some marshalling optimizations are only available in specific scenarios. Generally, there are 4 basic marshalling contexts:

- P/Invoke
- Reverse P/Invoke
- User-defined structure marshalling
- Non-blittable array marshalling

This experiment generally is currently only focusing on two of the concepts: P/Invoke and non-blittable array marshalling (in the context of a P/Invoke).

There are three features for specialized marshalling features that may only be available in some contexts:

- Pinning to marshal data without copying (the `fixed` statement)
- Stack allocation across the native context (using the `stackalloc` keyword or https://github.com/dotnet/runtime/issues/25423)
- Storing additional temporary state in extra local variables

Support for these features is indicated in code by the `abstract` `SingleFrameSpansNativeContext` and `AdditionalTemporaryStateLivesAcrossStages` properties on the `StubCodeContext` type. The `SingleFrameSpansNativeContext` property represents whether or not both pinning and stack-allocation are supported. These concepts are combined because we cannot safely support a conditional-stackalloc style API (such as https://github.com/dotnet/runtime/issues/52065) and safely get a pointer to data without also being able to pin.

The various scenarios mentioned above have different levels of support for these specialized features:

| Scenarios | Pinning and Stack allocation across the native context | Storing additional temporary state in locals |
|------|-----|-----|
| P/Invoke | supported | supported |
| Reverse P/Invoke | unsupported | supported |
| User-defined structure content marshalling | unsupported | unsupported |
| non-blittable array marshalling | unsupported | unuspported |

To help enable developers to use the full model described in the [Struct Marshalling design](./StructMarshalling.md), we declare that in contexts where `AdditionalTemporaryStateLivesAcrossStages` is false, developers can still assume that state declared in the `Setup` phase is valid in any phase, but any side effects in code emitted in a phase other than `Setup` will not be guaranteed to be visible in other phases. This enables developers to still use the identifiers declared in the `Setup` phase in their other phases, but they'll need to take care to design their generators to handle these rules.

### `SetLastError=true`

The stub code generation also handles [`SetLastError=true`][SetLastError] behaviour. This configuration indicates that system error code ([`errno`](https://en.wikipedia.org/wiki/Errno.h) on Unix, [`GetLastError`](https://docs.microsoft.com/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror) on Windows) should be stored after the native invocation, such that it can be retrieved using [`Marshal.GetLastWin32Error`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.getlastwin32error).

This means that, rather than simply invoke the native method, the generated stub will:

1. Clear the system error by setting it to 0
2. Invoke the native method
3. Get the system error
4. Set the stored error for the P/Invoke (accessible via `Marshal.GetLastWin32Error`)

A core requirement of this functionality is that the P/Invoke called in (2) is blittable (the purpose of the P/Invoke source generator), such that there will be no additional operations (e.g unmarshalling) after the invocation that could change the system error that is retrieved in (3). Similarly, (3) must not involve any operations before getting the system error that could change the system error. This also relies on the runtime itself handling preserving the last error (see `BEGIN/END_PRESERVE_LAST_ERROR` macros) during JIT and P/Invoke resolution.

Clearing the system error (1) is necessary because the native method may not set the error at all on success and the system error would retain its value from a previous operation. The developer should be able to check `Marshal.GetLastWin32Error` after a P/Inovke to determine success or failure, so the stub explicitly clears the error before the native invocation, such that the last error will indicate success if the native call does not change it.

## P/Invoke

The P/Invoke called by the stub is created based on the user's original declaration of the stub. The signature is generated using the syntax returned by `AsNativeType` and `AsParameter` of the marshalling generators for the return and parameters. Any marshalling attributes on the return and parameters of the managed method - [`MarshalAsAttribute`][MarshalAsAttribute], [`InAttribute`][InAttribute], [`OutAttribute`][OutAttribute] - are dropped.

The fields of the [`DllImportAttribute`][DllImportAttribute] are set based on the fields of `LibraryImportAttribute` as follows:

| Field                                             | Behaviour |
| ------------------------------------------------- | --------- |
| [`BestFitMapping`][BestFitMapping]                | Not supported. See [Compatibility](Compatibility.md).
| [`CallingConvention`][CallingConvention]          | Passed through to `DllImport`.
| [`CharSet`][CharSet]                              | Passed through to `DllImport`.
| [`EntryPoint`][EntryPoint]                        | If set, passed through to `DllImport`. If not set, explicitly set to method name.
| [`ExactSpelling`][ExactSpelling]                  | Passed through to `DllImport`.
| [`PreserveSig`][PreserveSig]                      | Handled by generated source. Not on generated `DllImport`.
| [`SetLastError`][SetLastError]                    | Handled by generated source. Not on generated `DllImport`.
| [`ThrowOnUnmappableChar`][ThrowOnUnmappableChar]  | Not supported. See [Compatibility](Compatibility.md).

### Examples

Explicit `EntryPoint`:

```C#
// Original declaration
[LibraryImport("Lib")]
static partial void Method(out int i);

// Generated P/Invoke
[DllImport("Lib", EntryPoint = "Method")]
static partial void Method__PInvoke__(int* i);
```

Passed through:

```C#
// Original declaration
[LibraryImport("Lib", EntryPoint = "EntryPoint", CharSet = CharSet.Unicode)]
static partial int Method(string s);

// Generated P/Invoke
[DllImport("Lib",  EntryPoint = "EntryPoint", CharSet = CharSet.Unicode)]
static partial int Method__PInvoke__(ushort* s);
```

Handled by generated source (dropped from `DllImport`):

```C#
// Original declaration
[LibraryImport("Lib", SetLastError = true)]
[return: [MarshalAs(UnmanagedType.U1)]
static partial bool Method([In][MarshasAs(UnmanagedType.LPWStr)] string s);

// Generated P/Invoke
[DllImport("Lib", EntryPoint = "Method")]
static partial byte Method__PInvoke__(ushort* s);
```

<!-- Links -->
[src-MarshallingAttributeInfo]: /src/libraries/System.Runtime.InteropServices/gen/Microsoft.Interop.SourceGeneration/MarshallingAttributeInfo.cs
[src-MarshallingGenerator]: /src/libraries/System.Runtime.InteropServices/gen/Microsoft.Interop.SourceGeneration/Marshalling/MarshallingGenerator.cs
[src-StubCodeContext]: /src/libraries/System.Runtime.InteropServices/gen/Microsoft.Interop.SourceGeneration/StubCodeContext.cs
[src-TypePositionInfo]: /src/libraries/System.Runtime.InteropServices/gen/Microsoft.Interop.SourceGeneration/TypePositionInfo.cs

[DllImportAttribute]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute
[MarshalAsAttribute]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute
[InAttribute]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.inattribute
[OutAttribute]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.outattribute

[BestFitMapping]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.bestfitmapping
[CallingConvention]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.callingconvention
[CharSet]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset
[EntryPoint]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.entrypoint
[ExactSpelling]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.exactspelling
[PreserveSig]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.preservesig
[SetLastError]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.setlasterror
[ThrowOnUnmappableChar]: https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.throwonunmappablechar
