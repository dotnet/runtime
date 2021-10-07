# Source Generator COM

The interop team has decided to invest some time into creating a source generator to help support COM scenarios. The basic goals of this work are as follows, in general order of importance:

1. Enable developers who use MCG to be able to move to the new source generator.
2. Enable developers to interoperate with COM without needing to use linker-unsafe code (the built-in system is linker unsafe)
3. Enable WinForms to move to the source generated COM support to help make it NativeAOT-compatible.
4. Generate .NET 6-compatible code for internal partners

This plan includes checkpoints for when we complete the different goals above to help guide team planning. The order of checkpoints below is primarily of convenience; some of them may be implemented in a different order than provided.

## Architecture

The COM Source Generator should be designed as a Roslyn source generator that uses C# as the "source of truth". By using C# as the source of truth, we can use Roslyn's rich type system to inspect the various parameter and return types and determine the correct marshalling mechanism. Additionally, we would not have to encode policy for mapping non-.NET types to .NET types, which can be an error prone and opinionated process.

COM has a platform-agnostic source of truth with IDL files and TLBs (type libraries). We propose that conversions from these files to a C# source of truth that the COM source generator can consume should be implemented by .NET CLI tools. These tools can be manually invoked before a build if the source of truth changes rarely, or they can be included in the build pipeline for a project to produce the C# source of truth at build-time. Alternatively, we can provide some of the hooks for the COM source generator as an API, and another source generator that reads IDL or TLB files from the AdditionalFiles collection could re-use the core of the COM source generator to generate similar code directly from the IDL or TLBs. Since Roslyn source generators cannot have dependencies on other generators, we cannot sequence both an IDL/TLB to C# generator and a C# to C# generator together.

> Open Question: Do we need a mechanism for cross-assembly compatibility? Specifically, given a COM object `c` that was created in assembly A, is casting `c` to a COM interface defined in assembly B (which A does not reference) a supported scenario?
> Providing this feature will require at least some shared types. I explored using type equivalence as a workaround here, but it doesn't work on non-COM interfaces, which would likely provide difficulties for using it to avoid shared types.

### Checkpoint 1: IUnknown compatibility

One of the primary use cases of MCG today is to provide a basic level of COM interop support on non-Windows platforms. Specifically, it provides enough of a compatibility layer to enable IUnknown-style ABIs to function as seamlessly as the built-in COM interop system. It does not enable IDispatch support, and none of the internal MCG customers use COM aggregation with MCG on non-Windows platforms. Additionally, COM activation is only well-defined on Windows, so it is also not applicable for non-Windows platforms.

As a result, to satisfy our first goal, we only need to provide basic support for IUnknown-style ABIs. We will use the `ComWrappers` and `IDynamicInterfaceCastable` APIs to create implementations of the provided COM interfaces. Since COM Activation is not supported, users will need to manually activate their COM objects or retrieve pointers to them in another fashion, and then use `ComWrappers.GetOrCreateObjectForComInstance` to create a wrapper object.

### Checkpoint 2: COM-focused marshaller types

Many COM APIs use types within the COM ecosystem to pass across the COM boundary.Many of these types, like `VARIANT`, `BSTR`, and `SAFEARRAY`, have built-in support in the runtime today. We should provide custom marshallers following the patterns defined in the custom type marshalling design in DllImportGenerator to implement these conversions. Users will be able to manually opt-into using them with the `MarshalUsingAttribute`.

We should also provide a marshaller that uses the generated `ComWrappers` type to easily enable marshalling a COM interface in method calls to another COM interface.

> Open Question: Do we want to enable using the `MarshalAsAttribute` as well for these types?
> This would require us to hard-code in support for these marshallers into the COM source generator, but would provide better backward compatibility and easier migration.

### Checkpoint 3: Activation support

Currently, .NET provides some mechanisms to more easily support COM Activation. Although this feature is unused in MCG-like scenarios, it may be used in WinForms scenarios and other internal customer scenarios we are targeting. We should consider providing some support similar to the built-in support to streamline the "activate a COM object and get a .NET wrapper object" workflow.

