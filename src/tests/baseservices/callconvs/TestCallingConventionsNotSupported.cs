// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using TestLibrary;

unsafe class Program
{
    static void BlittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(BlittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load(NativeFunctions.GetFullPath());
        var cb = NativeLibrary.GetExport(mod, "DoubleInt").ToPointer();

        const int a = 7;
        {
            // Multiple modopts with calling conventions
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(cdecl)");
            var ex = Assert.Throws<NotSupportedException>(
                () => CallFunctionPointersNotSupported.CallUnmanagedIntInt_ModOptStdcall_ModOptCdecl(cb, a),
                "Multiple modopts with calling conventions should fail");
            Assert.AreEqual("Multiple unmanaged calling conventions are specified. Only a single calling convention is supported.", ex.Message);
        }
    }

    static void NonblittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(NonblittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load(NativeFunctions.GetFullPath());
        var cb = NativeLibrary.GetExport(mod, "ToUpper").ToPointer();

        const char a = 'i';
        {
            // Multiple modopts with calling conventions
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(cdecl)");
            var ex = Assert.Throws<NotSupportedException>(
                () => CallFunctionPointersNotSupported.CallUnmanagedCharChar_ModOptStdcall_ModOptCdecl(cb, a),
                "Multiple modopts with calling conventions should fail");
            Assert.AreEqual("Multiple unmanaged calling conventions are specified. Only a single calling convention is supported.", ex.Message);
        }
    }

    static int Main(string[] doNotUse)
    {
        try
        {
            BlittableFunctionPointers();
            NonblittableFunctionPointers();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
