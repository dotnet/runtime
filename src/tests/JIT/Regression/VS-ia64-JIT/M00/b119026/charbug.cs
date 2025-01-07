// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class test
{
    static sbyte si8;
    static char sc;

    [Fact]
    public static int TestEntryPoint()
    {
        int exitCode = -1;
        sbyte i8 = -1;
        char c = (char) i8;

        Console.WriteLine("{0:X}: {1}", Convert.ToUInt32(c), ((ushort)c));

        if (c == char.MaxValue)
        {
            Console.WriteLine("Pass");
            exitCode = 100;
        }
        else
        {
            Console.WriteLine("Fail");
            exitCode = 101;
        }

        si8 = -1;
        sc = (char)si8;

        Console.WriteLine("{0:X}: {1}", Convert.ToUInt32(sc), ((ushort)sc));

        if (sc == char.MaxValue)
        {
            System.Console.WriteLine("Pass");
        }
        else
        {
            System.Console.WriteLine("Fail");
            if (exitCode == 100)
                exitCode = 101;
        }

        return exitCode;
    }
}
