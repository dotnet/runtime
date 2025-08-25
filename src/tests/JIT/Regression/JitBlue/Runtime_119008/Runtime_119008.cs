// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

public class Runtime_119008
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector<float> left = Vector.Create(-65f);
        Vector<float> right = Vector.Create(-1264f);

        Assert.Equal(Vector<int>.Zero, Vector.LessThan(left, right));
        Assert.Equal(Vector<int>.AllBitsSet, Vector.LessThan(right, left));
    }
}