### Checkpoint 4: IDispatch compatibility

Many APIs used in UI scenarios, in particular WinForms and Office APIs, use the late-binding provided by the IDispatch interface. To successfully support WinForms in full with a COM source generator solution, we will need to support IDispatch-based APIs.

> Open Question: How do we plan on supporting "Variants containing COM records"? Is this something that is required for supporting WinForms?

We do not plan on supporting `IDispatch` integration with C# `dynamic`, at least for the first release of the COM source generator. Although the built-in system currently supports it, the integration is primarily used with the PIAs provided for Office, which we do not plan on regenerating with this tooling. Additionally, System.Text.Json just backed out their `dynamic` integration for .NET 6.0, so we should consider following suit unless we get strong feedback otherwise. In any case, we should be sure to design the integration in a trimmable manner if possible to reduce overhead.

### Checkpoint 5: .NET 6-compatible output

A very important component of source generators is determining how to trigger them. For the DllImportGenerator, we trigger on a new attribute type, `GeneratedDllImportAttribute`, that is applied in place of the previous `DllImportAttribute`. For the JSON source generator, the team decided to have developers define an empty `JsonSerializerContext`-derived class and add `JsonSerializableAttribute` attribute on that context type that each point to a type that the generated serialization context should support. Below I've included some of the ideas that we've had for potential designs:

#### Option 1: Annotated ComWrappers stub

Option 1 is a similar design to the JSON generator, where the user defines a stub derived from a well-defined type and attributes it:

```csharp
// UserProvided.cs

[Guid("4b69d271-5c99-4f95-b1eb-381e6e689f1a")]
interface IMyComInterface
{
    void Foo();
}

[Guid("cb95a067-10f6-41cc-bef5-946aa018eb29")]
interface IMyOtherComInterface
{
    void Baz();
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
[GenerateComWrapperFor(typeof(IMyOtherComInterface))]
partial class MyComWrappers : ComWrappers
{
}
```

```csharp
// Generated.g.cs
partial class MyComWrappers : ComWrappers
{
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        if (obj is IMyComInterface)
        {
            // ...
        }
        if (obj is IMyOtherComInterface)
        {
            // ...
        }
        count = 0;
        return null;
    }

    protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        return new ComObject(externalComObject);
    }

    private class ComObject : IDynamicInterfaceCastable
    {
        private IntPtr iMyComInterface;
        private IntPtr iMyOtherComInterface;

        public ComObject(IntPtr externalComObject)
        {
            // ...
        }
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyComInterfaceImpl : IMyComInterface
    {
        // ...
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyOtherComInterfaceImpl : IMyOtherComInterface
    {
        // ...
    }

    public static readonly MyComWrappers Instance = new();

    public struct Marshaller<T>
    {

    }
}
```

In this model, the only attributes required are the built-in `GuidAttribute` on the interface, and a new `GenerateComWrappersForAttribute` which would be inserted into the compilation with the "post initialization sources" functionality in a source generator.

Pros:

- The user only has to annotate the ComWrappers type to enable the source generation.
- The ComWrappers type exists in the user's source.
- As the user can define multiple attributed ComWrappers-derived types, this design can be a little more linker optimized as it won't have type checks for every COM interface in the assembly.
  - Well grouped sets of interfaces might all be able to be linked out completely.

Cons:

- This design with the runtime wrapper object being defined internally makes using the same runtime wrapper object between multiple attributed `ComWrappers`-derived types difficult as the wrappers are completely distinct types, even within the same assembly.
- This design causes difficulties with automatic integration with the "custom type marshalling" design used with DllImportGenerator as it becomes difficult to determine how to tie an interface to a ComWrappers that implements it.
  - What if two different `ComWrappers`-derived types have a `GenerateComWrappersForAttribute` that point to the same interface? Which one do we use for default marshalling?
- There is no mechanism in this design to determine which interfaces to "export" with a C# -> TLB tool (such as a new implementation of TlbExp)

