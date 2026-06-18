// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Verifies that finalizers queued at process exit actually run during EE
// shutdown on WASI (no dedicated finalizer thread, no JS event loop). The
// shutdown path in FinalizerThread::RaiseShutdownEvents is responsible for
// pumping any pending finalizable objects.
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
        // Validates the WASI/Browser-shared FinalizerThreadWait drain path:
        // before this fix GC.WaitForPendingFinalizers was a no-op on WASM and
        // user finalizers never ran without a JS host event loop ticking the
        // queue. Now the QCall synchronously runs one FinalizerThreadWorker
        // iteration on the calling thread.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return 100;
    }
}
