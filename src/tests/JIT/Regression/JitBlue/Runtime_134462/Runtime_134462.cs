// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;

public class Runtime_134462
{
    private static int s_beforeDereference;
    private static int s_afterDereference;
    private static int s_ehAfterDereference;
    private static int s_ehCatch;
    private static int s_ehFinally;

    [Xunit.Fact]
    public static void TestEntryPoint()
    {
        TestIssueReproduction();
        TestPairedTenMerge();
        TestNullableInitialAndMergedValues();
        TestSameIncomingValue();
        TestLoopCarriedValues();
        TestSideEffects();
        TestEhNullPath();
        TestIndependentQueries();
    }


    private static void TestIssueReproduction()
    {
        for (int pattern = 0; pattern < 1024; pattern++)
        {
            var values = new string?[10];
            for (int i = 0; i < values.Length; i++)
            {
                if ((pattern & (1 << i)) != 0) values[i] = new string('x', i + 1);
            }
            for (int mask = 0; mask < 64; mask++)
            {
                int expected = 3;
                if (mask == 63)
                {
                    for (int i = 0; i < values.Length; i++)
                        if (values[i] != null) expected = i + 1;
                }
                Check(IssueReproduction(values, mask) == expected, "issue reproduction");
            }
        }
        ExpectNullReference(() => { _ = IssueReproduction(null, 63); }, "null input array");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static int IssueReproduction(string?[]? a,int m) {
    string x="abc";
    if(a![0] is { } t0 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t0;
    if(a![1] is { } t1 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t1;
    if(a![2] is { } t2 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t2;
    if(a![3] is { } t3 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t3;
    if(a![4] is { } t4 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t4;
    if(a![5] is { } t5 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t5;
    if(a![6] is { } t6 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t6;
    if(a![7] is { } t7 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t7;
    if(a![8] is { } t8 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t8;
    if(a![9] is { } t9 && (m&1)>0 && (m&2)>0 && (m&4)>0 && (m&8)>0 && (m&16)>0 && (m&32)>0) x=t9;
    return x.Length;
    }

    private static void TestPairedTenMerge()
    {
        int[] shortArray = new int[3];
        int[] longArray  = new int[17];
        int[] emptyArray = Array.Empty<int>();

        (int[]? Left, int[]? Right)[] patterns =
        {
            (shortArray, longArray),
            (longArray, shortArray),
            (shortArray, shortArray),
            (emptyArray, longArray),
            (null, longArray),
            (shortArray, null),
            (null, null),
        };

        for (int mask = 0; mask < 64; mask++)
        {
            foreach ((int[]? left, int[]? right) in patterns)
            {
                int expected = ExpectedTenMerge(mask, left, right);
                int actual   = PairedTenMerge(mask, left, right);
                Check(actual == expected, $"ten-merge mask={mask}, expected={expected}, actual={actual}");
            }
        }
    }

    // Ten unrolled merge stages retain a small IR graph while creating repeated reaching-VN paths.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int PairedTenMerge(int mask, int[]? initialLeft, int[]? initialRight)
    {
        if (initialLeft is null)
        {
            return -1001;
        }

        if (initialRight is null)
        {
            return -1002;
        }

        int[]? left0  = initialLeft;
        int[]? right0 = initialRight;

        int[]? left1  = (mask & 1) != 0 ? right0 : left0;
        int[]? right1 = (mask & 1) != 0 ? left0 : right0;
        int[]? left2  = (mask & 2) != 0 ? right1 : left1;
        int[]? right2 = (mask & 2) != 0 ? left1 : right1;
        int[]? left3  = (mask & 4) != 0 ? right2 : left2;
        int[]? right3 = (mask & 4) != 0 ? left2 : right2;
        int[]? left4  = (mask & 8) != 0 ? right3 : left3;
        int[]? right4 = (mask & 8) != 0 ? left3 : right3;
        int[]? left5  = (mask & 16) != 0 ? right4 : left4;
        int[]? right5 = (mask & 16) != 0 ? left4 : right4;
        int[]? left6  = (mask & 32) != 0 ? right5 : left5;
        int[]? right6 = (mask & 32) != 0 ? left5 : right5;
        int[]? left7  = (mask & 1) != 0 ? right6 : left6;
        int[]? right7 = (mask & 1) != 0 ? left6 : right6;
        int[]? left8  = (mask & 4) != 0 ? right7 : left7;
        int[]? right8 = (mask & 4) != 0 ? left7 : right7;
        int[]? left9  = (mask & 16) != 0 ? right8 : left8;
        int[]? right9 = (mask & 16) != 0 ? left8 : right8;
        int[]? left10 = (mask & 32) != 0 ? right9 : left9;

        return left10!.Length;
    }

    private static int ExpectedTenMerge(int mask, int[]? initialLeft, int[]? initialRight)
    {
        if (initialLeft is null)
        {
            return -1001;
        }

        if (initialRight is null)
        {
            return -1002;
        }

        int[] left  = initialLeft;
        int[] right = initialRight;
        int[] selectorBits = { 1, 2, 4, 8, 16, 32, 1, 4, 16, 32 };

        foreach (int selectorBit in selectorBits)
        {
            if ((mask & selectorBit) != 0)
            {
                int[] temporary = left;
                left            = right;
                right           = temporary;
            }
        }

        return left.Length;
    }

    private static void TestNullableInitialAndMergedValues()
    {
        int[] three = new int[3];
        int[] five  = new int[5];

        Check(NullableInitialOrMerged(false, three, null) == 3, "non-null initial value");
        Check(NullableInitialOrMerged(true, null, five) == 5, "non-null merged value");
        ExpectNullReference(() => { _ = NullableInitialOrMerged(false, null, five); }, "null initial value");
        ExpectNullReference(() => { _ = NullableInitialOrMerged(true, three, null); }, "null merged value");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NullableInitialOrMerged(bool useMerged, int[]? initial, int[]? merged)
    {
        int[]? value = initial;
        if (useMerged)
        {
            value = merged;
        }

        return value!.Length;
    }

    private static void TestSameIncomingValue()
    {
        int[] value = new int[9];
        Check(SameIncomingValue(true, value) == 9, "same incoming value, first path");
        Check(SameIncomingValue(false, value) == 9, "same incoming value, second path");
        Check(SameIncomingValue(true, null) == -101, "same null value, first path");
        Check(SameIncomingValue(false, null) == -102, "same null value, second path");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SameIncomingValue(bool firstPath, int[]? incoming)
    {
        int[] value;
        if (firstPath)
        {
            if (incoming is null)
            {
                return -101;
            }

            value = incoming;
        }
        else
        {
            if (incoming is null)
            {
                return -102;
            }

            value = incoming;
        }

        return value.Length;
    }

    private static void TestLoopCarriedValues()
    {
        int[] four = new int[4];
        int[] six  = new int[6];

        Check(LoopCarriedValue(0, four, null) == 4, "zero-iteration loop");
        Check(LoopCarriedValue(1, null, six) == 6, "loop replaces nullable initial value");
        Check(LoopCarriedValue(3, four, six) == 6, "loop-carried non-null value");
        ExpectNullReference(() => { _ = LoopCarriedValue(0, null, six); }, "zero-iteration null value");
        ExpectNullReference(() => { _ = LoopCarriedValue(1, four, null); }, "loop-carried null value");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoopCarriedValue(int iterations, int[]? initial, int[]? next)
    {
        int[]? value = initial;
        for (int i = 0; i < iterations; i++)
        {
            value = ((i & 1) == 0) ? next : value;
        }

        return value!.Length;
    }

    private static void TestSideEffects()
    {
        int[] value = new int[8];

        s_beforeDereference = 0;
        s_afterDereference  = 0;
        Check(DereferenceWithSideEffects(false, value, null) == 8, "side-effect non-null result");
        Check(s_beforeDereference == 1 && s_afterDereference == 1, "side effects around successful dereference");

        s_beforeDereference = 0;
        s_afterDereference  = 0;
        ExpectNullReference(
            () => { _ = DereferenceWithSideEffects(true, value, null); }, "side effects around null dereference");
        Check(s_beforeDereference == 1 && s_afterDereference == 0, "side-effect ordering on null path");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DereferenceWithSideEffects(bool chooseRight, int[]? left, int[]? right)
    {
        int[]? value = chooseRight ? right : left;
        s_beforeDereference++;
        int length = value!.Length;
        s_afterDereference++;
        return length;
    }

    private static void TestEhNullPath()
    {
        int[] value = new int[12];

        s_ehAfterDereference = 0;
        s_ehCatch            = 0;
        s_ehFinally          = 0;
        Check(EhNullPath(false, value) == 12, "EH non-null result");
        Check(s_ehAfterDereference == 1 && s_ehCatch == 0 && s_ehFinally == 1, "EH non-null effects");

        s_ehAfterDereference = 0;
        s_ehCatch            = 0;
        s_ehFinally          = 0;
        Check(EhNullPath(true, value) == -301, "EH null result");
        Check(s_ehAfterDereference == 0 && s_ehCatch == 1 && s_ehFinally == 1, "EH null effects");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int EhNullPath(bool chooseNull, int[] nonNull)
    {
        try
        {
            int[]? value = chooseNull ? null : nonNull;
            int length = value!.Length;
            s_ehAfterDereference++;
            return length;
        }
        catch (NullReferenceException)
        {
            s_ehCatch++;
            return -301;
        }
        finally
        {
            s_ehFinally++;
        }
    }

    private static void TestIndependentQueries()
    {
        int[]?[] values = new int[]?[12];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new int[i + 1];
        }

        Check(IndependentQueries(0, values) == 36, "independent even queries");
        Check(IndependentQueries(63, values) == 42, "independent odd queries");
        Check(IndependentQueries(0, values) == 36, "repeated independent queries");

        values[11] = null;
        ExpectNullReference(() => { _ = IndependentQueries(63, values); }, "late independent null query");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IndependentQueries(int mask, int[]?[] values)
    {
        int[]? q0 = (mask & 1) != 0 ? values[1] : values[0];
        int[]? q1 = (mask & 2) != 0 ? values[3] : values[2];
        int[]? q2 = (mask & 4) != 0 ? values[5] : values[4];
        int[]? q3 = (mask & 8) != 0 ? values[7] : values[6];
        int[]? q4 = (mask & 16) != 0 ? values[9] : values[8];
        int[]? q5 = (mask & 32) != 0 ? values[11] : values[10];

        return q0!.Length + q1!.Length + q2!.Length + q3!.Length + q4!.Length + q5!.Length;
    }

    private static void ExpectNullReference(Action action, string name)
    {
        try
        {
            action();
        }
        catch (NullReferenceException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new Exception($"{name}: unexpected exception {exception.GetType().FullName}", exception);
        }

        throw new Exception($"{name}: expected NullReferenceException");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
