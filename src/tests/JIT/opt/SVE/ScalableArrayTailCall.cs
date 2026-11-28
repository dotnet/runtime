// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class ScalableArrayTailCall
{
    private delegate ref T Selector<T>(T[] values, bool last);

    [Fact]
    public static void TestEntryPoint()
    {
        Vector<int>[] values = { new Vector<int>(17), new Vector<int>(42) };
        Assert.Equal(17, Invoke(Select, values, false)[0]);
        Assert.Equal(42, Invoke(Select, values, true)[0]);
        Assert.Equal(17, Invoke(Select, new[] { new Vector<int>(111), values[0], values[1] }, false)[0]);

        int[] scalars = { 17, 42 };
        Assert.Equal(17, Invoke(Select, scalars, false));
        Assert.Equal(42, Invoke(Select, scalars, true));
    }

    // Keep Select independently compiled without NoInlining, which would also disable its tail calls.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref T Invoke<T>(Selector<T> select, T[] values, bool last) => ref select(values, last);

    private static ref T Select<T>(T[] values, bool last)
    {
        if (!last)
        {
            return ref values[values.Length - 2];
        }

        return ref Last(values);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref T Last<T>(T[] values) => ref values[values.Length - 1];
}
