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
using InvalidCSharp;

public unsafe class Program
{
    public static class UnmanagedCallersOnlyDll
    {
        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        // Returns -1 if exception was throw and caught.
        public static extern int CallManagedProcCatchException(IntPtr callbackProc, int n);
    }

    private delegate int IntNativeMethodInvoker();
    private delegate void NativeMethodInvoker();

    public static void Main()
    {
        NegativeTest_ViaCalli();
        throw new Exception("Fatal error - runtime crash expected!");
    }

    [UnmanagedCallersOnly]
    public static void CallbackViaCalli(int val)
    {
        Assert.Fail($"Functions with attribute {nameof(UnmanagedCallersOnlyAttribute)} cannot be called via calli");
    }

    public static void NegativeTest_ViaCalli()
    {
        Console.WriteLine($"{nameof(NegativeTest_ViaCalli)} function via calli instruction. The CLR _will_ crash.");

        // It is not possible to catch the resulting ExecutionEngineException exception.
        // To observe the crashing behavior set a breakpoint in the ReversePInvokeBadTransition() function
        // located in src/vm/dllimportcallback.cpp.
        TestNativeMethod();

        static void TestNativeMethod()
        {
            ((delegate*<int, void>)(delegate* unmanaged<int, void>)&CallbackViaCalli)(1234);
        }
    }
}
