// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Hello1
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i, j, k, l, m, t;

        i = j = k = l = m = 0;

        while (i < 10)
        {
            i++;
        }

    LOOP_START:

        switch (i)
        {
            case 1:
                j += 2;
                break;
            case 2:
                j += 3;
                break;
        }

        switch (j)
        {
            case 0:
            case 1:
            case 2:
                k = 1;
                break;

            case 3:
                goto LOOP_START;
                break;
        }


    LOOPEXIT:

        t = i;
        System.Console.WriteLine("i is {0}", t);
        t = j;
        System.Console.WriteLine("j is {0}", t);
        t = k;
        System.Console.WriteLine("k is {0}", t);
        t = l;
        System.Console.WriteLine("l is {0}", t);
        t = m;
        System.Console.WriteLine("m is {0}", t);

        return 100;
    }
}
