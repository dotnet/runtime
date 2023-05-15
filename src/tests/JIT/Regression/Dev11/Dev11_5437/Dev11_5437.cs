// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Program
{
    static void f(int c, int d, int e)
    {
        Console.WriteLine("c={0}, d={1}, e={2}", c, d, e);
        if (c + d != 4)
        {
            Console.WriteLine("FAILED: c + d != 4"); //We are hitting the bug so bailing out
            throw new Exception("FAILED");
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int d = 0;
        int i = 3;
        for (int e = 0; e < 2; e++)
        {
            while (true)
            {
                int c = 3 - d++;
                f(c, d, e); //  c == 3-d+1 !
                if (--i < 1) break;
            }
        }
        Console.WriteLine("PASSED");
        return 100; //Didn't hit the bug so return success
    }
}
