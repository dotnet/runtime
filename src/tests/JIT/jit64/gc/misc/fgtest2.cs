// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class T
{
    public static int x = 4;

    [Fact]
    public static int TestEntryPoint()
    {
        int exitcode = 94;

        goto L3;
    L:
        x = 5;
        exitcode++;
        Console.WriteLine(3);
    L3:
        try
        {
            Console.WriteLine("1/4");
            exitcode++;

            if (x == 5)
            {
                try
                {
                    exitcode++;
                    Console.WriteLine(5);

                    if (x == 5) throw new Exception();
                }
                catch
                {
                    goto L2;
                }
            }
            else
            {
                exitcode++;
                Console.WriteLine(2);
                throw new Exception();
            }

            exitcode++;
            Console.WriteLine(4);
        }
        catch
        {
            goto L;
        }

        exitcode++;
        Console.WriteLine(-1);
    L2:
        exitcode++;
        Console.WriteLine(6);
        return exitcode;
    }
}
