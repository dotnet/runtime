// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Baseline smoke: corerun loads CoreLib, JIT/interp executes Main, args
// are passed through, Console.WriteLine reaches stdout. The runner scans
// stdout for "WASI-SMOKE-PASS:<name>" because corerun on WASI does not
// currently propagate the managed Main return as the process exit code.
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine($"HelloWorld: args.Length={args.Length}");
        for (int i = 0; i < args.Length; i++)
        {
            Console.WriteLine($"  args[{i}]={args[i]}");
        }

        Console.WriteLine("WASI-SMOKE-PASS:HelloWorld");
        return 100;
    }
}
