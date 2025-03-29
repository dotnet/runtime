// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
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
        [Fact]
        [SkipOnCoreClr("Depends heavily on folded type comparisons", RuntimeTestModes.JitMinOpts)]
        public void ConvertTruncatingAndSaturating()
        {
            // A few cases. More exhaustive testing is done in the OuterLoop test.

            ConvertTruncatingImpl<float, double>();
            ConvertTruncatingImpl<double, float>();
            ConvertTruncatingImpl<long, byte>();
            ConvertTruncatingImpl<short, uint>();
            ConvertTruncatingImpl<Half, int>();

            ConvertSaturatingImpl<float, double>();
            ConvertSaturatingImpl<double, float>();
            ConvertSaturatingImpl<long, byte>();
            ConvertSaturatingImpl<short, uint>();
            ConvertSaturatingImpl<Half, int>();
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [SkipOnCoreClr("Depends heavily on folded type comparisons", RuntimeTestModes.JitMinOpts)]
        public void ConvertTruncatingAndSaturating_Outerloop()
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
            ConvertCheckedImpl<byte, short>();
            ConvertCheckedImpl<byte, uint>();
            ConvertCheckedImpl<byte, long>();
            ConvertCheckedImpl<byte, float>();
            ConvertCheckedImpl<Half, Half>();
            ConvertCheckedImpl<Half, float>();
            ConvertCheckedImpl<Half, double>();
            ConvertCheckedImpl<float, double>();
            ConvertCheckedImpl<double, double>();

            // Conversions that may overflow. This isn't an exhaustive list; just a sampling.
            ConvertCheckedImpl<float, int>(42f, float.MaxValue);
            ConvertCheckedImpl<long, int>(42, int.MaxValue + 1L);
        }

        private static void ConvertTruncatingImpl<TFrom, TTo>()
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertTruncating<TFrom, TTo>(new TFrom[3], new TTo[2]));

            Random rand = new(42);
            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

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
                    if (!Helpers.IsEqualWithTolerance(TTo.CreateTruncating(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateTruncating(source.Span[i])}.");
                    }
                }
            }
        }

        private static void ConvertSaturatingImpl<TFrom, TTo>()
            where TFrom : unmanaged, INumber<TFrom>
            where TTo : unmanaged, INumber<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertSaturating<TFrom, TTo>(new TFrom[3], new TTo[2]));

            Random rand = new(42);
            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

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
                    if (!Helpers.IsEqualWithTolerance(TTo.CreateSaturating(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateSaturating(source.Span[i])}.");
                    }
                }
            }
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
                    if (!Helpers.IsEqualWithTolerance(TTo.CreateChecked(source.Span[i]), destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Actual: {destination.Span[i]}. Expected: {TTo.CreateChecked(source.Span[i])}.");
                    }
                }
            }
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
                    Assert.True(Helpers.IsEqualWithTolerance(TTo.CreateChecked(valid), result));
                }

                // Test with at least one invalid
                foreach (int invalidPosition in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    source.Span.Fill(valid);
                    source.Span[invalidPosition] = invalid;
                    Assert.Throws<OverflowException>(() => TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span));
                }
            }
        }

#if !SNT_NET8_TESTS
        [Fact]
        public void ConvertToInteger()
        {
            ConvertToIntegerImpl<Half, ushort>();
            ConvertToIntegerImpl<Half, int>();
            ConvertToIntegerImpl<Half, uint>();
            ConvertToIntegerImpl<Half, ulong>();

            ConvertToIntegerImpl<float, ushort>();
            ConvertToIntegerImpl<float, int>();
            ConvertToIntegerImpl<float, uint>();
            ConvertToIntegerImpl<float, ulong>();

            ConvertToIntegerImpl<double, ushort>();
            ConvertToIntegerImpl<double, int>();
            ConvertToIntegerImpl<double, long>();
            ConvertToIntegerImpl<double, ulong>();
        }

        [Fact]
        public void ConvertToIntegerNative()
        {
            ConvertToIntegerNativeImpl<Half, ushort>();
            ConvertToIntegerNativeImpl<Half, int>();
            ConvertToIntegerNativeImpl<Half, uint>();
            ConvertToIntegerNativeImpl<Half, ulong>();

            ConvertToIntegerNativeImpl<float, ushort>();
            ConvertToIntegerNativeImpl<float, int>();
            ConvertToIntegerNativeImpl<float, uint>();
            ConvertToIntegerNativeImpl<float, ulong>();

            ConvertToIntegerNativeImpl<double, ushort>();
            ConvertToIntegerNativeImpl<double, int>();
            ConvertToIntegerNativeImpl<double, long>();
            ConvertToIntegerNativeImpl<double, ulong>();
        }

        private static void ConvertToIntegerImpl<TFrom, TTo>()
            where TFrom : unmanaged, IFloatingPoint<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToInteger<TFrom, TTo>(new TFrom[3], new TTo[2]));

            Random rand = new(42);
            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                Span<TFrom> sourceSpan = source.Span;
                for (int i = 0; i < tensorLength; i++)
                {
                    sourceSpan[i] = TFrom.CreateTruncating(rand.NextDouble() * int.MaxValue);
                }

                TensorPrimitives.ConvertToInteger(source.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    TTo expected = TFrom.ConvertToInteger<TTo>(source.Span[i]);
                    if (!Helpers.IsEqualWithTolerance(expected, destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Expected: {expected}. Actual: {destination.Span[i]}.");
                    }
                }
            }
        }

        private static void ConvertToIntegerNativeImpl<TFrom, TTo>()
            where TFrom : unmanaged, IFloatingPoint<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToIntegerNative<TFrom, TTo>(new TFrom[3], new TTo[2]));

            Random rand = new(42);
            foreach (int tensorLength in Helpers.TensorLengthsIncluding0)
            {
                using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(tensorLength);
                using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(tensorLength);

                Span<TFrom> sourceSpan = source.Span;
                for (int i = 0; i < tensorLength; i++)
                {
                    sourceSpan[i] = TFrom.CreateTruncating(rand.NextDouble() * int.MaxValue);
                }

                TensorPrimitives.ConvertToIntegerNative(source.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    TTo expected = TFrom.ConvertToIntegerNative<TTo>(source.Span[i]);
                    if (!Helpers.IsEqualWithTolerance(expected, destination.Span[i]))
                    {
                        throw new XunitException($"{typeof(TFrom).Name} => {typeof(TTo).Name}. Input: {source.Span[i]}. Expected: {expected}. Actual: {destination.Span[i]}.");
                    }
                }
            }
        }
