// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace Runtime_133718;

public class Runtime_133718
{
    public enum Operation
    {
        Min,
        Max,
        ShiftRight,
        GetElement,
        WithElementVector,
        WithElementIndex,
        WithElementVector64,
        MultiplyInt32,
        MultiplyInt64Vector64,
        MultiplyInt64Vector128,
        CreateSequence,
        CreateAlternatingSequence,
        StoreUnsafe,
        StoreUnsafeOffset,
        StoreAligned,
        StoreAlignedNonTemporal,
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestEntryPoint(bool nanFirst)
    {
        double[] values = new double[1];
        Assert.Throws<IndexOutOfRangeException>(() => Min(values, values.Length, nanFirst));
    }

    [Theory]
    [InlineData(Operation.Min)]
    [InlineData(Operation.Max)]
    [InlineData(Operation.ShiftRight)]
    [InlineData(Operation.GetElement)]
    [InlineData(Operation.WithElementVector)]
    [InlineData(Operation.WithElementIndex)]
    [InlineData(Operation.WithElementVector64)]
    [InlineData(Operation.MultiplyInt32)]
    [InlineData(Operation.MultiplyInt64Vector64)]
    [InlineData(Operation.MultiplyInt64Vector128)]
    [InlineData(Operation.CreateSequence)]
    [InlineData(Operation.CreateAlternatingSequence)]
    [InlineData(Operation.StoreUnsafe)]
    [InlineData(Operation.StoreUnsafeOffset)]
    [InlineData(Operation.StoreAligned)]
    [InlineData(Operation.StoreAlignedNonTemporal)]
    public static void OperandEvaluationOrder(Operation operation)
    {
        // The earlier operand must throw before a later operand accesses a null array.
        Action action = operation switch
        {
            Operation.Min => () => MinMax(Array.Empty<double>(), null, isMax: false),
            Operation.Max => () => MinMax(Array.Empty<double>(), null, isMax: true),
            Operation.ShiftRight => () => ShiftRight(Array.Empty<Vector128<sbyte>>(), null),
            Operation.GetElement => () => GetElement(Array.Empty<Vector128<int>>(), null),
            Operation.WithElementVector => () => WithElement(Array.Empty<Vector128<int>>(), null, null),
            Operation.WithElementIndex => () => WithElement(new Vector128<int>[1], Array.Empty<int>(), null),
            Operation.WithElementVector64 => () => WithElementVector64(Array.Empty<Vector64<long>>(), null),
            Operation.MultiplyInt32 => () => MultiplyInt32(Array.Empty<int>(), null),
            Operation.MultiplyInt64Vector64 => () => MultiplyInt64Vector64(Array.Empty<long>(), null),
            Operation.MultiplyInt64Vector128 => () => MultiplyInt64Vector128(Array.Empty<long>(), null),
            Operation.CreateSequence => () => CreateSequence(Array.Empty<int>(), null),
            Operation.CreateAlternatingSequence => () => CreateAlternatingSequence(Array.Empty<int>(), null),
            Operation.StoreUnsafe => () => StoreUnsafe(Array.Empty<Vector128<int>>(), null),
            Operation.StoreUnsafeOffset => () => StoreUnsafeOffset(Array.Empty<Vector128<int>>(), null, null),
            Operation.StoreAligned => () => StoreAligned(Array.Empty<Vector128<int>>(), null),
            Operation.StoreAlignedNonTemporal => () => StoreAlignedNonTemporal(Array.Empty<Vector128<int>>(), null),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        Assert.Throws<IndexOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void WithElementValueBeforeRangeCheck(bool upperBound)
    {
        Vector128<int>[] vectors = new Vector128<int>[1];
        int[] indices = [upperBound ? Vector128<int>.Count : -1];

        Assert.Throws<NullReferenceException>(() => WithElement(vectors, indices, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => WithElement(vectors, indices, new int[1]));
    }

    [Fact]
    public static void StoreAddressBeforeOffset()
    {
        Assert.Throws<NullReferenceException>(() =>
            StoreUnsafeOffset(new Vector128<int>[1], null, Array.Empty<nuint>()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void StoreLocalValue(bool field)
    {
        Vector128<int> value = Vector128.Create(1, 2, 3, 4);
        Assert.Equal(value, StoreToLocal(value, field));
    }

    private struct VectorHolder
    {
        public Vector128<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> StoreToLocal(Vector128<int> value, bool field)
    {
        if (field)
        {
            VectorHolder destination = default;
            Vector128.StoreUnsafe(value, ref Unsafe.As<Vector128<int>, int>(ref destination.Value));
            return destination.Value;
        }

        Vector128<int> local = default;
        Vector128.StoreUnsafe(value, ref Unsafe.As<Vector128<int>, int>(ref local));
        return local;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double Min(double[] values, int index, bool nanFirst) =>
        nanFirst
            ? Math.Min(double.NaN, values[index])
            : Math.Min(values[index], double.NaN);

    // Vector operands use direct loads rather than calls, which the importer can spill before constructing the intrinsic.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double MinMax(double[] left, double[] right, bool isMax) =>
        isMax ? Math.Max(left[0], right[0]) : Math.Min(left[0], right[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<sbyte> ShiftRight(Vector128<sbyte>[] vectors, int[] counts) =>
        vectors[0] >> counts[0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GetElement(Vector128<int>[] vectors, int[] indices) =>
        Vector128.GetElement(vectors[0], indices[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> WithElement(Vector128<int>[] vectors, int[] indices, int[] values) =>
        Vector128.WithElement(vectors[0], indices[0], values[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector64<long> WithElementVector64(Vector64<long>[] vectors, long[] values) =>
        Vector64.WithElement(vectors[0], 0, values[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> MultiplyInt32(int[] scalars, Vector128<int>[] vectors) =>
        scalars[0] * vectors[0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector64<long> MultiplyInt64Vector64(long[] scalars, Vector64<long>[] vectors) =>
        scalars[0] * vectors[0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<long> MultiplyInt64Vector128(long[] scalars, Vector128<long>[] vectors) =>
        scalars[0] * vectors[0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> CreateSequence(int[] starts, int[] steps) =>
        Vector128.CreateSequence(starts[0], steps[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> CreateAlternatingSequence(int[] even, int[] odd) =>
        Vector128.CreateAlternatingSequence(even[0], odd[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreUnsafe(Vector128<int>[] vectors, int[] destination) =>
        Vector128.StoreUnsafe(vectors[0], ref destination[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreUnsafeOffset(Vector128<int>[] vectors, int[] destination, nuint[] offsets) =>
        Vector128.StoreUnsafe(vectors[0], ref destination[0], offsets[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void StoreAligned(Vector128<int>[] vectors, nint[] destinations) =>
        Vector128.StoreAligned(vectors[0], (int*)destinations[0]);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void StoreAlignedNonTemporal(Vector128<int>[] vectors, nint[] destinations) =>
        Vector128.StoreAlignedNonTemporal(vectors[0], (int*)destinations[0]);
}
