// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_109981
{
    [Fact]
    public static int TestEntryPoint() => Foo(14);

    public static int Foo(int x)
    {
        if (x == 123)
            return 0;

        int sum = 9;
        for (int i = 0; i < x; i++)
        {
            sum += i;
        }

        try
        {
            if (x != 123)
                return sum;

            try
            {
                try
                {
                    Bar();
                }
                finally
                {
                    sum += 1000;
                }
            }
            catch (ArgumentException)
            {
                sum += 10000;
            }
        }
        catch when (Filter())
        {
            sum += 100000;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bar()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Filter()
    {
        return true;
    }
}
