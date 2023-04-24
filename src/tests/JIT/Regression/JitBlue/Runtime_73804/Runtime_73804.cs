// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_73804
{
    [Fact]
    public static int TestEntryPoint()
    {
        short value = 0x1000;
        int r = Problem(&value);

        return 100 + r - System.Numerics.BitOperations.LeadingZeroCount((uint)value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(short* s)
    {
        if (System.Runtime.Intrinsics.Arm.ArmBase.IsSupported)
        {
            return System.Runtime.Intrinsics.Arm.ArmBase.LeadingZeroCount(*s);
        }
        else
        {
            return System.Numerics.BitOperations.LeadingZeroCount((uint)*s);
        }
    }
}
