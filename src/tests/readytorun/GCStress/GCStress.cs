// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GCStress
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestEntryPoint(bool generic)
    {
        object value = new object();
        GC.CollectionCount(0);
        int collections;
        object result = generic ? R2RGCStress<object>(value, out collections) : R2RGCStress(value, out collections);

        Console.WriteLine($"R2R GC stress (generic: {generic}): {collections} collections");
        Assert.Same(value, result);
        Assert.True(collections > 0, "R2R instruction stress did not collect.");

        if (generic)
        {
            string other = "shared generic code";
            Assert.Same(other, R2RGCStress<string>(other, out _));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object R2RGCStress(object value, out int collections)
    {
        Poll();
        int before = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Poll();
        }

        collections = GC.CollectionCount(0) - before;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T R2RGCStress<T>(T value, out int collections) where T : class
    {
        Poll();
        int before = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Poll();
        }

        collections = GC.CollectionCount(0) - before;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Poll() { }
}
