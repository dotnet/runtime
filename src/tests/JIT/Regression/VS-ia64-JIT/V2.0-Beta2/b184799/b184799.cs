// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class foo
{

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("testTableSwitch:  ");
        int s = 2, r = 3;
        s = s * 3;
        switch (s)
        {
            case 0: goto case 4;
            case 4: r = 0; break;
            case 1: goto case 2;
            case 2: r = 1; break;
            case 3: goto case 5;//was 3
            case 5: goto case 6;
            case 6: r = 3; break;
            default: r = -1;
                break;
        }
        if (r != 3)
        {
            Console.WriteLine("took wrong case branch, FAILED");
            return 101;
        }
        s = s + 100;
        switch (s)
        {
            case 0: goto case 4;
            case 4: r = 0; break;
            case 1: goto case 2;
            case 2: r = 1; break;
            case 3: goto case 5;//was 3
            case 5: goto case 6;
            case 6: r = 3; break;
            default: r = -1;
                break;
        }
        if (r != -1)
        {
            Console.WriteLine("failed to take default branch, FAILED");
            return 101;
        }
        Console.WriteLine("Test SUCCESS");
        return 100;

    }
} ;
