// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_90323
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ConvertToSingle(long value) => (float)value;

    [Fact]
    public static int TestEntryPoint()
    {
        long value = 0x4000_0040_0000_0001L;

        if (ConvertToSingle(value) != (float)(value))
        {
            return 0;
        }

        if ((float)(value) != 4.6116866E+18f)
        {
            return 0;
        }

        return 100;
    }
}
