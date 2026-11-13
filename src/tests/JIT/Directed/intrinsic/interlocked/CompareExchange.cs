// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

namespace JitTest_Directed_intrinsic_interlocked_compareexchange;

public class CompareExchangeTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Exchange(ref int location, int value, int comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Exchange(ref long location, long value, long comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Exchange(ref uint location, uint value, uint comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Exchange(ref ulong location, ulong value, ulong comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ExchangeZero(ref int location, int value) => Interlocked.CompareExchange(ref location, value, 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ExchangeZero(ref long location, long value) => Interlocked.CompareExchange(ref location, value, 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Clear(ref int location, int comparand) => Interlocked.CompareExchange(ref location, 0, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Clear(ref long location, long comparand) => Interlocked.CompareExchange(ref location, 0, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadZero(ref int location) => Interlocked.CompareExchange(ref location, 0, 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadZero(ref long location) => Interlocked.CompareExchange(ref location, 0, 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SameOperand(ref int location, int value) => Interlocked.CompareExchange(ref location, value, value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SameOperand(ref long location, long value) => Interlocked.CompareExchange(ref location, value, value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Discard(ref int location, int value, int comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Discard(ref long location, long value, long comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int KeepLive(ref int location, int value, int comparand)
    {
        int original = Interlocked.CompareExchange(ref location, value, comparand);
        return original ^ location ^ value ^ comparand;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long KeepLive(ref long location, long value, long comparand)
    {
        long original = Interlocked.CompareExchange(ref location, value, comparand);
        return original ^ location ^ value ^ comparand;
    }

    [Fact]
    public static void IntegralValues()
    {
        int[] values32 = { 0, 1, -1, int.MinValue, int.MaxValue, unchecked((int)0xA5A55A5A) };
        long[] values64 = { 0, 1, -1, long.MinValue, long.MaxValue, 0x1234567887654321, unchecked((long)0xA5A55A5AF0F00F0F) };

        foreach (int original in values32)
        {
            foreach (int value in values32)
            {
                foreach (int comparand in values32)
                {
                    int expected = original == comparand ? value : original;
                    int location = original;
                    Assert.Equal(original, Exchange(ref location, value, comparand));
                    Assert.Equal(expected, location);

                    uint unsignedLocation = unchecked((uint)original);
                    Assert.Equal(unchecked((uint)original), Exchange(ref unsignedLocation, unchecked((uint)value), unchecked((uint)comparand)));
                    Assert.Equal(unchecked((uint)expected), unsignedLocation);

                    location = original;
                    Discard(ref location, value, comparand);
                    Assert.Equal(expected, location);

                    location = original;
                    Assert.Equal(original ^ expected ^ value ^ comparand, KeepLive(ref location, value, comparand));
                    Assert.Equal(expected, location);
                }
            }

            int zeroLocation = original;
            Assert.Equal(original, ReadZero(ref zeroLocation));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, SameOperand(ref zeroLocation, original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, SameOperand(ref zeroLocation, ~original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, Clear(ref zeroLocation, ~original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, Clear(ref zeroLocation, original));
            Assert.Equal(0, zeroLocation);
            Assert.Equal(0, ExchangeZero(ref zeroLocation, original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, ExchangeZero(ref zeroLocation, 1));
            Assert.Equal(original == 0 ? 1 : original, zeroLocation);
        }

        foreach (long original in values64)
        {
            foreach (long value in values64)
            {
                foreach (long comparand in values64)
                {
                    long expected = original == comparand ? value : original;
                    long location = original;
                    Assert.Equal(original, Exchange(ref location, value, comparand));
                    Assert.Equal(expected, location);

                    ulong unsignedLocation = unchecked((ulong)original);
                    Assert.Equal(unchecked((ulong)original), Exchange(ref unsignedLocation, unchecked((ulong)value), unchecked((ulong)comparand)));
                    Assert.Equal(unchecked((ulong)expected), unsignedLocation);

                    location = original;
                    Discard(ref location, value, comparand);
                    Assert.Equal(expected, location);

                    location = original;
                    Assert.Equal(original ^ expected ^ value ^ comparand, KeepLive(ref location, value, comparand));
                    Assert.Equal(expected, location);
                }
            }

            long zeroLocation = original;
            Assert.Equal(original, ReadZero(ref zeroLocation));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, SameOperand(ref zeroLocation, original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, SameOperand(ref zeroLocation, ~original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, Clear(ref zeroLocation, ~original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, Clear(ref zeroLocation, original));
            Assert.Equal(0L, zeroLocation);
            Assert.Equal(0L, ExchangeZero(ref zeroLocation, original));
            Assert.Equal(original, zeroLocation);
            Assert.Equal(original, ExchangeZero(ref zeroLocation, 1));
            Assert.Equal(original == 0 ? 1 : original, zeroLocation);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Exchange(ref float location, float value, float comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double Exchange(ref double location, double value, double comparand) => Interlocked.CompareExchange(ref location, value, comparand);

    [Fact]
    public static void FloatingPointBits()
    {
        int[] bits32 = { 0, int.MinValue, 0x7FC00001, 0x7FC00002 };
        long[] bits64 = { 0, long.MinValue, 0x7FF8000000000001, 0x7FF8000000000002 };
        foreach (int original in bits32)
        {
            foreach (int comparand in bits32)
            {
                float location = BitConverter.Int32BitsToSingle(original);
                float result = Exchange(ref location, 1.0f, BitConverter.Int32BitsToSingle(comparand));
                Assert.Equal(original, BitConverter.SingleToInt32Bits(result));
                Assert.Equal(original == comparand ? BitConverter.SingleToInt32Bits(1.0f) : original, BitConverter.SingleToInt32Bits(location));
            }
        }
        foreach (long original in bits64)
        {
            foreach (long comparand in bits64)
            {
                double location = BitConverter.Int64BitsToDouble(original);
                double result = Exchange(ref location, 1.0, BitConverter.Int64BitsToDouble(comparand));
                Assert.Equal(original, BitConverter.DoubleToInt64Bits(result));
                Assert.Equal(original == comparand ? BitConverter.DoubleToInt64Bits(1.0) : original, BitConverter.DoubleToInt64Bits(location));
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Clear(ref object location, object comparand) => Interlocked.CompareExchange(ref location, null, comparand);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ClearAndKeepLive(ref object location, object comparand)
    {
        object original = Interlocked.CompareExchange(ref location, null, comparand);
        return original == comparand && location is null;
    }

    [Fact]
    public static void ReferenceValues()
    {
        var original = new object();
        object[] locations = { original };
        Assert.Same(original, Clear(ref locations[0], new object()));
        Assert.Same(original, locations[0]);
        Assert.Same(original, Clear(ref locations[0], original));
        Assert.Null(locations[0]);
        Assert.Null(Clear(ref locations[0], null));
        Assert.Null(locations[0]);

        locations[0] = original;
        Assert.False(ClearAndKeepLive(ref locations[0], new object()));
        Assert.Same(original, locations[0]);
        Assert.True(ClearAndKeepLive(ref locations[0], original));
        Assert.Null(locations[0]);
        Assert.True(ClearAndKeepLive(ref locations[0], null));
        Assert.Null(locations[0]);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void Contention()
    {
        const int Iterations = 2000;
        int counter32 = 0;
        long counter64 = 0;
        using var start = new ManualResetEventSlim();
        var threads = new Thread[4];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                start.Wait();
                for (int j = 0; j < Iterations; j++)
                {
                    int old32;
                    do
                    {
                        old32 = Volatile.Read(ref counter32);
                    }
                    while (Exchange(ref counter32, old32 + 1, old32) != old32);

                    long old64;
                    do
                    {
                        old64 = Volatile.Read(ref counter64);
                    }
                    while (Exchange(ref counter64, old64 + 1, old64) != old64);
                }
            });
            threads[i].Start();
        }
        start.Set();
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Assert.Equal(threads.Length * Iterations, counter32);
        Assert.Equal((long)threads.Length * Iterations, counter64);
    }
}
