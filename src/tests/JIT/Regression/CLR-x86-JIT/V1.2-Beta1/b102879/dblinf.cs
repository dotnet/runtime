// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// compile with csc /o+

namespace b102879;

using System;
using Xunit;
public class MyClass
{

    [OuterLoop]
    [Fact]
    public static int TestEntryPoint()
    {

        double d1 = double.PositiveInfinity;
        double d2 = -0.0;
        double d3 = d1 / d2;

        if (d3 == double.NegativeInfinity)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
