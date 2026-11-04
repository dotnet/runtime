// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134193
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.True(Test<string>());
        Assert.Throws<NullReferenceException>(() => Test<object>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Test<T>()
    {
        object o = "hi";
        IComparable<T> c = o as IComparable<T>;
        return c.GetType() == typeof(string);
    }
}
