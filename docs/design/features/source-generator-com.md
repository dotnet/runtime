# Source Generator COM

The interop team has decided to invest some time into creating a source generator to help support COM scenarios. The basic goals of this work are as follows, in general order of importance:

1. Enable developers who use MCG to be able to move to the new source generator.
2. Enable developers to interoperate with COM without needing to use linker-unsafe code (the built-in system is linker unsafe)
3. Enable dotnet/runtime developers to migrate the hand-written linker-safe COM interop code to use the generator instead.
4. Enable WinForms to move to the source generated COM support to help make it NativeAOT-compatible.
5. Generate .NET 6-compatible code for internal partners

This plan includes checkpoints for when we complete the different goals above to help guide team planning. The order of checkpoints below is primarily of convenience; some of them may be implemented in a different order than provided.

## Architecture

The COM Source Generator should be designed as a Roslyn source generator that uses C# as the "source of truth". By using C# as the source of truth, we can use Roslyn's rich type system to inspect the various parameter and return types and determine the correct marshalling mechanism. Additionally, we would not have to encode policy for mapping non-.NET types to .NET types, which can be an error prone and opinionated process.

### Other Sources of Truth

#### COM IDL and Type Libraries

COM has a platform-agnostic source of truth with IDL files and TLBs (type libraries). One alternative would be to use TLBs as a source of truth instead of C#. The TlbImp tool today uses TLBs as the source of truth.

Using TLBs or IDL as a source of truth, however, would cause issues with easily supporting COM APIs that are only used on non-Windows platforms, where the tooling to create a TLB does not exist. This is an important customer segment for us, as most of the users of MCG today use it due to requirements to run on non-Windows platforms. Additionally, some COM APIs that run on Windows do not provide a TLB, so those APIs would not be easily supportable if our source of truth was TLBs.

We propose that one of the following options should be taken as a Pri2 feature after we have a generator that uses C# as the source of truth:

1. A .NET CLI tool is created that takes in either IDL files or a TLB and generates C# definitions compatible with the COM Source Generator's "C# source of truth". Then the COM source generator will generate all of the underlying marshalling code.
2. Another Roslyn Source Generator is created that reads in IDL files or a TLB and shares code from the COM Source Generator to directly generate both the public surface area and the underlying marshalling code.

Since we cannot chain source generators such that one generator can see the outputs of another generator, these are the current options. Of these two options, I prefer option 1. Option 1 has the most parity to the existing TlbImp tool for PIA scenarios in my opinion.

#### C/C++ Headers

As COM interop is based on a small subset of the C and C++ ABIs, another natural option as a source of truth would be C or C++ header files. This direction is taken by the SharpGenTools and ClangSharp projects. As the C++ ABI surface area is extremely complex, is unstable between platforms and architectures, and extends well outside the bounds of a COM-like ABI, the Interop team does not want to own a project that uses C++ as a source of truth. We feel that this space is better served by the community.

Our suggestion would be for the community to follow a similar pattern of either generating the C# source of truth from C++, or sharing code and generating the C# surface area and implementation by using some of the COM Source Generator code internally.

Alternatively, the generator that reads C++ could generate its own implementations from scratch and could use the custom native marshalling attributes to provide default marshalling rules that make interoperability with the Interop team's source generated interop solutions much cleaner.

#### Win32Metadata winmd

The Win32Metadata project provides a richly typed surface area for interop scenarios with Win32 APIs, but it has some serious limitations that make it undesireable for us to use as our source of truth:

- The tooling only runs on Windows
- The tooling is extremely focused on the Win32 API
- The produced winmd has mappings for both COM and non-COM scenarios

The first two limitations would provide serious limitations for the current consumers of MCG today which all consume their own COM APIs, not only Win32 APIs, and primarily use MCG to run and/or build on non-Windows platforms.

The third limitation basically would mean that the COM source generator would have to ignore all of the non-COM APIs in the metadata, which would make it a poor replacement/companion for CsWin32, which can already handle all of the cases that the Win32Metadata project uses.

