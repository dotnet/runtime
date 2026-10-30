// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises in-flight exception root reporting.
/// Builds a nested exception chain: a superseded <see cref="FileNotFoundException"/>
/// (kept alive on the thread's ExInfo chain as the previous nested tracker) plus the
/// current <see cref="InvalidOperationException"/>, then crashes via FailFast while both
/// are still in flight. The GC reports these via gcenv.ee.cpp ScanStackRoots; the test verifies
/// WalkStackReferences reports them as stack references.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        try
        {
            ThrowNested();
        }
        catch (Exception current)
        {
            // 'current' is the InvalidOperationException; the superseded
            // FileNotFoundException is still held on the thread's ExInfo chain.
            GC.KeepAlive(current);
            Environment.FailFast("cDAC dump test: NestedException debuggee intentional crash");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNested()
    {
        try
        {
            throw new FileNotFoundException("cDAC-NestedException-inner");
        }
        catch (FileNotFoundException inner)
        {
            // Throwing while handling 'inner' supersedes its ExInfo (kept as the
            // previous nested tracker) and starts a new tracker for the outer exception.
            throw new InvalidOperationException("cDAC-NestedException-outer", inner);
        }
    }
}
