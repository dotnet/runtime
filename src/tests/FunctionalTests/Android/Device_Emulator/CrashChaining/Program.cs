// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Test that crash chaining preserves signal handlers during
/// mono_handle_native_crash. When crash_chaining is enabled,
/// mono_handle_native_crash should NOT reset SIGABRT etc. to SIG_DFL,
/// because that would let secondary signals (e.g. FORTIFY aborts on
/// other threads) kill the process before the crash can be chained.
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
