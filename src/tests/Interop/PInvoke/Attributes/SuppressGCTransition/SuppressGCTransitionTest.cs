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

unsafe class SuppressGCTransitionTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_NoGCTransition(int expected)
    {
        Console.WriteLine($"{nameof(Inline_NoGCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.NextUInt_Inline_NoGCTransition(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_GCTransition(int expected)
    {
        Console.WriteLine($"{nameof(Inline_GCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.NextUInt_Inline_GCTransition(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_NoGCTransition(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_NoGCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.NextUInt_NoInline_NoGCTransition(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_GCTransition(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_GCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.NextUInt_NoInline_GCTransition(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mixed(int expected)
    {
        Console.WriteLine($"{nameof(Mixed)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.NextUInt_NoInline_GCTransition(&n);
        Assert.AreEqual(expected++, n);
        SuppressGCTransitionNative.NextUInt_NoInline_NoGCTransition(&n);
        Assert.AreEqual(expected++, n);
        SuppressGCTransitionNative.NextUInt_Inline_GCTransition(&n);
        Assert.AreEqual(expected++, n);
        SuppressGCTransitionNative.NextUInt_Inline_NoGCTransition(&n);
        Assert.AreEqual(expected++, n);
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
        SuppressGCTransitionNative.GetNextUIntFunctionPointer_Inline_NoGCTransition()(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inline_GCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(Inline_GCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.GetNextUIntFunctionPointer_Inline_GCTransition()(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_NoGCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_NoGCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.GetNextUIntFunctionPointer_NoInline_NoGCTransition()(&n);
        Assert.AreEqual(expected, n);
        return n + 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInline_GCTransition_FunctionPointer(int expected)
    {
        Console.WriteLine($"{nameof(NoInline_GCTransition)} ({expected}) ...");
        int n;
        SuppressGCTransitionNative.GetNextUIntFunctionPointer_NoInline_GCTransition()(&n);
        Assert.AreEqual(expected, n);
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
        callNextUInt.Invoke(null, new object[] { fptr, boxedN });
        Assert.AreEqual(expected, n);
        return n + 1;
    }

    public static int Main()
    {
        try
        {
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 101;
        }
        return 100;
    }
}
