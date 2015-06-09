// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class test
{
    public static int Main()
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
