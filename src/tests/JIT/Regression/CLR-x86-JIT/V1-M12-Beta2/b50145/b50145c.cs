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
        float x = 2;
        x *= x * 3;

        if (x != 12)
        {
            System.Console.WriteLine("\nx is {0}.  Expected: 12", x);
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
