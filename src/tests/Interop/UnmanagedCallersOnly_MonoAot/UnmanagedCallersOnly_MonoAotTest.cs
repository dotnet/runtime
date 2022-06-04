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

// Stripped-down variant of Interop/UnmanagedCallersOnly/* test used for testing Mono AOT support for UnmanagedCallersOnly attribute
public unsafe class Program
{
    public static class UnmanagedCallersOnly_MonoAotDll
    {
        [DllImport(nameof(UnmanagedCallersOnly_MonoAotDll))]
        public static extern int CallManagedProc_Stdcall(delegate* unmanaged[Stdcall]<int, int> callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnly_MonoAotDll))]
        public static extern int CallManagedProc_Cdecl(delegate* unmanaged[Cdecl]<int, int> callbackProc, int n);
    }

    public static int Main(string[] args)
    {
        var result = 100;

        result += TestUnmanagedCallersOnlyValid_CallConvStdcall();
        result += TestUnmanagedCallersOnlyValid_CallConvCdecl();

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int ManagedDoubleCallback_Stdcall(int n)
    {
        return DoubleImpl(n);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int ManagedDoubleCallback_Cdecl(int n)
    {
        return DoubleImpl(n);
    }

    private static int DoubleImpl(int n)
    {
        return 2 * n;
    }

    public static int TestUnmanagedCallersOnlyValid_CallConvStdcall()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_CallConvStdcall)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        int actual = UnmanagedCallersOnly_MonoAotDll.CallManagedProc_Stdcall(&ManagedDoubleCallback_Stdcall, n);

        return expected == actual ? 0 : -1;
    }

    public static int TestUnmanagedCallersOnlyValid_CallConvCdecl()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_CallConvCdecl)}...");

        int n = 12345;
        int expected = DoubleImpl(n);
        int actual = UnmanagedCallersOnly_MonoAotDll.CallManagedProc_Cdecl(&ManagedDoubleCallback_Cdecl, n);

        return expected == actual ? 0 : -1;
    }
}