We would suggest to the CsWin32 team that they should instead adopt using some of our shared code in our generator infrastructure to push all of the mechanical marshalling logic to be generated by the Interop team source generator tooling, while they generate the public surface area how they see fit.

### Open Architectural Questions

We have some open questions for the overall architecture, which I have listed here:

1. Given two COM interfaces, `IComA` and `IComB`, each defined in their own project, project `A` and `B` respectively, should it be possible to cast a runtime wrapper object of type `IComA` to `IComB`?

In this scenario, we would have generated two different wrappers, one in assembly `A` for `IComA` and one in assembly `B` for `IComB`. Assuming `A` and `B` do not reference each other, the wrappers generated for each interface would not be able to statically have knowledge of the other interface.

Today, this cast would work today if the underlying COM object implements the respective interface based on its IID.

To implement this support, we would need to introduce a strong contract to be able to correctly identify that an interface is a COM interface. This would require either introducing a new public API, which would make .NET 6 compatibility more difficult, or emitting a new API, like an attribute, internally into each assembly and matching it by name (as the Roslyn compiler does with some types that are used in metadata or attributes). Additionally, there would need to be some shared interface to enable marshalling without having to manually track which ComWrappers instance created the wrapper object (to preserve object identity across marshalling).

2. Given two assemblies, `A` and `B`, that each define their own `IComFoo` interface that is a C# projection of an `IFoo` COM interface, should it be possible to cast from `A`'s `IComFoo` to `B`'s `IComFoo`?

Basic support for this feature would naturally fall out from supporting the scenario described in open question 1. Depending on implementation, we may be able to make this more or less efficient.

3. Given two assemblies, `A` and `B`, that each define their own `IComFoo` interface that is a C# projection of an `IFoo` COM interface, should `A`'s `IComFoo` be implicitly convertable to `B`'s `IComFoo`?

Supporting this would require either some form of type equivalence, which exists in a limited form in the runtime, or assembly-independent types, for which a C# proposal exists that has not been planned for any release. Since a solution here would require solutions for questions 1 and 2, we can likely require users to use explicit casts and avoid having any issues.

4. What about C-style interfaces that don't quite conform to COM requirements and don't inherit from `IUnknown`?

These interfaces are by definition not COM and would not be compatible with ComWrappers. With manually written marshalling code, a developer could use the custom native marshalling attributes to make the experience seamless to users for marshalling, although features like object identity would have to be implemented manually.

We could provide some assistance with some lower level building blocks in the generator to enable annotating a method with a "marshal the `this` object and call a method with the corresponding native signature to this managed signature at this vtable offset" attribute to allow people to manually put together their own vtables, but this would be a lower priority feature.

### Checkpoint 1: IUnknown compatibility

One of the primary use cases of MCG today is to provide a basic level of COM interop support on non-Windows platforms. Specifically, it provides enough of a compatibility layer to enable IUnknown-style ABIs to function as seamlessly as the built-in COM interop system. It does not enable IDispatch support, and none of the internal MCG customers use COM aggregation with MCG on non-Windows platforms. Additionally, COM activation is only well-defined on Windows, so it is also not applicable for non-Windows platforms.

As a result, to satisfy our first goal, we only need to provide basic support for IUnknown-style ABIs. We will use the `ComWrappers` and `IDynamicInterfaceCastable` APIs to create implementations of the provided COM interfaces. Since COM Activation is not supported, users will need to manually activate their COM objects or retrieve pointers to them in another fashion, and then use `ComWrappers.GetOrCreateObjectForComInstance` to create a wrapper object.

### Checkpoint 2: COM-focused marshaller types

Many COM APIs use types within the COM ecosystem to pass across the COM boundary.Many of these types, like `VARIANT`, `BSTR`, and `SAFEARRAY`, have built-in support in the runtime today. We should provide custom marshallers following the patterns defined in the custom type marshalling design in LibraryImportGenerator to implement these conversions. Users will be able to manually opt-into using them with the `MarshalUsingAttribute`.

