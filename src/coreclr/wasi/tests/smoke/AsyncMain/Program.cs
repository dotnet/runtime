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
        Console.WriteLine("WASI-SMOKE-STEP:AsyncMain-before-await");
        // Only synchronously-completed awaits; Task.Delay depends on
        // wasi:clocks/monotonic-clock@0.2.8 which needs a separate p/invoke
        // resolver wiring on CoreCLR-WASI (tracked separately). This test
        // targets AsyncHelpers.Wasi.cs -> WasiEventLoop.PollWasiEventLoopUntilResolved
        // wiring, not the clock plumbing.
        var v1 = await Task.FromResult(41);
        Console.WriteLine($"WASI-SMOKE-STEP:AsyncMain-v1={v1}");
        var v2 = await Task.FromResult(v1 + 1);
        Console.WriteLine($"WASI-SMOKE-STEP:AsyncMain-v2={v2}");
        if (v2 != 42) return 1;
        Console.WriteLine("WASI-SMOKE-PASS:AsyncMain");
        return 100;
    }
}
