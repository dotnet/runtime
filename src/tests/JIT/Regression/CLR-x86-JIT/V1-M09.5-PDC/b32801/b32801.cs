// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class bug
{
    [Fact]
    public static int TestEntryPoint()
    {
        Decimal cur1 = new Decimal(UInt32.MaxValue);
        Console.WriteLine("The decimal value is: " + cur1);
        Console.WriteLine("The decimal value should be: " + UInt32.MaxValue);

        if ((long)cur1 != (long)UInt32.MaxValue)
        {
            Console.WriteLine("Test failed");
            return 1;
        }
        else
        {
            Console.WriteLine("Test passed");
            return 100;
        }
    }
}
