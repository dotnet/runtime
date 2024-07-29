// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class AA
{
    [Fact]
    public static int TestEntryPoint()
    {
        uint SMALL1 = 0x00000100;
        uint SMALL2 = 0x7fffffff;
        uint BIG1 = 0x80000000;
        uint BIG2 = 0xffffffff;
        int[] array = null;
        AA[,,] marray = null;
        try
        {
            array = new int[SMALL1];
            Console.WriteLine("Test 1 passed");
        }
        catch (Exception)
        {
            Console.WriteLine("Test 1 failed");
            return 101;
        }
        try
        {
            array = new int[SMALL2];
            Console.WriteLine("Test 2 failed");
            return 102;
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine("Test 2 passed");
        }
        try
        {
            array = new int[BIG1];
            Console.WriteLine("Test 3 failed");
            return 103;
        }
        catch (OverflowException)
        {
            Console.WriteLine("Test 3 passed");
        }
        try
        {
            array = new int[BIG2];
            Console.WriteLine("Test 4 failed");
            return 104;
        }
        catch (OverflowException)
        {
            Console.WriteLine("Test 4 passed");
        }
        try
        {
            marray = new AA[SMALL1, 1, SMALL1];
            Console.WriteLine("Test 5 passed");
        }
        catch (Exception)
        {
            Console.WriteLine("Test 5 failed");
            return 105;
        }
        try
        {
            marray = new AA[2, SMALL2, SMALL2];
            Console.WriteLine("Test 6 failed");
            return 106;
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine("Test 6 passed");
        }
        try
        {
            marray = new AA[BIG1, BIG1, 2];
            Console.WriteLine("Test 7 failed");
            return 107;
        }
        catch (OverflowException)
        {
            Console.WriteLine("Test 7 passed");
        }
        try
        {
            marray = new AA[BIG2, 0, 1];
            Console.WriteLine("Test 8 failed");
            return 108;
        }
        catch (OverflowException)
        {
            Console.WriteLine("Test 8 passed");
        }
        Console.WriteLine("All tests passed");
        return 100;
    }
}
