// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public unsafe class UnmanagedCallersOnlyBasicTest
{
    public static class UnmanagedCallersOnlyDll
    {
        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int DoubleImplNative(int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc_Stdcall(delegate* unmanaged[Stdcall]<int, int> callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc_Cdecl(delegate* unmanaged[Cdecl]<int, int> callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcMultipleTimes(int m, IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcOnNewThread(IntPtr callbackProc, int n);
    }

    private static int DoubleImpl(int n)
    {
        return 2 * n;
    }

    [UnmanagedCallersOnly]
    public static int ManagedDoubleCallback(int n)
    {
        return DoubleImpl(n);
    }

    [Fact]
    public static void TestUnmanagedCallersOnlyValid()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        Assert.Equal(expected, UnmanagedCallersOnlyDll.CallManagedProc((IntPtr)(delegate* unmanaged<int, int>)&ManagedDoubleCallback, n));
    }

   [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int ManagedDoubleCallback_Stdcall(int n)
    {
        return DoubleImpl(n);
    }

    [Fact]
    public static void TestUnmanagedCallersOnlyValid_CallConvStdcall()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_CallConvStdcall)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        int actual = UnmanagedCallersOnlyDll.CallManagedProc_Stdcall(&ManagedDoubleCallback_Stdcall, n);

        Assert.Equal(expected, actual);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int ManagedDoubleCallback_Cdecl(int n)
    {
        return DoubleImpl(n);
    }

    [Fact]
    public static void TestUnmanagedCallersOnlyValid_CallConvCdecl()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_CallConvCdecl)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        int actual = UnmanagedCallersOnlyDll.CallManagedProc_Cdecl(&ManagedDoubleCallback_Cdecl, n);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void TestUnmanagedCallersOnlyValid_OnNewNativeThread()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_OnNewNativeThread)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        Assert.Equal(expected, UnmanagedCallersOnlyDll.CallManagedProcOnNewThread((IntPtr)(delegate* unmanaged<int, int>)&ManagedDoubleCallback, n));
    }

    [UnmanagedCallersOnly]
    public static int ManagedCallback_Prepared(int n)
    {
        return DoubleImpl(n);
    }

    [Fact]
    // This test is about the interaction between Tiered Compilation and the UnmanagedCallersOnlyAttribute.
    public static void TestUnmanagedCallersOnlyValid_PrepareMethod()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_PrepareMethod)}...");
        // Prepare the managed callback.
        var preparedCallback = typeof(UnmanagedCallersOnlyBasicTest).GetMethod(nameof(ManagedCallback_Prepared));
        RuntimeHelpers.PrepareMethod(preparedCallback.MethodHandle);

        UnmanagedCallersOnlyOnNewNativeThread(12345);

        static void UnmanagedCallersOnlyOnNewNativeThread(int n)
        {
            // Call enough to attempt to trigger Tiered Compilation from a new thread.
            for (int i = 0; i < 100; ++i)
            {
                UnmanagedCallersOnlyDll.CallManagedProcOnNewThread((IntPtr)(delegate* unmanaged<int, int>)&ManagedCallback_Prepared, n);
            }
        }
    }

    [UnmanagedCallersOnly]
    public static int ManagedDoubleInNativeCallback(int n)
    {
        // This callback is designed to test if the JIT handles
        // cases where a P/Invoke is inlined into a function
        // marked with UnmanagedCallersOnly.
        return UnmanagedCallersOnlyDll.DoubleImplNative(n);
    }

    [Fact]
    public static void TestUnmanagedCallersOnlyMultipleTimesValid()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyMultipleTimesValid)}...");

        int callCount = 7;
        int n = 12345;
        int expected = 0;
        for (int i = 0; i < callCount; ++i)
        {
            expected += DoubleImpl(n);
        }
        Assert.Equal(expected, UnmanagedCallersOnlyDll.CallManagedProcMultipleTimes(callCount, (IntPtr)(delegate* unmanaged<int, int>)&ManagedDoubleInNativeCallback, n));
    }
}
