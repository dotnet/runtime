// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Repro
{
    static char c1 = (char)32768;
    static char c2 = (char)0;

    static int Main()
    {
        //This testcase ensures that we correctly generate character comparisons

        if (c1 < c2)
        {
            Console.WriteLine("FAIL!");
            Console.WriteLine("Incorrect character comparison.");
            return 101;
        }
        else
        {
            Console.WriteLine("PASS!");
            return 100;
        }
    }
}
