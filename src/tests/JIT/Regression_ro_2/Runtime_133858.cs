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
        [FieldOffset(4)] public Pair P1;
        [FieldOffset(8)] public Pair P2;
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/7539")]
    public static void TestEntryPoint()
    {
        Assert.Equal((1, 1, 2, 4), Copy(1, 2, 3, 4));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static (int, int, int, int) Copy(int a, int b, int c, int d)
    {
        S s = default;
        s.P0.A = a;
        s.P0.B = b;
        s.P1.B = c;
        s.P2.B = d;

        s.P1 = s.P0;

        return (s.P0.A, s.P0.B, s.P1.B, s.P2.B);
    }
}
