// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Test that runtime crash chaining preserves previously installed signal handlers.
/// A secondary signal raised from a chained crash handler must reach the handler
/// that was installed before the runtime initialized.
/// </summary>
public static class Program
{
    // Returns 0 on success (SIGABRT handler preserved during crash chaining),
    // non-zero on failure.
    [DllImport("__Internal")]
    private static extern int test_crash_chaining();

    public static int Main()
    {
        int result = test_crash_chaining();
        Console.WriteLine(result == 0
            ? "PASS: crash chaining preserved signal handlers"
            : $"FAIL: crash chaining test returned {result}");
        return result == 0 ? 42 : result;
    }
}
