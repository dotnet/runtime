// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_1104
{
    static int TestOutOfBoundProxy(Func<int> actualTest)
    {
        try
        {
            actualTest();
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("caught IndexOutOfRangeException");
            return 100;
        }
        Debug.Fail("unreached");
        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestOutOfBoundLowerDecreasing()
    {
        int[] arr = new int[10];
        int i = 9;
        int j = 15;
        int sum = 0;

        // our range check optimizer is very naive, so if you don't have
        // i < 10, then it thinks `i` can overflow and doesn't bother 
        // calling `Widen` at all.
        //
        while (j >= 0 && i < 10)
        {
            --j;
            --i;
            sum += arr[i];  // range check will use 9 as lower bound.

            Console.WriteLine("i = " + i + ", j = " + j);
        }
        return sum;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            TestOutOfBoundProxy(TestOutOfBoundLowerDecreasing);
        }
        catch (Exception)
        {
            return 101;
        }

        return 100;
    }
}
