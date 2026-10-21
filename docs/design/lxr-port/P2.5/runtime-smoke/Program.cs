// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

string libraryPath =
    Environment.GetEnvironmentVariable("P25_NATIVE_HOOK_LIBRARY")
    ?? throw new InvalidOperationException("P25_NATIVE_HOOK_LIBRARY is required.");
IntPtr library = NativeLibrary.Load(libraryPath);
try
{
    IntPtr export = NativeLibrary.GetExport(library, "GC_LargeObjectSpaceTest_Run");
    RunDelegate run = Marshal.GetDelegateForFunctionPointer<RunDelegate>(export);
    int result = run();
    if (result != 0)
    {
        throw new InvalidOperationException($"Native large object space smoke failed: {result}.");
    }

    Console.WriteLine("PASS: large object space runtime smoke");
}
finally
{
    NativeLibrary.Free(library);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int RunDelegate();
