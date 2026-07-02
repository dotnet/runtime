// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Verifies that finalization eventually runs on WASI: WASI corerun
// dispatches the finalizer queue through WasiEventLoop, which only
// makes progress when control yields back to the host event loop
// (e.g. via `await`). A purely synchronous loop of GC.Collect +
// WaitForPendingFinalizers does NOT guarantee finalizers run — that's
// .NET semantics, not a WASI-specific bug — so this test gives the
// event loop turns via Task.Yield and asserts that finalization makes
// progress within a bounded number of yields.
internal static class Program
{
    private static int s_finalized;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateGarbage()
    {
        for (int i = 0; i < 100; i++)
        {
            _ = new Finalizable();
        }
    }

    private static async Task<int> Main()
    {
        AllocateGarbage();
        GC.Collect();

        for (int turn = 0; turn < 32 && s_finalized == 0; turn++)
        {
            GC.WaitForPendingFinalizers();
            await Task.Yield();
        }

        if (s_finalized == 0)
        {
            Console.Error.WriteLine("Finalization: no finalizers ran after 32 yields");
            return 1;
        }

        Console.WriteLine($"Finalization: finalized={s_finalized}");
        Console.WriteLine("WASI-SMOKE-PASS:Finalization");
        return 100;
    }

    private sealed class Finalizable
    {
        ~Finalizable()
        {
            s_finalized++;
        }
    }
}
