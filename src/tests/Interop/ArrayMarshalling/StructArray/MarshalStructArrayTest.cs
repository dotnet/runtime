// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        // Validate that we free the native resources of fields that use "managed" marshallers in CoreCLR's IL Stub
        // marshalling system when they are fields of structs and we don't have the managed value avaliable at the time we are
        // releasing the resources.

        // To test this, we are using a structure with a "ByValArray" field that contains a runtime-implemented RCW.
        // The marshaller for the ByValArray unmanaged type uses the marshaller kind under test.
        // Using an RCW as the element type allows us to hook the release mechanism to validate that the value has been freed.
        // Since the runtime will unwrap an RCW when passing it down to native code (instead of re-wrapping in a CCW), this will
        // allow us to directly test with the custom release mechanism we have implemented without needing to do GC.Collect calls.
        object underlyingObject = new();
        SimpleComWrappers wrappers = new();
        nint unk = wrappers.GetOrCreateComInterfaceForObject(underlyingObject, CreateComInterfaceFlags.CallerDefinedIUnknown);
        object builtinWrapper = Marshal.GetUniqueObjectForIUnknown(unk);
        Marshal.Release(unk);
        
        StructWithObjectArrayField str = new() { objs = [builtinWrapper] };

        IntPtr ptr = (IntPtr)NativeMemory.Alloc((nuint)Marshal.SizeOf<StructWithObjectArrayField>());
        Marshal.StructureToPtr(str, ptr, false);
        SimpleComWrappers.ReleaseCalled = false;
        Marshal.DestroyStructure<StructWithObjectArrayField>(ptr);

        Assert.True(SimpleComWrappers.ReleaseCalled);
        NativeMemory.Free((void*)ptr);

        // Make sure that the runtime-implemented RCW isn't collected (and Release called)
        // when we are trying to test that DestroyStructure calls release.
        GC.KeepAlive(builtinWrapper);
    }
}

public struct StructWithObjectArrayField
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public object[] objs;
}

public sealed unsafe class SimpleComWrappers : ComWrappers
{
    private static readonly ComInterfaceEntry* customIUnknown = CreateCustomIUnknownInterfaceEntry();

    private static ComInterfaceEntry* CreateCustomIUnknownInterfaceEntry()
    {
        ComInterfaceEntry* entry = (ComInterfaceEntry*)NativeMemory.AllocZeroed((nuint)sizeof(ComInterfaceEntry));
        entry->IID = Guid.Parse("00000000-0000-0000-C000-000000000046");
        nint* vtable = (nint*)NativeMemory.Alloc((nuint)(sizeof(void*) * 3));
        GetIUnknownImpl(out vtable[0], out vtable[1], out _);
        vtable[2] = (nint)(delegate* unmanaged<nint, uint>)&Release;
        entry->Vtable = (nint)vtable;
        return entry;
    }

    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        Assert.True(flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown));
        count = 1;
        return customIUnknown;
    }

    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        throw new NotImplementedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }

    [ThreadStatic]
    public static bool ReleaseCalled = false;

    [UnmanagedCallersOnly]
    private static uint Release(nint thisPtr)
    {
        ReleaseCalled = true;
        GetIUnknownImpl(out _, out _, out var release);
        return ((delegate* unmanaged<IntPtr, uint>)release)(thisPtr);
    }
}