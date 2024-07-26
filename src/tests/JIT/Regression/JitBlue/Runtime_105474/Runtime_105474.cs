// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

#nullable disable

public class Runtime_105474_A
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Method1()
    {
        Vector128<ulong> vr0 = Vector128.CreateScalar(1698800584428641629UL);
        AdvSimd.ShiftLeftLogicalSaturate(vr0, 229);
    }

    private void Method0()
    {
        if (AdvSimd.IsSupported)
        {
            try
            {
                Method1();
                throw new Exception("Expected an ArgumentOutOfRangeException.");
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
    }

    [Fact]
    public static void TestEntryPoint() => new Runtime_105474_A().Method0();
}
