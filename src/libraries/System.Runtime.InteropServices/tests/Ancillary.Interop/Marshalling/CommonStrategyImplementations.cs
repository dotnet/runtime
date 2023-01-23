// Implementations of the COM strategy interfaces defined in Com.cs that we would want to ship (can be internal only if we don't want to allow users to provide their own implementations in v1).
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;


public sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
{
    public static readonly IIUnknownInterfaceDetailsStrategy Instance = new DefaultIUnknownInterfaceDetailsStrategy();

    public IUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
    {
        return IUnknownDerivedDetails.GetFromAttribute(type);
    }
}

public sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
{
    public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

    void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
    {
        return unknown;
    }

    unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
    {
        int hr = Marshal.QueryInterface((nint)thisPtr, ref Unsafe.AsRef(in handle), out nint ppv);
        if (hr < 0)
        {
            ppObj = null;
        }
        else
        {
            ppObj = (void*)ppv;
        }
        return hr;
    }

    unsafe int IIUnknownStrategy.Release(void* thisPtr)
        => Marshal.Release((nint)thisPtr);
}

public sealed unsafe class DefaultCaching : IIUnknownCacheStrategy
{
    // [TODO] Implement some smart/thread-safe caching
    private readonly Dictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new();

    IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IUnknownDerivedDetails details, void* ptr)
    {
        var obj = (void***)ptr;
        return new IIUnknownCacheStrategy.TableInfo()
        {
            ThisPtr = obj,
            Table = *obj,
            ManagedType = details.Implementation.TypeHandle
        };
    }

    bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryGetValue(handle, out info);
    }

    bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryAdd(handle, info);
    }

    void IIUnknownCacheStrategy.Clear(IIUnknownStrategy unknownStrategy)
    {
        foreach (var info in _cache.Values)
        {
            _ = unknownStrategy.Release(info.ThisPtr);
        }
        _cache.Clear();
    }
}