#### Option 2: `GeneratedComImportAttribute` and `GeneratedComVisibleAttribute`

Option 2 has more parallels to the designs of the DllImportGenerator and the proposed design for custom native type marshalling. The developer would use the `GeneratedComImportAttribute` or the `GeneratedComVisibleAttribute` on their defined interfaces, and the source generator would generate a `ComWrappers`-derived type that handles all of the annotated interfaces. The name of this `ComWrappers` type would be supplied by an analyzer config option, possibly provided through MSBuild.

```csharp
// UserProvided.cs

[GeneratedComImport]
[Guid("4b69d271-5c99-4f95-b1eb-381e6e689f1a")]
partial interface IMyComInterface
{
    void Foo();
}

[GeneratedComVisible]
[Guid("cb95a067-10f6-41cc-bef5-946aa018eb29")]
partial interface IMyOtherComInterface
{
    void Baz();
}
```

```xml
<!-- UserProvided.csproj -->
<PropertyGroup>
    <CsComGeneratedComWrappersName>MyComWrappers</CsComGeneratedComWrappersName>
</PropertyGroup>
```

```csharp
// Generated.g.cs

[NativeMarshalling(typeof(MyComWrappers.Marshaller<IMyComInterface>))]
partial interface IMyComInterface {}

[NativeMarshalling(typeof(MyComWrappers.Marshaller<IMyOtherComInterface>))]
partial interface IMyOtherComInterface {}

partial class MyComWrappers : ComWrappers
{
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        if (obj is IMyComInterface)
        {
            // ...
        }
        if (obj is IMyOtherComInterface)
        {
            // ...
        }
        count = 0;
        return null;
    }

    protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        return new ComObject(externalComObject);
    }

    private class ComObject : IDynamicInterfaceCastable
    {
        private IntPtr iMyComInterface;
        private IntPtr iMyOtherComInterface;

        public ComObject(IntPtr externalComObject)
        {
            // ...
        }
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyComInterfaceImpl : IMyComInterface
    {
        // ...
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyOtherComInterfaceImpl : IMyOtherComInterface
    {
        // ...
    }

    public static readonly MyComWrappers Instance = new();

    public struct Marshaller<T>
    {

    }
}
```

Pros:

- Similar experience to the `GeneratedDllImportAttribute`, where it basically replaces its built-in equivalent as a drop-in.
- Very easy to automatically hook up generated marshalling and to provide an easy process for other source generators to duplicate to support side-by-side as the policy is very simple.
- Since we only generate a single `ComWrappers`-derived type, we could also decide to make the `ComObject` type public for .NET 7+ scenarios and make it private for .NET 6 scenarios as we know there will only ever be one.
- The `GeneratedComImportAttribute` and `GeneratedComVisibleAttribute` attributes mirror the existing `ComImportAttribute` and `ComVisibleAttribute`, which will help provide a more intuitive view of the types and how to hook in tools that process C# -> TLB or TLB -> C# into the generator's flow.

Cons:

- This implementation may be slightly less linker-friendly as the single ComWrappers implementation will reference all annotated interfaces.
- The `ComWrappers`-derived type is not defined by the user in their source, instead being generated from other inputs.

#### Option 3: Annotated `ComWrappers`-derived type and `GeneratedComImportAttribute`/`GeneratedComVisibleAttribute`

In this design, the user would both annotate a `ComWrappers`-derived type and annotate the interfaces themselves.

```csharp
// UserProvided.cs

[GeneratedComImport]
[Guid("4b69d271-5c99-4f95-b1eb-381e6e689f1a")]
partial interface IMyComInterface
{
    void Foo();
}

[GeneratedComVisible]
[Guid("cb95a067-10f6-41cc-bef5-946aa018eb29")]
partial interface IMyOtherComInterface
{
    void Baz();
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
[GenerateComWrapperFor(typeof(IMyOtherComInterface))]
partial class MyComWrappers : ComWrappers
{
}
```

