// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Test
{
    /// <summary>
    /// Another 64 bit optimization issue where we dont do the coversion correctly. The following output is seen when this program fails
    /// Error! expected, -4.54403989493052E+18, returned: -66876.654654
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [Fact]
    public static int TestEntryPoint()
    {
        double expected = -4.54403989493052E+18;
        double value = -66876.654654;
        double result = (double)BitConverter.DoubleToInt64Bits(value);
        if (result > -4.5E18)
        {
            Console.WriteLine("Error! expected, {0}, returned: {1}", expected, result);
            return -1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
