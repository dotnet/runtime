# Generating Virtual Method Table Stubs

As a building block for the COM interface source generator, we've decided to build a source generator that enables developers to mark that a given interface method should invoke a function pointer at a particular offset into an unmanaged virtual method table, or vtable. We've decided to build this generator as a building block for a few reasons:

1. As part of the migration of dotnet/runtime to use source-generated P/Invokes, we encountered a few scenarios, particularly in the networking stacks, where non-blittable delegate interop was used because the native APIs do not have static entry points. For at least one of these scenarios, MsQuic, the native library provides a table of function pointers. From our experience, this mechanism for versioning is not uncommon and we feel that supporting native libraries that use a versioning scheme similar to this model is worthwhile for us to support.
2. There are native APIs that we are likely to interoperate with in the future that use native vtables but are not COM-oriented. In particular, the Java Native Interface API, which both dotnet/runtime and xamarin/java.interop interface with in various capacities, uses a vtable model to support exposing their APIs to C and C++. Additionally, its API does not conform to a COM-style IUnknown-based API.
3. Some COM-style APIs have some corner cases with non-COM-style interfaces. Specifically, some corners of the DirectX APIs are still vtable-based, but do not implement IUnknown. Providing this building block will allow developers to more easily consume these APIs with similar gestures as the rest of the DirectX API surface.
4. Our future COM interface source generator can build on this building block to provide defaults while allowing developers to use the features of this generator to override any default settings provided by the COM generator.

## Defined types

To support this generator, we will define the following APIs.

The `VirtualMethodIndexAttribute` can be applied to an interface method to trigger the generator. This method will provide the index into the vtable for the method, whether or not the method implicitly takes the native `this` pointer, and which marshalling directions to support. It also has many of the same members as `LibraryImportAttribute` to consistently provide the same marshalling support across source-generated marshalling.

```csharp
namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class VirtualMethodIndexAttribute : Attribute
{
    public VirtualMethodIndexAttribute(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public bool ImplicitThisParameter { get; set; } = true;

    public MarshalDirection Direction { get; set; } = MarshalDirection.Bidirectional;

    /// <summary>
    /// Gets or sets how to marshal string arguments to the method.
    /// </summary>
    /// <remarks>
    /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
    /// <see cref="StringMarshallingCustomType" /> must not be specified.
    /// </remarks>
    public StringMarshalling StringMarshalling { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> used to control how string arguments to the method are marshalled.
    /// </summary>
    /// <remarks>
    /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
    /// or must be set to <see cref="StringMarshalling.Custom" />.
    /// </remarks>
    public Type? StringMarshallingCustomType { get; set; }

    /// <summary>
    /// Gets or sets whether the callee sets an error (SetLastError on Windows or errno
    /// on other platforms) before returning from the attributed method.
    /// </summary>
    public bool SetLastError { get; set; }
}

```

New interfaces will be defined and used by the source generator to fetch the native `this` pointer and the vtable that the function pointer is stored in. These interfaces are designed to provide an API that various native platforms, like COM, WinRT, or Swift, could use to provide support for multiple managed interface wrappers from a single native object. In particular, these interfaces are designed to ensure it is possible support a managed gesture to do an unmanaged "type cast" (i.e., `QueryInterface` in the COM and WinRT worlds).

```csharp
namespace System.Runtime.InteropServices;

public readonly ref struct VirtualMethodTableInfo
{
    public VirtualMethodTableInfo(IntPtr thisPointer, ReadOnlySpan<IntPtr> virtualMethodTable)
    {
        ThisPointer = thisPointer;
        VirtualMethodTable = virtualMethodTable;
    }

    public IntPtr ThisPointer { get; }
    public ReadOnlySpan<IntPtr> VirtualMethodTable { get; }

    public void Deconstruct(out IntPtr thisPointer, out ReadOnlySpan<IntPtr> virtualMethodTable) // This method allows tuple-style `var (thisPtr, vtable) = virtualMethodTableInfo;` statements from this type.
    {
        thisPointer = ThisPointer;
        virtualMethodTable = VirtualMethodTable;
    }
}

public interface IUnmanagedVirtualMethodTableProvider
{
    protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);

    public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
        where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
    {
        // Dispatch from a non-virtual generic to a virtual non-generic with System.Type
        // to avoid generic virtual method dispatch, which is very slow.
        return GetVirtualMethodTableInfoForKey(typeof(TUnmanagedInterfaceType));
    }
}

public interface IUnmanagedInterfaceType<TUnmanagedInterfaceType> where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
{
}
```

## Required API Shapes

