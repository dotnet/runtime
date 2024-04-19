// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class test
{
    static short si16;
    static uint su32;

    [Fact]
    public static int TestEntryPoint()
    {
        int exitCode = -1;
        short i16 = -1;
        uint u32 = (uint) i16;

        Console.WriteLine(u32);

        if (u32 == uint.MaxValue)
        {
            System.Console.WriteLine("Pass");
            exitCode = 100;
        }
        else
        {
            System.Console.WriteLine("Fail");
            exitCode = 101;
        }

        si16 = -1;
        su32 = (uint) si16;

        Console.WriteLine(su32);

        if (su32 == uint.MaxValue)
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
