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

    [Theory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    public static void SimdOperands(int width)
    {
        s_calls = 0;
        Assert.Throws<NullReferenceException>(() => Concat(null, width));
        Assert.Equal(0, s_calls);

        Assert.Equal(73, Concat(new Receiver(), width));
        Assert.Equal(1, s_calls);
    }

    [Theory]
    [InlineData(true, 7)]
    [InlineData(false, 10)]
    public static void ScalarOperands(bool store, int expected)
    {
        s_calls = 0;
        Assert.Throws<NullReferenceException>(() => Scalar(null, store));
        Assert.Equal(0, s_calls);

        var receiver = new Receiver();
        Assert.Equal(expected, Scalar(receiver, store));
        Assert.Equal(1, s_calls);
    }

    public enum ScalarRewrite
    {
        SubtractNegated,
        AddNegated,
        AddConstants,
        ShiftRight,
        ShiftRightUnsigned,
        RotateLeft,
    }

    [Theory]
    [InlineData(ScalarRewrite.SubtractNegated, -2)]
    [InlineData(ScalarRewrite.AddNegated, -2)]
    [InlineData(ScalarRewrite.AddConstants, 28)]
    [InlineData(ScalarRewrite.ShiftRight, 0)]
    [InlineData(ScalarRewrite.ShiftRightUnsigned, 0)]
    [InlineData(ScalarRewrite.RotateLeft, 6)]
    public static void ScalarRewrites(ScalarRewrite operation, int expected)
    {
        Assert.Throws<NullReferenceException>(() => Rewrite(null, 0, operation));
        Assert.Throws<DivideByZeroException>(() => Rewrite(new int[3], 0, operation));
        Assert.Equal(expected, Rewrite(new int[3], 1, operation));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Rewrite(int[] values, int divisor, ScalarRewrite operation)
    {
        return operation switch
        {
            ScalarRewrite.SubtractNegated => -values.Length - -(1 / divisor),
            ScalarRewrite.AddNegated => -values.Length + (1 / divisor),
            ScalarRewrite.AddConstants => (values.Length + 11) + ((1 / divisor) + 13),
            ScalarRewrite.ShiftRight => ((values.Length >> (1 / divisor)) & 1) == 0 ? 1 : 0,
            ScalarRewrite.ShiftRightUnsigned => ((values.Length >>> (1 / divisor)) & 1) == 0 ? 1 : 0,
            ScalarRewrite.RotateLeft => (values.Length << (1 / divisor)) | (values.Length >>> (32 - (1 / divisor))),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    [Theory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    public static void SimdNegationOperands(int width)
    {
        Assert.Throws<IndexOutOfRangeException>(() => SubtractNegated(Array.Empty<int>(), null, width));
        Assert.Throws<IndexOutOfRangeException>(() => SubtractNegated(Array.Empty<float>(), null, width));
        Assert.Throws<IndexOutOfRangeException>(() => SubtractNegated(Array.Empty<double>(), null, width));
        Assert.Equal(4, SubtractNegated(new[] { 3 }, new[] { 7 }, width));
    }

    private static T SubtractNegated<T>(T[] first, T[] second, int width) where T : unmanaged
    {
        return width switch
        {
            128 => SubtractNegated128(first, second),
            256 => SubtractNegated256(first, second),
            512 => SubtractNegated512(first, second),
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static T SubtractNegated128<T>(T[] first, T[] second) where T : unmanaged
    {
        Vector128<T> result = -Vector128.Create(first[0]) - -Vector128.Create(second[0]);
        return result[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static T SubtractNegated256<T>(T[] first, T[] second) where T : unmanaged
    {
        Vector256<T> result = -Vector256.Create(first[0]) - -Vector256.Create(second[0]);
        return result[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static T SubtractNegated512<T>(T[] first, T[] second) where T : unmanaged
    {
        Vector512<T> result = -Vector512.Create(first[0]) - -Vector512.Create(second[0]);
        return result[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Scalar(Receiver receiver, bool store)
    {
        if (store)
        {
            receiver.Field = receiver.GetValue();
            return receiver.Field;
        }

        return Subtract(receiver, 20);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Subtract(Receiver receiver, int value)
    {
        return value - (receiver.GetValue() + receiver.Field);
    }

    private static int Concat(Receiver receiver, int width)
    {
        return width switch
        {
            128 => Concat128(receiver),
            256 => Concat256(receiver),
            512 => Concat512(receiver),
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat128(Receiver receiver)
    {
        Vector128<int> result = Vector128.ConcatUpperUpper(
            Vector128.Create(receiver.GetValue()), Vector128.Create(receiver.Field));
        return result[0] * 10 + result[Vector128<int>.Count / 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat256(Receiver receiver)
    {
        Vector256<int> result = Vector256.ConcatUpperUpper(
            Vector256.Create(receiver.GetValue()), Vector256.Create(receiver.Field));
        return result[0] * 10 + result[Vector256<int>.Count / 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Concat512(Receiver receiver)
    {
        Vector512<int> result = Vector512.ConcatUpperUpper(
            Vector512.Create(receiver.GetValue()), Vector512.Create(receiver.Field));
        return result[0] * 10 + result[Vector512<int>.Count / 2];
    }
}
