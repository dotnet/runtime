// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// compile with csc /o+
using System;
class MyClass
{

    public static int Main()
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
