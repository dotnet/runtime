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
        int exitcode = -1;
        try
        {
            Console.WriteLine(1);

            if (x == 5)
            {
                Console.WriteLine(2);
            }
            else
            {
                Console.WriteLine(3);
                throw new Exception();
            }

            Console.WriteLine(4);
        }
        catch
        {
            goto L;
        }

        Console.WriteLine(5);
        return exitcode;
    L:
        Console.WriteLine(6);
        exitcode = 100;
        return exitcode;
    }
}
