// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_133858
{
    private struct Pair
    {
        public int A;
        public int B;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct S
    {
        [FieldOffset(0)] public Pair P0;
        [FieldOffset(0)] public Pair P0Alias;
        [FieldOffset(4)] public Pair P1;
        [FieldOffset(8)] public Pair P2;
    }

    [Theory]
    [InlineData(0, 1, 1, 2, 4)]
    [InlineData(1, 2, 3, 3, 4)]
    [InlineData(2, 1, 2, 1, 2)]
    [InlineData(3, 3, 4, 3, 4)]
    [InlineData(4, 1, 2, 3, 4)]
    [InlineData(5, 1, 1, 2, 4)]
    public static void TestEntryPoint(int copy, int a, int b, int c, int d)
    {
        Assert.Equal((a, b, c, d), Copy(copy, 1, 2, 3, 4));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static (int, int, int, int) Copy(int copy, int a, int b, int c, int d)
    {
        S s = default;
        s.P0.A = a;
        s.P0.B = b;
        s.P1.B = c;
        s.P2.B = d;

        switch (copy)
        {
            case 0:
                s.P1 = s.P0;
                break;
            case 1:
                s.P0 = s.P1;
                break;
            case 2:
                s.P2 = s.P0;
                break;
            case 3:
                s.P0 = s.P2;
                break;
            case 4:
                s.P0 = s.P0Alias;
                break;
            case 5:
                S other = s;
                s.P1 = other.P0;
                break;
        }

        return (s.P0.A, s.P0.B, s.P1.B, s.P2.B);
    }
}
