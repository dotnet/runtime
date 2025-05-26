// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using Xunit;

public class CompareTest
{
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Lt2((int, float) x) => (x.Item1 < 2);
    
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Gt2((int, float) x) => (x.Item1 > 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Le2((int, float) x) => (x.Item1 <= 2);
    
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Ge2((int, float) x) => (x.Item1 >= 2);

    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Eq0((int, float) x) => (x.Item1 == 0);
    
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Ne0((int, float) x) => (x.Item1 != 0);

    [Fact]
    public static void Test()
    {
        // The integer field of a struct passed in a register according to RISC-V floating-point calling convention is
        // not extended. Comparisons on RISC-V, however, always operate on full registers.
        // Note: reflection calls poison the undefined bits in runtime debug mode making it a better repro.
        var type = typeof(CompareTest);
        var args = new object[] {(2, 0f)};
        Assert.False((bool)type.GetMethod("Lt2").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Gt2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Le2").Invoke(null, args));
        Assert.True((bool)type.GetMethod("Ge2").Invoke(null, args));
        args = new object[] {(0, 0f)};
        Assert.True((bool)type.GetMethod("Eq0").Invoke(null, args));
        Assert.False((bool)type.GetMethod("Ne0").Invoke(null, args));
    }
}
