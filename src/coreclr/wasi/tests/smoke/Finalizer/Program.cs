// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Verifies that GC.Collect() + GC.WaitForPendingFinalizers() runs user
// finalizers synchronously on WASI (no dedicated finalizer thread, no host
// event loop). FinalizerThreadWait's WASM branch drains the queue via
// FinalizerThreadWorkerIteration on the calling thread.
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
        // Without the WASM-side FinalizerThreadWait drain, GC.WaitForPendingFinalizers
        // is a no-op on WASI and the ~Tracker() finalizer never runs before Main
        // returns; the process exits without printing the sentinel. Finalizers
        // queued strictly by process exit are NOT pumped and leak by design,
        // matching every other CoreCLR target -- so the sentinel is emitted
        // from inside GC.WaitForPendingFinalizers, not from EE shutdown.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return 100;
    }
}

