// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using InterpreterStack.Trampoline;

/// <summary>
/// Debuggee for cDAC dump tests — validates interpreter stack frame walking on a
/// thread that has a full interpreter call chain. A worker thread builds the chain
/// and spins in interpreted code, then the main thread triggers a FailFast dump.
///
/// Under DOTNET_Interpreter=Method*, methods from this assembly that match
/// the filter are interpreted. The call chain routes through JitTrampoline.Bounce
/// (in a separate assembly, always JIT'd) to create two distinct InterpreterFrame
/// regions on the stack:
///
///   Worker thread:
///     MethodA (interp) -> MethodB (interp) -> [InterpreterFrame 1]
///       -> JitTrampoline.Bounce (JIT) -> MethodC (interp) -> MethodD (interp) -> [InterpreterFrame 2]
///         -> spinning via SpinStep() calls (each call through interpreter precode)
///
///   Main thread:
///     Main (JIT) -> waits for signal -> FailFast
///
/// Note: Even though the worker is executing interpreted code, in a FailFast dump
/// the CPU IP is inside the native interpreter engine. However, when a debugger
/// breaks the thread at SpinStep's interpreter precode (via cdb breakpoint), the
/// OS thread context has IP = precode address, which IS managed code registered as
/// JitType.Interpreter. This enables the SkipNextInterpreterFrame double-walk
/// prevention to be exercised from a debugger-collected dump.
/// </summary>
internal static class Program
{
    private static readonly ManualResetEventSlim s_workerReady = new(false);

    private static void Main()
    {
        Thread worker = new(MethodA)
        {
            IsBackground = true,
            Name = "InterpreterWorker",
        };
        worker.Start();

        // Wait for the worker to reach MethodD (full call chain on stack).
        s_workerReady.Wait();

        Environment.FailFast("cDAC dump test: InterpreterStackDoubleWalk debuggee intentional crash");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodA()
    {
        MethodB();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodB()
    {
        JitTrampoline.Bounce(MethodC);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodC()
    {
        MethodD();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodD()
    {
        // Signal the main thread that the full call chain is on the stack.
        s_workerReady.Set();

        // Spin by repeatedly calling SpinStep(). Each call goes through SpinStep's
        // interpreter precode, allowing a debugger to break at the precode and
        // capture a dump where the thread's IP is in interpreter-managed code.
        while (s_keepSpinning) { SpinStep(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpinStep() { }

    private static volatile bool s_keepSpinning = true;
}
