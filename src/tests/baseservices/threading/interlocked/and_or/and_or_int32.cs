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
        int[] testData = new int[] { int.MinValue, int.MinValue + 1, -1, 0, 1, 2, 1000, int.MaxValue - 1, int.MaxValue };
        for (int i = 0; i < testData.Length; i++)
        {
            for (int j = 0; j < testData.Length; j++)
            {
                // XAnd
                int test1Value = testData[i];
                int test1Arg = testData[j];
                int ret1Value = RefImpl.XAnd32(ref test1Value, test1Arg);

                int test2Value = testData[i];
                int test2Arg = testData[j];
                int ret2Value = InterlockedImpl.XAnd32(ref test2Value, test2Arg);
                AssertEquals(test1Value, test2Value);
                AssertEquals(ret1Value, ret2Value);

                // XAnd_noret
                int test3Value = testData[i];
                int test3Arg = testData[j];
                RefImpl.XAnd32_noret(ref test3Value, test3Arg);

                int test4Value = testData[i];
                int test4Arg = testData[j];
                InterlockedImpl.XAnd32_noret(ref test4Value, test4Arg);
                AssertEquals(test3Value, test4Value);

                // XOr
                int test5Value = testData[i];
                int test5Arg = testData[j];
                int ret5Value = RefImpl.XOr32(ref test5Value, test5Arg);

                int test6Value = testData[i];
                int test6Arg = testData[j];
                int ret6Value = InterlockedImpl.XOr32(ref test6Value, test6Arg);
                AssertEquals(test5Value, test6Value);
                AssertEquals(ret5Value, ret6Value);

                // XOr_noret
                int test7Value = testData[i];
                int test7Arg = testData[j];
                RefImpl.XOr32_noret(ref test7Value, test7Arg);

                int test8Value = testData[i];
                int test8Arg = testData[j];
                InterlockedImpl.XOr32_noret(ref test8Value, test8Arg);
                AssertEquals(test7Value, test8Value);
            }

            ThrowsNRE(() =>
            {
                ref int nullref = ref Unsafe.NullRef<int>();
                InterlockedImpl.XAnd32(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref int nullref = ref Unsafe.NullRef<int>();
                InterlockedImpl.XAnd32_noret(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref int nullref = ref Unsafe.NullRef<int>();
                InterlockedImpl.XOr32(ref nullref, testData[i]);
            });

            ThrowsNRE(() =>
            {
                ref int nullref = ref Unsafe.NullRef<int>();
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

    static void AssertEquals(int expected, int actual, [CallerLineNumber] int line = 0)
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
    public static int XAnd32(ref int a, int b) { int src = a; a &= b; return src; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XAnd32_noret(ref int a, int b) => a &= b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int XOr32(ref int a, int b) { int src = a; a |= b; return src; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XOr32_noret(ref int a, int b) => a |= b;
}

class InterlockedImpl
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int XAnd32(ref int a, int b) => Interlocked.And(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XAnd32_noret(ref int a, int b) => Interlocked.And(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int XOr32(ref int a, int b) => Interlocked.Or(ref a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XOr32_noret(ref int a, int b) => Interlocked.Or(ref a, b);
}