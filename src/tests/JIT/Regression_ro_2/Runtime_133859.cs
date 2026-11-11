// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133859
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, Test(0, new int[300], 44));
        Assert.Equal(0, Test(0, new int[300], 45));
        Assert.Equal(3, Test(0, new int[44], 44));
        Assert.Equal(3, Test(0, new int[0], 256));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int value)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(byte b, int[] array, int x)
    {
        b = (byte)x;
        int length = array.Length;
        int result = 0;
        try
        {
            if (b != length)
            {
                Consume(b);
                Consume(length);
                result = (b == length) ? 1 : 0;
            }
            else
            {
                result = 3;
            }

            if (x == 12345)
            {
                length = 7;
            }
        }
        catch
        {
            result = length;
        }
        return result;
    }
}
