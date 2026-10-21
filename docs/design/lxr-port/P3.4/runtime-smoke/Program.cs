// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: runtime-smoke <native-library> <label>");
    return 2;
}

nint library = NativeLibrary.Load(args[0]);
try
{
    nint export = NativeLibrary.GetExport(library, "GC_LxrCompletionCounterTest_Run");
    RunCompletionCounterSmoke run =
        Marshal.GetDelegateForFunctionPointer<RunCompletionCounterSmoke>(export);
    int result = run();
    if (result != 0)
    {
        Console.Error.WriteLine($"Completion counter smoke returned {result}.");
        return result;
    }

    Console.WriteLine($"P3.4-COMPLETION-COUNTER-SMOKE {args[1]} PASS");
    return 0;
}
finally
{
    NativeLibrary.Free(library);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int RunCompletionCounterSmoke();
