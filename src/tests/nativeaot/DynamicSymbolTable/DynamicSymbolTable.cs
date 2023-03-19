// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DynamicSymbolTable
{
    unsafe class Program
    {
        [UnmanagedCallersOnly(EntryPoint = "ReversePInvokeEntry")]
        static void ReversePInvokeEntry() => Console.WriteLine($"Hello from {nameof(ReversePInvokeEntry)}");

        static int Main()
        {
            IntPtr libHandle;
            
            libHandle = NativeLibrary.Load(GetFullNativeLibraryName("NativeLibWithReversePInvokes"));
            if (libHandle == IntPtr.Zero)
            {
                return 1;
            }

            if (NativeLibrary.TryGetExport(libHandle, "PInvokeEntry", out IntPtr funcHandle))
            {
                var PInvokeEntryPtr = (delegate* unmanaged <void>) funcHandle;
                Console.WriteLine("PInvoke called");
                PInvokeEntryPtr();
                Console.WriteLine("PInvoke returned");
            }
            else
            {
                return 2;
            }

            return 100;
        }

        public static string GetFullNativeLibraryName(string baseName)
        {
            if (OperatingSystem.IsWindows())
                return $"{baseName}.dll";

            if (OperatingSystem.IsLinux())
                return $"lib{baseName}.so";

            if (OperatingSystem.IsMacOS())
                return $"lib{baseName}.dylib";

            throw new PlatformNotSupportedException();
        }
    }
}