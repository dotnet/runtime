// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.CompilerServices;
using Xunit;

// This test is a reduced repro case for DevDiv VSO bug 278365.
// The failure mode is that the RyuJIT/x86 backend changed call to ROUND intrinsic
// with double return type to ROUND intrinsic with int return type, that is not supported.

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Bar()
    {
        int sum = 0;
        for (int i = 0; i < 100; ++i)
        {
            int v = (int)Math.Round(4.4 + i);
            sum += v;
        }
        sum -= 4 * 100;
        if (sum != 100 * 99 / 2)
        {
            return 0;
        }
        else
        {
            return 100;
        }        
    }
	
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            if (Bar() != 100)
                return 0;
        }
        catch (Exception)
        {
        }

        Console.WriteLine("Pass");
        return 100;
    }
}
