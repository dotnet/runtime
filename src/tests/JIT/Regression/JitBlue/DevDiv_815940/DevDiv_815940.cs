// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Repro
{
    static char c1 = (char)32768;
    static char c2 = (char)0;

    [Fact]
    public static int TestEntryPoint()
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
