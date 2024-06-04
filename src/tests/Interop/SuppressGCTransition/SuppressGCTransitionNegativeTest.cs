// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

unsafe static class SuppressGCTransitionNative
{
    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl)]
    public static extern unsafe void SetIsInCooperativeModeFunction(delegate* unmanaged<int> fn);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "NextUInt")]
    [SuppressGCTransition]
    public static extern unsafe int NextUInt_Inline_NoGCTransition(int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "NextUInt")]
    public static extern unsafe int NextUInt_Inline_GCTransition(int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "NextUInt")]
    [SuppressGCTransition]
    public static extern unsafe bool NextUInt_NoInline_NoGCTransition(int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "NextUInt")]
    public static extern unsafe bool NextUInt_NoInline_GCTransition(int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    [SuppressGCTransition]
    public static extern unsafe int InvokeCallbackFuncPtr_Inline_NoGCTransition(delegate* unmanaged[Cdecl]<int, int> cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    public static extern unsafe int InvokeCallbackFuncPtr_Inline_GCTransition(delegate* unmanaged[Cdecl]<int, int> cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    [SuppressGCTransition]
    public static extern unsafe bool InvokeCallbackFuncPtr_NoInline_NoGCTransition(delegate* unmanaged[Cdecl]<int, int> cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    public static extern unsafe bool InvokeCallbackFuncPtr_NoInline_GCTransition(delegate* unmanaged[Cdecl]<int, int> cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    [SuppressGCTransition]
    public static extern unsafe int InvokeCallbackVoidPtr_Inline_NoGCTransition(void* cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    public static extern unsafe int InvokeCallbackVoidPtr_Inline_GCTransition(void* cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    [SuppressGCTransition]
    public static extern unsafe bool InvokeCallbackVoidPtr_NoInline_NoGCTransition(void* cb, int* n);

    [DllImport(nameof(SuppressGCTransitionNative), CallingConvention=CallingConvention.Cdecl, EntryPoint = "InvokeCallback")]
    public static extern unsafe bool InvokeCallbackVoidPtr_NoInline_GCTransition(void* cb, int* n);

    private static IntPtr nativeLibrary;

    public static IntPtr GetNextUIntFunctionPointer()
    {
        if (nativeLibrary == IntPtr.Zero)
        {
            nativeLibrary = GetNativeLibrary();
        }

        IntPtr fptr;
        if (NativeLibrary.TryGetExport(nativeLibrary, "NextUInt", out fptr))
        {
            return fptr;
        }

        throw new Exception($"Failed to find native export");
    }

    public static delegate* unmanaged[Cdecl]<int*, int> GetNextUIntFunctionPointer_Inline_GCTransition()
        => (delegate* unmanaged[Cdecl]<int*, int>)GetNextUIntFunctionPointer();

    public static delegate* unmanaged[Cdecl, SuppressGCTransition]<int*, int> GetNextUIntFunctionPointer_Inline_NoGCTransition()
        => (delegate* unmanaged[Cdecl, SuppressGCTransition]<int*, int>)GetNextUIntFunctionPointer();

    public static delegate* unmanaged[Cdecl]<int*, bool> GetNextUIntFunctionPointer_NoInline_GCTransition()
        => (delegate* unmanaged[Cdecl]<int*, bool>)GetNextUIntFunctionPointer();

    public static delegate* unmanaged[Cdecl, SuppressGCTransition]<int*, bool> GetNextUIntFunctionPointer_NoInline_NoGCTransition()
        => (delegate* unmanaged[Cdecl, SuppressGCTransition]<int*, bool>)GetNextUIntFunctionPointer();

    private static IntPtr GetNativeLibrary()
    {
        var libNames = new []
        {
            $"{nameof(SuppressGCTransitionNative)}.dll",
            $"lib{nameof(SuppressGCTransitionNative)}.so",
            $"lib{nameof(SuppressGCTransitionNative)}.dylib",
        };

        string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        foreach (var ln in libNames)
        {
            if (NativeLibrary.TryLoad(Path.Combine(binDir, ln), out IntPtr mod))
            {
                return mod;
            }
        }

        throw new Exception($"Failed to find native library {nameof(SuppressGCTransitionNative)}");
    }
}

public unsafe class SuppressGCTransitionTest
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int ReturnInt(int value)
    {
        return value;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ILStubCache_GCTransition_NoGCTransition(int expected)
    {
        // This test uses a callback marked UnmanagedCallersOnly as a way to verify that
        // SuppressGCTransition is taken into account when caching IL stubs.
        // It calls functions with the same signature, differing only in SuppressGCTransition.
        // When calling an UnmanagedCallersOnly method, the runtime validates that the GC is in
        // pre-emptive mode. If not, it throws a fatal error that cannot be caught and crashes.
        Console.WriteLine($"{nameof(ILStubCache_GCTransition_NoGCTransition)} ({expected}) ...");

        int n;

        void* cb = (delegate* unmanaged[Cdecl]<int, int>)&ReturnInt;

        // Call function that does not have SuppressGCTransition
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_Inline_GCTransition(cb, &n);
        Assert.Equal(expected++, n);

        // Call function with same (blittable) signature, but with SuppressGCTransition.
        // IL stub should not be re-used, GC transition not should occur, and callback invocation should fail.
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_Inline_NoGCTransition(cb, &n);
        Assert.Equal(expected++, n);

        // Call function that does not have SuppressGCTransition
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_NoInline_GCTransition(cb, &n);
        Assert.Equal(expected++, n);

        // Call function with same (non-blittable) signature, but with SuppressGCTransition
        // IL stub should not be re-used, GC transition not should occur, and callback invocation should fail.
        expected = n + 1;
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_NoInline_NoGCTransition(cb, &n);
        Assert.Equal(expected++, n);

        return n + 1;
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void TestEntryPoint()
    {
        CheckGCMode.Initialize(&SuppressGCTransitionNative.SetIsInCooperativeModeFunction);

        int n = 1;
        // This test intentionally results in a fatal error, so only run when manually specified
        n = ILStubCache_GCTransition_NoGCTransition(n);

        throw new Exception("Failure - fatal error expected!");
    }
}
