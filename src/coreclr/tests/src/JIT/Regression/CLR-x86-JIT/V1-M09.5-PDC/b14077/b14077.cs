// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal class Test
{
    static public int Main(string[] args)
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
