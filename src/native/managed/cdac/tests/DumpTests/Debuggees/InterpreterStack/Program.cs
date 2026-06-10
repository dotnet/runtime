// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using InterpreterStack.Trampoline;

/// <summary>
/// Debuggee for cDAC dump tests — exercises interpreter stack walking with
/// interleaved JIT and interpreter frames.
///
/// Under DOTNET_Interpreter=MethodA, methods from this assembly that match
/// the filter are interpreted. The call chain routes through JitTrampoline.Bounce
/// (in a separate assembly, always JIT'd) to create two distinct InterpreterFrame
/// regions on the stack:
///
///   Main (JIT) -> MethodA (interp) -> MethodB (interp) -> [InterpreterFrame 1]
///     -> JitTrampoline.Bounce (JIT) -> MethodC (interp) -> MethodD (interp) -> [InterpreterFrame 2]
///       -> FailFast (JIT)
/// </summary>
internal static class Program
{
    private static void Main()
    {
        MethodA();
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
        Environment.FailFast("cDAC dump test: InterpreterStack debuggee intentional crash");
    }
}
