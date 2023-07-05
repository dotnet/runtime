// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class C
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float L(float a)
    {
        return M(float.NegativeInfinity, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float M(float a, float b)
    {
        return (float)Math.Pow(a, (float)Math.Pow(b, a));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return L(0) == M(float.NegativeInfinity, 0) ? 100 : 0;
    }
}
