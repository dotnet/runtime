// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using TestLibrary;

unsafe class Program
{
    class NativeFunctions
    {
        public const string Name = nameof(NativeFunctions);

        public static string GetFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"{Name}.dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return $"lib{Name}.so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return $"lib{Name}.dylib";

            throw new PlatformNotSupportedException();
        }

        public static string GetFullPath()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string directory = Path.GetDirectoryName(assembly.Location);
            return Path.Combine(directory, GetFileName());
        }
    }

    static void BlittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(BlittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load(NativeFunctions.GetFullPath());

        const int a = 7;
        const int expected = a * 2;
        {
            var cb = NativeLibrary.GetExport(mod, "DoubleInt").ToPointer();
            Assert.Throws<NotImplementedException>(() => CallFunctionPointers.CallUnmanagedIntInt(cb, a));
        }

        {
            var cb = NativeLibrary.GetExport(mod, "DoubleIntCdecl").ToPointer();
            int b = CallFunctionPointers.CallUnmanagedCdeclIntInt(cb, a);
            Assert.AreEqual(b, expected);
        }

        {
            var cb = NativeLibrary.GetExport(mod, "DoubleIntStdcall").ToPointer();
            int b = CallFunctionPointers.CallUnmanagedStdcallIntInt(cb, a);
            Assert.AreEqual(b, expected);
        }
    }

    static void NonblittableFunctionPointers()
    {
        Console.WriteLine($"Running {nameof(NonblittableFunctionPointers)}...");

        IntPtr mod = NativeLibrary.Load(NativeFunctions.GetFullPath());

        const char a = 'i';
        const char expected = 'I';
        {
            var cb = NativeLibrary.GetExport(mod, "ToUpper").ToPointer();
            Assert.Throws<NotImplementedException>(() => CallFunctionPointers.CallUnmanagedCharChar(cb, a));
        }

        {
            var cb = NativeLibrary.GetExport(mod, "ToUpperCdecl").ToPointer();
            var b = CallFunctionPointers.CallUnmanagedCdeclCharChar(cb, a);
            Assert.AreEqual(b, expected);
        }

        {
            var cb = NativeLibrary.GetExport(mod, "ToUpperStdcall").ToPointer();
            var b = CallFunctionPointers.CallUnmanagedStdcallCharChar(cb, a);
            Assert.AreEqual(b, expected);
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
