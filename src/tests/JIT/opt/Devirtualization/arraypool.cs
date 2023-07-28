// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Xunit;

public class X
{
    static int N;
    static int J;
    static int K;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void S() { J = N / 2; K = J; }

    // We expect calls to Rent and Return to be 
    // devirtualized.
    [Fact]
    public static int TestEntryPoint()
    {
        N = 100;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(N);
        int r = -1;

        try {
            S();
            buffer[J] = 100;
            r = (int) buffer[K];
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        return r;
    }
}
