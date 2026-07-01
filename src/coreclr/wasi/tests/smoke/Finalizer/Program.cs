// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Smoke test: verifies GC.WaitForPendingFinalizers actually runs user
// finalizers on WASI (drained synchronously via FinalizerThreadWait's
// WASM branch). Finalizers queued strictly by process exit are NOT
// pumped, matching every other CoreCLR target.
internal static class Program
{
    private sealed class Tracker
    {
        ~Tracker() => Console.WriteLine("WASI-SMOKE-PASS:Finalizer");
    }

    private static void AllocateAndDrop()
    {
        _ = new Tracker();
    }

    private static int Main(string[] _)
    {
        AllocateAndDrop();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return 100;
    }
}


