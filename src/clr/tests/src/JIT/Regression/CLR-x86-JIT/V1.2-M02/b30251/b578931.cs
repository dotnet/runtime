// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

class Test
{
    static int Main()
    {
        int N = 3;
        int tmp = (1 << N) - 1;
        // This works as expected, evaluating to false ...
        bool evaluatesFalse = tmp > 0x7fffffff;  // OK, false
        // Same computation - evaluates to TRUE
        bool evaluatesTrue = ((1 << N) - 1) > 0x7fffffff;

        if (evaluatesFalse)
        {
            Console.WriteLine("Fail evaluatesFalse");
            return 1;
        }
        if (evaluatesTrue)
        {
            Console.WriteLine("Fail evaluatesTrue");
            return 1;
        }
        Console.WriteLine("PASS");
        return 100;
    }
}
