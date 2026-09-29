// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134334
{
    private static int s_calls;

    private sealed class Receiver
    {
        public int Field = 3;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetValue()
        {
            s_calls++;
            return 7;
        }
    }

    [Fact]
    [SkipOnMono("CoreCLR JIT regression test")]
    public static void ConcatUpperUpperPreservesOrder()
    {
        s_calls = 0;
        Assert.Throws<NullReferenceException>(() => Concat128Receiver(null));
        Assert.Throws<NullReferenceException>(() => Concat256Receiver(null));
        Assert.Throws<NullReferenceException>(() => Concat512Receiver(null));
        Assert.Equal(0, s_calls);

        Assert.Equal(73, Concat128Receiver(new Receiver()));
        Assert.Equal(73, Concat256Receiver(new Receiver()));
        Assert.Equal(73, Concat512Receiver(new Receiver()));
        Assert.Equal(3, s_calls);
    }

    [Fact]
    [SkipOnMono("CoreCLR JIT regression test")]
    public static void ShufflePreservesOrder()
    {
        s_calls = 0;
        Assert.Throws<NullReferenceException>(() => Shuffle256Receiver(null));
        Assert.Equal(0, s_calls);

        Assert.Equal(7, Shuffle256Receiver(new Receiver()));
        Assert.Equal(1, s_calls);
    }

    [Fact]
    public static void ShuffleExpandedDuringRationalizationPreservesOrder()
    {
        s_calls = 0;
        Assert.Equal(8, Shuffle256AfterImport(new Receiver()));
        Assert.Equal(1, s_calls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void ConcatUpperUpperPreservesLocalReads(bool useCall, bool writeFirst)
    {
        Vector128<int> result = Concat128WithLocalWrite(Vector128.Create(3), useCall, writeFirst);
        Assert.Equal(Vector128.Create(writeFirst ? 7 : 3, writeFirst ? 7 : 3, 7, 7), result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<int> Concat128WithLocalWrite(Vector128<int> value, bool useCall, bool writeFirst)
    {
        if (useCall)
        {
            if (writeFirst)
            {
                return Vector128.ConcatUpperUpper(Replace(ref value), value);
            }

            return Vector128.ConcatUpperUpper(value, Replace(ref value));
        }

        if (writeFirst)
        {
            return Vector128.ConcatUpperUpper(value = Vector128.Create(7), value);
        }

        return Vector128.ConcatUpperUpper(value, value = Vector128.Create(7));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<int> Replace(ref Vector128<int> value)
    {
        value = Vector128.Create(7);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Shuffle256Receiver(Receiver receiver)
    {
        return Vector256.Shuffle(Vector256.Create(receiver.GetValue()), Vector256.Create(receiver.Field))[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Shuffle256AfterImport(Receiver receiver)
    {
        Vector256<int> result = Vector256.Shuffle(
            Vector256<int>.Indices + Vector256.Create(receiver.GetValue()), Vector256.Create(s_calls));
        return result[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat128Receiver(Receiver receiver)
    {
        Vector128<int> result = Vector128.ConcatUpperUpper(
            Vector128.Create(receiver.GetValue()), Vector128.Create(receiver.Field));
        return result[0] * 10 + result[Vector128<int>.Count / 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat256Receiver(Receiver receiver)
    {
        Vector256<int> result = Vector256.ConcatUpperUpper(
            Vector256.Create(receiver.GetValue()), Vector256.Create(receiver.Field));
        return result[0] * 10 + result[Vector256<int>.Count / 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat512Receiver(Receiver receiver)
    {
        Vector512<int> result = Vector512.ConcatUpperUpper(
            Vector512.Create(receiver.GetValue()), Vector512.Create(receiver.Field));
        return result[0] * 10 + result[Vector512<int>.Count / 2];
    }
}
