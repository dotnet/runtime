// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void StoreComputedValueWithExceptionalAddress(bool validDestination)
    {
        Vector128<int> left = Vector128.Create(1, 2, 3, 4);
        Vector128<int> right = Vector128.Create(5, 6, 7, 8);
        Vector128<int>[] destination = validDestination ? new Vector128<int>[1] : Array.Empty<Vector128<int>>();

        if (validDestination)
        {
            StoreComputedVectorToArray(left, right, destination);
            Assert.Equal(left + right, destination[0]);
        }
        else
        {
            Assert.Throws<IndexOutOfRangeException>(() => StoreComputedVectorToArray(left, right, destination));
        }
    }

    [Fact]
    public static void CreateSequenceWithLocalStep()
    {
        Assert.Equal(Vector128.Create(3, 5, 7, 9), CreateSequence([3], 2));
        Assert.Throws<IndexOutOfRangeException>(() => CreateSequence(Array.Empty<int>(), 2));
    }

    [Theory]
    [InlineData(3, 7, 2)]
    [InlineData(-4, 9, -3)]
    public static void CreateScalableSequenceBeforeStepMutation(int start, int replacement, int step)
    {
        int originalStart = start;
        Vector<int> result = CreateSequenceWithMutatingStep(ref start, replacement, step);

        Assert.Equal(replacement, start);
        Assert.Equal(originalStart, result[0]);
        Assert.Equal(originalStart + step, result[1]);
    }

    [Fact]
    public static void StoreValueBeforeAddressMutation()
    {
        Vector128<int> original = Vector128.Create(1, 2, 3, 4);
        Vector128<int> value = original;
        Vector128.StoreUnsafe(value, ref MutateAndGetAddress(ref value));
        Assert.Equal(original, value);
    }

    [Theory]
    [InlineData(Operation.StoreUnsafe)]
    [InlineData(Operation.StoreUnsafeOffset)]
    [InlineData(Operation.StoreAligned)]
    [InlineData(Operation.StoreAlignedNonTemporal)]
    public static void StoreValueNullCheckBeforeOffset(Operation operation)
    {
        Assert.Throws<NullReferenceException>(() => StoreWithSharedObject(null, Array.Empty<nuint>(), operation));
        Assert.Throws<IndexOutOfRangeException>(() => StoreWithSharedObject(new VectorObject(), Array.Empty<nuint>(), operation));
    }

    [Theory]
    [InlineData(Operation.CreateSequence)]
    [InlineData(Operation.MultiplyInt32)]
    public static void ArithmeticValueNullCheckBeforeOffset(Operation operation)
    {
        Assert.Throws<NullReferenceException>(() => ArithmeticWithSharedObject(null, Array.Empty<nuint>(), operation));
        Assert.Throws<IndexOutOfRangeException>(() => ArithmeticWithSharedObject(new VectorObject(), Array.Empty<nuint>(), operation));
    }

    private struct VectorHolder
    {
        public Vector128<int> Value;
    }

    private sealed class VectorObject
    {
        public Vector128<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> ArithmeticWithSharedObject(VectorObject holder, nuint[] offsets, Operation operation) =>
        operation switch
        {
            Operation.CreateSequence => Vector128.CreateSequence(
                Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]).ToScalar(),
                holder.Value.ToScalar()),
            Operation.MultiplyInt32 =>
                Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]).ToScalar() * holder.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void StoreWithSharedObject(VectorObject holder, nuint[] offsets, Operation operation)
    {
        // The store address must not establish that holder is non-null before morphing the value.
        switch (operation)
        {
            case Operation.StoreUnsafe:
                Vector128.StoreUnsafe(
                    Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]),
                    ref Unsafe.As<Vector128<int>, int>(ref holder.Value));
                break;

            case Operation.StoreUnsafeOffset:
                Vector128.StoreUnsafe(
                    Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]),
                    ref Unsafe.As<Vector128<int>, int>(ref holder.Value), 0);
                break;

            case Operation.StoreAligned:
                Vector128.StoreAligned(
                    Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]),
                    (int*)Unsafe.AsPointer(ref holder.Value));
                break;

            case Operation.StoreAlignedNonTemporal:
                Vector128.StoreAlignedNonTemporal(
                    Vector128.LoadUnsafe(ref Unsafe.As<Vector128<int>, int>(ref holder.Value), offsets[0]),
                    (int*)Unsafe.AsPointer(ref holder.Value));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
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
    private static void StoreComputedVectorToArray(
        Vector128<int> left, Vector128<int> right, Vector128<int>[] destination) =>
        Vector128.StoreUnsafe(left + right, ref Unsafe.As<Vector128<int>, int>(ref destination[0]));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref int MutateAndGetAddress(ref Vector128<int> value)
    {
        value = Vector128.Create(5, 6, 7, 8);
        return ref Unsafe.As<Vector128<int>, int>(ref value);
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
    private static Vector128<int> CreateSequence(int[] starts, int step) =>
        Vector128.CreateSequence(starts[0], step);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector<int> CreateSequenceWithMutatingStep(ref int start, int replacement, int step) =>
        Vector.CreateSequence(start, MutateAndReturnStep(ref start, replacement, step));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MutateAndReturnStep(ref int start, int replacement, int step)
    {
        start = replacement;
        return step;
    }

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
