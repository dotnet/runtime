// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_92784
{
    [Fact]
    public static void TestEntryPoint()
    {
        PoisonStack();
        int result = Problem(null);
        Assert.True(result == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(int* p)
    {
        // This uses 'optRemoveRedundantZeroInits' to expose the fact atomics were not modeled as throwing.
        int x;
        int y;
        int z;
        try
        {
            Interlocked.CompareExchange(ref *p, 0, 0);
            x = 1;
            y = 1;
            z = 1;
            Interlocked.Exchange(ref *&x, 0);
            Interlocked.Exchange(ref *&y, 0);
            Interlocked.Exchange(ref *&z, 0);
        }
        catch
        {
            return *&x + *&y + *&z;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void PoisonStack()
    {
        LargeStruct x;
        Unsafe.InitBlock(&x, 0xDF, (uint)sizeof(LargeStruct));
    }

    [StructLayout(LayoutKind.Explicit, Size = 96)]
    struct LargeStruct { }
}
