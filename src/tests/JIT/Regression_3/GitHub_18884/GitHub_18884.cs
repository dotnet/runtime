// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This bug had to do with the handling (reserving and killing) of RCX
// for variable shift operations on X64.

using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Xunit;

public static class GitHub_18884
{
    static ushort s_3;
    static long s_5;
    static int returnVal = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        s_3 = 0; // avoid runtime checks in M15
        ReproWindows(0, 0, 1, 0);
        ReproUx(0, 0, 1, 0);
        Set_Mask_AllTest();
        return returnVal;
    }

    internal static void ReproWindows(byte arg0, long arg1, ushort arg2, ulong arg3)
    {
        s_5 >>= 50 / arg2;  // the value shifted by here
        if (arg0 != 0)
        {
            s_3 = s_3;
        }

        // Is in arg0 here
        if (arg0 != 0)
        {
            Console.WriteLine("FAIL: ReproWindows");
            returnVal = -1;
        }
    }

    internal static void ReproUx(ulong arg0, long arg1, ushort arg2, byte arg3)
    {
        s_5 >>= 50 / arg2;  // the value shifted by here
        if (arg3 != 0)
        {
            s_3 = s_3;
        }

        // Is in arg3 here
        if (arg3 != 0)
        {
            Console.WriteLine("FAIL: ReproUx");
            returnVal = -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CheckValue(int value, int expectedValue)
    {
        if (value != expectedValue)
        {
            returnVal = -1;
            Console.WriteLine("FAIL: Set_Mask_AllTest");
        }
    }

    // While fixing the above failures, this test (from corefx) failed.
    internal static void Set_Mask_AllTest()
    {
        BitVector32 flip = new BitVector32();
        int mask = 0;
        for (int bit = 1; bit < 32 + 1; bit++)
        {
            mask = BitVector32.CreateMask(mask);
            BitVector32 single = new BitVector32();
            single[mask] = true;

            // The bug was exposed by passing the result of a shift in RCX on x64/ux.
            CheckValue(1 << (bit - 1), single.Data);
        }
    }
}