The user will be required to implement `IUnmanagedVirtualMethodTableProvider` on the type that provides the method tables, and `IUnmanagedInterfaceType<TUnmanagedInterfaceType>` on the type that defines the unmanaged interface. The `TUnmanagedInterfaceType` follows the same design principles as the generic math designs as somewhat of a "self" type to enable us to use the derived interface type in any additional APIs we add to support unmanaged-to-managed stubs.

Previously, each of these interface types were also generic on another type `T`. The `T` types were required to match between the two interfaces. This mechanism was designed to enable each native API platform to provide their own casting key, for example `IID`s in COM, without interfering with each other or requiring using reflection-based types like `System.Type`. However, practical implementation showed that providing just a "type key" was not enough information to cover any non-trivial scenarios (like COM) efficiently without effectively forcing a two-level lookup model or hard-coding type support in the `IUnmanagedVirtualMethodTableProvider<T>` implementation. Additionally, we determined that using reflection to get to attributes is considered "okay" and using generic attributes would enable APIs that build on this model like COM to effectively retrieve information from the `System.Type` instance without causing additional problems.

## Example Usage

### Flat function table

In this example, the native API provides a flat table of functions based on the provided version.

```cpp
// NativeAPI.cpp

struct NativeAPI
{
    int(*getVersion)();
    int(*add)(int x, int y);
    int(*multiply)(int x, int y);
};

namespace
{
    int getVersion()
    {
        return 1;
    }
    int add(int x, int y)
    {
        return x + y;
    }
    int multiply(int x, int y)
    {
        return x * y;
    }
    const NativeAPI g_nativeAPI = {
        &getVersion,
        &add,
        &multiply
    };
}

extern "C" bool GetNativeAPI(int version, NativeAPI const** ppNativeAPI)
{
    if (version > getVersion())
    {
        *ppNativeAPI = nullptr;
        return false;
    }
    *ppNativeAPI = &g_nativeAPI;
    return true;
}

```

```csharp
// User-written code
// NativeAPI.cs
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:DisableRuntimeMarshalling]

// Define the interface of the native API
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{

    [VirtualMethodIndex(0, ImplicitThisParameter = false, Direction = CustomTypeMarshallerDirection.In)]
    int GetVersion();

    [VirtualMethodIndex(1, ImplicitThisParameter = false, Direction = CustomTypeMarshallerDirection.In)]
    int Add(int x, int y);

    [VirtualMethodIndex(2, ImplicitThisParameter = false, Direction = CustomTypeMarshallerDirection.In)]
    int Multiply(int x, int y);
}

// Define our runtime wrapper type for the native interface.
unsafe class NativeAPI : IUnmanagedVirtualMethodTableProvider, INativeAPI.Native
{
    private CNativeAPI* _nativeAPI;

    public NativeAPI()
    {
        if (!CNativeAPI.GetNativeAPI(1, out _nativeAPI))
        {
            throw new InvalidOperationException();
        }
    }

    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type _)
    {
        return new(IntPtr.Zero, MemoryMarshal.Cast<CNativeAPI, IntPtr>(new ReadOnlySpan<CNativeAPI>(_nativeAPI, 1)));
    }
}

// This struct represent a flat table of function pointers that implements the native API.
// This can either be returned by the API (MSQuic) or be constructed manually from calling
// a method that returns function pointers and be used as a cache (OpenGL and Vulkan)
unsafe partial struct CNativeAPI
{
    IntPtr getVersion;
    IntPtr add;
    IntPtr multiply;

    [LibraryImport(nameof(NativeAPI))]
    public static partial bool GetNativeAPI(int version, out CNativeAPI* ppNativeAPI);
};

// Generated code for VirtualMethodIndex generator

// NativeInterfaces.g.cs
partial interface INativeAPI
{
    [DynamicInterfaceCastableImplementation]
    partial interface Native : INativeAPI
    {
    }
}

// ManagedToNativeStubs.g.cs
partial interface INativeAPI
{
    unsafe partial interface Native
    {
        int INativeAPI.GetVersion()
        {
            var (_, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<INativeAPI>();
            int retVal;
            retVal = ((delegate* unmanaged<int>)vtable[0])();
            return retVal;
        }
    }
}
partial interface INativeAPI
{
    unsafe partial interface Native
    {
        int INativeAPI.Add(int x, int y)
        {
            var (_, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<INativeAPI>();
            int retVal;
            retVal = ((delegate* unmanaged<int, int, int>)vtable[1])(x, y);
            return retVal;
        }
    }
}
partial interface INativeAPI
{
    unsafe partial interface Native
    {
        int INativeAPI.Multiply(int x, int y)
        {
            var (_, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<INativeAPI>();
            int retVal;
            retVal = ((delegate* unmanaged<int, int, int>)vtable[2])(x, y);
            return retVal;
        }
    }
}

// LibraryImport-generated code omitted for brevity
```

