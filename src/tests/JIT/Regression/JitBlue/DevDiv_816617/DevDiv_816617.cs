// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public static class Repro
{
    static double NegativeZero = -0.0;

    [Fact]
    public static int TestEntryPoint()
    {
        // This testcase ensures that we explicitly add Negative zero
        // and Positive Zero producing Positive Zero(0x00000000 000000000)
        // and converting it to Int64 bits results in zero

        Int64 value = BitConverter.DoubleToInt64Bits(NegativeZero + 0.0);

        if (value == 0)
        {
            Console.WriteLine("PASS!");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL!");
            Console.WriteLine("(-0.0 + 0.0) != 0.0");
            return 101;
        }
    }
}
