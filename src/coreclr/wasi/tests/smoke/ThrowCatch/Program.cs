// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Single-frame managed EH: throw, catch the expected exception type,
// observe the message, and confirm the catch is reached. Verifies the
// basic throw/catch path works on WASI corerun without falling through
// to Thread::VirtualUnwindCallFrame(T_CONTEXT*) — EH stack-walking is
// expected to use the explicit Frame chain on WASM.
internal static class Program
{
    private static int Main()
    {
        try
        {
            throw new InvalidOperationException("expected");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message != "expected")
            {
                Console.Error.WriteLine($"ThrowCatch: wrong message '{ex.Message}'");
                return 1;
            }
            Console.WriteLine("ThrowCatch: caught");
            Console.WriteLine("WASI-SMOKE-PASS:ThrowCatch");
            return 100;
        }
    }
}
