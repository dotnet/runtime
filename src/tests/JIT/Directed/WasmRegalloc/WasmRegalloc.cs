// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class WasmRegalloc
{
    private sealed class Box
    {
        public Box Next;
        public int A;
        public int B;
    }

    private struct SingleFloat
    {
        public float Value;
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 4)]
    [InlineData(10, 8)]
    [InlineData(-3, 0)]
    [InlineData(0, 0)]
    public static void ConditionalBackedge(int n, int expected)
    {
        if (n == 0)
        {
            Assert.Throws<DivideByZeroException>(() => ConditionalBackedgeMultiUse(n));
        }
        else
        {
            Assert.Equal(expected, ConditionalBackedgeMultiUse(n));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConditionalBackedgeMultiUse(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += i;
        }

        int result = sum / n;
        return result + result;
    }

    [Theory]
    [InlineData(4, 7, 28)]
    [InlineData(-4, -7, 28)]
    [InlineData(-4, 7, int.MaxValue)]
    [InlineData(int.MaxValue, 1, int.MaxValue)]
    [InlineData(46341, 46341, int.MaxValue)]
    [InlineData(int.MinValue, -1, int.MaxValue)]
    public static void CheckedConversion(int a, int b, int expected)
    {
        Assert.Equal(expected, EHNonEH(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int EHNonEH(int a, int b)
    {
        uint value;
        try
        {
            value = checked((uint)(a * b));
        }
        catch (OverflowException)
        {
            return int.MaxValue;
        }

        return (int)value;
    }

    [Theory]
    [InlineData(0, false, 23)]
    [InlineData(1, false, 40)]
    [InlineData(2, false, 57)]
    [InlineData(1, true, 34)]
    public static void ContainedTree(int index, bool alias, int expected)
    {
        Box first = new Box { A = 17, B = -1 };
        Box second = alias ? first : new Box { A = 23, B = -2 };
        first.Next = second;
        Box[] boxes = { first, first, first };
        Assert.Equal(expected, CandidateInContainedTree(boxes, index));
        Assert.Equal(expected, second.B);
        Assert.Equal(17, first.A);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CandidateInContainedTree(Box[] boxes, int k)
    {
        Box p = boxes[k];
        Box q = p.Next;
        int a = p.A;
        q.B = a * k + q.A;
        return q.B;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 50)]
    [InlineData(-7, 98)]
    public static void ContainedStore(int seed, int expected)
    {
        Box sentinel = new Box { B = -1 };
        Box destination = new Box { B = -2 };
        StoreCandidate(new Box[] { sentinel, destination }, 1, seed);
        Assert.Equal(expected, destination.B);
        Assert.Equal(-1, sentinel.B);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreCandidate(Box[] boxes, int index, int seed)
    {
        Box destination = boxes[index];
        // The CSE definition on the RHS must not reuse the still-needed store base.
        destination.B = checked(seed * seed + seed * seed);
    }

    [Theory]
    [InlineData(41, 5, 8)]
    [InlineData(-41, 5, -8)]
    [InlineData(int.MinValue, 1, int.MinValue)]
    [InlineData(int.MinValue, -1, 0)]
    [InlineData(1, 0, 0)]
    public static void SignedDivision(int numerator, int denominator, int quotient)
    {
        if (denominator == 0)
        {
            Assert.Throws<DivideByZeroException>(() => DivideCandidate(numerator, denominator));
        }
        else if (denominator == -1 && numerator == int.MinValue)
        {
            Assert.Throws<OverflowException>(() => DivideCandidate(numerator, denominator));
        }
        else
        {
            Assert.Equal(quotient, DivideCandidate(numerator, denominator));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DivideCandidate(int numerator, int denominator)
    {
        int divisor = Opaque(denominator);
        int dividend = Opaque(numerator);
        return dividend / divisor;
    }

    [Theory]
    [InlineData(11, 101L, 1.5f, 2.25, 262.75)]
    [InlineData(-5, -11L, -1.5f, 0.25, 1.75)]
    public static void ABIHomeRetention(int i, long l, float f, double d, double expected)
    {
        int home = 7;
        Box box = new Box { A = 3, B = 5 };
        Assert.Equal(expected, RetainABIHomes(i, l, f, d, box, ref home));
        Assert.Equal(12, home);
        Assert.Equal(f + i, RetainPromotedHome(new SingleFloat { Value = f }, i));
        Assert.Equal(2 * (i + 13), AddressExposedHome(i));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double RetainABIHomes(int i, long l, float f, double d, Box box, ref int home)
    {
        i += box.A;
        l += home;
        f += i;
        d += l;
        home += box.B;
        return Opaque(i) + l + f + d + home + box.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float RetainPromotedHome(SingleFloat value, int delta)
    {
        return value.Value + Opaque(delta);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AddressExposedHome(int argument)
    {
        Mutate(ref argument);
        return argument + Opaque(argument);
    }

    [Theory]
    [InlineData(0, 25004)]
    [InlineData(1, 134124)]
    [InlineData(2, 29145)]
    [InlineData(3, 53034)]
    public static void FuncletHomes(int scenario, int expected)
    {
        Assert.Equal(expected, MultipleFunclets(scenario));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MultipleFunclets(int scenario)
    {
        int shared = Opaque(17);
        int trace = 0;
        try
        {
            try
            {
                ThrowForScenario(scenario);
                shared += Opaque(3);
            }
            catch (DivideByZeroException) when (CheckFilter(scenario, ref trace))
            {
                int a = Opaque(11);
                long b = Opaque(101L);
                shared += a + (int)b;
                trace = trace * 10 + 2;
            }
            catch (OverflowException)
            {
                float c = Opaque(13.5f);
                double d = Opaque(17.5);
                shared += (int)(c + d);
                trace = trace * 10 + 3;
            }
            finally
            {
                shared += Opaque(5);
                trace = trace * 10 + 4;
            }
        }
        catch (DivideByZeroException)
        {
            shared += Opaque(7);
            trace = trace * 10 + 5;
        }

        return shared * 1000 + trace;
    }

    [Theory]
    [InlineData(3, 2, 45, 69)]
    [InlineData(-5, 7, 29, 34)]
    [InlineData(11, 32, 61, 139)]
    public static void FrameHomes(int seed, int length, int fixedExpected, int dynamicExpected)
    {
        Assert.Equal(fixedExpected, FixedFrame(seed));
        Assert.Equal(dynamicExpected, DynamicFrame(seed, length));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FixedFrame(int seed)
    {
        int home = seed;
        Mutate(ref home);
        int saved = Opaque(home);
        Mutate(ref home);
        return saved + home;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DynamicFrame(int seed, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 32);
        int home = seed;
        Mutate(ref home);
        Span<int> buffer = stackalloc int[length];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = home + i;
        }

        int saved = Opaque(home);
        buffer[0] += Opaque(seed + 1);
        return saved + buffer[0] + buffer[length - 1] + home;
    }

    [Theory]
    [InlineData(0, 2, 69364)]
    [InlineData(0, 32, 69364)]
    [InlineData(1, 2, 190484)]
    [InlineData(1, 7, 190484)]
    [InlineData(2, 7, 215584)]
    [InlineData(3, 2, 91705)]
    [InlineData(3, 32, 91705)]
    public static void DynamicFuncletHomes(int scenario, int length, int expected)
    {
        Assert.Equal(expected, MultipleFuncletsLocalloc(scenario, length));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MultipleFuncletsLocalloc(int scenario, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 32);
        Span<int> buffer = stackalloc int[length];
        buffer[0] = 11;
        buffer[length - 1] = 23;
        int shared = Opaque(19);
        int trace = 0;
        try
        {
            try
            {
                buffer[0] += Opaque(scenario);
                ThrowForScenario(scenario);
                shared += buffer[length - 1];
            }
            catch (DivideByZeroException)
            {
                int a = Opaque(buffer[0]);
                int b = Opaque(shared);
                shared = a * a + b;
                trace = 2;
            }
            finally
            {
                buffer[length - 1] += Opaque(3);
                shared += buffer[length - 1];
                trace = trace * 10 + 4;
            }
        }
        catch (OverflowException)
        {
            long value = Opaque((long)shared);
            shared = (int)(value * 2);
            trace = trace * 10 + 5;
        }

        return shared * 1000 + buffer[0] * 100 + buffer[length - 1] * 10 + trace;
    }

    [Theory]
    [InlineData(3, 103, 72, 115, -53, 261.5)]
    [InlineData(-5, 671, -72, -5, -77, 197.5)]
    public static void IntervalBoundaries(
        int seed, int disjointExpected, int overlapExpected, int firstExpected, int secondExpected, double banksExpected)
    {
        Assert.Equal(disjointExpected, DisjointIntervals(seed));
        Assert.Equal(overlapExpected, OverlappingIntervals(seed));
        Assert.Equal(firstExpected, BranchIntervals(seed, true));
        Assert.Equal(secondExpected, BranchIntervals(seed, false));
        Assert.Equal(banksExpected, TypedIntervals(seed));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DisjointIntervals(int seed)
    {
        int a = Opaque(seed);
        int b = a * a + 1;
        return b * b + Opaque(seed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int OverlappingIntervals(int seed)
    {
        int a = Opaque(seed);
        int b = Opaque(seed + 1);
        int c = a * a + b;
        int d = Opaque(seed + 2);
        return c * d + a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchIntervals(int seed, bool first)
    {
        int a = Opaque(seed);
        int b = first ? Opaque(a + 5) : Opaque(a - 7);
        int c = Opaque(seed + 11);
        return a + b * c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double TypedIntervals(int seed)
    {
        int a = Opaque(seed);
        long b = Opaque(seed + 101L);
        float c = Opaque(seed + 0.5f);
        double d = Opaque(seed + 0.25);
        int a2 = Opaque(a + 7);
        long b2 = Opaque(b + 11);
        float c2 = Opaque(c + 3);
        double d2 = Opaque(d + 13);
        return a + a2 + b + b2 + c + c2 + d + d2;
    }

    [Theory]
    [InlineData(0, 58)]
    [InlineData(1, 173)]
    [InlineData(2, 248)]
    [InlineData(3, 456)]
    [InlineData(4, 757)]
    public static void BackedgeIntervals(int count, int expected)
    {
        Assert.Equal(expected, LoopIntervals(count));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoopIntervals(int count)
    {
        int left = Opaque(3);
        int right = Opaque(7);
        for (int i = 0; i < count; i++)
        {
            int next = Opaque(left + i);
            left = right + next;
            right = next - i;
        }

        return left * 17 + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckFilter(int scenario, ref int trace)
    {
        trace = trace * 10 + 1;
        return (scenario & 1) != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForScenario(int scenario)
    {
        if (scenario is 1 or 2)
        {
            throw new DivideByZeroException();
        }

        if (scenario == 3)
        {
            throw new OverflowException();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(ref int value)
    {
        value += 13;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Opaque(int value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Opaque(long value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Opaque(float value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double Opaque(double value)
    {
        return value;
    }
}
