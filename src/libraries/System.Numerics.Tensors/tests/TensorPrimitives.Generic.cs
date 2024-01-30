// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

// Tests specific to .NET Core generic APIs.
// Some of the tests are written with functionality abstracted into helpers that provide the core operation: this
// is done when the tests are shared with legacy float-specific tests. Tests that don't need to be shared access
// the generic APIs directly.

namespace System.Numerics.Tensors.Tests
{
    public class ConvertTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void ConvertTruncatingAndSaturating()
        {
            MethodInfo convertTruncatingImpl = typeof(ConvertTests).GetMethod(nameof(ConvertTruncatingImpl), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            Assert.NotNull(convertTruncatingImpl);

            MethodInfo convertSaturatingImpl = typeof(ConvertTests).GetMethod(nameof(ConvertSaturatingImpl), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            Assert.NotNull(convertSaturatingImpl);

            Type[] types =
            [
                typeof(sbyte), typeof(byte),
                typeof(short), typeof(ushort), typeof(char),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(nint), typeof(nuint),
                typeof(Half), typeof(float), typeof(double), typeof(NFloat),
                typeof(Int128), typeof(UInt128),
            ];

            foreach (Type from in types)
            {
                foreach (Type to in types)
                {
                    convertTruncatingImpl.MakeGenericMethod(from, to).Invoke(null, null);
                    convertSaturatingImpl.MakeGenericMethod(from, to).Invoke(null, null);
                }
            }
        }

        [Fact]
        public void ConvertChecked()
        {
            // Conversions that never overflow. This isn't an exhaustive list; just a sampling.
            ConvertCheckedImpl<byte, byte>();
            ConvertCheckedImpl<byte, ushort>();
            ConvertCheckedImpl<byte, short>();
            ConvertCheckedImpl<byte, uint>();
            ConvertCheckedImpl<byte, int>();
            ConvertCheckedImpl<byte, ulong>();
            ConvertCheckedImpl<byte, long>();
            ConvertCheckedImpl<byte, float>();
            ConvertCheckedImpl<Half, Half>();
            ConvertCheckedImpl<Half, float>();
            ConvertCheckedImpl<Half, double>();
            ConvertCheckedImpl<float, double>();
            ConvertCheckedImpl<double, float>();

            // Conversions that may overflow. This isn't an exhaustive list; just a sampling.
            ConvertCheckedImpl<float, int>(42f, float.MaxValue);
            ConvertCheckedImpl<long, int>(42, int.MaxValue + 1L);
        }

        private static void ConvertTruncatingImpl<TFrom, TTo>()
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertTruncating<TFrom, TTo>(new TFrom[3], new TTo[2]));

            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                Random rand = new(42);
                Span<TFrom> sourceSpan = source.Span;
                for (int i = 0; i < tensorLength; i++)
                {
                    sourceSpan[i] = TFrom.CreateTruncating(new Int128(
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue),
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue)));
                }

                TensorPrimitives.ConvertTruncating<TFrom, TTo>(source.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    if (!IsEqualWithTolerance(TTo.CreateTruncating(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateTruncating(source.Span[i])}.");
                    }
                }
            };
        }

        private static void ConvertSaturatingImpl<TFrom, TTo>()
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertSaturating<TFrom, TTo>(new TFrom[3], new TTo[2]));

            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                Random rand = new(42);
                Span<TFrom> sourceSpan = source.Span;
                for (int i = 0; i < tensorLength; i++)
                {
                    sourceSpan[i] = TFrom.CreateTruncating(new Int128(
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue),
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue)));
                }

                TensorPrimitives.ConvertSaturating<TFrom, TTo>(source.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    if (!IsEqualWithTolerance(TTo.CreateSaturating(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateSaturating(source.Span[i])}.");
                    }
                }
            };
        }

        private static void ConvertCheckedImpl<TFrom, TTo>()
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertChecked<TFrom, TTo>(new TFrom[3], new TTo[2]));

            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                Random rand = new(42);
                Span<TFrom> sourceSpan = source.Span;
                for (int i = 0; i < tensorLength; i++)
                {
                    sourceSpan[i] = TFrom.CreateTruncating(new Int128(
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue),
                        (ulong)rand.NextInt64(long.MinValue, long.MaxValue)));
                }

                TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    if (!IsEqualWithTolerance(TTo.CreateChecked(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateChecked(source.Span[i])}.");
                    }
                }
            };
        }

        private static void ConvertCheckedImpl<TFrom, TTo>(TFrom valid, TFrom invalid)
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            foreach (int tensorLength in Helpers.TensorLengths)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                // Test with valid
                source.Span.Fill(valid);
                TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span);
                foreach (TTo result in destination.Span)
                {
                    Assert.True(IsEqualWithTolerance(TTo.CreateChecked(valid), result));
                }

                // Test with at least one invalid
                foreach (int invalidPosition in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    source.Span.Fill(valid);
                    source.Span[invalidPosition] = invalid;
                    Assert.Throws<OverflowException>(() => TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span));
                }
            };
        }

        private static bool IsEqualWithTolerance<T>(T expected, T actual, T? tolerance = null) where T : unmanaged, INumber<T>
        {
            tolerance ??= T.CreateTruncating(0.0001);

            T diff = T.Abs(expected - actual);
            if (diff > tolerance && diff > T.Max(T.Abs(expected), T.Abs(actual)) * tolerance)
            {
                return false;
            }

            return true;
        }
    }

    public class DoubleGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<double> { }
    public class SingleGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<float> { }
    public class HalfGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<Half>
    {
        protected override void AssertEqualTolerance(Half expected, Half actual, Half? tolerance = null) =>
            base.AssertEqualTolerance(expected, actual, tolerance ?? Half.CreateTruncating(0.001));
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

        #region Span -> Destination
        public static IEnumerable<object[]> SpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Acosh), new Func<T, T>(T.Acosh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.AcosPi), new Func<T, T>(T.AcosPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Acos), new Func<T, T>(T.Acos) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Asinh), new Func<T, T>(T.Asinh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.AsinPi), new Func<T, T>(T.AsinPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Asin), new Func<T, T>(T.Asin) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Atanh), new Func<T, T>(T.Atanh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.AtanPi), new Func<T, T>(T.AtanPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Atan), new Func<T, T>(T.Atan) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Cbrt), new Func<T, T>(T.Cbrt) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Ceiling), new Func<T, T>(T.Ceiling) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Cos), new Func<T, T>(T.Cos) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Cosh), new Func<T, T>(T.Cosh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.CosPi), new Func<T, T>(T.CosPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.DegreesToRadians), new Func<T, T>(T.DegreesToRadians) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Exp), new Func<T, T>(T.Exp) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Exp2), new Func<T, T>(T.Exp2) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Exp10), new Func<T, T>(T.Exp10) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.ExpM1), new Func<T, T>(T.ExpM1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Exp2M1), new Func<T, T>(T.Exp2M1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Exp10M1), new Func<T, T>(T.Exp10M1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Floor), new Func<T, T>(T.Floor) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Log), new Func<T, T>(T.Log) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Log2), new Func<T, T>(T.Log2) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Log10), new Func<T, T>(T.Log10) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.LogP1), new Func<T, T>(T.LogP1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Log2P1), new Func<T, T>(T.Log2P1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Log10P1), new Func<T, T>(T.Log10P1) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.RadiansToDegrees), new Func<T, T>(T.RadiansToDegrees) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Reciprocal), new Func<T, T>(f => T.One / f) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.ReciprocalEstimate), new Func<T, T>(T.ReciprocalEstimate), T.CreateTruncating(1.171875e-02) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.ReciprocalSqrt), new Func<T, T>(f => T.One / T.Sqrt(f)) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.ReciprocalSqrtEstimate), new Func<T, T>(T.ReciprocalSqrtEstimate), T.CreateTruncating(1.171875e-02) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Round), new Func<T, T>(T.Round) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Sin), new Func<T, T>(T.Sin) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Sinh), new Func<T, T>(T.Sinh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.SinPi), new Func<T, T>(T.SinPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Sqrt), new Func<T, T>(T.Sqrt) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Tan), new Func<T, T>(T.Tan) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Tanh), new Func<T, T>(T.Tanh) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.TanPi), new Func<T, T>(T.TanPi) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.Truncate), new Func<T, T>(T.Truncate) };
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_AllLengths(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_InPlace(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x.Span, x.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i]), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_SpecialValues(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i]), destination[i], tolerance);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ValueRange(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(VectorLengthAndIteratedRange(ConvertFromSingle(-100f), ConvertFromSingle(100f), ConvertFromSingle(3f)), arg =>
            {
                T[] x = new T[arg.Length];
                T[] dest = new T[arg.Length];

                x.AsSpan().Fill(arg.Element);
                tensorPrimitivesMethod(x.AsSpan(), dest.AsSpan());

                T expected = expectedMethod(arg.Element);
                foreach (T actual in dest)
                {
                    AssertEqualTolerance(expected, actual, tolerance);
                }
            });
        }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ThrowsForTooShortDestination(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> _, T? __ = null)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x.Span, destination.Span));
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ThrowsForOverlapppingInputsWithOutputs(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> _, T? __ = null)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
#pragma warning restore xUnit1026
        #endregion

        #region Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Atan2), new Func<T, T, T>(T.Atan2) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Atan2Pi), new Func<T, T, T>(T.Atan2Pi) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.CopySign), new Func<T, T, T>(T.CopySign) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Hypot), new Func<T, T, T>(T.Hypot) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Ieee754Remainder), new Func<T, T, T>(T.Ieee754Remainder) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Log), new Func<T, T, T>(T.Log) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Pow), new Func<T, T, T>(T.Pow) };
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_AllLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_InPlace(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_SpecialValues(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i]);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i]);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x, y, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(y, x, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForTooShortDestination(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForOverlapppingInputsWithOutputs(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(6, 2)));
        }
        #endregion

        #region Span,Scalar -> Destination
        public static IEnumerable<object[]> SpanScalarDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Atan2), new Func<T, T, T>(T.Atan2) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Atan2Pi), new Func<T, T, T>(T.Atan2Pi) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.CopySign), new Func<T, T, T>(T.CopySign) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Ieee754Remainder), new Func<T, T, T>(T.Ieee754Remainder) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Pow), new Func<T, T, T>(T.Pow) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Log), new Func<T, T, T>(T.Log) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Max), new Func<T, T, T>(T.Max) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.MaxMagnitude), new Func<T, T, T>(T.MaxMagnitude) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Min), new Func<T, T, T>(T.Min) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.MinMagnitude), new Func<T, T, T>(T.MinMagnitude) };
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_AllLengths(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_InPlace(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_SpecialValues(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_ThrowsForOverlapppingInputsWithOutputs(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(2, 2)));
        }
        #endregion

        #region Scalar,Span -> Destination
        public static IEnumerable<object[]> ScalarSpanFloatDestinationFunctionsToTest()
        {
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Atan2), new Func<T, T, T>(T.Atan2) };
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Atan2Pi), new Func<T, T, T>(T.Atan2Pi) };
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Pow), new Func<T, T, T>(T.Pow) };
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Ieee754Remainder), new Func<T, T, T>(T.Ieee754Remainder) };
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_AllLengths(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, y[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_InPlace(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] yOrig = y.Span.ToArray();

                tensorPrimitivesMethod(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, yOrig[i]), y[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void ScalarSpanDestination_SpecialValues(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x, y.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x, y[i]), destination[i]);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_ThrowsForTooShortDestination(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_ThrowsForOverlapppingInputsWithOutputs(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Int,Span -> Destination
        public static IEnumerable<object[]> SpanIntDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.RootN), new Func<T, int, T>(T.RootN) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.ScaleB), new Func<T, int, T>(T.ScaleB) };
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_AllLengths(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = Random.Next(1, 10);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_InPlace(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                int y = Random.Next(1, 10);

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_SpecialValues(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = Random.Next(1, 10);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = 2;
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_ThrowsForOverlapppingInputsWithOutputs(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), 2, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), 2, array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanSpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanSpanSpanDestinationDelegate(TensorPrimitives.Lerp), new Func<T, T, T, T>(T.Lerp) };
            yield return new object[] { new SpanSpanSpanDestinationDelegate(TensorPrimitives.FusedMultiplyAdd), new Func<T, T, T, T>(T.FusedMultiplyAdd) };
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_AllLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, z, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_InPlace(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_SpecialValues(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i]);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i]);
                    }
                }, y);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i]);
                    }
                }, z);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x, y, z, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x, z, y, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(y, x, z, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(y, z, x, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(z, x, y, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(z, y, x, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_ThrowsForTooShortDestination(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, z, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_ThrowsForOverlapppingInputsWithOutputs(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }
        #endregion

        #region Span,Span,Scalar -> Destination
        public static IEnumerable<object[]> SpanSpanScalarDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanSpanScalarDestinationDelegate(TensorPrimitives.FusedMultiplyAdd), new Func<T, T, T, T>(T.FusedMultiplyAdd) };
            yield return new object[] { new SpanSpanScalarDestinationDelegate(TensorPrimitives.Lerp), new Func<T, T, T, T>(T.Lerp) };
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_AllLengths(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T z = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, z, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_InPlace(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T z = NextRandom();

                tensorPrimitivesMethod(x, x, z, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i], z), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_SpecialValues(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T z = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i]);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i]);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_ThrowsForTooShortDestination(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T z = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, z, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_ThrowsForOverlapppingInputsWithOutputs(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(4, 2), default, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(4, 2), default, array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(4, 2), default, array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(4, 2), default, array.AsSpan(5, 2)));
        }
        #endregion

        #region Span,Scalar,Span -> Destination
        public static IEnumerable<object[]> SpanScalarSpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanScalarSpanDestinationDelegate(TensorPrimitives.FusedMultiplyAdd), new Func<T, T, T, T>(T.FusedMultiplyAdd) };
            yield return new object[] { new SpanScalarSpanDestinationDelegate(TensorPrimitives.Lerp), new Func<T, T, T, T>(T.Lerp) };
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_AllLengths(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, z, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_InPlace(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                tensorPrimitivesMethod(x, y, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y, xOrig[i]), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_SpecialValues(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i]);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i]);
                    }
                }, z);
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_ThrowsForTooShortDestination(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, z, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_ThrowsForOverlapppingInputsWithOutputs(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Span -> Destination,Destination
        public static IEnumerable<object[]> SpanDestinationDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanDestinationDestinationDelegate(TensorPrimitives.SinCos), new Func<T, (T, T)>(T.SinCos) };
            yield return new object[] { new SpanDestinationDestinationDelegate(TensorPrimitives.SinCosPi), new Func<T, (T, T)>(T.SinCosPi) };
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_AllLengths(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination1 = CreateTensor(tensorLength);
                using BoundedMemory<T> destination2 = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x.Span, destination1.Span, destination2.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    (T expected1, T expected2) = expectedMethod(x[i]);
                    AssertEqualTolerance(expected1, destination1[i]);
                    AssertEqualTolerance(expected2, destination2[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_InPlace(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                using BoundedMemory<T> destination2 = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x.Span, x.Span, destination2.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    (T expected1, T expected2) = expectedMethod(xOrig[i]);
                    AssertEqualTolerance(expected1, x[i]);
                    AssertEqualTolerance(expected2, destination2[i]);
                }
            });

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                using BoundedMemory<T> destination1 = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x.Span, destination1.Span, x.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    (T expected1, T expected2) = expectedMethod(xOrig[i]);
                    AssertEqualTolerance(expected1, destination1[i]);
                    AssertEqualTolerance(expected2, x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_SpecialValues(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination1 = CreateTensor(tensorLength);
                using BoundedMemory<T> destination2 = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, destination1.Span, destination2.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        (T expected1, T expected2) = expectedMethod(x[i]);
                        AssertEqualTolerance(expected1, destination1[i]);
                        AssertEqualTolerance(expected2, destination2[i]);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_ValueRange(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> expectedMethod)
        {
            Assert.All(VectorLengthAndIteratedRange(ConvertFromSingle(-100f), ConvertFromSingle(100f), ConvertFromSingle(3f)), arg =>
            {
                T[] x = new T[arg.Length];
                T[] dest1 = new T[arg.Length];
                T[] dest2 = new T[arg.Length];

                x.AsSpan().Fill(arg.Element);
                tensorPrimitivesMethod(x.AsSpan(), dest1.AsSpan(), dest2.AsSpan());

                (T expected1, T expected2) = expectedMethod(arg.Element);
                foreach (T actual in dest1)
                {
                    AssertEqualTolerance(expected1, actual);
                }
                foreach (T actual in dest2)
                {
                    AssertEqualTolerance(expected2, actual);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_ThrowsForTooShortDestination(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination1 = CreateTensor(tensorLength - 1);
                using BoundedMemory<T> destination2 = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x.Span, destination1.Span, destination2.Span));
            });

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination1 = CreateTensor(tensorLength);
                using BoundedMemory<T> destination2 = CreateTensor(tensorLength - 1);

                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x.Span, destination1.Span, destination2.Span));
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationDestinationFunctionsToTest))]
        public void SpanDestinationDestinationFunctions_ThrowsForOverlapppingInputsWithOutputs(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> _)
        {
            T[] array = new T[10];
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(3, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(5, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
        }
        #endregion

        #region ILogB
        [Fact]
        public void ILogB_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength);

                TensorPrimitives.ILogB<T>(x.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(T.CreateTruncating(T.ILogB(x[i])), T.CreateTruncating(destination[i]));
                }
            });
        }

        [Fact]
        public void ILogB_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    TensorPrimitives.ILogB<T>(x.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(T.CreateTruncating(T.ILogB(x[i])), T.CreateTruncating(destination[i]));
                    }
                }, x);
            });
        }

        [Fact]
        public void ILogB_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ILogB<T>(x.Span, destination.Span));
            });
        }
        #endregion

        #region Round
        public static IEnumerable<object[]> RoundData()
        {
            foreach (MidpointRounding mode in Enum.GetValues(typeof(MidpointRounding)))
            {
                foreach (int digits in new[] { 0, 1, 4 })
                {
                    yield return new object[] { mode, digits };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RoundData))]
        public void Round_AllLengths(MidpointRounding mode, int digits)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                if (digits == 0)
                {
                    if (mode == MidpointRounding.ToEven)
                    {
                        TensorPrimitives.Round(x.Span, destination.Span);
                        for (int i = 0; i < tensorLength; i++)
                        {
                            AssertEqualTolerance(T.Round(x[i]), destination[i]);
                        }
                    }

                    TensorPrimitives.Round(x.Span, mode, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(T.Round(x[i], mode), destination[i]);
                    }
                }

                if (mode == MidpointRounding.ToEven)
                {
                    TensorPrimitives.Round(x.Span, digits, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(T.Round(x[i], digits), destination[i]);
                    }
                }

                TensorPrimitives.Round(x.Span, digits, mode, destination.Span);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(T.Round(x[i], digits, mode), destination[i]);
                }
            });
        }

        [Fact]
        public void Round_InvalidMode_Throws()
        {
            T[] x = new T[10];
            AssertExtensions.Throws<ArgumentException>("mode", () => TensorPrimitives.Round(x.AsSpan(), (MidpointRounding)(-1), x.AsSpan()));
            AssertExtensions.Throws<ArgumentException>("mode", () => TensorPrimitives.Round(x.AsSpan(), (MidpointRounding)5, x.AsSpan()));
        }

        [Fact]
        public void Round_InvalidDigits_Throws()
        {
            T[] x = new T[10];
            AssertExtensions.Throws<ArgumentOutOfRangeException>("digits", () => TensorPrimitives.Round(x.AsSpan(), -1, x.AsSpan()));
        }
        #endregion
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

                Assert.Throws<OverflowException>(() => TensorPrimitives.Abs(x.Span, destination.Span));
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

                Assert.Throws<OverflowException>(() => TensorPrimitives.SumOfMagnitudes<T>(x.Span));
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

                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.Divide(x.Span, y.Span, destination.Span));
            });
        }

        [Fact]
        public void Divide_TensorScalar_ByZero_Throw()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.Divide(x, T.Zero, destination));
            });
        }

        [Fact]
        public void Divide_ScalarTensor_ByZero_Throw()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);
                x[Random.Next(x.Length)] = T.Zero;

                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.Divide(T.One, x, destination));
            });
        }

        #region Span -> Destination
        public static IEnumerable<object[]> SpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.OnesComplement), new Func<T, T>(i => ~i) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.PopCount), new Func<T, T>(T.PopCount) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.LeadingZeroCount), new Func<T, T>(T.LeadingZeroCount) };
            yield return new object[] { new SpanDestinationDelegate(TensorPrimitives.TrailingZeroCount), new Func<T, T>(T.TrailingZeroCount) };
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_AllLengths(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_InPlace(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i]), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ThrowsForTooShortDestination(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ThrowsForOverlapppingInputsWithOutputs(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.BitwiseAnd), new Func<T, T, T>((x, y) => x & y) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.BitwiseOr), new Func<T, T, T>((x, y) => x | y) };
            yield return new object[] { new SpanSpanDestinationDelegate(TensorPrimitives.Xor), new Func<T, T, T>((x, y) => x ^ y) };
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_AllLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_InPlace(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(x, y, destination));
                Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(y, x, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForTooShortDestination(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForOverlapppingInputsWithOutputs(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(6, 2)));
        }
        #endregion

        #region Span,Scalar -> Destination
        public static IEnumerable<object[]> SpanScalarDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.BitwiseAnd), new Func<T, T, T>((x, y) => x & y) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.BitwiseOr), new Func<T, T, T>((x, y) => x | y) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Max), new Func<T, T, T>(T.Max) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.MaxMagnitude), new Func<T, T, T>(T.MaxMagnitude) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Min), new Func<T, T, T>(T.Min) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.MinMagnitude), new Func<T, T, T>(T.MinMagnitude) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, T, T>(TensorPrimitives.Xor), new Func<T, T, T>((x, y) => x ^ y) };
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_AllLengths(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_InPlace(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_ThrowsForOverlapppingInputWithOutputs(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            T y = NextRandom();
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), y, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), y, array.AsSpan(2, 2)));
        }
        #endregion

        #region Shifting/Rotating
        public static IEnumerable<object[]> ShiftRotateDestinationFunctionsToTest()
        {
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.ShiftLeft), new Func<T, int, T>((x, n) => x << n) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.ShiftRightArithmetic), new Func<T, int, T>((x, n) => x >> n) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.ShiftRightLogical), new Func<T, int, T>((x, n) => x >>> n) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.RotateLeft), new Func<T, int, T>(T.RotateLeft) };
            yield return new object[] { new SpanScalarDestinationDelegate<T, int, T>(TensorPrimitives.RotateRight), new Func<T, int, T>(T.RotateRight) };
        }

        [Theory]
        [MemberData(nameof(ShiftRotateDestinationFunctionsToTest))]
        public void ShiftRotateDestination_AllLengths(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = Random.Next(0, T.MaxValue.GetByteCount() * 8);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ShiftRotateDestinationFunctionsToTest))]
        public void ShiftRotateDestination_InPlace(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = Random.Next(0, T.MaxValue.GetByteCount() * 8);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ShiftRotateDestinationFunctionsToTest))]
        public void ShiftRotateDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, default, destination));
            });
        }

        [Theory]
        [MemberData(nameof(ShiftRotateDestinationFunctionsToTest))]
        public void ShiftRotateDestination_ThrowsForOverlapppingInputWithOutputs(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(2, 2)));
        }
        #endregion

        #region CopySign
        private void RemoveSignedMinValue(Span<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                while (T.Sign(span[i]) < 0 && span[i] == T.MinValue)
                {
                    span[i] = NextRandom();
                }
            }
        }

        [Fact]
        public void CopySign_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                RemoveSignedMinValue(x); // CopySign doesn't work with MinValue for signed integers, so remove any MinValue values from the input.
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                TensorPrimitives.CopySign<T>(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(T.CopySign(x[i], y[i]), destination[i]);
                }

                if (tensorLength > 0)
                {
                    TensorPrimitives.CopySign<T>(x, y[0], destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(T.CopySign(x[i], y[0]), destination[i]);
                    }
                }
            });
        }

        [Fact]
        public void CopySign_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                RemoveSignedMinValue(x); // CopySign doesn't work with MinValue for signed integers, so remove any MinValue values from the input.

                T[] xOrig = x.Span.ToArray();

                TensorPrimitives.CopySign<T>(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(T.CopySign(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void CopySign_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => TensorPrimitives.CopySign<T>(x, y, destination));
                Assert.Throws<ArgumentException>(() => TensorPrimitives.CopySign<T>(y, x, destination));
            });
        }

        [Fact]
        public void CopySign_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign<T>(x, y, destination));
                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign<T>(x, y[0], destination));
            });
        }

        [Fact]
        public void CopySign_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(6, 2)));

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.CopySign(array.AsSpan(1, 2), default(T), array.AsSpan(2, 2)));
        }
        #endregion
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
        protected override int IndexOfMax(ReadOnlySpan<T> x) => TensorPrimitives.IndexOfMax(x);
        protected override int IndexOfMaxMagnitude(ReadOnlySpan<T> x) => TensorPrimitives.IndexOfMaxMagnitude(x);
        protected override int IndexOfMin(ReadOnlySpan<T> x) => TensorPrimitives.IndexOfMin(x);
        protected override int IndexOfMinMagnitude(ReadOnlySpan<T> x) => TensorPrimitives.IndexOfMinMagnitude(x);
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

        protected override T ConvertFromSingle(float f) => T.CreateTruncating(f);
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

        protected override void AssertEqualTolerance(T expected, T actual, T? tolerance = null)
        {
            tolerance ??= T.CreateTruncating(0.0001);

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

        #region Scalar,Span -> Destination
        public static IEnumerable<object[]> ScalarSpanDestinationFunctionsToTest()
        {
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Divide), new Func<T, T, T>((x, y) => x / y) };
            yield return new object[] { new ScalarSpanDestinationDelegate(TensorPrimitives.Subtract), new Func<T, T, T>((x, y) => x - y) };
        }

        [Theory]
        [MemberData(nameof(ScalarSpanDestinationFunctionsToTest))]
        public void ScalarSpanDestination_AllLengths(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                FillTensor(y.Span, default);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, y[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanDestinationFunctionsToTest))]
        public void ScalarSpanDestination_InPlace(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                FillTensor(y.Span, default);
                T[] yOrig = y.Span.ToArray();

                tensorPrimitivesMethod(x, y.Span, y.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, yOrig[i]), y[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanDestinationFunctionsToTest))]
        public void ScalarSpanDestination_ThrowsForTooShortDestination(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, y, destination));
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanDestinationFunctionsToTest))]
        public void ScalarSpanDestination_ThrowsForOverlapppingInputsWithOutputs(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion
    }
}
