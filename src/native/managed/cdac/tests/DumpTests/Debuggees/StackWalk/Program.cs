// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the StackWalk contract.
/// Establishes a deep, deterministic call stack then crashes.
/// Each method uses NoInlining to ensure distinct stack frames.
/// </summary>
internal static class Program
{
    public const int ExpectedManagedFrameCount = 4;

    private static void Main()
    {
        MethodA(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodA(int depth)
    {
        if (depth < 0)
            return;
        MethodB();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodB()
    {
        // The try/finally ensures localObj is stored as a real IL local variable,
        // because the evaluation stack is empty at finally-block entry per ECMA-335.
        object localObj = new object();
        try
        {
            MethodC();
        }
        finally
        {
            GC.KeepAlive(localObj);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodC()
    {
        Environment.FailFast("cDAC dump test: StackWalk debuggee intentional crash");
    }
}