We should also provide a marshaller that uses the generated `ComWrappers` type to easily enable marshalling a COM interface in method calls to another COM interface.

> Open Question: Do we want to enable using the `MarshalAsAttribute` as well for these types?
> This would require us to hard-code in support for these marshallers into the COM source generator, but would provide better backward compatibility and easier migration.

### Checkpoint 3: Activation support

Currently, .NET provides some mechanisms to more easily support COM Activation. Although this feature is unused in MCG-like scenarios, it may be used in WinForms scenarios and other internal customer scenarios we are targeting. We should consider providing some support similar to the built-in support to streamline the "activate a COM object and get a .NET wrapper object" workflow.

### Checkpoint 4: IDispatch compatibility

Many APIs used in UI scenarios, in particular WinForms and Office APIs, use the late-binding provided by the IDispatch interface. To successfully support WinForms in full with a COM source generator solution, we will need to support IDispatch-based APIs. This support would be a best-effort design based on what concepts can be easily translated to C# or .NET concepts. See https://github.com/dotnet/csharplang/discussions/471 for a conversation about some IDispatch cases that are not easily representable in C#.

> Open Question: How do we plan on supporting "Variants containing COM records"? Is this something that is required for supporting WinForms?

We do not plan on supporting `IDispatch` integration with C# `dynamic`, at least for the first release of the COM source generator. Although the built-in system currently supports it, the integration is primarily used with the PIAs provided for Office, which we do not plan on regenerating with this tooling. Additionally, System.Text.Json just backed out their `dynamic` integration for .NET 6.0, so we should consider following suit unless we get strong feedback otherwise. In any case, we should be sure to design the integration in a trimmable manner if possible to reduce overhead.

### Checkpoint 5: .NET 6-compatible output

A very important component of source generators is determining how to trigger them. For the LibraryImportGenerator, we trigger on a new attribute type, `LibraryImportAttribute`, that is applied in place of the previous `DllImportAttribute`. For the JSON source generator, the team decided to have developers define an empty `JsonSerializerContext`-derived class and add `JsonSerializableAttribute` attribute on that context type that each point to a type that the generated serialization context should support. Below are the potential API designs we considered. All options below would support the `GuidAttribute` attribute to specify an IID, the `InterfaceTypeAttribute` attribute with the `InterfaceIsIUnknown` member (and `InterfaceIsIDispatch` if Checkpoint 4 is achieved), and the `DispIdAttribute` for `IDispatch` scenarios. We selected Option 5 as it gives us the most flexibility to express the switches we want to express to the user without tying us down to legacy requirements or requiring additional metadata in basic scenarios.

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

[Guid("0b7f2845-9076-4560-87cb-c5c893d84b37")]
interface IMyDerivedComInterface : IMyComInterface
{
    void DerivedMethod();
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
[GenerateComWrapperFor(typeof(IMyOtherComInterface))]
[GenerateComWrapperFor(typeof(IDerivedComInterface))]
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
        if (obj is IMyDerivedComInterface)
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
        private IntPtr iDerivedComInterface;

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

    [DynamicInterfaceCastableImplementation]
    private interface IMyDerivedComInterfaceImpl : IMyDerivedComInterface
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
- This design causes difficulties with automatic integration with the "custom type marshalling" design used with LibraryImportGenerator as it becomes difficult to determine how to tie an interface to a ComWrappers that implements it.
  - What if two different `ComWrappers`-derived types have a `GenerateComWrappersForAttribute` that point to the same interface? Which one do we use for default marshalling?

To expand on this problem with a concrete example, let's take the following code snippet:

```csharp
// UserProvided.cs

[Guid("4b69d271-5c99-4f95-b1eb-381e6e689f1a")]
interface IMyComInterface
{
    void Foo();
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
partial class MyComWrappers : ComWrappers
{
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
partial class MyOtherComWrappers : ComWrappers
{
}
```

How would the COM source generator know which `ComWrappers`-derived type to have provide the default marshalling for `IMyComInterface`? Would an error diagnostic be required?

- There is no mechanism in this design to determine which interfaces to "export" with a C# -> TLB tool (such as a new implementation of TlbExp)

#### Option 2: `GeneratedComImportAttribute` and `GeneratedComVisibleAttribute`

Option 2 has more parallels to the designs of the LibraryImportGenerator and the proposed design for custom native type marshalling. The developer would use the `GeneratedComImportAttribute` or the `GeneratedComVisibleAttribute` on their defined interfaces, and the source generator would generate a `ComWrappers`-derived type that handles all of the annotated interfaces. The name of this `ComWrappers` type would be supplied by an analyzer config option, possibly provided through MSBuild.

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

[GeneratedComImport]
[Guid("0b7f2845-9076-4560-87cb-c5c893d84b37")]
interface IMyDerivedComInterface : IMyComInterface
{
    void DerivedMethod();
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

[NativeMarshalling(typeof(MyComWrappers.Marshaller<IMyDerivedComInterface>))]
partial interface IMyDerivedComInterface {}

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
        if (obj is IMyDerivedComInterface)
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
        private IntPtr iDerivedComInterface;

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

