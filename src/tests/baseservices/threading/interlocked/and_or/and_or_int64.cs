// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public class Program
{
    private static int s_RetCode = 100;

    public static int Main()
    {
        long[] testData = new long[] { long.MinValue, long.MinValue + 1, -1, 0, 1, 2, 1000, long.MaxValue - 1, long.MaxValue };
        for (long i = 0; i < testData.Length; i++)
        {
            for (long j = 0; j < testData.Length; j++)
            {
                long test1Value = testData[i];
                long test1Arg = testData[j];
                long ret1Value = RefImpl.XAnd32(ref test1Value, test1Arg);

                long test2Value = testData[i];
                long test2Arg = testData[j];
                long ret2Value = InterlockedImpl.XAnd32(ref test2Value, test2Arg);
                AssertEquals(test1Value, test2Value);
                AssertEquals(ret1Value, ret2Value);

                long test3Value = testData[i];
                long test3Arg = testData[j];
                RefImpl.XAnd32_noret(ref test3Value, test3Arg);

                long test4Value = testData[i];
                long test4Arg = testData[j];
                InterlockedImpl.XAnd32_noret(ref test4Value, test4Arg);
                AssertEquals(test3Value, test4Value);
            }

            ThrowsNRE(() =>
            {
                ref long nullref = ref Unsafe.NullRef<long>();
                InterlockedImpl.XAnd32(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref long nullref = ref Unsafe.NullRef<long>();
                InterlockedImpl.XAnd32_noret(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref long nullref = ref Unsafe.NullRef<long>();
                InterlockedImpl.XOr32(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref long nullref = ref Unsafe.NullRef<long>();
                InterlockedImpl.XOr32_noret(ref nullref, testData[i]);
            });
        }


        return s_RetCode;
    }

    static void ThrowsNRE(Action action)
    {
        try
        {
            action();
        }
        catch (NullReferenceException)
        {
            return;
        }

        Console.WriteLine("ERROR: NullReferenceException was expected");
        s_RetCode++;
    }

    static void AssertEquals(long expected, long actual, [CallerLineNumber] long line = 0)
    {
        if (expected != actual)
        {
            Console.WriteLine($"ERROR: {expected} != {actual} (Line:{line})");
            s_RetCode++;
        }
    }
}

class RefImpl
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long XAnd32(ref long a, long b) { long src = a; a &= b; return src; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XAnd32_noret(ref long a, long b) => a &= b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long XOr32(ref long a, long b) { long src = a; a |= b; return src; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XOr32_noret(ref long a, long b) => a |= b;
}

class InterlockedImpl
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long XAnd32(ref long a, long b) => Interlocked.And(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XAnd32_noret(ref long a, long b) => Interlocked.And(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long XOr32(ref long a, long b) => Interlocked.Or(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XOr32_noret(ref long a, long b) => Interlocked.And(ref a, b);
}