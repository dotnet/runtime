// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

namespace System.Numerics.Tensors.Tests
{
    public class DoubleGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<double> { }
    public class FloatGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<float> { }
    public class HalfGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<Half>
    {
        protected override void AssertEqualTolerance(Half expected, Half actual) => AssertEqualTolerance(expected, actual, Half.CreateTruncating(0.001));
    }
    public class NFloatGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<NFloat> { }

    public class SByteGenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<sbyte> { }
    public class Int16GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<short> { }
    public class Int32GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<int> { }
    public class Int64GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<long> { }
    public class IntPtrGenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<nint> { }
    public class Int128GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<Int128> { }

    public class ByteGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<byte> { }
    public class UInt16GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<ushort> { }
    public class CharGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<char> { }
    public class UInt32GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<uint> { }
    public class UInt64GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<ulong> { }
    public class UIntPtrGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<nuint> { }
    public class UInt128GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<UInt128> { }

    public unsafe abstract class GenericFloatingPointNumberTensorPrimitivesTests<T> : GenericNumberTensorPrimitivesTests<T>
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        protected override T Cosh(T x) => T.Cosh(x);
        protected override void Cosh(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Cosh(x, destination);
        protected override T CosineSimilarity(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => TensorPrimitives.CosineSimilarity(x, y);
        protected override T Distance(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => TensorPrimitives.Distance(x, y);
        protected override void Exp(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Exp(x, destination);
        protected override T Exp(T x) => T.Exp(x);
        protected override T Log(T x) => T.Log(x);
        protected override void Log(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Log(x, destination);
        protected override T Log2(T x) => T.Log2(x);
        protected override void Log2(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Log2(x, destination);
        protected override T Norm(ReadOnlySpan<T> x) => TensorPrimitives.Norm(x);
        protected override void Sigmoid(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Sigmoid(x, destination);
        protected override void Sinh(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Sinh(x, destination);
        protected override T Sinh(T x) => T.Sinh(x);
        protected override void SoftMax(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.SoftMax(x, destination);
        protected override T Sqrt(T x) => T.Sqrt(x);
        protected override void Tanh(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Tanh(x, destination);
        protected override T Tanh(T x) => T.Tanh(x);

        protected override T NaN => T.NaN;

        protected override T NextRandom() => T.CreateTruncating((Random.NextDouble() * 2) - 1); // For testing purposes, get a mix of negative and positive values.

        protected override IEnumerable<T> GetSpecialValues()
        {
            // NaN
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0xFFC0_0000)); // -qNaN / float.NaN
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0xFFFF_FFFF)); // -qNaN / all-bits-set
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x7FC0_0000)); // +qNaN
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0xFFA0_0000)); // -sNaN
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x7FA0_0000)); // +sNaN

            // +Infinity, -Infinity
            yield return T.CreateTruncating(float.PositiveInfinity);
            yield return T.CreateTruncating(float.NegativeInfinity);

            // +Zero, -Zero
            yield return T.Zero;
            yield return T.NegativeZero;

            // Subnormals
            yield return T.Epsilon;
            yield return -T.Epsilon;
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x007F_FFFF));
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x807F_FFFF));

            // Normals
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x0080_0000));
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x8080_0000));
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x7F7F_FFFF)); // MaxValue
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0xFF7F_FFFF)); // MinValue
        }

        protected override void SetSpecialValues(Span<T> x, Span<T> y)
        {
            int pos;

            // NaNs
            pos = Random.Next(x.Length);
            x[pos] = T.NaN;
            y[pos] = T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x7FC0_0000));

            // +Infinity, -Infinity
            pos = Random.Next(x.Length);
            x[pos] = T.PositiveInfinity;
            y[pos] = T.NegativeInfinity;

            // +Zero, -Zero
            pos = Random.Next(x.Length);
            x[pos] = T.Zero;
            y[pos] = T.NegativeZero;

            // +Epsilon, -Epsilon
            pos = Random.Next(x.Length);
            x[pos] = T.Epsilon;
            y[pos] = -T.Epsilon;

            // Same magnitude, opposite sign
            pos = Random.Next(x.Length);
            x[pos] = T.CreateTruncating(5);
            y[pos] = T.CreateTruncating(-5);
        }
    }

    public unsafe abstract class GenericSignedIntegerTensorPrimitivesTests<T> : GenericIntegerTensorPrimitivesTests<T>
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        [Fact]
        public void Abs_MinValue_Throws()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                FillTensor(x.Span, T.MinValue);
                x[^1] = T.MinValue;

                Assert.Throws<OverflowException>(() => Abs(x, destination));
            });
        }

        [Fact]
        public void SumOfMagnitudes_MinValue_Throws()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                FillTensor(x.Span, T.MinValue);
                x[^1] = T.MinValue;

                Assert.Throws<OverflowException>(() => SumOfMagnitudes(x));
            });
        }
    }

    public unsafe abstract class GenericIntegerTensorPrimitivesTests<T> : GenericNumberTensorPrimitivesTests<T>
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        [Fact]
        public void Divide_TwoTensors_ByZero_Throws()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                FillTensor(y.Span, T.Zero);
                y[^1] = T.Zero;

                Exception e = Record.Exception(() => Divide(x, y, destination));
                Assert.True(e is DivideByZeroException or ArgumentOutOfRangeException); // TODO https://github.com/dotnet/runtime/issues/94593: Fix exception type
            });
        }

        [Fact]
        public void Divide_TensorScalar_ByZero_Throw()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Exception e = Record.Exception(() => Divide(x, T.Zero, destination));
                Assert.True(e is DivideByZeroException or ArgumentOutOfRangeException); // TODO https://github.com/dotnet/runtime/issues/94593: Fix exception type
            });
        }
    }

    public unsafe abstract class GenericNumberTensorPrimitivesTests<T> : TensorPrimitivesTests<T>
        where T : unmanaged, INumber<T>, IMinMaxValue<T>
    {
        protected override void Abs(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Abs(x, destination);
        protected override T Abs(T x) => T.Abs(x);
        protected override void Add(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Add(x, y, destination);
        protected override void Add(ReadOnlySpan<T> x, T y, Span<T> destination) => TensorPrimitives.Add(x, y, destination);
        protected override T Add(T x, T y) => x + y;
        protected override void AddMultiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override void AddMultiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override void AddMultiply(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override T AddMultiply(T x, T y, T z) => (x + y) * z;
        protected override void Divide(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Divide(x, y, destination);
        protected override void Divide(ReadOnlySpan<T> x, T y, Span<T> destination) => TensorPrimitives.Divide(x, y, destination);
        protected override T Divide(T x, T y) => x / y;
        protected override T Dot(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => TensorPrimitives.Dot(x, y);
        protected override T Max(ReadOnlySpan<T> x) => TensorPrimitives.Max(x);
        protected override void Max(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Max(x, y, destination);
        protected override T Max(T x, T y) => T.Max(x, y);
        protected override T MaxMagnitude(ReadOnlySpan<T> x) => TensorPrimitives.MaxMagnitude(x);
        protected override void MaxMagnitude(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.MaxMagnitude(x, y, destination);
        protected override T MaxMagnitude(T x, T y) => T.MaxMagnitude(x, y);
        protected override T Min(ReadOnlySpan<T> x) => TensorPrimitives.Min(x);
        protected override void Min(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Min(x, y, destination);
        protected override T Min(T x, T y) => T.Min(x, y);
        protected override T MinMagnitude(ReadOnlySpan<T> x) => TensorPrimitives.MinMagnitude(x);
        protected override void MinMagnitude(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.MinMagnitude(x, y, destination);
        protected override T MinMagnitude(T x, T y) => T.MinMagnitude(x, y);
        protected override void Multiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Multiply(x, y, destination);
        protected override void Multiply(ReadOnlySpan<T> x, T y, Span<T> destination) => TensorPrimitives.Multiply(x, y, destination);
        protected override T Multiply(T x, T y) => x * y;
        protected override void MultiplyAdd(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void MultiplyAdd(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void MultiplyAdd(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void Negate(ReadOnlySpan<T> x, Span<T> destination) => TensorPrimitives.Negate(x, destination);
        protected override T Product(ReadOnlySpan<T> x) => TensorPrimitives.Product(x);
        protected override T ProductOfSums(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => TensorPrimitives.ProductOfSums(x, y);
        protected override T ProductOfDifferences(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => TensorPrimitives.ProductOfDifferences(x, y);
        protected override void Subtract(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination) => TensorPrimitives.Subtract(x, y, destination);
        protected override void Subtract(ReadOnlySpan<T> x, T y, Span<T> destination) => TensorPrimitives.Subtract(x, y, destination);
        protected override T Subtract(T x, T y) => x - y;
        protected override T Sum(ReadOnlySpan<T> x) => TensorPrimitives.Sum(x);
        protected override T SumOfMagnitudes(ReadOnlySpan<T> x) => TensorPrimitives.SumOfMagnitudes(x);
        protected override T SumOfSquares(ReadOnlySpan<T> x) => TensorPrimitives.SumOfSquares(x);

        protected override T ConvertFromFloat(float f) => T.CreateTruncating(f);
        protected override bool IsFloatingPoint => typeof(T) == typeof(Half) || base.IsFloatingPoint;

        protected override T NextRandom()
        {
            T value = default;
            Random.NextBytes(MemoryMarshal.AsBytes(new Span<T>(ref value)));
            return value;
        }

        protected override T NegativeZero => -T.Zero;
        protected override T Zero => T.Zero;
        protected override T One => T.One;
        protected override T NegativeOne => -T.One;
        protected override T MinValue => T.MinValue;

        protected override IEnumerable<(int Length, T Element)> VectorLengthAndIteratedRange(T min, T max, T increment)
        {
            foreach (int length in new[] { 4, 8, 16 })
            {
                for (T f = min; f <= max; f += increment)
                {
                    yield return (length, f);
                }
            }
        }

        protected override void AssertEqualTolerance(T expected, T actual) => AssertEqualTolerance(expected, actual, T.CreateTruncating(0.0001));

        protected override void AssertEqualTolerance(T expected, T actual, T tolerance)
        {
            T diff = T.Abs(expected - actual);
            if (diff > tolerance && diff > T.Max(T.Abs(expected), T.Abs(actual)) * tolerance)
            {
                throw EqualException.ForMismatchedValues(expected, actual);
            }
        }

        protected override T Cosh(T x) => throw new NotSupportedException();
        protected override void Cosh(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T CosineSimilarity(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => throw new NotSupportedException();
        protected override T Distance(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => throw new NotSupportedException();
        protected override void Exp(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Exp(T x) => throw new NotSupportedException();
        protected override T Log(T x) => throw new NotSupportedException();
        protected override void Log(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Log2(T x) => throw new NotSupportedException();
        protected override void Log2(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Norm(ReadOnlySpan<T> x) => throw new NotSupportedException(    );
        protected override void Sigmoid(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override void Sinh(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Sinh(T x) => throw new NotSupportedException();
        protected override void SoftMax(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Sqrt(T x) => throw new NotSupportedException();
        protected override void Tanh(ReadOnlySpan<T> x, Span<T> destination) => throw new NotSupportedException();
        protected override T Tanh(T x) => throw new NotSupportedException();
        protected override T NaN => throw new NotSupportedException();
        protected override IEnumerable<T> GetSpecialValues() => Enumerable.Empty<T>();
        protected override void SetSpecialValues(Span<T> x, Span<T> y) { }
    }
}
