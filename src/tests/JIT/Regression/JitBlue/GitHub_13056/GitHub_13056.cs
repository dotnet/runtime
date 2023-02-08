// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Collections;
using Xunit;

public class Regression13056
{
    const int Pass = 100;
    const int Fail = -1;

    private static void ThrowMinMaxException<T>(T min, T max)
    {
        throw new ArgumentException("min > max");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Double Clamp(Double value, Double min, Double max)
    {
        if (min > max)
            ThrowMinMaxException(min, max);
        if (value < min)
            return min;
        else if (value > max)
            return max;
        return value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (Clamp(1, 2, 3)   != 2)  return Fail;
        if (Clamp(4, 2, 10)  != 4)  return Fail;
        if (Clamp(8, 2, 9)   != 8)  return Fail;
        if (Clamp(10, 2, 11) != 10) return Fail;

        return Pass;
    }
}
