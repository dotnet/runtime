// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for null check assertion.

using System;
using Xunit;

public class Sample7
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(int[,] a1)
    {
        int h;

        h = a1.Length;

        if (a1 == null)
        {
            throw new Exception();
        }

        return h;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            int h = func(new int[3, 3]);
            Console.WriteLine(h);
            if (h == 9)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
