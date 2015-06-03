// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Repro
{
    static int Main()
    {
        // This testcase ensures that we correctly handle static field
        // reads of different size than the destination for mul

        if (Three * 3 != 9)
        {
            Console.WriteLine("FAIL!");
            Console.WriteLine(Three * 3);
            return 101;
        }
        Console.WriteLine("PASS!");
        return 100;
    }

    static short Three = 3;
    static short Dummy = -1;
}