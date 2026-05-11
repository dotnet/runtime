// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the ComWrappers contract.
/// Creates exactly one MOW (via GetOrCreateComInterfaceForObject) and exactly one
/// RCW (via GetOrCreateObjectForComInstance) for a single managed object, then
/// crashes via FailFast.
/// </summary>
internal static class Program
{
    private static GCHandle s_objHandle;
    private static IntPtr s_mowPtr;
    private static object? s_rcwObject;
    private static GCHandle s_rcwGcHandle;

    private static void Main()
    {
        var wrappers = new TestComWrappers();
        var obj = new ManagedTestObject();

        s_objHandle = GCHandle.Alloc(obj, GCHandleType.Normal);

        // Create exactly one MOW for the managed object.
        s_mowPtr = wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);

        // Create exactly one RCW by wrapping the MOW back as a COM object.
        s_rcwObject = wrappers.GetOrCreateObjectForComInstance(s_mowPtr, CreateObjectFlags.None);
        s_rcwGcHandle = GCHandle.Alloc(s_rcwObject, GCHandleType.Normal);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        GC.KeepAlive(wrappers);
        GC.KeepAlive(s_objHandle);
        GC.KeepAlive(s_mowPtr);
        GC.KeepAlive(s_rcwObject);
        GC.KeepAlive(s_rcwGcHandle);

        Environment.FailFast("cDAC dump test: ComWrappers debuggee intentional crash");
    }
}

/// <summary>
/// Simple managed object used as a MOW source.
/// </summary>
internal class ManagedTestObject { }

/// <summary>
/// Wrapper returned by <see cref="TestComWrappers.CreateObject"/> for RCW creation.
/// </summary>
internal class NativeObjectWrapper
{
    public IntPtr Pointer { get; }

    public NativeObjectWrapper(IntPtr ptr)
    {
        Pointer = ptr;
    }
}

/// <summary>
/// Minimal <see cref="ComWrappers"/> subclass that can produce both MOWs and RCWs.
/// </summary>
internal unsafe class TestComWrappers : ComWrappers
{
    [Guid("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09")]
    internal interface ITestInterface
    {
        void Dummy();
    }

    private struct IUnknownVtbl
    {
        public IntPtr QueryInterface;
        public IntPtr AddRef;
        public IntPtr Release;
    }

    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        GetIUnknownImpl(out IntPtr fpQI, out IntPtr fpAddRef, out IntPtr fpRelease);

        var vtbl = new IUnknownVtbl
        {
            QueryInterface = fpQI,
            AddRef = fpAddRef,
            Release = fpRelease,
        };

        IntPtr vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(TestComWrappers), sizeof(IUnknownVtbl));
        Marshal.StructureToPtr(vtbl, vtblRaw, false);

        var entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(
            typeof(TestComWrappers), sizeof(ComInterfaceEntry));

        entryRaw[0].IID = typeof(ITestInterface).GUID;
        entryRaw[0].Vtable = vtblRaw;

        count = 1;
        return entryRaw;
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        return new NativeObjectWrapper(externalComObject);
    }

    protected override void ReleaseObjects(IEnumerable objects) { }
}
