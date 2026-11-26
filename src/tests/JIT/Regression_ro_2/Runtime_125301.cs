// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_125301
{
    [Fact]
    public static void TestEntryPoint()
    {
        const short expected = 6;
        short swapped = Swap(expected);
        short result = Swap(swapped);

        Assert.Equal(expected, result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe short Swap(short value)
    {
        short returnValue = short.MinValue;

        Swap2((byte*)&value, (byte*)&returnValue);

        return returnValue;
    }

    private static unsafe void Swap2(byte* originalBytes, byte* returnBytes)
    {
        returnBytes[0] = originalBytes[1];
        returnBytes[1] = originalBytes[0];
    }
}
