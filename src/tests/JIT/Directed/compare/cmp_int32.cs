// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using Xunit;


// The integer field of a struct passed in a register according to RISC-V floating-point calling convention is
// not extended. Comparisons on RISC-V, however, always operate on full registers.
// Note: reflection calls poison the undefined bits in runtime debug mode making it a better repro.

public static class CompareTestInt
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt2((int, float) x) => (x.Item1 < 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt2((int, float) x) => (x.Item1 > 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le2((int, float) x) => (x.Item1 <= 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge2((int, float) x) => (x.Item1 >= 2);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt0((int, float) x) => (x.Item1 < 0);
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt0((int, float) x) => (x.Item1 > 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le0((int, float) x) => (x.Item1 <= 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge0((int, float) x) => (x.Item1 >= 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq0((int, float) x) => (x.Item1 == 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne0((int, float) x) => (x.Item1 != 0);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt1((int, float) x) => (x.Item1 < 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt1((int, float) x) => (x.Item1 > 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le1((int, float) x) => (x.Item1 <= 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge1((int, float) x) => (x.Item1 >= 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq1((int, float) x) => (x.Item1 == 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne1((int, float) x) => (x.Item1 != 1);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool LtMinus1((int, float) x) => (x.Item1 < -1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool GtMinus1((int, float) x) => (x.Item1 > -1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool LeMinus1((int, float) x) => (x.Item1 <= -1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool GeMinus1((int, float) x) => (x.Item1 >= -1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool EqMinus1((int, float) x) => (x.Item1 == -1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool NeMinus1((int, float) x) => (x.Item1 != -1);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq2048((int, float) x) => (x.Item1 == 2048);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne2048((int, float) x) => (x.Item1 != 2048);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool EqMinus2048((int, float) x) => (x.Item1 == -2048);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool NeMinus2048((int, float) x) => (x.Item1 != -2048);

    [Fact]
    public static void Test()
    {
        var type = typeof(CompareTestInt);
        var args = new object[] {(2, 0f)};
        Assert.False((bool)type.GetMethod("Lt2").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge2").Invoke(null, args));
        args = new object[] {(0, 0f)};
        Assert.False((bool)type.GetMethod("Lt0").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Eq0").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne0").Invoke(null, args));
        args = new object[] {(1, 0f)};
        Assert.False((bool)type.GetMethod("Lt1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Eq1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne1").Invoke(null, args));
        args = new object[] {(-1, 0f)};
        Assert.False((bool)type.GetMethod("LtMinus1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("GtMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("LeMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("GeMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("EqMinus1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("NeMinus1").Invoke(null, args));
        args = new object[] {(2048, 0f)};
        Assert.True((bool)type.GetMethod("Eq2048").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne2048").Invoke(null, args));
        args = new object[] {(-2048, 0f)};
        Assert.True((bool)type.GetMethod("EqMinus2048").Invoke(null, args));
        Assert.False((bool)type.GetMethod("NeMinus2048").Invoke(null, args));
    }
}

public static class CompareTestUint
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt2((uint, float) x) => (x.Item1 < 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt2((uint, float) x) => (x.Item1 > 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le2((uint, float) x) => (x.Item1 <= 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge2((uint, float) x) => (x.Item1 >= 2);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt0((uint, float) x) => (x.Item1 < 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt0((uint, float) x) => (x.Item1 > 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le0((uint, float) x) => (x.Item1 <= 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge0((uint, float) x) => (x.Item1 >= 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq0((uint, float) x) => (x.Item1 == 0);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne0((uint, float) x) => (x.Item1 != 0);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Lt1((uint, float) x) => (x.Item1 < 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Gt1((uint, float) x) => (x.Item1 > 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Le1((uint, float) x) => (x.Item1 <= 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ge1((uint, float) x) => (x.Item1 >= 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq1((uint, float) x) => (x.Item1 == 1);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne1((uint, float) x) => (x.Item1 != 1);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool LtMinus1((uint, float) x) => (x.Item1 < 0xFFFF_FFFFu);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool GtMinus1((uint, float) x) => (x.Item1 > 0xFFFF_FFFFu);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool LeMinus1((uint, float) x) => (x.Item1 <= 0xFFFF_FFFFu);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool GeMinus1((uint, float) x) => (x.Item1 >= 0xFFFF_FFFFu);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool EqMinus1((uint, float) x) => (x.Item1 == 0xFFFF_FFFFu);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool NeMinus1((uint, float) x) => (x.Item1 != 0xFFFF_FFFFu);


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Eq2048((uint, float) x) => (x.Item1 == 2048);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool Ne2048((uint, float) x) => (x.Item1 != 2048);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool EqMinus2048((uint, float) x) => (x.Item1 == 0xFFFF_F800u);

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool NeMinus2048((uint, float) x) => (x.Item1 != 0xFFFF_F800u);

    [Fact]
    public static void Test()
    {
        var type = typeof(CompareTestUint);
        var args = new object[] {(2u, 0f)};
        Assert.False((bool)type.GetMethod("Lt2").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge2").Invoke(null, args));
        args = new object[] {(0u, 0f)};
        Assert.False((bool)type.GetMethod("Lt0").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge0").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Eq0").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne0").Invoke(null, args));
        args = new object[] {(1u, 0f)};
        Assert.False((bool)type.GetMethod("Lt1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Eq1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne1").Invoke(null, args));
        args = new object[] {(0xFFFF_FFFFu, 0f)};
        Assert.False((bool)type.GetMethod("LtMinus1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("GtMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("LeMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("GeMinus1").Invoke(null, args));
        Assert.True((bool)type.GetMethod("EqMinus1").Invoke(null, args));
        Assert.False((bool)type.GetMethod("NeMinus1").Invoke(null, args));
        args = new object[] {(2048u, 0f)};
        Assert.True((bool)type.GetMethod("Eq2048").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne2048").Invoke(null, args));
        args = new object[] {(0xFFFF_F800u, 0f)};
        Assert.True((bool)type.GetMethod("EqMinus2048").Invoke(null, args));
        Assert.False((bool)type.GetMethod("NeMinus2048").Invoke(null, args));
    }
}
