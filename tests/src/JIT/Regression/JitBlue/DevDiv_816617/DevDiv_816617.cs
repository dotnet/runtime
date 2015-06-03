// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

static class Repro
{
    static double NegativeZero = -0.0;

    static int Main()
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
