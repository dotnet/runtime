// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135393
{
    public struct Pair
    {
        public Vector4 A;
        public Vector4 B;
    }

    public struct Triple
    {
        public Vector4 A;
        public Vector4 B;
        public Vector4 C;
    }

    public struct Quad
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
        public Vector2 D;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Pair p = WidenFirstField(1);
        Assert.Equal(new Vector4(1), p.A);
        Assert.Equal(new Vector4(2), p.B);

        Triple t = WidenSecondField(1);
        Assert.Equal(new Vector4(10), t.A);
        Assert.Equal(new Vector4(1), t.B);
        Assert.Equal(new Vector4(2), t.C);

        Quad q = WidenSmallField(1);
        Assert.Equal(new Vector2(1), q.A);
        Assert.Equal(new Vector2(1), q.B);
        Assert.Equal(new Vector2(2), q.C);
        Assert.Equal(new Vector2(2), q.D);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Pair Make(float x) => new Pair { A = new Vector4(x), B = new Vector4(x + 1) };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Pair WidenFirstField(float x)
    {
        Pair p = default;
        Unsafe.As<Vector4, Pair>(ref p.A) = Make(x);
        return p;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Triple WidenSecondField(float x)
    {
        Triple t = new Triple { A = new Vector4(10) };
        Unsafe.As<Vector4, Pair>(ref t.B) = Make(x);
        return t;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Quad WidenSmallField(float x)
    {
        Quad q = default;
        Unsafe.As<Vector2, Pair>(ref q.A) = Make(x);
        return q;
    }
}
