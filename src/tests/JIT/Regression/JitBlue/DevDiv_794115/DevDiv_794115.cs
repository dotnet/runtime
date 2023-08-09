// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Repro
{
    [Fact]
    public static int TestEntryPoint()
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
