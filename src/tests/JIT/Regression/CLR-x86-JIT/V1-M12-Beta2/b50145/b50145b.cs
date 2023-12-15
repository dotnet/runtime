// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        float x = 1;
        x /= x * 2;

        if (x != 0.5)
        {
            System.Console.WriteLine("\nx is {0}.  Expected: 0.5", x);
            System.Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            System.Console.WriteLine("PASSED");
            return 100;
        }
    }
}
