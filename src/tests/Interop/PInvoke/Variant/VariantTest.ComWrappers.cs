// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static VariantNative;
using ComTypes = System.Runtime.InteropServices.ComTypes;

#pragma warning disable CS0612, CS0618
public partial class Test_VariantTest
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static int TestEntryPoint()
    {
        bool testComMarshal=true;
        ComWrappers.RegisterForMarshalling(new ComWrappersImpl());
        try
        {
            TestByValue(testComMarshal);
            TestByRef(testComMarshal);
            TestOut();
            TestFieldByValue(testComMarshal);
            TestFieldByRef(testComMarshal);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test failed: {e}");
            return 101;
        }
        return 100;
    }
}

internal unsafe class ComWrappersImpl : ComWrappers
{
    private static readonly ComInterfaceEntry* wrapperEntry;

    static ComWrappersImpl()
    {
        var vtblRaw = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IDispatchVtbl), sizeof(IntPtr) * 7);
        GetIUnknownImpl(out vtblRaw[0], out vtblRaw[1], out vtblRaw[2]);

        vtblRaw[3] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&IDispatchVtbl.GetTypeInfoCountInternal;
        vtblRaw[4] = (IntPtr)(delegate* unmanaged<IntPtr, int, int, IntPtr, int>)&IDispatchVtbl.GetTypeInfoInternal;
        vtblRaw[5] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, int, int, IntPtr, int>)&IDispatchVtbl.GetIDsOfNamesInternal;
        vtblRaw[6] = (IntPtr)(delegate* unmanaged<IntPtr, int, IntPtr, int, ComTypes.INVOKEKIND, IntPtr, IntPtr, IntPtr, IntPtr, int>)&IDispatchVtbl.InvokeInternal;

        wrapperEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IDispatchVtbl), sizeof(ComInterfaceEntry));
        wrapperEntry->IID = IDispatchVtbl.IID_IDispatch;
        wrapperEntry->Vtable = (IntPtr)vtblRaw;
    }

    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        // Always return the same table mappings.
        count = 1;
        return wrapperEntry;
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        throw new NotImplementedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}
public struct IDispatchVtbl
{
    internal static readonly Guid IID_IDispatch = new Guid("00020400-0000-0000-C000-000000000046");

    [UnmanagedCallersOnly]
    public static int GetTypeInfoCountInternal(IntPtr thisPtr, IntPtr i)
    {
        return 0; // S_OK;
    }

    [UnmanagedCallersOnly]
    public static int GetTypeInfoInternal(IntPtr thisPtr, int itinfo, int lcid, IntPtr i)
    {
        return 0; // S_OK;
    }

    [UnmanagedCallersOnly]
    public static int GetIDsOfNamesInternal(
        IntPtr thisPtr,
        IntPtr iid,
        IntPtr names,
        int namesCount,
        int lcid,
        IntPtr dispIds)
    {
        return 0; // S_OK;
    }

    [UnmanagedCallersOnly]
    public static int InvokeInternal(
        IntPtr thisPtr,
        int dispIdMember,
        IntPtr riid,
        int lcid,
        ComTypes.INVOKEKIND wFlags,
        IntPtr pDispParams,
        IntPtr VarResult,
        IntPtr pExcepInfo,
        IntPtr puArgErr)
    {
        return 0; // S_OK;
    }
}
