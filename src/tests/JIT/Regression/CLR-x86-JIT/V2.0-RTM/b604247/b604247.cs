// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool testCaseSucceeded = false;
        string[] theArray = { "Wrong =0", "Correct =1", "Wrong =2", "Wrong =3" };

        for (int i = 0; i < 1; i++)
        {
            if (theArray[i * 3 + 1] != "-")
            {
                testCaseSucceeded = (theArray[(i * 3) + 1] == theArray[1]);
                Console.WriteLine("First Result: " + theArray[(i * 3) + 1]);
                int temp = (i * 3) + 1;
                Console.WriteLine("Second Result: " + theArray[temp]);
            }
        }

        if (testCaseSucceeded)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 666;
        }
    }
}
