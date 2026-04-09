// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HostApiInvokerApp
{
    public static class Utils
    {
#if WINDOWS
        internal static class kernel32
        {
            [DllImport(nameof(kernel32), CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(String moduleName);

            [DllImport(nameof(kernel32), CharSet = CharSet.Auto)]
            internal static extern uint GetModuleFileName(IntPtr hModule, StringBuilder fileName, int size);
        }
#endif

        public static void LogModulePath(string moduleName)
        {
#if WINDOWS
            IntPtr hModule = kernel32.GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero)
            {
                Console.WriteLine($"Can't find module {moduleName} in the process.");
                return;
            }

            StringBuilder buffer = new StringBuilder(2048);
            if (kernel32.GetModuleFileName(hModule, buffer, buffer.Capacity) <= 0)
            {
                Console.WriteLine($"Failed to get module file path for module {moduleName}.");
                return;
            }

            Console.WriteLine($"{moduleName}: {buffer.ToString()}");
#endif
        }
    }
}