```csharp
// Generated.g.cs
partial interface IMyComInterface {}

partial interface IMyOtherComInterface {}

partial class MyComWrappers : ComWrappers
{
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        if (obj is IMyComInterface)
        {
            // ...
        }
        if (obj is IMyOtherComInterface)
        {
            // ...
        }
        count = 0;
        return null;
    }

    protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        return new ComObject(externalComObject);
    }

    private class ComObject : IDynamicInterfaceCastable
    {
        private IntPtr iMyComInterface;
        private IntPtr iMyOtherComInterface;

        public ComObject(IntPtr externalComObject)
        {
            // ...
        }
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyComInterfaceImpl : IMyComInterface
    {
        // ...
    }

    [DynamicInterfaceCastableImplementation]
    private interface IMyOtherComInterfaceImpl : IMyOtherComInterface
    {
        // ...
    }

    public static readonly MyComWrappers Instance = new();

    public struct Marshaller<T>
    {

    }
}
```

Pros:

- Similar experience to the `GeneratedDllImportAttribute`, where it basically replaces its built-in equivalent as a drop-in.
- The `GeneratedComImportAttribute` and `GeneratedComVisibleAttribute` attributes mirror the existing `ComImportAttribute` and `ComVisibleAttribute`, which will help provide a more intuitive view of the types and how to hook in tools that process C# -> TLB or TLB -> C# into the generator's flow.

Cons:

- Multiple attributes would be required to actually trigger the code generation. There would be more scenarios that require error diagnostics.
- This design with the runtime wrapper object being defined internally makes using the same runtime wrapper object between multiple attributed `ComWrappers`-derived types difficult as the wrappers are completely distinct types, even within the same assembly.
- This design causes difficulties with automatic integration with the "custom type marshalling" design used with DllImportGenerator as it becomes difficult to determine how to tie an interface to a ComWrappers that implements it.
  - What if two different `ComWrappers`-derived types have a `GenerateComWrappersForAttribute` that point to the same interface? Which one do we use for default marshalling?

#### Option 4: Reuse built-in COM attributes with Generated `ComWrappers`-derived type

The built-in COM interop system doesn't enforce usage only with COM scenarios, so one option would be to re-use all existing attributes, `ComImportAttribute`, `ComVisibleAttribute`, `CoClassAttribute`, etc. to act as triggers for the source generator. Then, using either a model from Option 2 or 3 to define the `ComWrappers`-derived type, the generator would generate all the support code for using these COM interfaces without using the built-in system.

Pros:

- Can use de-compiled TlbImp-generated COM Interop code with minimal changes.
- Can be designed such that no new types are required to ship with the generator or that the generator must add to the compilation.

Cons:

- Some changes from the TlbImp-generated code would still be required to avoid falling into the built-in system.
- We wouldn't be able to easily fix the warts of the existing system because that would break possible back-compat.
- The runtime might have cases where it makes assumptions based on how types are marked that this may interact with.
- It would be much easier to accidentally use the old system and get in a place where 2 objects, one from the built-in system and one from the generated system, both represent the same native object.

#### Option 5: Something Else?

I'm open to any other ideas people have on how to trigger the generator.

### Checkpoint 6: Aggregation support

COM supports a concept of aggregation, which the built-in .NET COM system supports. We currently don't have plans to support aggregation in the COM source generator, but we take care to avoid designing ourselves into a corner where implementing support is difficult if we decide to support it.

### Checkpoint 7: COM Event support

COM with IDispatch has a pattern that supports events. Supporting COM "events" requires quite a bit of support code, so we should consider only providing support if and when a feature request comes in for it, and possibly not supporting it in a .NET 6 compatibility mode where all support code needs to be source-included in the assembly.

### Extra: Analyzers to help write COM Interop code

The built-in COM system has quite a few gotchas and rough corners. We should consider writing some analyzers to assist developers with writing their COM-interop APIs/interfaces. If we decide to implement the COM source generator by using the built-in COM attributes as our triggering mechanism, then we could ship these analyzers with the generator since their diagnostics would apply to both the built-in and source-generated scenarios.
