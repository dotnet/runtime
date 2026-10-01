// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133641
{
    private struct S
    {
        public long A;
        public long B;
        public long C;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(8L, Test(new S { A = 1, B = 2, C = 3 }));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Combine(S s, long a, long b) => s.A + s.B + s.C + a + b;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long Test(S input)
    {
        S s = input;
        input.A = 42;
        long p = s.A;
        return Combine(s, p, p);
    }
}
