// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for a WebAssembly R2R (crossgen) codegen bug in async
// exception-handling lowering. When an async method's try block completes
// normally by branching out early (a `return` after an `await`), the
// normal-completion branch was bound to a Block that ended at the try's own
// end cursor -- nested inside the exception-ref wrapper funclet -- so the
// branch landed on the wrapper's trailing validation `unreachable` and
// trapped (RuntimeError: unreachable) instead of resuming.
//
// The defect is in codegen, independent of whether the await actually
// suspends: `await Task.CompletedTask` still generates the state-machine
// normal-completion branch that traps. A completed await is used deliberately
// so the synchronous .GetAwaiter().GetResult() does not block the single wasm
// thread (blocking on incomplete work traps elsewhere in corelib on wasm).
//
// Reproduces only under crossgen wasm R2R (TargetOS=browser), where the
// unfixed JIT aborts the module (exit != 100 => test fails). Passes trivially
// on all other targets.

using System;
using System.Threading.Tasks;
using Xunit;

public class Runtime_131285
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Sync [Fact]: the merged runner invokes this via a bare call, so drive
        // the async method to completion synchronously here. RunAsync's MoveNext
        // takes the normal-completion `return` path -- the edge that trapped
        // under the unfixed wasm R2R JIT.
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        try
        {
            await Task.CompletedTask;
            return;
        }
        catch (Exception ex)
        {
            GC.KeepAlive(ex);
        }
    }
}
