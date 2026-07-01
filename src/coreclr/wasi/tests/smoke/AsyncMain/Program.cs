// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

// Verifies that `async Task<int> Main()` works on CoreCLR-WASI without
// throwing PlatformNotSupportedException. Roslyn emits an entry-point
// wrapper that calls AsyncHelpers.HandleAsyncEntryPoint(task); WASI's
// AsyncHelpers.Wasi.cs routes that through WasiEventLoop's pump instead
// of a blocking Task.Wait (which throws PNSE from
// RuntimeFeature.ThrowIfMultithreadingIsNotSupported on WASI).
internal static class Program
{
    private static async Task<int> Main()
    {
        Console.WriteLine("WASI-SMOKE-STEP:AsyncMain-before-delay");
        await Task.Delay(50);
        Console.WriteLine("WASI-SMOKE-STEP:AsyncMain-after-delay");
        var v = await Task.FromResult(42);
        if (v != 42) return 1;
        Console.WriteLine("WASI-SMOKE-PASS:AsyncMain");
        return 100;
    }
}
