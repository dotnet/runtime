// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace AppWithCustomEntryPoints
{
    public static class Program
    {
        static Program()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Console.WriteLine($"{asm.GetName().Name}: AssemblyLoadContext = {AssemblyLoadContext.GetLoadContext(asm)}");
        }

        public static void Main(string[] args)
        {
        }

        private static int functionPointerCallCount = 0;
        private static int entryPoint1CallCount = 0;
        private static int entryPoint2CallCount = 0;
        private static int unmanagedEntryPoint1CallCount = 0;

        private static void PrintFunctionPointerCallLog(string name, IntPtr arg, int size)
        {
            Console.WriteLine($"Called {name}(0x{arg.ToString("x")}, {size}) - call count: {functionPointerCallCount}");

            Assembly asm = Assembly.GetExecutingAssembly();
            Console.WriteLine($"{asm.GetName().Name}: AssemblyLoadContext = {AssemblyLoadContext.GetLoadContext(asm)}");
        }

        public static int FunctionPointerEntryPoint1(IntPtr arg, int size)
        {
            functionPointerCallCount++;
            entryPoint1CallCount++;
            PrintFunctionPointerCallLog(nameof(FunctionPointerEntryPoint1), arg, size);
            return entryPoint1CallCount;
        }

        public static int FunctionPointerEntryPoint2(IntPtr arg, int size)
        {
            functionPointerCallCount++;
            entryPoint2CallCount++;
            PrintFunctionPointerCallLog(nameof(FunctionPointerEntryPoint2), arg, size);
            return entryPoint2CallCount;
        }

        public static int ThrowException(IntPtr arg, int size)
        {
            functionPointerCallCount++;
            PrintFunctionPointerCallLog(nameof(ThrowException), arg, size);
            throw new InvalidOperationException(nameof(ThrowException));
        }

        [UnmanagedCallersOnly]
        public static int UnmanagedFunctionPointerEntryPoint1(IntPtr arg, int size)
        {
            functionPointerCallCount++;
            unmanagedEntryPoint1CallCount++;
            PrintFunctionPointerCallLog(nameof(UnmanagedFunctionPointerEntryPoint1), arg, size);
            return unmanagedEntryPoint1CallCount;
        }
    }
}
