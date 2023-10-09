using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

public static unsafe class MarshalStructArrayTest
{
    [SkipOnMono("Mono doesn't support built-in COM interop, which this test requires")]
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
    public static void ArrayElementsInStructFreed()
    {
        var (underlyingWeakRef, wrapperWeakRef) = CallNative();

        // Try to GC 10 times until the built-in RCW is collected.
        for (int i = 0; i < 10 && wrapperWeakRef.IsAlive; i++)
        {
            GC.Collect();
        }

        Assert.False(wrapperWeakRef.IsAlive);

        // Run two more GCs, one to collect the ComWrappers MOW and one to collect the underlying weak reference.
        GC.Collect();
        GC.Collect();
        Assert.False(underlyingWeakRef.IsAlive);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static (WeakReference underlyingObject, WeakReference builtinWrapperRef) CallNative()
        {
            var (weakRef, obj) = CreateComObject();
            StructWithObjectArrayField[] arr = [ new() { objs = [obj] } ];
            MarshalStructArrayNative.ForwardToCallback(arr, &Callback);
            return (weakRef, new WeakReference(obj));
        }

        [UnmanagedCallersOnly]
        static void Callback(void* arg)
        {
            Assert.True(arg != null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static (WeakReference weakRef, object builtinWrapper) CreateComObject()
        {
            object underlyingObject = new();
            SimpleComWrappers wrappers = new();
            nint unk = wrappers.GetOrCreateComInterfaceForObject(underlyingObject, CreateComInterfaceFlags.None);
            object builtinWrapper = Marshal.GetUniqueObjectForIUnknown(unk);
            Marshal.Release(unk);
            return (new WeakReference(underlyingObject), builtinWrapper);
        }
    }
}

public struct StructWithObjectArrayField
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public object[] objs;
}

public static unsafe class MarshalStructArrayNative
{
    [DllImport(nameof(MarshalStructArrayNative))]
    public static extern void ForwardToCallback(StructWithObjectArrayField[] arr, delegate* unmanaged<void*, void> cb);
}

public sealed unsafe class SimpleComWrappers : ComWrappers
{
    private static readonly ComInterfaceEntry* s_fakeVtable = CreateVtableEntry();

    // Create a vtable for a fake interface just so we have at least one to provide to ComWrappers.
    private static ComInterfaceEntry* CreateVtableEntry()
    {
        ComInterfaceEntry* entry = (ComInterfaceEntry*)NativeMemory.AllocZeroed((nuint)sizeof(ComInterfaceEntry));
        entry->IID = Guid.NewGuid();
        nint* vtable = (nint*)NativeMemory.Alloc((nuint)(sizeof(void*) * 4));
        GetIUnknownImpl(out vtable[0], out vtable[1], out vtable[2]);
        vtable[3] = (nint)(delegate* unmanaged<nint, int>)&NativeMethodImpl;
        entry->Vtable = (nint)vtable;
        return entry;
    }

    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        count = 1;
        return s_fakeVtable;
    }

    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        throw new NotImplementedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
    
    [UnmanagedCallersOnly]
    static int NativeMethodImpl(nint thisPtr)
    {
        return 0;
    }
}