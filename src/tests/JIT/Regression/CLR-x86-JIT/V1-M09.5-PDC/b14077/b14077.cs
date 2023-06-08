// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test
{
    [Fact]
    static public int TestEntryPoint()
    {
        bool b1 = Double.IsPositiveInfinity(Math.Exp(Double.PositiveInfinity));
        bool b2 = 0 == Math.Exp(Double.NegativeInfinity);
        Console.WriteLine(b1);
        Console.WriteLine(b2);
        if (b1 && b2)
        {
            Console.WriteLine("***PASSED***");
            return 100;
        }
        else
        {
            Console.WriteLine("***FAILED***");
            return 1;
        }
    }
}
