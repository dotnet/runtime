// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Xunit;

public unsafe class Program
{
    static void BlittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(BlittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load("NativeFunctions", Assembly.GetExecutingAssembly(), null);
        var cbDefault = NativeLibrary.GetExport(mod, "DoubleInt").ToPointer();
        var cbCdecl = NativeLibrary.GetExport(mod, "DoubleIntCdecl").ToPointer();
        var cbStdcall = NativeLibrary.GetExport(mod, "DoubleIntStdcall").ToPointer();

        const int a = 7;
        const int expected = a * 2;

        {
            // No modopt
            Console.WriteLine($" -- unmanaged");
            int b = CallFunctionPointers.CallUnmanagedIntInt(cbDefault, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged cdecl");
            int b = CallFunctionPointers.CallUnmanagedCdeclIntInt(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged stdcall");
            int b = CallFunctionPointers.CallUnmanagedStdcallIntInt(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(cdecl)");
            int b = CallFunctionPointers.CallUnmanagedIntInt_ModOptCdecl(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(stdcall)");
            int b = CallFunctionPointers.CallUnmanagedIntInt_ModOptStdcall(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            // Value in modopt is not a recognized calling convention
            Console.WriteLine($" -- unmanaged modopt unrecognized");
            int b = CallFunctionPointers.CallUnmanagedIntInt_ModOptUnknown(cbDefault, a);
            Assert.Equal(expected, b);
        }

        {
            // Multiple modopts with calling conventions
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(cdecl)");
            var ex = Assert.Throws<InvalidProgramException>(
                () => CallFunctionPointers.CallUnmanagedIntInt_ModOptStdcall_ModOptCdecl(cbCdecl, a));
            Assert.Equal("Multiple unmanaged calling conventions are specified. Only a single calling convention is supported.", ex.Message);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(unrecognized)");
            int b = CallFunctionPointers.CallUnmanagedIntInt_ModOptStdcall_ModOptUnknown(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged cdecl modopt(stdcall)");
            int b = CallFunctionPointers.CallUnmanagedCdeclIntInt_ModOptStdcall(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged stdcall modopt(cdecl)");
            int b = CallFunctionPointers.CallUnmanagedStdcallIntInt_ModOptCdecl(cbStdcall, a);
            Assert.Equal(expected, b);
        }
    }

    static void NonblittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(NonblittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load("NativeFunctions", Assembly.GetExecutingAssembly(), null);
        var cbDefault = NativeLibrary.GetExport(mod, "ToUpper").ToPointer();
        var cbCdecl = NativeLibrary.GetExport(mod, "ToUpperCdecl").ToPointer();
        var cbStdcall = NativeLibrary.GetExport(mod, "ToUpperStdcall").ToPointer();

        const char a = 'i';
        const char expected = 'I';

        {
            // No modopt
            Console.WriteLine($" -- unmanaged");
            var b = CallFunctionPointers.CallUnmanagedCharChar(cbDefault, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged cdecl");
            var b = CallFunctionPointers.CallUnmanagedCdeclCharChar(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged stdcall");
            var b = CallFunctionPointers.CallUnmanagedStdcallCharChar(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(cdecl)");
            var b = CallFunctionPointers.CallUnmanagedCharChar_ModOptCdecl(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(stdcall)");
            var b = CallFunctionPointers.CallUnmanagedCharChar_ModOptStdcall(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            // Value in modopt is not a recognized calling convention
            Console.WriteLine($" -- unmanaged modopt(unrecognized)");
            var b = CallFunctionPointers.CallUnmanagedCharChar_ModOptUnknown(cbDefault, a);
            Assert.Equal(expected, b);
        }

        {
            // Multiple modopts with calling conventions
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(cdecl)");
            var ex = Assert.Throws<InvalidProgramException>(
                () => CallFunctionPointers.CallUnmanagedCharChar_ModOptStdcall_ModOptCdecl(cbCdecl, a));
            Assert.Equal("Multiple unmanaged calling conventions are specified. Only a single calling convention is supported.", ex.Message);
        }

        {
            Console.WriteLine($" -- unmanaged modopt(stdcall) modopt(unrecognized)");
            var b = CallFunctionPointers.CallUnmanagedCharChar_ModOptStdcall_ModOptUnknown(cbStdcall, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged cdecl modopt(stdcall)");
            var b = CallFunctionPointers.CallUnmanagedCdeclCharChar_ModOptStdcall(cbCdecl, a);
            Assert.Equal(expected, b);
        }

        {
            Console.WriteLine($" -- unmanaged stdcall modopt(cdecl)");
            var b = CallFunctionPointers.CallUnmanagedStdcallCharChar_ModOptCdecl(cbStdcall, a);
            Assert.Equal(expected, b);
        }
    }

    [Fact]
    public static int TestEntryPoint()
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
