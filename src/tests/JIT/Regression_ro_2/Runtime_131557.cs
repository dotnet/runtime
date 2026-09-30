// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When a localloc result flows straight into a call argument, the localloc must be evaluated
// before any argument is set up. On wasm the shadow stack pointer is an implicit first argument
// and is updated by the localloc, so pushing it first handed the callee a stack pointer inside
// the freshly allocated region and the callee's frame overwrote the buffer.

using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_131557
{
    private static int s_sink;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, Test());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test()
    {
        byte* buffer = stackalloc byte[256];
        return Use(buffer, 256);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Use(byte* p, int n)
    {
        for (int i = 0; i < n; i++)
        {
            p[i] = (byte)(i ^ 0x5a);
        }

        // Give this frame some address-exposed locals of its own to clobber the buffer with.
        int a = n, b = n + 1, c = n + 2, d = n + 3;
        Mix(ref a, ref b, ref c, ref d);
        s_sink += a + b + c + d;

        int corrupted = 0;
        for (int i = 0; i < n; i++)
        {
            if (p[i] != (byte)(i ^ 0x5a))
            {
                corrupted++;
            }
        }

        return corrupted;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mix(ref int a, ref int b, ref int c, ref int d)
    {
        a = ~a;
        b = ~b;
        c = ~c;
        d = ~d;
        s_sink += a + b + c + d;
    }
}
