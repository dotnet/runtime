// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

// Smoke test: verifies async Main + Task.Delay works on WASI. Depends on
// AsyncHelpers.Wasi.cs routing through WasiEventLoop, and on the WIT
// wasi:clocks p/invokes reaching the callhelpers pinvoke table.
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

