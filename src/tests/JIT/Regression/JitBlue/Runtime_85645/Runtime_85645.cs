// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_85645
{
    [Fact]
    public static int Test()
    {
        try
        {
            Bar(null, new int[0]);
            Console.WriteLine("FAIL: Should have thrown exception");
            return -1;
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("PASS: Caught NullReferenceException");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: {0}", ex);
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bar(int[] a, int[] b)
    {
        Consume(a[0], b[0] + 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int a, int b)
    {
    }
}
