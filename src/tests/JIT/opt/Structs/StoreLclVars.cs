// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

/// <summary>Checks independent local definitions from multi-register results.</summary>
public class StoreLclVars
{
    // Explicit layout prevents legacy promotion. On platforms with register
    // struct returns, physical promotion can consume the two results directly.
    [StructLayout(LayoutKind.Explicit)]
    private struct Pair
    {
        [FieldOffset(0)] public long First;
        [FieldOffset(8)] public long Second;
        [FieldOffset(0)] public (ulong Quotient, ulong Remainder) Results;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct RefPair
    {
        [FieldOffset(0)] public object First;
        [FieldOffset(0)] public object Alias;
        [FieldOffset(8)] public object Second;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct VectorPair
    {
        [FieldOffset(0)] public Vector128<byte> First;
        [FieldOffset(16)] public Vector128<byte> Second;
        [FieldOffset(0)] public (Vector128<byte>, Vector128<byte>) Results;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int VectorResults(byte* address)
    {
        VectorPair pair = new VectorPair { Results = AdvSimd.Arm64.LoadPairVector128(address) };
        return pair.First.GetElement(0) + pair.First.GetElement(15) +
               pair.Second.GetElement(0) + pair.Second.GetElement(15);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static RefPair MakeRefs(object first, object second) => new RefPair { Alias = first, Second = second };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckRefs(object first, object second)
    {
        RefPair pair = MakeRefs(first, second);
        if (!ReferenceEquals(pair.First, first) || !ReferenceEquals(pair.Second, second))
            return false;
        GC.Collect();
        return ReferenceEquals(pair.First, first) && ReferenceEquals(pair.Second, second);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Pair MakePair(long first, long second) => new Pair { First = first, Second = second };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Pair MakePairOrThrow(long first, long second, bool throws)
    {
        if (throws)
            throw new InvalidOperationException();
        return MakePair(first, second);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CallResults(long first, long second)
    {
        Pair pair = MakePair(first, second);
        return pair.First * 17 + pair.Second + (pair.First ^ pair.Second);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CallOverwrite(long first, long second)
    {
        Pair pair = MakePair(first, second);
        pair.First = 5;
        return pair.First * 17 + pair.Second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Divide(ulong value, ulong divisor)
    {
        Pair result = new Pair { Results = X86Base.X64.DivRem(value, 0, divisor) };
        return (ulong)result.First * 17 + (ulong)result.Second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Remainder(ulong value, ulong divisor)
    {
        var result = X86Base.X64.DivRem(value, 0, divisor);
        return result.Remainder;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DiscardFirst(ulong value, ulong divisor)
    {
        Pair result = new Pair { Results = X86Base.X64.DivRem(value, 0, divisor) };
        return (ulong)(result.Second + (result.First ^ result.First));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong OverwriteResults(ulong value, ulong divisor)
    {
        Pair result = new Pair { Results = X86Base.X64.DivRem(value, 0, divisor) };
        ulong previousQuotient = (ulong)result.First;
        result.Results = X86Base.X64.DivRem((ulong)result.Second + 100, 0, divisor);
        return previousQuotient + (ulong)result.First * 17 + (ulong)result.Second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LoopResults(ulong value, ulong divisor, int count)
    {
        Pair result = new Pair { First = (long)value };
        for (int i = 0; i < count; i++)
            result.Results = X86Base.X64.DivRem((ulong)result.First + (ulong)result.Second, 0, divisor);
        return (ulong)result.First * 17 + (ulong)result.Second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong UnrolledResults(ulong value, ulong divisor)
    {
        Pair result = new Pair { First = (long)value };
        for (int i = 0; i < 4; i++)
            result.Results = X86Base.X64.DivRem((ulong)result.First + (ulong)result.Second, 0, divisor);
        return (ulong)result.First * 17 + (ulong)result.Second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long DiscardCall(bool throws)
    {
        Pair result = MakePairOrThrow(100, 23, throws);
        return (result.First ^ result.First) + (result.Second ^ result.Second);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ExceptionPreservesPrevious(bool throws)
    {
        Pair result = new Pair { First = 13, Second = 7 };
        try
        {
            result = MakePairOrThrow(100, 23, throws);
        }
        catch (InvalidOperationException)
        {
            return result.First * 17 + result.Second + (result.First ^ result.Second);
        }
        return result.First * 17 + result.Second + (result.First ^ result.Second);
    }

    /// <summary>Checks both results, dead results, and exceptional definitions.</summary>
    [Fact]
    public static unsafe void TestEntryPoint()
    {
        Assert.Equal(1838, CallResults(100, 23));
        Assert.Equal(108, CallOverwrite(100, 23));
        Assert.Equal(238, ExceptionPreservesPrevious(true));
        Assert.Equal(1838, ExceptionPreservesPrevious(false));
        Assert.True(CheckRefs(new object(), new object()));
        Assert.Equal(0, DiscardCall(false));
        Assert.Throws<InvalidOperationException>(() => DiscardCall(true));
        if (AdvSimd.Arm64.IsSupported)
        {
            byte* values = stackalloc byte[32];
            for (int i = 0; i < 32; i++)
                values[i] = (byte)i;
            Assert.Equal(62, VectorResults(values));
        }
        if (X86Base.X64.IsSupported)
        {
            Assert.Equal(1700ul, LoopResults(100, 17, 0));
            ulong first = 123456789;
            ulong second = 0;
            for (int i = 0; i < 5; i++)
            {
                if (i == 4)
                    Assert.Equal(first * 17 + second, UnrolledResults(123456789, 17));
                ulong sum = first + second;
                first = sum / 17;
                second = sum % 17;
            }
            Assert.Equal(first * 17 + second, LoopResults(123456789, 17, 5));
            foreach (ulong value in new ulong[] { 0, 1, 17, 123456789, ulong.MaxValue })
            {
                foreach (ulong divisor in new ulong[] { 1, 3, 17, 65537, ulong.MaxValue })
                {
                    ulong expected = unchecked(value / divisor * 17 + value % divisor);
                    Assert.Equal(expected, Divide(value, divisor));
                    Assert.Equal(value % divisor, Remainder(value, divisor));
                    Assert.Equal(value % divisor, DiscardFirst(value, divisor));
                    ulong next = unchecked(value % divisor + 100);
                    Assert.Equal(unchecked(value / divisor + next / divisor * 17 + next % divisor),
                                 OverwriteResults(value, divisor));
                }
            }
        }
    }
}
