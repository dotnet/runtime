// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_36905
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool success = true;

        Vector3 a = new Vector3(1.0f, 2.0f, 3.0f);
        Vector3 b = new Vector3(1.0f, 2.0f, 3.0f);

        success &= ValidateResult(a == b, expected: true);
        success &= ValidateResult(a != b, expected: false);

        Vector3 c = new Vector3(1.0f, 2.0f, 3.0f);
        Vector3 d = new Vector3(10.0f, 2.0f, 3.0f);

        success &= ValidateResult(c == d, expected: false);
        success &= ValidateResult(c != d, expected: true);

        return success ? 100 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ValidateResult(bool actual, bool expected)
    {
        return actual == expected;
    }
}
