# P/Invoke Generation Pipeline

The P/Invoke source generator is responsible for finding all methods marked with `GeneratedDllImportAttribute` and generating code for their implementations (stubs) and corresponding P/Invokes that will be called by the stubs. For every method, the steps are:

1. [Process the symbols and metadata](#symbols-and-metadata-processing) for the method, its parameters, and its return type.
1. [Determine the marshalling generators](#marshalling-generators) that will be responsible for generating the stub code for each parameter and return
1. [Generate the stub code](#stub-code-generation) and corresponding P/Invoke
1. Add the generated source to the compilation.

The pipeline uses the Roslyn [Syntax APIs](https://docs.microsoft.com/dotnet/api/microsoft.codeanalysis.csharp.syntax) to create the generated code. This imposes some structure for the marshalling generators and allows for easier inspection or modification (if desired) of the generated code.

## Symbol and metadata processing

The generator processes the method's `GeneratedDllImportAttribute` data, the method's parameter and return types, and the metadata on them (e.g. [`LCIDConversionAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute), [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute), [struct marshalling attributes](StructMarshalling.md)). This information is used to determine the corresponding native type for each managed parameter/return type and how they will be marshalled.

A [`TypePositionInfo`](../DllImportGenerator/TypePositionInfo.cs) is created for each type that needs to be marshalled. This includes any implicit parameter/return types that are required for the P/Invoke, but not part of the managed method signature; for example, a method with `PreserveSig=false` requires an HRESULT return type and potentially an out parameter matching the managed method's return type.

## Marshalling generators

Each parameter and return for the method is handled by an [`IMarshallingGenerator`](../DllImportGenerator/Marshalling/MarshallingGenerator.cs) instance. The processed information for each parameter and return type is used to determine the appropriate marshalling generator for handling that type. Support for different types can be added in the form of new implementations of `IMarshallingGenerator`.

The marshalling generators are responsible for generating the code for each [stage](#stages) of the stub. They are intended to be stateless, such that they are given all the data ([`TypePositionInfo`](../DllImportGenerator/TypePositionInfo.cs)) for which they need to generate marshalling code and the context ([`StubCodeContext`](../DllImportGenerator/StubCodeContext.cs)) under which that code should be generated.

## Stub code generation

Generation of the stub code happens in stages. The marshalling generator for each parameter and return is called to generate code for each stage of the stub. The statements and syntax provided by each marshalling generator for each stage combine to form the full stub implementation.

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

### Stub conditional features

Some marshalling optimizations are only available in specific scenarios. Generally, there are 4 basic marshalling contexts:

- P/Invoke
- Reverse P/Invoke
- User-defined structure marshalling
- Non-blittable array marshalling

This experiment generally is currently only focusing on two of the concepts: P/Invoke and non-blittable array marshalling (in the context of a P/Invoke).

There are three categories for specialized marshalling features that may only be available in some contexts:

- Pinning to marshal data without copying (the `fixed` statement)
- Stack allocation across the native context (using the `stackalloc` keyword or https://github.com/dotnet/runtime/issues/25423)
- Storing additional temporary state in extra local variables

Support for these features is indicated in code by various `bool` properties on the `StubCodeContext`-derived type.

These various scenarios have different levels of support for these three features:


| Scenarios |Pinning | Stack allocation across the native context | Storing additional temporary state in locals |
|------|-----|-----|---------|
| P/Invoke | supported | supported | supported |
| Reverse P/Invoke | unsupported | unsupported | supported |
| User-defined structures | unsupported | unsupported for individual members | unsupported |
| non-blittable array marshalling in a P/Invoke | unsupported | unsupported (supportable with https://github.com/dotnet/runtime/issues/25423) | unuspported |
| non-blittable array marshalling not in a P/Invoke | unsupported | unsupported | unuspported |


### P/Invoke

The P/Invoke called by the stub is created based on the user's original declaration of the stub. The signature is generated using the syntax returned by `AsNativeType` and `AsParameter` of the marshalling generators for the return and parameters.