As this generator is primarily designed to provide building blocks for future work, it has a larger requirement on user-written code. In particular, this generator does not provide any support for authoring a runtime wrapper object that stores the native pointers for the underlying object or the virtual method table. However, this lack of support also provides significant flexibility for developers. The only requirement for the runtime wrapper object type is that it implements `IUnmanagedVirtualMethodTableProvider`.

The emitted interface implementation can be used in two ways:

1. The user's runtime wrapper object can directly implement the emitted `Native` interface. This method works for cases where all interfaces are statically known to exist (interfaces are not conditionally implemented on each object).
2. The user's runtime wrapper object can implement `IDynamicInterfaceCastable` and can return the handle of `INativeAPI.Native` when user code casts the wrapper to `INativeAPI`. This style is more commonly used for COM-style APIs.

### COM interface

```cpp
// C++ code
struct IUnknown
{
    virtual HRESULT QueryInterface(REFIID riid, void **ppvObject) = 0;
    virtual ULONG AddRef() = 0;
    virtual ULONG Release() = 0;
};

```
```csharp
// User-defined C# code
using System;
using System.Runtime.InteropServices;

interface IUnknown: IUnmanagedInterfaceType<IUnknown>
{
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(0)]
    int QueryInterface(in Guid riid, out IntPtr ppvObject);

    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(1)]
    uint AddRef();

    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(2)]
    uint Release();
}

class BaseIUnknownComObject : IUnmanagedVirtualMethodTableProvider, IDynamicInterfaceCastable
{
    private IntPtr _unknownPtr;

    public BaseIUnknownComObject(IntPtr unknown)
    {
        _unknownPtr = unknown;
    }

    unsafe VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        if (type == typeof(IUnknown))
        {
            return new VirtualMethodTableInfo(_unknownPtr, new ReadOnlySpan<IntPtr>(**(IntPtr***)_unknownPtr), 3);
        }
        return default;
    }

    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        if (Type.GetTypeFromHandle(interfaceType) == typeof(IUnknown))
        {
            return typeof(IUnknown.Native).TypeHandle;
        }
        return default;
    }

    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        return Type.GetTypeFromHandle(interfaceType) == typeof(IUnknown);
    }
}

// Generated code for VirtualMethodIndex generator

// NativeInterfaces.g.cs
partial interface IUnknown
{
    [DynamicInterfaceCastableImplementation]
    partial interface Native : IUnknown
    {
    }
}

// ManagedToNativeStubs.g.cs
partial interface IUnknown
{
    partial interface Native
    {
        int IUnknown.QueryInterface(in Guid riid, out IntPtr ppvObject)
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IUnknown>();
            int retVal;
            fixed (Guid* riid__gen_native = &riid)
            fixed (IntPtr* ppvObject__gen_native = &ppvObject)
            {
                retVal = ((delegate* unmanaged[Stdcall, MemberFunction]<IntPtr, Guid*, IntPtr*, int>)vtable[0])(thisPtr, riid__gen_native, ppvObject__gen_native);
            }
            return retVal;
        }
    }
}
partial interface IUnknown
{
    partial interface Native
    {
        uint IUnknown.AddRef()
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IUnknown>();
            uint retVal;
            retVal = ((delegate* unmanaged[Stdcall, MemberFunction]<IntPtr, uint>)vtable[1])(thisPtr);
            return retVal;
        }
    }
}
partial interface IUnknown
{
    partial interface Native
    {
        uint IUnknown.Release()
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IUnknown>();
            uint retVal;
            retVal = ((delegate* unmanaged[Stdcall, MemberFunction]<IntPtr, uint>)vtable[2])(thisPtr);
            return retVal;
        }
    }
}

// Native-To-Managed code omitted as the design has not been finalized yet.
```

This example shows how we can build COM support on top of the vtable stub generator. The generator will support specifying a custom calling convention using the already-existing `UnmanagedCallConvAttribute`, so it will automatically support forwarding any calling conventions we implement with our extensible calling convention support to the function pointer signature.

## FAQ

- Why emit a nested interface instead of a DIM on the existing interface?
    - By emitting a nested interface, we enable flexibility in the implementation of the user-defined interface without our implementations getting in the way. With the current design, a managed implementation of a given interface would require the user to implement all members. If we emitted the member implementations directly as DIM implementations, then the compiler would happily allow a developer to only override one method and leave the rest using the native implementation, which would make the development experience of a managed implementation more difficult as there would be no IDE/compiler assistance to fully implement the contract.

## Open Questions

- Should we automatically apply the `[DynamicInterfaceCastableImplementation]` attribute to the generated `Native` interface?
    - It is a nice convenience, but it isn't applicable in all scenarios and bloats the metadata size. Additionally, since the generated interface is `partial`, we could direct users to add it themselves to the generated interface.

