// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_122254
{
    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            Test(new int[0]);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(int[] arr)
    {
        int block = arr.Length;
        switch (block)
        {
            case 2:
                Console.WriteLine("2");
                break;
            case 3:
                Console.WriteLine("3");
                break;
            case 4:
                Console.WriteLine("4");
                break;
            default:
                if (block == 0)
                {
                    throw new InvalidOperationException();
                }
                break;
        }
    }
}