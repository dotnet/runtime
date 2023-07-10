// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    [Fact]
    public static int TestEntryPoint() => Run(new string[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(string[] args)
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

    internal static void Check(int i)
    {
        int nav = i;
        int[] av = new int[8];

        for (i = 0; i < nav; i++)
        {
            av[i]--;
        }
    }
}
