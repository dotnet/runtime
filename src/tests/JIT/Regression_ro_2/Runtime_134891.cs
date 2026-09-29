// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134891
{
    private const int Threshold = 0x1000000;

    [Fact]
    public static void TestEntryPoint()
    {
        Compare(typeof(Runtime_134891).TypeHandle.Value, typeof(object).TypeHandle.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Compare(nint value, nint otherValue)
    {
        nint handle = typeof(Runtime_134891).TypeHandle.Value;
        nint otherHandle = typeof(object).TypeHandle.Value;

        Assert.Equal(value > Threshold, handle > Threshold);
        Assert.Equal(value >= Threshold, handle >= Threshold);
        Assert.Equal(value < Threshold, handle < Threshold);
        Assert.Equal(value <= Threshold, handle <= Threshold);
        Assert.Equal(value == Threshold, handle == Threshold);
        Assert.Equal(value != Threshold, handle != Threshold);

        Assert.Equal(Threshold > value, Threshold > handle);
        Assert.Equal(Threshold >= value, Threshold >= handle);
        Assert.Equal(Threshold < value, Threshold < handle);
        Assert.Equal(Threshold <= value, Threshold <= handle);
        Assert.Equal(Threshold == value, Threshold == handle);
        Assert.Equal(Threshold != value, Threshold != handle);

        Assert.Equal((nuint)value > Threshold, (nuint)handle > Threshold);
        Assert.Equal((nuint)value >= Threshold, (nuint)handle >= Threshold);
        Assert.Equal((nuint)value < Threshold, (nuint)handle < Threshold);
        Assert.Equal((nuint)value <= Threshold, (nuint)handle <= Threshold);
        Assert.Equal(Threshold > (nuint)value, Threshold > (nuint)handle);
        Assert.Equal(Threshold >= (nuint)value, Threshold >= (nuint)handle);
        Assert.Equal(Threshold < (nuint)value, Threshold < (nuint)handle);
        Assert.Equal(Threshold <= (nuint)value, Threshold <= (nuint)handle);

        Assert.Equal(value > 0, handle > 0);
        Assert.Equal(value >= 0, handle >= 0);
        Assert.Equal(value < 0, handle < 0);
        Assert.Equal(value <= 0, handle <= 0);
        Assert.Equal(0 > value, 0 > handle);
        Assert.Equal(0 >= value, 0 >= handle);
        Assert.Equal(0 < value, 0 < handle);
        Assert.Equal(0 <= value, 0 <= handle);
        Assert.False(handle == 0);
        Assert.True(handle != 0);

        Assert.Equal(value > otherValue, handle > otherHandle);
        Assert.Equal(value >= otherValue, handle >= otherHandle);
        Assert.Equal(value < otherValue, handle < otherHandle);
        Assert.Equal(value <= otherValue, handle <= otherHandle);
        Assert.Equal((nuint)value > (nuint)otherValue, (nuint)handle > (nuint)otherHandle);
        Assert.Equal((nuint)value >= (nuint)otherValue, (nuint)handle >= (nuint)otherHandle);
        Assert.Equal((nuint)value < (nuint)otherValue, (nuint)handle < (nuint)otherHandle);
        Assert.Equal((nuint)value <= (nuint)otherValue, (nuint)handle <= (nuint)otherHandle);
        Assert.False(handle == otherHandle);
        Assert.True(handle != otherHandle);
        Assert.True(handle == typeof(Runtime_134891).TypeHandle.Value);
        Assert.False(handle != typeof(Runtime_134891).TypeHandle.Value);
    }
}
