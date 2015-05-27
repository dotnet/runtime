// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////
//
// Description 
// ____________
// Code that walks arrays using for loops (or similar coding 
// constructs) could incorrectly get IndexOutOfRangeException.  
//
// Right Behavior
// ________________
// No Exception should be thrown
//
// Wrong Behavior
// ________________
// Throwing some kind of exception, mostly IndexOutOfRange
//
// Commands to issue
// __________________
// > test1.exe
////////////////////////////////////////////////////////////////

using System;

public class Test
{
    public static int Main(string[] args)
    {
        int retCode = 100;

        try
        {
            Test.Check(args.Length);
        }
        catch (IndexOutOfRangeException e)
        {
            System.Console.WriteLine("Exception thrown: " + e);
            retCode = 1;
        }

        return retCode;
    }

    public static void Check(int i)
    {
        int nav = i;
        int[] av = new int[8];

        for (i = 0; i < nav; i++)
        {
            av[i]--;
        }
    }
}