#endif
    }

    // The tests for some types have been marked as OuterLoop simply to decrease inner loop testing time.

    public class DoubleGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<double> { }
    public class SingleGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<float> { }
    public class HalfGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<Half>
    {
        protected override void AssertEqualTolerance(Half expected, Half actual, Half? tolerance = null) =>
            base.AssertEqualTolerance(expected, actual, tolerance ?? Half.CreateTruncating(0.001));
    }

    [OuterLoop]
    public class NFloatGenericTensorPrimitives : GenericFloatingPointNumberTensorPrimitivesTests<NFloat> { }

    [OuterLoop]
    public class SByteGenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<sbyte> { }
    public class Int16GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<short> { }
    [OuterLoop]
    public class Int32GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<int> { }
    public class Int64GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<long> { }
    [OuterLoop]
    public class IntPtrGenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<nint> { }
    public class Int128GenericTensorPrimitives : GenericSignedIntegerTensorPrimitivesTests<Int128> { }

    public class ByteGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<byte> { }
    [OuterLoop]
    public class UInt16GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<ushort> { }
    [OuterLoop]
    public class CharGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<char> { }
    public class UInt32GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<uint> { }
    [OuterLoop]
    public class UInt64GenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<ulong> { }

    public class UIntPtrGenericTensorPrimitives : GenericIntegerTensorPrimitivesTests<nuint> { }
    [OuterLoop]
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
            yield return T.PositiveInfinity;
            yield return T.NegativeInfinity;

            // +0, -0
            yield return T.Zero;
            yield return T.NegativeZero;

            // +1, -1
            yield return T.One;
            yield return T.NegativeOne;

            // Subnormals
            yield return T.Epsilon;
            yield return -T.Epsilon;
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x007F_FFFF));
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x807F_FFFF));

            // Normals
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x0080_0000));
            yield return T.CreateTruncating(BitConverter.UInt32BitsToSingle(0x8080_0000));
            yield return T.CreateTruncating(float.MinValue);
            yield return T.CreateTruncating(float.MaxValue);
            yield return T.CreateTruncating(double.MinValue);
            yield return T.CreateTruncating(double.MaxValue);

            // Other known constants
            yield return T.E;
            yield return T.Pi;
            yield return T.Tau;
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
            // The current trigonometric algorithm depends on hardware FMA support for best precision.
            T? trigTolerance = IsFmaSupported ? null : Helpers.DetermineTolerance<T>(doubleTolerance: 1e-10, floatTolerance: 1e-4f);

            yield return Create(TensorPrimitives.Acosh, T.Acosh);
            yield return Create(TensorPrimitives.AcosPi, T.AcosPi);
            yield return Create(TensorPrimitives.Acos, T.Acos);
            yield return Create(TensorPrimitives.Asinh, T.Asinh);
            yield return Create(TensorPrimitives.AsinPi, T.AsinPi);
            yield return Create(TensorPrimitives.Asin, T.Asin);
            yield return Create(TensorPrimitives.Atanh, T.Atanh);
            yield return Create(TensorPrimitives.AtanPi, T.AtanPi);
            yield return Create(TensorPrimitives.Atan, T.Atan);
            yield return Create(TensorPrimitives.BitDecrement, T.BitDecrement);
            yield return Create(TensorPrimitives.BitIncrement, T.BitIncrement);
            yield return Create(TensorPrimitives.Cbrt, T.Cbrt, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13));
            yield return Create(TensorPrimitives.Ceiling, T.Ceiling);
            yield return Create(TensorPrimitives.Cos, T.Cos, trigTolerance);
            yield return Create(TensorPrimitives.Cosh, T.Cosh, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-14));
            yield return Create(TensorPrimitives.CosPi, T.CosPi, trigTolerance ?? Helpers.DetermineTolerance<T>(floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Decrement, f => --f);
            yield return Create(TensorPrimitives.DegreesToRadians, T.DegreesToRadians);
            yield return Create(TensorPrimitives.Exp, T.Exp);
            yield return Create(TensorPrimitives.Exp2, T.Exp2, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-14, floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Exp10, T.Exp10, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13, floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.ExpM1, T.ExpM1);
            yield return Create(TensorPrimitives.Exp2M1, T.Exp2M1, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-14, floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Exp10M1, T.Exp10M1, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13, floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Floor, T.Floor);
            yield return Create(TensorPrimitives.Increment, f => ++f);
            yield return Create(TensorPrimitives.Log, T.Log);
            yield return Create(TensorPrimitives.Log2, T.Log2);
            yield return Create(TensorPrimitives.Log10, T.Log10);
            yield return Create(TensorPrimitives.LogP1, T.LogP1);
            yield return Create(TensorPrimitives.Log2P1, T.Log2P1);
            yield return Create(TensorPrimitives.Log10P1, T.Log10P1);
            yield return Create(TensorPrimitives.RadiansToDegrees, T.RadiansToDegrees);
            yield return Create(TensorPrimitives.Reciprocal, f => T.One / f);
            yield return Create(TensorPrimitives.ReciprocalEstimate, T.ReciprocalEstimate, T.CreateTruncating(Helpers.DefaultToleranceForEstimates));
            yield return Create(TensorPrimitives.ReciprocalSqrt, f => T.One / T.Sqrt(f));

#if !SNT_NET8_TESTS
            // Avoid running with the net8 tests due to: https://github.com/dotnet/runtime/issues/101846
            yield return Create(TensorPrimitives.ReciprocalSqrtEstimate, T.ReciprocalSqrtEstimate, T.CreateTruncating(Helpers.DefaultToleranceForEstimates));
#endif

            yield return Create(TensorPrimitives.Round, T.Round);
            yield return Create(TensorPrimitives.Sin, T.Sin, trigTolerance);
            yield return Create(TensorPrimitives.Sinh, T.Sinh, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-14));
            yield return Create(TensorPrimitives.SinPi, T.SinPi, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13, floatTolerance: 1e-4f));
            yield return Create(TensorPrimitives.Sqrt, T.Sqrt);
            yield return Create(TensorPrimitives.Tan, T.Tan, trigTolerance);
            yield return Create(TensorPrimitives.Tanh, T.Tanh);
            yield return Create(TensorPrimitives.TanPi, T.TanPi);
            yield return Create(TensorPrimitives.Truncate, T.Truncate);

            static object[] Create(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
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
        public void SpanDestinationFunctions_ThrowsForTooShortDestination(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x.Span, destination.Span));
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationFunctionsToTest))]
        public void SpanDestinationFunctions_ThrowsForOverlappingInputsWithOutputs(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
#pragma warning restore xUnit1026
        #endregion

        #region Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.Atan2, T.Atan2);
            yield return Create(TensorPrimitives.Atan2Pi, T.Atan2Pi);
            yield return Create(TensorPrimitives.CopySign, T.CopySign);
            yield return Create(TensorPrimitives.Hypot, T.Hypot);
            yield return Create(TensorPrimitives.Ieee754Remainder, T.Ieee754Remainder);
            yield return Create(TensorPrimitives.Log, T.Log);
            yield return Create(TensorPrimitives.Max, T.Max);
            yield return Create(TensorPrimitives.MaxNumber, T.MaxNumber);
            yield return Create(TensorPrimitives.MaxMagnitude, T.MaxMagnitude);
            yield return Create(TensorPrimitives.MaxMagnitudeNumber, T.MaxMagnitudeNumber);
            yield return Create(TensorPrimitives.Min, T.Min);
            yield return Create(TensorPrimitives.MinNumber, T.MinNumber);
            yield return Create(TensorPrimitives.MinMagnitude, T.MinMagnitude);
            yield return Create(TensorPrimitives.MinMagnitudeNumber, T.MinMagnitudeNumber);
            yield return Create(TensorPrimitives.Pow, T.Pow, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13, floatTolerance: 1e-5f));

            static object[] Create(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_AllLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_InPlace(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i]), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_SpecialValues(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i], tolerance);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i]), destination[i], tolerance);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanDestinationFunctionsToTest))]
        public void SpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanDestination_ThrowsForTooShortDestination(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanDestination_ThrowsForOverlappingInputsWithOutputs(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
            yield return Create(TensorPrimitives.Atan2, T.Atan2);
            yield return Create(TensorPrimitives.Atan2Pi, T.Atan2Pi);
            yield return Create(TensorPrimitives.CopySign, T.CopySign);
            yield return Create(TensorPrimitives.Ieee754Remainder, T.Ieee754Remainder);
            yield return Create(TensorPrimitives.Pow, T.Pow, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13, floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Log, T.Log);
            yield return Create(TensorPrimitives.Max, T.Max);
            yield return Create(TensorPrimitives.MaxNumber, T.MaxNumber);
            yield return Create(TensorPrimitives.MaxMagnitude, T.MaxMagnitude);
            yield return Create(TensorPrimitives.MaxMagnitudeNumber, T.MaxMagnitudeNumber);
            yield return Create(TensorPrimitives.Min, T.Min);
            yield return Create(TensorPrimitives.MinNumber, T.MinNumber);
            yield return Create(TensorPrimitives.MinMagnitude, T.MinMagnitude);
            yield return Create(TensorPrimitives.MinMagnitudeNumber, T.MinMagnitudeNumber);

            static object[] Create(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_AllLengths(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_InPlace(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_SpecialValues(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y), destination[i], tolerance);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarDestinationFunctionsToTest))]
        public void SpanScalarDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanScalarDestination_ThrowsForOverlappingInputsWithOutputs(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), default, array.AsSpan(2, 2)));
        }
        #endregion

        #region Scalar,Span -> Destination
        public static IEnumerable<object[]> ScalarSpanFloatDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.Atan2, T.Atan2);
            yield return Create(TensorPrimitives.Atan2Pi, T.Atan2Pi);
            yield return Create(TensorPrimitives.Pow, T.Pow, Helpers.DetermineTolerance<T>(floatTolerance: 1e-5f));
            yield return Create(TensorPrimitives.Ieee754Remainder, T.Ieee754Remainder);

            static object[] Create(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_AllLengths(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, y[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_InPlace(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                T x = NextRandom();
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] yOrig = y.Span.ToArray();

                tensorPrimitivesMethod(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x, yOrig[i]), y[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void ScalarSpanDestination_SpecialValues(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x, y[i]), destination[i], tolerance);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(ScalarSpanFloatDestinationFunctionsToTest))]
        public void SpanScalarFloatDestination_ThrowsForTooShortDestination(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanScalarFloatDestination_ThrowsForOverlappingInputsWithOutputs(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Int,Span -> Destination
        public static IEnumerable<object[]> SpanIntDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.RootN, T.RootN, Helpers.DetermineTolerance<T>(doubleTolerance: 1e-13));
            yield return Create(TensorPrimitives.ScaleB, T.ScaleB);

            static object[] Create(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_AllLengths(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                int y = Random.Next(1, 10);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                tensorPrimitivesMethod(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(x[i], y), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_InPlace(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                int y = Random.Next(1, 10);

                tensorPrimitivesMethod(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_SpecialValues(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y), destination[i], tolerance);
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanIntDestinationFunctionsToTest))]
        public void SpanIntDestination_ThrowsForTooShortDestination(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanIntDestination_ThrowsForOverlappingInputsWithOutputs(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), 2, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), 2, array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanSpanDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.FusedMultiplyAdd, T.FusedMultiplyAdd);
            yield return Create(TensorPrimitives.Lerp, T.Lerp);
            yield return Create(TensorPrimitives.MultiplyAddEstimate, T.FusedMultiplyAdd, T.CreateTruncating(Helpers.DefaultToleranceForEstimates)); // TODO: Change T.FusedMultiplyAdd to T.MultiplyAddEstimate when available

            static object[] Create(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_AllLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                    AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_InPlace(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i], xOrig[i]), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_SpecialValues(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, y);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, z);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanSpanDestination_ThrowsForTooShortDestination(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanSpanDestination_ThrowsForOverlappingInputsWithOutputs(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
            yield return Create(TensorPrimitives.FusedMultiplyAdd, T.FusedMultiplyAdd);
            yield return Create(TensorPrimitives.Lerp, T.Lerp);
            yield return Create(TensorPrimitives.MultiplyAddEstimate, T.MultiplyAddEstimate, T.CreateTruncating(Helpers.DefaultToleranceForEstimates));

            static object[] Create(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_AllLengths(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                    AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_InPlace(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T z = NextRandom();

                tensorPrimitivesMethod(x, x, z, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i], z), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_SpecialValues(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i], tolerance);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z), destination[i], tolerance);
                    }
                }, y);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanScalarDestinationFunctionsToTest))]
        public void SpanSpanScalarDestination_ThrowsForTooShortDestination(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanScalarDestination_ThrowsForOverlappingInputsWithOutputs(SpanSpanScalarDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
            yield return Create(TensorPrimitives.FusedMultiplyAdd, T.FusedMultiplyAdd);
            yield return Create(TensorPrimitives.Lerp, T.Lerp);
            yield return Create(TensorPrimitives.MultiplyAddEstimate, T.FusedMultiplyAdd, T.CreateTruncating(Helpers.DefaultToleranceForEstimates)); // TODO: Change T.FusedMultiplyAdd to T.MultiplyAddEstimate when available

            static object[] Create(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_AllLengths(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                    AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_InPlace(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                tensorPrimitivesMethod(x, y, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], y, xOrig[i]), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_SpecialValues(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i], tolerance);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y, z[i]), destination[i], tolerance);
                    }
                }, z);
            });
        }

        [Theory]
        [MemberData(nameof(SpanScalarSpanDestinationFunctionsToTest))]
        public void SpanScalarSpanDestination_ThrowsForTooShortDestination(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanScalarSpanDestination_ThrowsForOverlappingInputsWithOutputs(SpanScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
            yield return Create(TensorPrimitives.SinCos, T.SinCos);
            yield return Create(TensorPrimitives.SinCosPi, T.SinCosPi);

            static object[] Create(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void SpanDestinationDestinationFunctions_ThrowsForOverlappingInputsWithOutputs(SpanDestinationDestinationDelegate tensorPrimitivesMethod, Func<T, (T, T)> _)
        {
            T[] array = new T[10];
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(3, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
            Assert.Throws<ArgumentException>(() => tensorPrimitivesMethod(array.AsSpan(5, 2), array.AsSpan(0, 2), array.AsSpan(4, 2)));
        }
        #endregion

        #region Span -> Bool Destination
        public static IEnumerable<object[]> SpanInt32DestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.ILogB, T.ILogB);
            yield return Create(TensorPrimitives.Sign, T.Sign);

            static object[] Create(SpanDestinationDelegate<T, int> tensorPrimitivesMethod, Func<T, int> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
        }

        [Theory]
        [MemberData(nameof(SpanInt32DestinationFunctionsToTest))]
        public void SpanInt32Destination_AllLengths(SpanDestinationDelegate<T, int> tensorPrimitivesMethod, Func<T, int> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength);

                tensorPrimitivesMethod(x.Span, destination.Span);

                for (int i = 0; i < tensorLength; i++)
                {
                    Assert.Equal(expectedMethod(x[i]), destination[i]);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanInt32DestinationFunctionsToTest))]
        public void SpanInt32Destination_SpecialValues(SpanDestinationDelegate<T, int> tensorPrimitivesMethod, Func<T, int> expectedMethod)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Exception failure = Record.Exception(() => tensorPrimitivesMethod(x.Span, destination.Span));
                    if (failure is not null)
                    {
                        Assert.Throws(failure.GetType(), () =>
                        {
                            for (int i = 0; i < x.Length; i++)
                            {
                                expectedMethod(x[i]);
                            }
                        });
                    }
                    else
                    {
                        for (int i = 0; i < tensorLength; i++)
                        {
                            Assert.Equal(expectedMethod(x[i]), destination[i]);
                        }
                    }
                }, x);
            });
        }

        [Theory]
        [MemberData(nameof(SpanInt32DestinationFunctionsToTest))]
        public void SpanInt32Destination_ThrowsForTooShortDestination(SpanDestinationDelegate<T, int> tensorPrimitivesMethod, Func<T, int> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<int> destination = BoundedMemory.Allocate<int>(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x.Span, destination.Span));
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

        #region StdDev
        [Fact]
        public void StdDev_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.StdDev(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void StdDev_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T mean = x[0];
                for (int i = 1; i < x.Length; i++)
                {
                    mean = Add(mean, x[i]);
                }

                mean /= T.CreateChecked(x.Length);

                T sumOfSquares = T.Zero;
                for (int i = 0; i < x.Length; i++)
                {
                    T diff = x[i] - mean;
                    sumOfSquares += (diff * diff);
                }

                T variance = sumOfSquares / T.CreateChecked(x.Length);

                AssertEqualTolerance(T.Sqrt(variance), TensorPrimitives.StdDev<T>(x));
            });
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
        #region Divide
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
                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.Divide(x, T.Zero, destination));
                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.Divide(T.One, y.Span, destination));
            });
        }
        #endregion

        #region DivRem
        [Fact]
        public void DivRem_ByZero_Throws()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                using BoundedMemory<T> quotientDestination = CreateTensor(tensorLength);
                using BoundedMemory<T> remainderDestination = CreateTensor(tensorLength);

                FillTensor(y.Span, T.Zero);
                y[^1] = T.Zero;

                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.DivRem(x.Span, y.Span, quotientDestination.Span, remainderDestination.Span));
                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.DivRem(x, T.Zero, quotientDestination.Span, remainderDestination.Span));
                Assert.Throws<DivideByZeroException>(() => TensorPrimitives.DivRem(T.One, y.Span, quotientDestination.Span, remainderDestination.Span));
            });
        }

        [Fact]
        public void DivRem_TwoTensors_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                using BoundedMemory<T> quotientDestination = CreateTensor(tensorLength);
                using BoundedMemory<T> remainderDestination = CreateTensor(tensorLength);

                FillTensor(y.Span, T.Zero);

                TensorPrimitives.DivRem(x.Span, y.Span, quotientDestination.Span, remainderDestination.Span);
                for (int i = 0; i < tensorLength; i++)
                {
                    Assert.Equal(x[i] / y[i], quotientDestination[i]);
                    Assert.Equal(x[i] % y[i], remainderDestination[i]);
                }
            });
        }

        [Fact]
        public void DivRem_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                FillTensor(y.Span, T.Zero);

                T[] xOrig = x.Span.ToArray();
                T[] yOrig = y.Span.ToArray();

                TensorPrimitives.DivRem(x.Span, y, x, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(xOrig[i] / yOrig[i], x[i]);
                    AssertEqualTolerance(xOrig[i] % yOrig[i], y[i]);
                }
            });
        }

        [Fact]
        public void DivRem_SpanScalar_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> quotientDestination = CreateTensor(tensorLength);
                using BoundedMemory<T> remainderDestination = CreateTensor(tensorLength);

                T y = NextRandom(T.Zero);

                TensorPrimitives.DivRem(x.Span, y, quotientDestination.Span, remainderDestination.Span);
                for (int i = 0; i < tensorLength; i++)
                {
                    Assert.Equal(x[i] / y, quotientDestination[i]);
                    Assert.Equal(x[i] % y, remainderDestination[i]);
                }
            });
        }

        [Fact]
        public void DivRem_SpanScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> d = CreateAndFillTensor(tensorLength);

                T y = NextRandom(T.Zero);
                T[] xOrig = x.Span.ToArray();

                TensorPrimitives.DivRem(x.Span, y, x, d);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(xOrig[i] / y, x[i]);
                    AssertEqualTolerance(xOrig[i] % y, d[i]);
                }
            });
        }

        [Fact]
        public void DivRem_ScalarSpan_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                using BoundedMemory<T> quotientDestination = CreateTensor(tensorLength);
                using BoundedMemory<T> remainderDestination = CreateTensor(tensorLength);

                FillTensor(y.Span, T.Zero);
                T x = NextRandom();

                TensorPrimitives.DivRem(x, y.Span, quotientDestination.Span, remainderDestination.Span);
                for (int i = 0; i < tensorLength; i++)
                {
                    Assert.Equal(x / y[i], quotientDestination[i]);
                    Assert.Equal(x % y[i], remainderDestination[i]);
                }
            });
        }

        [Fact]
        public void DivRem_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> d1 = CreateTensor(tensorLength);
                using BoundedMemory<T> d2 = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem<T>(x, y, d1, d2));
                Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem<T>(y, x, d1, d2));
            });
        }

        [Fact]
        public void DivRem_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> d1 = CreateTensor(tensorLength);
                using BoundedMemory<T> d2 = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination2", () => TensorPrimitives.DivRem<T>(x, y, d1, d2));
                AssertExtensions.Throws<ArgumentException>("destination1", () => TensorPrimitives.DivRem<T>(y, x, d2, d1));
            });
        }

        [Fact]
        public void DivRem_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[20];

            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2), array.AsSpan(10)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2), array.AsSpan(10)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2), array.AsSpan(10)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2), array.AsSpan(10)));

            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(10), array.AsSpan(0, 2)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(10), array.AsSpan(2, 2)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(10), array.AsSpan(3, 2)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(10), array.AsSpan(5, 2)));

            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), array.AsSpan(3, 4), array.AsSpan(10), array.AsSpan(10)));

            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(array.AsSpan(1, 2), default(T), array.AsSpan(10), array.AsSpan(10)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.DivRem(default(T), array.AsSpan(1, 2), array.AsSpan(10), array.AsSpan(10)));
        }
        #endregion

        #region Span -> Destination
        public static IEnumerable<object[]> SpanDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.OnesComplement, i => ~i);
            yield return Create(TensorPrimitives.PopCount, T.PopCount);
            yield return Create(TensorPrimitives.LeadingZeroCount, T.LeadingZeroCount);
            yield return Create(TensorPrimitives.TrailingZeroCount, T.TrailingZeroCount);

            static object[] Create(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void SpanDestinationFunctions_ThrowsForOverlappingInputsWithOutputs(SpanDestinationDelegate tensorPrimitivesMethod, Func<T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.BitwiseAnd, (x, y) => x & y);
            yield return Create(TensorPrimitives.BitwiseOr, (x, y) => x | y);
            yield return Create(TensorPrimitives.Xor, (x, y) => x ^ y);

            static object[] Create(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void SpanSpanDestination_ThrowsForOverlappingInputsWithOutputs(SpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
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
            yield return Create(TensorPrimitives.BitwiseAnd, (x, y) => x & y);
            yield return Create(TensorPrimitives.BitwiseOr, (x, y) => x | y);
            yield return Create(TensorPrimitives.Max, T.Max);
            yield return Create(TensorPrimitives.MaxNumber, T.MaxNumber);
            yield return Create(TensorPrimitives.MaxMagnitude, T.MaxMagnitude);
            yield return Create(TensorPrimitives.MaxMagnitudeNumber, T.MaxMagnitudeNumber);
            yield return Create(TensorPrimitives.Min, T.Min);
            yield return Create(TensorPrimitives.MinNumber, T.MinNumber);
            yield return Create(TensorPrimitives.MinMagnitude, T.MinMagnitude);
            yield return Create(TensorPrimitives.MinMagnitudeNumber, T.MinMagnitudeNumber);
            yield return Create(TensorPrimitives.Xor, (x, y) => x ^ y);

            static object[] Create(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void SpanScalarDestination_ThrowsForOverlappingInputWithOutputs(SpanScalarDestinationDelegate<T, T, T> tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            T y = NextRandom();
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), y, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), y, array.AsSpan(2, 2)));
        }
        #endregion

        #region Span,Span,Span -> Destination
        public static IEnumerable<object[]> SpanSpanSpanDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.MultiplyAddEstimate, T.MultiplyAddEstimate, T.CreateTruncating(Helpers.DefaultToleranceForEstimates));

            static object[] Create(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
                => new object[] { tensorPrimitivesMethod, expectedMethod, tolerance };
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_AllLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                    AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_InPlace(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                tensorPrimitivesMethod(x, x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(expectedMethod(xOrig[i], xOrig[i], xOrig[i]), x[i], tolerance);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_SpecialValues(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
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
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, x);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, y);

                RunForEachSpecialValue(() =>
                {
                    tensorPrimitivesMethod(x.Span, y.Span, z.Span, destination.Span);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(expectedMethod(x[i], y[i], z[i]), destination[i], tolerance);
                    }
                }, z);
            });
        }

        [Theory]
        [MemberData(nameof(SpanSpanSpanDestinationFunctionsToTest))]
        public void SpanSpanSpanDestination_ThrowsForMismatchedLengths(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanSpanDestination_ThrowsForTooShortDestination(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

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
        public void SpanSpanSpanDestination_ThrowsForOverlappingInputsWithOutputs(SpanSpanSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T, T> expectedMethod, T? tolerance = null)
        {
            _ = expectedMethod;
            _ = tolerance;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }
        #endregion

        #region Shifting/Rotating
        public static IEnumerable<object[]> ShiftRotateDestinationFunctionsToTest()
        {
            yield return Create(TensorPrimitives.ShiftLeft, (x, n) => x << n);
            yield return Create(TensorPrimitives.ShiftRightArithmetic, (x, n) => x >> n);
            yield return Create(TensorPrimitives.ShiftRightLogical, (x, n) => x >>> n);
            yield return Create(TensorPrimitives.RotateLeft, T.RotateLeft);
            yield return Create(TensorPrimitives.RotateRight, T.RotateRight);

            static object[] Create(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void ShiftRotateDestination_ThrowsForOverlappingInputWithOutputs(SpanScalarDestinationDelegate<T, int, T> tensorPrimitivesMethod, Func<T, int, T> _)
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
        public void CopySign_ThrowsForOverlappingInputsWithOutputs()
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

        #region HammingBitDistance
        [Fact]
        public void HammingBitDistance_ThrowsForMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.HammingBitDistance<int>(new int[1], new int[2]));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.HammingBitDistance<int>(new int[2], new int[1]));
        }

        [Fact]
        public void HammingBitDistance_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                long expected = 0;
                for (int i = 0; i < tensorLength; i++)
                {
                    expected += long.CreateTruncating(T.PopCount(x[i] ^ y[i]));
                }

                Assert.Equal(expected, TensorPrimitives.HammingBitDistance<T>(x.Span, y.Span));
            });
        }

        [Fact]
        public void HammingBitDistance_KnownValues()
        {
            T value42 = T.CreateTruncating(42);
            T value84 = T.CreateTruncating(84);

            T[] values1 = new T[100];
            T[] values2 = new T[100];

            Array.Fill(values1, value42);
            Array.Fill(values2, value84);

            Assert.Equal(0, TensorPrimitives.HammingBitDistance<T>(values1, values1));
            Assert.Equal(600, TensorPrimitives.HammingBitDistance<T>(values1, values2));
            Assert.Equal(0, TensorPrimitives.HammingBitDistance<T>(values2, values2));
        }
        #endregion

        #region PopCount
        [Fact]
        public void PopCount_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                long expected = 0;
                for (int i = 0; i < tensorLength; i++)
                {
                    expected += long.CreateTruncating(T.PopCount(x[i]));
                }

                Assert.Equal(expected, TensorPrimitives.PopCount<T>(x.Span));
            });
        }

        [Fact]
        public void PopCount_KnownValues()
        {
            T[] values = new T[255];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = T.CreateTruncating(i);
            }

            Assert.Equal(1016, TensorPrimitives.PopCount<T>(values));
        }
        #endregion
    }

    public unsafe abstract class GenericNumberTensorPrimitivesTests<T> : TensorPrimitivesTests<T>
        where T : unmanaged, INumber<T>, IMinMaxValue<T>
    {
        protected static bool IsFmaSupported => Fma.IsSupported || AdvSimd.Arm64.IsSupported || (AdvSimd.IsSupported && typeof(T) == typeof(float));
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
            if (!Helpers.IsEqualWithTolerance(expected, actual, tolerance))
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
            yield return Create(TensorPrimitives.Divide, (x, y) => x / y);
            yield return Create(TensorPrimitives.Remainder, (x, y) => x % y);
            yield return Create(TensorPrimitives.Subtract, (x, y) => x - y);

            static object[] Create(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
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
        public void ScalarSpanDestination_ThrowsForOverlappingInputsWithOutputs(ScalarSpanDestinationDelegate tensorPrimitivesMethod, Func<T, T, T> _)
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(default, array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region IsXx
        public static IEnumerable<object[]> SpanDestinationIsFunctionsToTest()
        {
            yield return Create(TensorPrimitives.IsCanonical, T.IsCanonical);
            yield return Create(TensorPrimitives.IsComplexNumber, T.IsComplexNumber);
            yield return Create(TensorPrimitives.IsEvenInteger, T.IsEvenInteger);
            yield return Create(TensorPrimitives.IsFinite, T.IsFinite);
            yield return Create(TensorPrimitives.IsImaginaryNumber, T.IsImaginaryNumber);
            yield return Create(TensorPrimitives.IsInfinity, T.IsInfinity);
            yield return Create(TensorPrimitives.IsInteger, T.IsInteger);
            yield return Create(TensorPrimitives.IsNaN, T.IsNaN);
            yield return Create(TensorPrimitives.IsNegative, T.IsNegative);
            yield return Create(TensorPrimitives.IsNegativeInfinity, T.IsNegativeInfinity);
            yield return Create(TensorPrimitives.IsNormal, T.IsNormal);
            yield return Create(TensorPrimitives.IsOddInteger, T.IsOddInteger);
            yield return Create(TensorPrimitives.IsPositive, T.IsPositive);
            yield return Create(TensorPrimitives.IsPositiveInfinity, T.IsPositiveInfinity);
            yield return Create(TensorPrimitives.IsRealNumber, T.IsRealNumber);
            yield return Create(TensorPrimitives.IsSubnormal, T.IsSubnormal);
            yield return Create(TensorPrimitives.IsZero, T.IsZero);

            static object[] Create(SpanDestinationIsDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
        }

        [Theory]
        [MemberData(nameof(SpanDestinationIsFunctionsToTest))]
        public void SpanDestionIs_AllLengths(SpanDestinationIsDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<bool> destination = BoundedMemory.Allocate<bool>(tensorLength);

                tensorPrimitivesMethod(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    Assert.Equal(expectedMethod(x[i]), destination[i]);
                    Assert.True(Unsafe.BitCast<bool, byte>(destination[i]) is 0 or 1);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SpanDestinationIsFunctionsToTest))]
        public void SpanDestionIs_ThrowsForTooShortDestination(SpanDestinationIsDelegate tensorPrimitivesMethod, Func<T, bool> _)
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<bool> destination = BoundedMemory.Allocate<bool>(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => tensorPrimitivesMethod(x, destination));
            });
        }

        public static IEnumerable<object[]> IsAllFunctionsToTest()
        {
            yield return Create(TensorPrimitives.IsCanonicalAll, T.IsCanonical);
            yield return Create(TensorPrimitives.IsComplexNumberAll, T.IsComplexNumber);
            yield return Create(TensorPrimitives.IsEvenIntegerAll, T.IsEvenInteger);
            yield return Create(TensorPrimitives.IsFiniteAll, T.IsFinite);
            yield return Create(TensorPrimitives.IsImaginaryNumberAll, T.IsImaginaryNumber);
            yield return Create(TensorPrimitives.IsInfinityAll, T.IsInfinity);
            yield return Create(TensorPrimitives.IsIntegerAll, T.IsInteger);
            yield return Create(TensorPrimitives.IsNaNAll, T.IsNaN);
            yield return Create(TensorPrimitives.IsNegativeAll, T.IsNegative);
            yield return Create(TensorPrimitives.IsNegativeInfinityAll, T.IsNegativeInfinity);
            yield return Create(TensorPrimitives.IsNormalAll, T.IsNormal);
            yield return Create(TensorPrimitives.IsOddIntegerAll, T.IsOddInteger);
            yield return Create(TensorPrimitives.IsPositiveAll, T.IsPositive);
            yield return Create(TensorPrimitives.IsPositiveInfinityAll, T.IsPositiveInfinity);
            yield return Create(TensorPrimitives.IsRealNumberAll, T.IsRealNumber);
            yield return Create(TensorPrimitives.IsSubnormalAll, T.IsSubnormal);
            yield return Create(TensorPrimitives.IsZeroAll, T.IsZero);

            static object[] Create(SpanIsAllAnyDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
        }

        [Theory]
        [MemberData(nameof(IsAllFunctionsToTest))]
        public void SpanDestionIsAll_AllLengths(SpanIsAllAnyDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
        {
            Assert.False(tensorPrimitivesMethod(default));

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                bool actual = tensorPrimitivesMethod(x);

                bool expected = true;
                for (int i = 0; i < tensorLength && expected; i++)
                {
                    expected = expectedMethod(x[i]);
                }

                Assert.Equal(expected, actual);
            });
        }

        public static IEnumerable<object[]> IsAnyFunctionsToTest()
        {
            yield return Create(TensorPrimitives.IsCanonicalAny, T.IsCanonical);
            yield return Create(TensorPrimitives.IsComplexNumberAny, T.IsComplexNumber);
            yield return Create(TensorPrimitives.IsEvenIntegerAny, T.IsEvenInteger);
            yield return Create(TensorPrimitives.IsFiniteAny, T.IsFinite);
            yield return Create(TensorPrimitives.IsImaginaryNumberAny, T.IsImaginaryNumber);
            yield return Create(TensorPrimitives.IsInfinityAny, T.IsInfinity);
            yield return Create(TensorPrimitives.IsIntegerAny, T.IsInteger);
            yield return Create(TensorPrimitives.IsNaNAny, T.IsNaN);
            yield return Create(TensorPrimitives.IsNegativeAny, T.IsNegative);
            yield return Create(TensorPrimitives.IsNegativeInfinityAny, T.IsNegativeInfinity);
            yield return Create(TensorPrimitives.IsNormalAny, T.IsNormal);
            yield return Create(TensorPrimitives.IsOddIntegerAny, T.IsOddInteger);
            yield return Create(TensorPrimitives.IsPositiveAny, T.IsPositive);
            yield return Create(TensorPrimitives.IsPositiveInfinityAny, T.IsPositiveInfinity);
            yield return Create(TensorPrimitives.IsRealNumberAny, T.IsRealNumber);
            yield return Create(TensorPrimitives.IsSubnormalAny, T.IsSubnormal);
            yield return Create(TensorPrimitives.IsZeroAny, T.IsZero);

            static object[] Create(SpanIsAllAnyDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
                => new object[] { tensorPrimitivesMethod, expectedMethod };
        }

        [Theory]
        [MemberData(nameof(IsAnyFunctionsToTest))]
        public void SpanDestionIsAny_AllLengths(SpanIsAllAnyDelegate tensorPrimitivesMethod, Func<T, bool> expectedMethod)
        {
            Assert.False(tensorPrimitivesMethod(default));

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                bool actual = tensorPrimitivesMethod(x);

                bool expected = false;
                for (int i = 0; i < tensorLength && !expected; i++)
                {
                    expected = expectedMethod(x[i]);
                }

                Assert.Equal(expected, actual);
            });
        }
        #endregion

        #region HammingDistance
        [Fact]
        public void HammingDistance_ThrowsForMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.HammingDistance<int>(new int[1], new int[2]));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.HammingDistance<int>(new int[2], new int[1]));
        }

        [Fact]
        public void HammingDistance_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                int expected = 0;
                ReadOnlySpan<T> xSpan = x, ySpan = y;
                for (int i = 0; i < xSpan.Length; i++)
                {
                    if (xSpan[i] != ySpan[i])
                    {
                        expected++;
                    }
                }

                Assert.Equal(expected, TensorPrimitives.HammingDistance<T>(x, y));
            });
        }
        #endregion

        #region Average
        [Fact]
        public void Average_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Average(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void Average_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T f = x[0];
                for (int i = 1; i < x.Length; i++)
                {
                    f = Add(f, x[i]);
                }

                T length = default;
                if (Record.Exception(() => length = T.CreateChecked(x.Length)) is Exception expectedException)
                {
                    Assert.Throws(expectedException.GetType(), () => TensorPrimitives.Average<T>(x));
                }
                else
                {
                    AssertEqualTolerance(f / length, TensorPrimitives.Average<T>(x));
                }
            });
        }
        #endregion
    }
}
