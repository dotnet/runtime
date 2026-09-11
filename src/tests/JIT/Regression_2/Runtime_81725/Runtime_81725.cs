// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

[module: SkipLocalsInit]

public class Runtime_81725
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector4 a = new Vector4(1.0f, 2.0f, -3.0f, -4.0f);
        a.Z = 0.0f;
        a.W = 0.0f;
        a.Z = 3.0f;

        if (a != new Vector4(1.0f, 2.0f, 3.0f, 0.0f))
        {
            return 0;
        }

        return 100;
    }
}
