// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test captures the redundant struct zeroing from https://github.com/dotnet/coreclr/issues/11816.
// Since the issue was filed, the 'TestStructManuallyInlined' case has apparently
// gotten worse, as there is a MEMSET of the large struct to 0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_11816
{
    struct StructType
    {
        public Vector<float> A;
        public Vector<float> B;
        public Vector<float> C;
        public Vector<float> D;
        public Vector<float> E;
        public Vector<float> F;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> GetVector()
    {
        return new Vector<float>(100);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoSomeWorkWithAStruct(ref Vector<float> source, out Vector<float> result)
    {
        StructType u;
        u.A = new Vector<float>(2) * source;
        u.B = new Vector<float>(3) * source;
        u.C = new Vector<float>(4) * source;
        u.D = new Vector<float>(5) * source;
        u.E = new Vector<float>(6) * source;
        u.F = new Vector<float>(7) * source;
        result = u.A + u.B + u.C + u.D + u.E + u.F;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float TestStruct()
    {
        Vector<float> f = GetVector();
        for (int i = 0; i < 100; ++i)
        {
            DoSomeWorkWithAStruct(ref f, out f);
        }
        return f[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void DoSomeWorkWithAStructAggressiveInlining(ref Vector<float> source, out Vector<float> result)
    {
        StructType u;
        u.A = new Vector<float>(2) * source;
        u.B = new Vector<float>(3) * source;
        u.C = new Vector<float>(4) * source;
        u.D = new Vector<float>(5) * source;
        u.E = new Vector<float>(6) * source;
        u.F = new Vector<float>(7) * source;
        result = u.A + u.B + u.C + u.D + u.E + u.F;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float TestStructAggressiveInlining()
    {
        Vector<float> f = GetVector();
        for (int i = 0; i < 100; ++i)
        {
            DoSomeWorkWithAStructAggressiveInlining(ref f, out f);
        }
        return f[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float TestStructManuallyInlined()
    {
        Vector<float> f = GetVector();
        for (int i = 0; i < 100; ++i)
        {
            StructType u;
            u.A = new Vector<float>(2) * f;
            u.B = new Vector<float>(3) * f;
            u.C = new Vector<float>(4) * f;
            u.D = new Vector<float>(5) * f;
            u.E = new Vector<float>(6) * f;
            u.F = new Vector<float>(7) * f;
            f = u.A + u.B + u.C + u.D + u.E + u.F;
        }
        return f[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void DoSomeWorkStructless(ref Vector<float> source, out Vector<float> result)
    {
        var a = new Vector<float>(2) * source;
        var b = new Vector<float>(3) * source;
        var c = new Vector<float>(4) * source;
        var d = new Vector<float>(5) * source;
        var e = new Vector<float>(6) * source;
        var f = new Vector<float>(7) * source;
        result = d + e + f + a + b + c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float TestStructless()
    {
        Vector<float> f = GetVector();
        for (int i = 0; i < 100; ++i)
        {
            DoSomeWorkStructless(ref f, out f);
        }
        return f[0];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float value = 0.0F;
        value += TestStruct();
        value -= TestStructAggressiveInlining();
        value += TestStructManuallyInlined();
        value -= TestStructless();
        if (!float.IsNaN(value))
        {
            Console.WriteLine(value.ToString());
            return -1;
        }
        return 100;
    }
}
