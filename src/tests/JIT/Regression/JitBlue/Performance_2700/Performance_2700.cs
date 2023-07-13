// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

public class Performance_2700
{
    private static readonly Vector<float> Value1 = Vector<float>.One;

    private static readonly Vector<float> Value2 = Vector<float>.One + Vector<float>.One;

    [Fact]
    public static int TestEntryPoint()
    {
        Vector<float> result = Vector.Multiply(Value1, Value2);
        return (result == new Vector<float>(2.0f)) ? 100 : 0;
    }
}
