// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;

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

        string binDir = AppContext.BaseDirectory;
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

unsafe class SuppressGCTransitionTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_NoGCTransition(int expected)
    {
        Console.WriteLine($"{nameof(Inline_NoGCTransition)} ({expected}) ...");
        int n;
        int ret = SuppressGCTransitionNative.NextUInt_Inline_NoGCTransition(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: true, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_GCTransition(int expected)
    {
        Console.WriteLine($"{nameof(Inline_GCTransition)} ({expected}) ...");
        int n;
        int ret = SuppressGCTransitionNative.NextUInt_Inline_GCTransition(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: false, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_NoGCTransition(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_NoGCTransition)} ({expected}) ...");
        int n;
        bool ret = SuppressGCTransitionNative.NextUInt_NoInline_NoGCTransition(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: true, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_GCTransition(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_GCTransition)} ({expected}) ...");
        int n;
        bool ret = SuppressGCTransitionNative.NextUInt_NoInline_GCTransition(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: false, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mixed(int expected)
    {
        Console.WriteLine($"{nameof(Mixed)} ({expected}) ...");
        int n;

        {
            bool ret = SuppressGCTransitionNative.NextUInt_NoInline_GCTransition(&n);
            Assert.AreEqual(expected++, n);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
            ret = SuppressGCTransitionNative.NextUInt_NoInline_NoGCTransition(&n);
            Assert.AreEqual(expected++, n);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            int ret = SuppressGCTransitionNative.NextUInt_Inline_GCTransition(&n);
            Assert.AreEqual(expected++, n);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
            ret = SuppressGCTransitionNative.NextUInt_Inline_NoGCTransition(&n);
            Assert.AreEqual(expected++, n);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mixed_TightLoop(int expected)
    {
        Console.WriteLine($"{nameof(Mixed_TightLoop)} ({expected}) ...");
        int n = 0;
        int count = 0x100;
        for (int i = 0; i < count; ++i)
        {
            SuppressGCTransitionNative.NextUInt_Inline_NoGCTransition(&n);
        }

        // Use the non-optimized version at the end so a GC poll is not
        // inserted here as well.
        SuppressGCTransitionNative.NextUInt_NoInline_GCTransition(&n);
        Assert.AreEqual(expected + count, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_NoGCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(Inline_NoGCTransition)} ({expected}) ...");
        int n;
        int ret = SuppressGCTransitionNative.GetNextUIntFunctionPointer_Inline_NoGCTransition()(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: true, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_GCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(Inline_GCTransition)} ({expected}) ...");
        int n;
        int ret = SuppressGCTransitionNative.GetNextUIntFunctionPointer_Inline_GCTransition()(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: false, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_NoGCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_NoGCTransition)} ({expected}) ...");
        int n;
        bool ret = SuppressGCTransitionNative.GetNextUIntFunctionPointer_NoInline_NoGCTransition()(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: true, ret);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_GCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_GCTransition)} ({expected}) ...");
        int n;
        bool ret = SuppressGCTransitionNative.GetNextUIntFunctionPointer_NoInline_GCTransition()(&n);
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: false, ret);
        return n + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallAsFunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(CallAsFunctionPointer)} ({expected}) ...");

        IntPtr fptr = SuppressGCTransitionNative.GetNextUIntFunctionPointer();

        int n = 0;
        int* pn = &n;
        object boxedN = Pointer.Box(pn, typeof(int*));

        MethodInfo callNextUInt = typeof(FunctionPointer).GetMethod("Call_NextUInt");
        int ret = (int)callNextUInt.Invoke(null, new object[] { fptr, boxedN });
        Assert.AreEqual(expected, n);
        CheckGCMode.Validate(transitionSuppressed: false, ret);
        return n + 1;
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int ReturnInt(int value)
    {
        return value;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ILStubCache_NoGCTransition_GCTransition(int expected)
    {
        // This test uses a callback marked UnmanagedCallersOnly as a way to verify that
        // SuppressGCTransition is taken into account when caching IL stubs.
        // It calls functions with the same signature, differing only in SuppressGCTransition.
        // When calling an UnmanagedCallersOnly method, the runtime validates that the GC is in
        // pre-emptive mode. If not, it throws a fatal error that cannot be caught and crashes.
        // If the stub for the p/invoke with the transition suppressed is incorrectly reused for
        // the p/invoke without the suppression, invoking the callback would produce a fatal error.
        Console.WriteLine($"{nameof(ILStubCache_NoGCTransition_GCTransition)} ({expected}) ...");

        int n;

        // Call function that has SuppressGCTransition
        SuppressGCTransitionNative.InvokeCallbackFuncPtr_Inline_NoGCTransition(null, null);

        // Call function with same (blittable) signature, but without SuppressGCTransition.
        // IL stub should not be re-used, GC transition should occur, and callback should be invoked.
        SuppressGCTransitionNative.InvokeCallbackFuncPtr_Inline_GCTransition(&ReturnInt, &n);
        Assert.AreEqual(expected++, n);

        // Call function that has SuppressGCTransition
        SuppressGCTransitionNative.InvokeCallbackFuncPtr_NoInline_NoGCTransition(null, null);

        // Call function with same (non-blittable) signature, but without SuppressGCTransition
        // IL stub should not be re-used, GC transition should occur, and callback should be invoked.
        SuppressGCTransitionNative.InvokeCallbackFuncPtr_NoInline_GCTransition(&ReturnInt, &n);
        Assert.AreEqual(expected++, n);

        return n + 1;
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
        Assert.AreEqual(expected++, n);

        // Call function with same (blittable) signature, but with SuppressGCTransition.
        // IL stub should not be re-used, GC transition not should occur, and callback invocation should fail.
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_Inline_NoGCTransition(cb, &n);
        Assert.AreEqual(expected++, n);

        // Call function that does not have SuppressGCTransition
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_NoInline_GCTransition(cb, &n);
        Assert.AreEqual(expected++, n);

        // Call function with same (non-blittable) signature, but with SuppressGCTransition
        // IL stub should not be re-used, GC transition not should occur, and callback invocation should fail.
        expected = n + 1;
        SuppressGCTransitionNative.InvokeCallbackVoidPtr_NoInline_NoGCTransition(cb, &n);
        Assert.AreEqual(expected++, n);

        return n + 1;
    }
    public static int Main(string[] args)
    {
        try
        {
            CheckGCMode.Initialize(&SuppressGCTransitionNative.SetIsInCooperativeModeFunction);

            int n = 1;
            n = Inline_NoGCTransition(n);
            n = Inline_GCTransition(n);
            n = NoInline_NoGCTransition(n);
            n = NoInline_GCTransition(n);
            n = Mixed(n);
            n = Mixed_TightLoop(n);
            n = Inline_NoGCTransition_FunctionPointer(n);
            n = Inline_GCTransition_FunctionPointer(n);
            n = NoInline_NoGCTransition_FunctionPointer(n);
            n = NoInline_GCTransition_FunctionPointer(n);
            n = CallAsFunctionPointer(n);
            n = ILStubCache_NoGCTransition_GCTransition(n);

            if (args.Length != 0 && args[0].Equals("ILStubCache", StringComparison.OrdinalIgnoreCase))
            {
                // This test intentionally results in a fatal error, so only run when manually specified
                n = ILStubCache_GCTransition_NoGCTransition(n);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 101;
        }
        return 100;
    }
}