    [DynamicInterfaceCastableImplementation]
    private interface IMyDerivedComInterfaceImpl : IMyDerivedComInterface
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

- Similar experience to the `LibraryImportAttribute`, where it basically replaces its built-in equivalent as a drop-in.
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

[GeneratedComImport]
[Guid("0b7f2845-9076-4560-87cb-c5c893d84b37")]
interface IMyDerivedComInterface : IMyComInterface
{
    void DerivedMethod();
}

[GenerateComWrapperFor(typeof(IMyComInterface))]
[GenerateComWrapperFor(typeof(IMyOtherComInterface))]
[GenerateComWrapperFor(typeof(IMyDerivedComInterface))]
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
        if (obj is IMyDerivedComInterface)
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
        private IntPtr iDerivedComInterface;

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

    [DynamicInterfaceCastableImplementation]
    private interface IMyDerivedComInterfaceImpl : IMyDerivedComInterface
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

- Similar experience to the `LibraryImportAttribute`, where it basically replaces its built-in equivalent as a drop-in.
- The `GeneratedComImportAttribute` and `GeneratedComVisibleAttribute` attributes mirror the existing `ComImportAttribute` and `ComVisibleAttribute`, which will help provide a more intuitive view of the types and how to hook in tools that process C# -> TLB or TLB -> C# into the generator's flow.

Cons:

- Multiple attributes would be required to actually trigger the code generation, one on the interface and one on the `ComWrappers`-derived type. There would be more scenarios that require error diagnostics, as only applying one attribute of the pair would be an invalid scenario.
- This design with the runtime wrapper object being defined internally makes using the same runtime wrapper object between multiple attributed `ComWrappers`-derived types difficult as the wrappers are completely distinct types, even within the same assembly.
- This design causes difficulties with automatic integration with the "custom type marshalling" design used with LibraryImportGenerator as it becomes difficult to determine how to tie an interface to a ComWrappers that implements it.
  - What if two different `ComWrappers`-derived types have a `GenerateComWrappersForAttribute` that point to the same interface? Which one do we use for default marshalling?

#### Option 4: Reuse built-in COM attributes with Generated `ComWrappers`-derived type

The built-in COM interop system doesn't enforce usage only with COM scenarios, so one option would be to re-use all existing attributes, `ComImportAttribute`, `ComVisibleAttribute`, `CoClassAttribute`, etc. to act as triggers for the source generator. Then, using either a model from Option 2 or 3 to define the `ComWrappers`-derived type, the generator would generate all the support code for using these COM interfaces without using the built-in system.

Below is an example using the Option 2 mechanism to define the `ComWrappers`-derived type:


```csharp
// UserProvided.cs

[ComImport]
[Guid("4b69d271-5c99-4f95-b1eb-381e6e689f1a")]
partial interface IMyComInterface
{
    void Foo();
}

[ComVisible]
[Guid("cb95a067-10f6-41cc-bef5-946aa018eb29")]
partial interface IMyOtherComInterface
{
    void Baz();
}

[ComImport]
[Guid("0b7f2845-9076-4560-87cb-c5c893d84b37")]
interface IMyDerivedComInterface : IMyComInterface
{
    new void Foo(); // The new slot mechanism would still be required in this scenario since this design focuses on backward compatibility as its primary focus.
    void DerivedMethod();
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

[NativeMarshalling(typeof(MyComWrappers.Marshaller<IMyDerivedComInterface>))]
partial interface IMyDerivedComInterface {}

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
        if (obj is IMyDerivedComInterface)
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
        private IntPtr iDerivedComInterface;

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

    [DynamicInterfaceCastableImplementation]
    private interface IMyDerivedComInterfaceImpl : IMyDerivedComInterface
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

- Can use de-compiled TlbImp-generated COM Interop code with minimal changes.
- Can be designed such that no new types are required to ship with the generator or that the generator must add to the compilation.

Cons:

- Some changes from the TlbImp-generated code would still be required to avoid falling into the built-in system.
- We wouldn't be able to easily fix the warts of the existing system because that would break possible back-compat.
- The runtime might have cases where it makes assumptions based on how types are marked that this may interact with.
- It would be much easier to accidentally use the old system and get in a place where 2 objects, one from the built-in system and one from the generated system, both represent the same native object.

#### Option 5 (Selected Design): Only `GeneratedComInterfaceAttribute` attribute with Generated `ComWrappers`-derived type

The built-in `ComImport` and `ComVisible` attributes have a lot of history and weird runtime behavior associated with them. Additionally the built-in `ComVisible` attribute actually takes a `bool` to determine if the applied to type is visible and it can be applied to methods as well to enable/disable COM visbility for the legacy automatic COM vtable generation that the .NET runtime has supported since .NET Framework 1.0. This option proposes introducing a single new attribute to cover the expected scenarios:

```csharp
[AttributeUsage(AttributeTargets.Interface)]
class GeneratedComInterfaceAttribute
{
    public GeneratedComInterfaceAttribute(Type comWrappersType);

