// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shlx64bit(ulong x, int y) => x << y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Sarx64bit(long x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shrx64bit(ulong x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe ulong ShrxRef64bit(ulong* x, int y) => *x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Rorx(ulong x) => BitOperations.RotateRight(x, 2);

    public static unsafe int Main()
    {
        const int PASS = 100;
        const int FAIL = 101;
        int returnCode = PASS;

        try
        {
            ulong valULong = 0;
            long valLong = 0;
            int shiftBy = 0;
            ulong resULong = 0;
            long resLong = 0;
            ulong expectedULong = 0;
            long expectedLong = 0;
            int MOD64 = 64;

            //
            // Shlx64bit tests
            //

            Console.WriteLine("### UnitTest: Shlx64bit ###############");
            ulong[] valShlx64bit = new ulong[] { 0, 8, 1, 1, 0xFFFFFFFFFFFFFFFF };
            int[] shiftByShlx64bit = new int[] { 1, 1, 63, 65, 1 };
            for (int idx = 0; idx < valShlx64bit.Length; idx++)
            {
                valULong = valShlx64bit[idx];
                shiftBy = shiftByShlx64bit[idx];
                resULong = Shlx64bit(valULong, shiftBy);
                if (idx == 4)
                    expectedULong = (ulong)(valULong ^ 1);
                else
                    expectedULong = (ulong)(valULong * Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Sarx64bit tests
            //

            Console.WriteLine("### UnitTest: Sarx64bit ###############");
            long[] valSarx64bit = new long[] { 1, -8, -8, 0x7FFFFFFFFFFFFFFF };
            int[] shiftBySarx64bit = new int[] { 1, 1, 65, 63 };
            for (int idx = 0; idx < valSarx64bit.Length; idx++)
            {
                valLong = valSarx64bit[idx];
                shiftBy = shiftBySarx64bit[idx];
                resLong = Sarx64bit(valLong, shiftBy);
                if (idx == 3)
                    expectedLong = 0;
                else
                    expectedLong = (long)(valLong / Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<long>(valLong, shiftBy, resLong, expectedLong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Shrx64bit tests
            //

            Console.WriteLine("### UnitTest: Shrx64bit ###############");
            ulong[] valShrx64bit = new ulong[] { 1, 8, 8, 0xFFFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF };
            int[] shiftByShrx64bit = new int[] { 1, 2, 65, 63, 65 };
            for (int idx = 0; idx < valShrx64bit.Length; idx++)
            {
                valULong = valShrx64bit[idx];
                shiftBy = shiftByShrx64bit[idx];
                resULong = Shrx64bit(valULong, shiftBy);
                if (idx == 3)
                    expectedULong = 1;
                else if (idx == 4)
                    expectedULong = 0x3FFFFFFFFFFFFFFF;
                else
                    expectedULong = (ulong)(valULong / Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // ShrxRef64bit
            //

            Console.WriteLine("### UnitTest: ShrxRef64bit ###############");
            valULong = 8;
            shiftBy = 1;
            resULong = ShrxRef64bit(&valULong, shiftBy);
            expectedULong = (ulong)(valULong / Math.Pow(2, (shiftBy % MOD64)));
            if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
            {
                returnCode = FAIL;
            }

            //
            // Rorx tests
            //

            Console.WriteLine("### UnitTest: Rorx ###############");
            valULong = 0xFF;
            shiftBy = 2;
            resULong = Rorx(valULong);
            expectedULong = 0xC00000000000003F;
            if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
            {
                returnCode = FAIL;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return FAIL;
        }

        if (returnCode == PASS)
        {
            Console.WriteLine("PASSED.");
        }
        else
        {
            Console.WriteLine("FAILED.");
        }
        return returnCode;
    }

    private static bool Validate<T>(T value, int shiftBy, T actual, T expected)
    {
        Console.Write("(value, shiftBy) ({0},{1}): {2}", value, shiftBy, actual);
        if (EqualityComparer<T>.Default.Equals(actual, expected))
        {
            Console.WriteLine(" == {0}   ==> Passed.", expected);
            return true;
        }
        else
        {
            Console.WriteLine(" != {0}    ==> Failed.", expected);
            return false;
        }
    }
}
