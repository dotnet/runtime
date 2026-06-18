// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Multi-frame EH plus finally: throw three frames deep, catch at Main,
// and observe finally blocks executing in reverse order during unwind.
// This exercises the Frame-chain stack walker through several managed
// frames and ensures finally semantics survive the WASM EH path.
internal static class Program
{
    private static int s_finallyCount;

    private static int Main()
    {
        try
        {
            A();
        }
        catch (ArgumentException ex)
        {
            if (ex.Message != "from C (Parameter 'x')" && ex.Message != "from C")
            {
                // Message format differs depending on whether ICU is available
                // (with ICU: "from C (Parameter 'x')"; invariant: same).
                Console.Error.WriteLine($"DeepEh: wrong message '{ex.Message}'");
                return 1;
            }

            if (ex.ParamName != "x")
            {
                Console.Error.WriteLine($"DeepEh: wrong ParamName '{ex.ParamName}'");
                return 1;
            }

            if (s_finallyCount != 3)
            {
                Console.Error.WriteLine($"DeepEh: expected 3 finally hits, got {s_finallyCount}");
                return 2;
            }

            Console.WriteLine($"DeepEh: caught '{ex.Message}', finallyCount={s_finallyCount}");
            Console.WriteLine("WASI-SMOKE-PASS:DeepEh");
            return 100;
        }

        Console.Error.WriteLine("DeepEh: missed catch");
        return 3;
    }

    private static void A()
    {
        try { B(); }
        finally { s_finallyCount++; }
    }

    private static void B()
    {
        try { C(); }
        finally { s_finallyCount++; }
    }

    private static void C()
    {
        try { throw new ArgumentException("from C", "x"); }
        finally { s_finallyCount++; }
    }
}
