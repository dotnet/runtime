// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class test
{
    static float f1(float x, float y)
    {
        x -= x * y;
        return x;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        // expected: 2 - 2 * 3 = -4
        // with bug: 2 * (1 + 3) = 8

        float result = f1(2, 3);

        System.Console.WriteLine(result);

        if (result != -4.0)
        {
            System.Console.WriteLine("FAILED");
            return 1;
        }
        return 100;
    }
}
