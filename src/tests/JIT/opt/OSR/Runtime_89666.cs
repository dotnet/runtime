// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_89666
{
    [Fact]
    public static int Test()
    {
        byte[] a1 = new byte[100_000];
        byte[] a2 = new byte[100_000];

        Problem(a1.Length, a1);
        Problem(a2.Length, a2);

        for (int i = 0; i < a1.Length; i++)
        {
            if (a1[i] != a2[i])
            {
                Console.WriteLine($"Found diff at {i}: {a1[i]} != {a2[i]}");
                return -1;
            }
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Problem(int n, byte[] a)
    {
        Random random = new Random(1);
        int value = 0;
        Span<byte> span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        for (int i = 0; i < n; i++)
        {
            // This write must be kept in the OSR method
            value = random.Next();
            Write(span, i, a);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Write(Span<byte> s, int i, byte[] a)
    {
        a[i] = s[0];
    }
}
