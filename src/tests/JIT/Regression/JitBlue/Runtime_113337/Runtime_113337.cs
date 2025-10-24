// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable SYSLIB5003 // Allow experimental SVE

public class Runtime_113337
{
    static sbyte[] s_7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void issue1()
    {
        try
        {
            var vr9 = Vector64.Create<float>(0);
            if ((2147483647 == (-(int)AdvSimd.Extract(vr9, 1))))
            {
                s_7 = s_7;
            }
        }
        catch (PlatformNotSupportedException e)
        {
        }
    }

    static int[][] s_2;
    static bool s_3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void issue2()
    {
        try
        {
            s_3 ^= (2021486855 != (-(long)s_2[0][0]));
        }
        catch (NullReferenceException e)
        {
        }
    }

    static Vector<ulong>[] s_4;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void issue3()
    {
        try
        {
            var vr3 = Vector.Create<short>(1);
            var vr4 = (short)0;
            var vr5 = Vector128.CreateScalar(vr4).AsVector();
            if ((Sve.TestFirstTrue(vr3, vr5) | (3268100580U != (-(uint)Sve.SaturatingDecrementBy16BitElementCount(0, 1)))))
            {
                s_4[0] = s_4[0];
            }
        }
        catch (PlatformNotSupportedException e)
        {
        }
        catch (NullReferenceException e)
        {
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // Checking for successful compilation
        issue1();
        issue2();
        issue3();
    }
}