// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace P24.RuntimeSmoke;

internal static class Program
{
    private static int Main()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P24_NATIVE_HOOK_LIBRARY")
            ?? throw new InvalidOperationException(
                "P24_NATIVE_HOOK_LIBRARY is required.");
        nint library = NativeLibrary.Load(libraryPath);
        try
        {
            nint export = NativeLibrary.GetExport(
                library,
                "GC_ImmixAllocationTest_Run");
            RunDelegate run =
                Marshal.GetDelegateForFunctionPointer<RunDelegate>(export);
            int result = run();
            if (result != 0)
            {
                Console.Error.WriteLine(
                    $"Immix allocation runtime hook failed: {result}.");
                return result;
            }

            Console.WriteLine("PASS: Immix allocation runtime hook");
            return 0;
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RunDelegate();
}