    public GeneratedComInterfaceAttribute(Type comWrappersType, bool generateManagedObjectWrapper, bool generateComObjectWrapper);

    public Type ComWrappersType { get; };

    public bool GenerateManagedObjectWrapper { get; } = true;

    public bool GenerateComObjectWrapper { get; } = true;

    public bool ExportInterfaceDefinition { get; }
}
```

This attribute could be applied to any interface to generate the marshalling code for either RCW or CCW scenarios. The `ExportInterfaceDefinition` property would be used by any tool that wants to generate a metadata file, like a TlbExp successor, to determine which interfaces to export.

The `ComWrappers`-derived type used to generate the wrappers would be specified as the first parameter to the attribute. We will use a `System.Type` parameter instead of generic attributes to support downlevel platforms as generic-attribute support is still in preview. As this attribute is exclusively used by a source-generator, we can validate that the provided type derives from `ComWrappers` at compile time, so we don't get many gains from using generic attributes. By specifying the `ComWrappers`-derived type in the attribute on the interface, we ensure that each interface is associated with one `ComWrappers`-derived type.

### Checkpoint 6: Aggregation support

COM supports a concept of aggregation, which the built-in .NET COM system supports. We currently don't have plans to support aggregation in the COM source generator, but we take care to avoid designing ourselves into a corner where implementing support is difficult if we decide to support it.

### Checkpoint 7: COM Event support

COM with IDispatch has a pattern that supports events. Supporting COM "events" requires quite a bit of support code, so we should consider only providing support if and when a feature request comes in for it, and possibly not supporting it in a .NET 6 compatibility mode where all support code needs to be source-included in the assembly.

### Extra: Analyzers to help write COM Interop code

The built-in COM system has quite a few gotchas and rough corners. We should consider writing some analyzers to assist developers with writing their COM-interop APIs/interfaces. If we decide to implement the COM source generator by using the built-in COM attributes as our triggering mechanism, then we could ship these analyzers with the generator since their diagnostics would apply to both the built-in and source-generated scenarios.
