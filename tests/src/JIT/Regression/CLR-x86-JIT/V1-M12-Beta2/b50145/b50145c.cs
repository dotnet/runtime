// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class test
{
    public static int Main()
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
