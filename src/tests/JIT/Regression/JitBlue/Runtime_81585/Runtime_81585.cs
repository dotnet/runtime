// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

[module: SkipLocalsInit]

public class Runtime_81585
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector4 a = new Vector4(new Vector2(1.051f, 2.05f), 3.478f, 1.0f);
        Vector4 b = new Vector4(new Vector3(1.051f, 2.05f, 3.478f), 0.0f);
        b.W = 1.0f;

        float actual = Vector4.Distance(a, b);
        AssertEqual(0.0f, actual);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertEqual(float expected, float actual)
    {
        if (expected != actual)
        {
            throw new Exception($"Expected: {expected}; Actual: {actual}");
        }
    }
}
