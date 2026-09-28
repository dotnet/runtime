// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_129176;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_129176
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ok = 0;

        byte[] limitArr = new byte[1];
        byte[] accessArr = new byte[5];

        // A decreasing loop whose limit array is different from the indexed array
        // must not have its bounds checks elided by loop cloning when init is
        // beyond the indexed array's length: the IV would visit OOB indices.
        // Using accessArr.Length as the bad init value exercises the failure
        // exactly one element past the end.
        try
        {
            DecreasingGT_VarInit(limitArr, accessArr, accessArr.Length);
        }
        catch (IndexOutOfRangeException)
        {
            ok++;
        }

        try
        {
            DecreasingGE_VarInit(limitArr, accessArr, accessArr.Length);
        }
        catch (IndexOutOfRangeException)
        {
            ok++;
        }

        // Same shape with a constant init -- exercises the HasConstInit decreasing
        // path that also previously had its `ident` overwritten by the limit array
        // length.
        try
        {
            DecreasingGT_ConstInit(limitArr, accessArr);
        }
        catch (IndexOutOfRangeException)
        {
            ok++;
        }

        // Safe patterns must still produce correct results.
        byte[] a = new byte[10];
        for (int i = 0; i < a.Length; i++) a[i] = (byte)i;

        if (DecreasingSelfGE(a) == 45) ok++;
        if (DecreasingSelfGT(a) == 45) ok++;
        if (DecreasingFromOffset(a, 5) == 1 + 2 + 3 + 4 + 5) ok++;

        return ok == 6 ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingGT_VarInit(byte[] limitArr, byte[] accessArr, int startIdx)
    {
        int sum = 0;
        for (int i = startIdx; i > limitArr.Length; i--)
            sum += accessArr[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingGE_VarInit(byte[] limitArr, byte[] accessArr, int startIdx)
    {
        int sum = 0;
        for (int i = startIdx; i >= limitArr.Length; i--)
            sum += accessArr[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingGT_ConstInit(byte[] limitArr, byte[] accessArr)
    {
        // accessArr.Length is 5; const init 5 is one past the end, must throw.
        int sum = 0;
        for (int i = 5; i > limitArr.Length; i--)
            sum += accessArr[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingSelfGE(byte[] a)
    {
        int sum = 0;
        for (int i = a.Length - 1; i >= 0; i--)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingSelfGT(byte[] a)
    {
        int sum = 0;
        for (int i = a.Length - 1; i > -1; i--)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DecreasingFromOffset(byte[] a, int start)
    {
        int sum = 0;
        for (int i = start; i > 0; i--)
            sum += a[i];
        return sum;
    }
}
