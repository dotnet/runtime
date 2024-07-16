// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorTests
    {
        #region TensorPrimitivesForwardsTests
        private void FillTensor<T>(Span<T> span)
            where T : INumberBase<T>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = T.CreateChecked((Random.Shared.NextSingle() * 100) - 50);
            }
        }

        private static nint CalculateTotalLength(ReadOnlySpan<nint> lengths)
        {
            if (lengths.IsEmpty)
                return 0;
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }

            return totalLength;
        }

        public delegate void PerformCalculationSpanInSpanOut<T>(ReadOnlySpan<T> input, Span<T> output);

        public static IEnumerable<object[]> SpanInSpanOutData()
        {
            yield return Create<float>(TensorPrimitives.Abs<float>, Tensor.Abs);
            yield return Create<float>(TensorPrimitives.Acos, Tensor.Acos);
            yield return Create<float>(TensorPrimitives.Acosh, Tensor.Acosh);
            yield return Create<float>(TensorPrimitives.AcosPi, Tensor.AcosPi);
            yield return Create<float>(TensorPrimitives.Asin, Tensor.Asin);
            yield return Create<float>(TensorPrimitives.Asinh, Tensor.Asinh);
            yield return Create<float>(TensorPrimitives.AsinPi, Tensor.AsinPi);
            yield return Create<float>(TensorPrimitives.Atan, Tensor.Atan);
            yield return Create<float>(TensorPrimitives.Atanh, Tensor.Atanh);
            yield return Create<float>(TensorPrimitives.AtanPi, Tensor.AtanPi);
            yield return Create<float>(TensorPrimitives.Cbrt, Tensor.Cbrt);
            yield return Create<float>(TensorPrimitives.Ceiling, Tensor.Ceiling);
            yield return Create<float>(TensorPrimitives.Cos, Tensor.Cos);
            yield return Create<float>(TensorPrimitives.Cosh, Tensor.Cosh);
            yield return Create<float>(TensorPrimitives.CosPi, Tensor.CosPi);
            yield return Create<float>(TensorPrimitives.DegreesToRadians, Tensor.DegreesToRadians);
            yield return Create<float>(TensorPrimitives.Exp, Tensor.Exp);
            yield return Create<float>(TensorPrimitives.Exp10, Tensor.Exp10);
            yield return Create<float>(TensorPrimitives.Exp10M1, Tensor.Exp10M1);
            yield return Create<float>(TensorPrimitives.Exp2, Tensor.Exp2);
            yield return Create<float>(TensorPrimitives.Exp2M1, Tensor.Exp2M1);
            yield return Create<float>(TensorPrimitives.ExpM1, Tensor.ExpM1);
            yield return Create<float>(TensorPrimitives.Floor, Tensor.Floor);
            yield return Create<int>(TensorPrimitives.LeadingZeroCount, Tensor.LeadingZeroCount);
            yield return Create<int>(TensorPrimitives.LeadingZeroCount, Tensor.LeadingZeroCount);
            yield return Create<float>(TensorPrimitives.Log, Tensor.Log);
            yield return Create<float>(TensorPrimitives.Log10, Tensor.Log10);
            yield return Create<float>(TensorPrimitives.Log10P1, Tensor.Log10P1);
            yield return Create<float>(TensorPrimitives.Log2, Tensor.Log2);
            yield return Create<float>(TensorPrimitives.Log2P1, Tensor.Log2P1);
            yield return Create<float>(TensorPrimitives.LogP1, Tensor.LogP1);
            yield return Create<float>(TensorPrimitives.Negate, Tensor.Negate);
            yield return Create<float>(TensorPrimitives.OnesComplement, Tensor.OnesComplement);
            yield return Create<int>(TensorPrimitives.PopCount, Tensor.PopCount);
            yield return Create<float>(TensorPrimitives.RadiansToDegrees, Tensor.RadiansToDegrees);
            yield return Create<float>(TensorPrimitives.Reciprocal, Tensor.Reciprocal);
            yield return Create<float>(TensorPrimitives.Round, Tensor.Round);
            yield return Create<float>(TensorPrimitives.Sigmoid, Tensor.Sigmoid);
            yield return Create<float>(TensorPrimitives.Sin, Tensor.Sin);
            yield return Create<float>(TensorPrimitives.Sinh, Tensor.Sinh);
            yield return Create<float>(TensorPrimitives.SinPi, Tensor.SinPi);
            yield return Create<float>(TensorPrimitives.SoftMax, Tensor.SoftMax);
            yield return Create<float>(TensorPrimitives.Sqrt, Tensor.Sqrt);
            yield return Create<float>(TensorPrimitives.Tan, Tensor.Tan);
            yield return Create<float>(TensorPrimitives.Tanh, Tensor.Tanh);
            yield return Create<float>(TensorPrimitives.TanPi, Tensor.TanPi);
            yield return Create<float>(TensorPrimitives.Truncate, Tensor.Truncate);

            static object[] Create<T>(PerformCalculationSpanInSpanOut<T> tensorPrimitivesMethod, Func<Tensor<T>, Tensor<T>> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(SpanInSpanOutData))]
        public void TensorExtensionsSpanInSpanOut<T>(PerformCalculationSpanInSpanOut<T> tensorPrimitivesOperation, Func<Tensor<T>, Tensor<T>> tensorOperation)
            where T: INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data = new T[length];
                T[] expectedOutput = new T[length];

                FillTensor<T>(data);
                Tensor<T> x = Tensor.Create<T>(data, tensorLength, []);
                tensorPrimitivesOperation((ReadOnlySpan<T>)data, expectedOutput);
                Tensor<T> results = tensorOperation(x);

                Assert.Equal(tensorLength, results.Lengths);
                nint[] startingIndex = new nint[tensorLength.Length];
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref results[startingIndex], (int)length);

                for (int i = 0; i < data.Length; i++)
                {
                    Assert.Equal(expectedOutput[i], span[i]);
                }
            });
        }

        public delegate T PerformCalculationSpanInTOut<T>(ReadOnlySpan<T> input);
        public static IEnumerable<object[]> SpanInFloatOutData()
        {
            yield return Create<float>(TensorPrimitives.Max, Tensor.Max);
            yield return Create<float>(TensorPrimitives.MaxMagnitude, Tensor.MaxMagnitude);
            yield return Create<float>(TensorPrimitives.MaxNumber, Tensor.MaxNumber);
            yield return Create<float>(TensorPrimitives.Min, Tensor.Min);
            yield return Create<float>(TensorPrimitives.MinMagnitude, Tensor.MinMagnitude);
            yield return Create<float>(TensorPrimitives.MinNumber, Tensor.MinNumber);
            yield return Create<float>(TensorPrimitives.Norm, Tensor.Norm);
            yield return Create<float>(TensorPrimitives.Product, Tensor.Product);
            yield return Create<float>(TensorPrimitives.Sum, Tensor.Sum);

            static object[] Create<T>(PerformCalculationSpanInTOut<T> tensorPrimitivesMethod, Func<Tensor<T>, T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(SpanInFloatOutData))]
        public void TensorExtensionsSpanInTOut<T>(PerformCalculationSpanInTOut<T> tensorPrimitivesOperation, Func<Tensor<T>, T> tensorOperation)
            where T : INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data = new T[length];

                FillTensor<T>(data);
                Tensor<T> x = Tensor.Create<T>(data, tensorLength, []);
                T expectedOutput = tensorPrimitivesOperation((ReadOnlySpan<T>)data);
                T results = tensorOperation(x);

                Assert.Equal(expectedOutput, results);
            });
        }

        public delegate void PerformCalculationTwoSpanInSpanOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output);
        public static IEnumerable<object[]> TwoSpanInSpanOutData()
        {
            yield return Create<float>(TensorPrimitives.Add, Tensor.Add);
            yield return Create<float>(TensorPrimitives.Atan2, Tensor.Atan2);
            yield return Create<float>(TensorPrimitives.Atan2Pi, Tensor.Atan2Pi);
            yield return Create<float>(TensorPrimitives.CopySign, Tensor.CopySign);
            yield return Create<float>(TensorPrimitives.Divide, Tensor.Divide);
            yield return Create<float>(TensorPrimitives.Hypot, Tensor.Hypot);
            yield return Create<float>(TensorPrimitives.Ieee754Remainder, Tensor.Ieee754Remainder);
            yield return Create<float>(TensorPrimitives.Multiply, Tensor.Multiply);
            yield return Create<float>(TensorPrimitives.Pow, Tensor.Pow);
            yield return Create<float>(TensorPrimitives.Subtract, Tensor.Subtract);

            static object[] Create<T>(PerformCalculationTwoSpanInSpanOut<T> tensorPrimitivesMethod, Func<Tensor<T>, Tensor<T>, Tensor<T>> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInSpanOutData))]
        public void TensorExtensionsTwoSpanInSpanOut<T>(PerformCalculationTwoSpanInSpanOut<T> tensorPrimitivesOperation, Func<Tensor<T>, Tensor<T>, Tensor<T>> tensorOperation)
            where T: INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data1 = new T[length];
                T[] data2 = new T[length];
                T[] expectedOutput = new T[length];

                FillTensor<T>(data1);
                FillTensor<T>(data2);
                Tensor<T> x = Tensor.Create<T>(data1, tensorLength, []);
                Tensor<T> y = Tensor.Create<T>(data2, tensorLength, []);
                tensorPrimitivesOperation((ReadOnlySpan<T>)data1, data2, expectedOutput);
                Tensor<T> results = tensorOperation(x, y);

                Assert.Equal(tensorLength, results.Lengths);
                nint[] startingIndex = new nint[tensorLength.Length];
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref results[startingIndex], (int)length);

                for (int i = 0; i < data1.Length; i++)
                {
                    Assert.Equal(expectedOutput[i], span[i]);
                }
            });
        }

        public delegate T PerformCalculationTwoSpanInFloatOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo);
        public static IEnumerable<object[]> TwoSpanInFloatOutData()
        {
            yield return Create<float>(TensorPrimitives.Distance, Tensor.Distance);
            yield return Create<float>(TensorPrimitives.Dot, Tensor.Dot);

            static object[] Create<T>(PerformCalculationTwoSpanInFloatOut<T> tensorPrimitivesMethod, Func<Tensor<T>, Tensor<T>, T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInFloatOutData))]
        public void TensorExtensionsTwoSpanInFloatOut<T>(PerformCalculationTwoSpanInFloatOut<T> tensorPrimitivesOperation, Func<Tensor<T>, Tensor<T>, T> tensorOperation)
            where T: INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data1 = new T[length];
                T[] data2 = new T[length];

                FillTensor<T>(data1);
                FillTensor<T>(data2);
                Tensor<T> x = Tensor.Create<T>(data1, tensorLength, []);
                Tensor<T> y = Tensor.Create<T>(data2, tensorLength, []);
                T expectedOutput = tensorPrimitivesOperation((ReadOnlySpan<T>)data1, data2);
                T results = tensorOperation(x, y);

                Assert.Equal(expectedOutput, results);
            });
        }

        #endregion

        [Fact]
        public static void TensorLargeDimensionsTests()
        {
            int[] a = { 91, 92, -93, 94, 95, -96 };
            int[] results = new int[6];
            Tensor<int> tensor = Tensor.Create<int>(a,[1, 1, 1, 1, 1, 6]);
            Assert.Equal(6, tensor.Rank);

            Assert.Equal(6, tensor.Lengths.Length);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);
            Assert.Equal(1, tensor.Lengths[3]);
            Assert.Equal(1, tensor.Lengths[4]);
            Assert.Equal(6, tensor.Lengths[5]);
            Assert.Equal(6, tensor.Strides.Length);
            Assert.Equal(6, tensor.Strides[0]);
            Assert.Equal(6, tensor.Strides[1]);
            Assert.Equal(6, tensor.Strides[2]);
            Assert.Equal(6, tensor.Strides[3]);
            Assert.Equal(6, tensor.Strides[4]);
            Assert.Equal(1, tensor.Strides[5]);
            Assert.Equal(91, tensor[0, 0, 0, 0, 0, 0]);
            Assert.Equal(92, tensor[0, 0, 0, 0, 0, 1]);
            Assert.Equal(-93, tensor[0, 0, 0, 0, 0, 2]);
            Assert.Equal(94, tensor[0, 0, 0, 0, 0, 3]);
            Assert.Equal(95, tensor[0, 0, 0, 0, 0, 4]);
            Assert.Equal(-96, tensor[0, 0, 0, 0, 0, 5]);
            tensor.FlattenTo(results);
            Assert.Equal(a, results);

            a = [91, 92, -93, 94, 95, -96, -91, -92, 93, -94, -95, 96];
            results = new int[12];
            tensor = Tensor.Create<int>(a, [1, 2, 2, 1, 1, 3]);
            Assert.Equal(6, tensor.Lengths.Length);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Lengths[2]);
            Assert.Equal(1, tensor.Lengths[3]);
            Assert.Equal(1, tensor.Lengths[4]);
            Assert.Equal(3, tensor.Lengths[5]);
            Assert.Equal(6, tensor.Strides.Length);
            Assert.Equal(12, tensor.Strides[0]);
            Assert.Equal(6, tensor.Strides[1]);
            Assert.Equal(3, tensor.Strides[2]);
            Assert.Equal(3, tensor.Strides[3]);
            Assert.Equal(3, tensor.Strides[4]);
            Assert.Equal(1, tensor.Strides[5]);
            Assert.Equal(91, tensor[0, 0, 0, 0, 0, 0]);
            Assert.Equal(92, tensor[0, 0, 0, 0, 0, 1]);
            Assert.Equal(-93, tensor[0, 0, 0, 0, 0, 2]);
            Assert.Equal(94, tensor[0, 0, 1, 0, 0, 0]);
            Assert.Equal(95, tensor[0, 0, 1, 0, 0, 1]);
            Assert.Equal(-96, tensor[0, 0, 1, 0, 0, 2]);
            Assert.Equal(-91, tensor[0, 1, 0, 0, 0, 0]);
            Assert.Equal(-92, tensor[0, 1, 0, 0, 0, 1]);
            Assert.Equal(93, tensor[0, 1, 0, 0, 0, 2]);
            Assert.Equal(-94, tensor[0, 1, 1, 0, 0, 0]);
            Assert.Equal(-95, tensor[0, 1, 1, 0, 0, 1]);
            Assert.Equal(96, tensor[0, 1, 1, 0, 0, 2]);
            tensor.FlattenTo(results);
            Assert.Equal(a, results);
        }

        [Fact]
        public static void TensorFactoryCreateUninitializedTests()
        {
            // Basic tensor creation
            Tensor<int> t1 = Tensor.CreateUninitialized<int>([1]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(1, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Make sure can't index too many dimensions
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[1, 1];
            });

            // Make sure can't index beyond end
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[1];
            });

            // Make sure can't index negative index
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[-1];
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Tensor<int> t1 = Tensor.CreateUninitialized<int>([-1]);
            });

            t1 = Tensor.CreateUninitialized<int>([0]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            t1 = Tensor.CreateUninitialized<int>([]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Null should behave like empty array since there is no "null" span.
            t1 = Tensor.CreateUninitialized<int>(null);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Make sure pinned works
            t1 = Tensor.CreateUninitialized<int>([1], true);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(1, t1.Strides[0]);
            Assert.True(t1.IsPinned);

            // Make sure 2D array works with basic strides
            t1 = Tensor.CreateUninitialized<int>([2, 2], [2, 1]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            // Can't validate actual values since it's uninitialized
            // So by checking the type we assert no errors were thrown
            Assert.IsType<int>(t1[0, 0]);
            Assert.IsType<int>(t1[0, 1]);
            Assert.IsType<int>(t1[1, 0]);
            Assert.IsType<int>(t1[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            t1 = Tensor.CreateUninitialized<int>([2, 2], [0, 1]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            // Can't validate actual values since it's uninitialized
            // But since it loops over the first 2 elements we can assert the results are the same for those.
            Assert.Equal(t1[0, 0], t1[1, 0]);
            Assert.Equal(t1[0, 1], t1[1, 1]);

            // Make sure 2D array works with strides of all 0 to loop over first element again
            t1 = Tensor.CreateUninitialized<int>([2, 2], [0, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            // Can't validate actual values since it's uninitialized
            // But since it loops over the first element we can assert the results are the same.
            Assert.Equal(t1[0, 0], t1[0, 1]);
            Assert.Equal(t1[0, 0], t1[1, 0]);
            Assert.Equal(t1[0, 0], t1[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateUninitialized<int>([1, 2], [-1, 0], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateUninitialized<int>([1, 2], [0, -1], false);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateUninitialized<int>([-1, 2], [], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateUninitialized<int>([1, -2], [], false);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            t1 = Tensor.CreateUninitialized<int>([2, 2], [2, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(t1[0, 0], t1[0, 1]);
            Assert.Equal(t1[1, 0], t1[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateUninitialized<int>([2, 2], [1, 1], false);
            });
        }

        [Fact]
        public static void TensorFactoryCreateTests()
        {
            // Basic tensor creation
            Tensor<int> t1 = Tensor.Create<int>((ReadOnlySpan<nint>)([1]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(1, t1.Strides[0]);
            Assert.False(t1.IsPinned);
            Assert.Equal(0, t1[0]);

            // Make sure can't index too many dimensions
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[1, 1];
            });

            // Make sure can't index beyond end
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[1];
            });

            // Make sure can't index negative index
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var x = t1[-1];
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Tensor<int> t1 = Tensor.Create<int>((ReadOnlySpan<nint>)([-1]));
            });

            t1 = Tensor.Create<int>((ReadOnlySpan<nint>)([0]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            t1 = Tensor.Create<int>((ReadOnlySpan<nint>)([]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Null should behave like empty array since there is no "null" span.
            t1 = Tensor.Create<int>(null);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Make sure pinned works
            t1 = Tensor.Create<int>([1], true);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(1, t1.Strides[0]);
            Assert.True(t1.IsPinned);
            Assert.Equal(0, t1[0]);

            int[] a = [91, 92, -93, 94];
            // Make sure 2D array works with basic strides
            t1 = Tensor.Create<int>(a, [2, 2], [2, 1]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(91, t1[0, 0]);
            Assert.Equal(92, t1[0, 1]);
            Assert.Equal(-93, t1[1, 0]);
            Assert.Equal(94, t1[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            t1 = Tensor.Create<int>(a, [2, 2], [0, 1]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(91, t1[0, 0]);
            Assert.Equal(92, t1[0, 1]);
            Assert.Equal(91, t1[1, 0]);
            Assert.Equal(92, t1[1, 1]);

            // Make sure 2D array works with strides of all 0 to loop over first element again
            t1 = Tensor.Create<int>(a, [2, 2], [0, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(91, t1[0, 0]);
            Assert.Equal(91, t1[0, 1]);
            Assert.Equal(91, t1[1, 0]);
            Assert.Equal(91, t1[1, 1]);

            // Make sure 2D array works with strides of all 0 only 1 element to make sure it doesn't leave that element
            t1 = Tensor.Create<int>([a[3]], [2, 2], [0, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(94, t1[0, 0]);
            Assert.Equal(94, t1[0, 1]);
            Assert.Equal(94, t1[1, 0]);
            Assert.Equal(94, t1[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.Create<int>([1, 2], [-1, 0], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.Create<int>([1, 2], [0, -1], false);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.Create<int>([-1, 2], [], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.Create<int>([1, -2], [], false);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            t1 = Tensor.Create<int>(a, [2, 2], [2, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(91, t1[0, 0]);
            Assert.Equal(91, t1[0, 1]);
            Assert.Equal(-93, t1[1, 0]);
            Assert.Equal(-93, t1[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.Create<int>([2, 2], [1, 1], false);
            });
        }
        
        [Fact]
        public static void TensorCosineSimilarityTests()
        {
            float[] a = [0, 0, 0, 1, 1, 1];
            float[] b = [1, 0, 0, 1, 1, 0];

            Tensor<float> left = Tensor.Create<float>(a, [2,3]);
            Tensor<float> right = Tensor.Create<float>(b, [2,3]);

            Tensor<float> result = Tensor.CosineSimilarity(left, right);
            Assert.Equal(2, result.Rank);
            Assert.Equal(2, result.Lengths[0]);
            Assert.Equal(2, result.Lengths[1]);


            Assert.Equal(float.NaN, result[0, 0]);
            Assert.Equal(float.NaN, result[0, 1]);

            Assert.Equal(0.57735, result[1, 0], .00001);
            Assert.Equal(0.81649, result[1, 1], .00001);
        }

        //[Fact]
        //public static void TensorSequenceEqualTests()
        //{
        //    Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    Tensor<bool> equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([3], equal.Lengths.ToArray());
        //    Assert.True(equal[0]);
        //    Assert.True(equal[1]);
        //    Assert.True(equal[2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3), [1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.True(equal[0, 1]);
        //    Assert.True(equal[0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3), [1, 1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0, 0]);
        //    Assert.True(equal[0, 0, 1]);
        //    Assert.True(equal[0, 0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), [1, 3]);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.True(equal[0, 1]);
        //    Assert.True(equal[0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), [3, 1]);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([3, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.False(equal[0, 1]);
        //    Assert.False(equal[0, 2]);
        //    Assert.False(equal[1, 0]);
        //    Assert.True(equal[1, 1]);
        //    Assert.False(equal[1, 2]);
        //    Assert.False(equal[2, 0]);
        //    Assert.False(equal[2, 1]);
        //    Assert.True(equal[2, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3), [1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), [3, 1]);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([3, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.False(equal[0, 1]);
        //    Assert.False(equal[0, 2]);
        //    Assert.False(equal[1, 0]);
        //    Assert.True(equal[1, 1]);
        //    Assert.False(equal[1, 2]);
        //    Assert.False(equal[2, 0]);
        //    Assert.False(equal[2, 1]);
        //    Assert.True(equal[2, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 4), default);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3), default);
        //    Assert.Throws<Exception>(() => Tensor.SequenceEqual(t0, t1));
        //}

        [Fact]
        public static void TensorMultiplyTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3), default);
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 3), [3, 1]);
            Tensor<int> t2 = Tensor.Multiply(t0, t1);

            Assert.Equal([3,3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            t2 = Tensor.Multiply(t1, t0);

            Assert.Equal([3, 3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            t1 = Tensor.Create(Enumerable.Range(0, 9), [3, 3]);
            t2 = Tensor.Multiply(t0, t1);

            Assert.Equal([3, 3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(4, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(4, t2[1, 1]);
            Assert.Equal(10, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(7, t2[2, 1]);
            Assert.Equal(16, t2[2, 2]);
        }

        [Fact]
        public static void TensorBroadcastTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3), [1, 3, 1, 1, 1]);
            Tensor<int> t1 = Tensor.Broadcast<int>(t0, [1, 3, 1, 2, 1]);

            Assert.Equal([1, 3, 1, 2, 1], t1.Lengths.ToArray());

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 1, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 1, 0]);

            t1 = Tensor.Broadcast<int>(t0, [1, 3, 2, 1, 1]);
            Assert.Equal([1, 3, 2, 1, 1], t1.Lengths.ToArray());

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 1, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 1, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 1, 0, 0]);

            t0 = Tensor.Create(Enumerable.Range(0, 3), [1, 3]);
            t1 = Tensor.Create(Enumerable.Range(0, 3), [3, 1]);
            var t2 = Tensor.Broadcast<int>(t0, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(2, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(1, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);

            t1 = Tensor.Create(Enumerable.Range(0, 3), [3, 1]);
            t2 = Tensor.Broadcast<int>(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(1, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(1, t2[1, 2]);
            Assert.Equal(2, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);

            var s1 = t2.AsTensorSpan();
            Assert.Equal(0, s1[0, 0]);
            Assert.Equal(0, s1[0, 1]);
            Assert.Equal(0, s1[0, 2]);
            Assert.Equal(1, s1[1, 0]);
            Assert.Equal(1, s1[1, 1]);
            Assert.Equal(1, s1[1, 2]);
            Assert.Equal(2, s1[2, 0]);
            Assert.Equal(2, s1[2, 1]);
            Assert.Equal(2, s1[2, 2]);

            var t3 = t2.Slice(0..1, ..);
            Assert.Equal([1, 3], t3.Lengths.ToArray());

            t1 = Tensor.Create(Enumerable.Range(0, 3), default);
            t2 = Tensor.Broadcast<int>(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(2, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(1, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);
        }

        [Fact]
        public static void TensorResizeTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8), [2, 2, 2]);
            var t1 = Tensor.Resize(t0, [1]);
            Assert.Equal([1], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);

            t1 = Tensor.Resize(t0, [1, 1]);
            Assert.Equal([1, 1], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0, 0]);

            t1 = Tensor.Resize(t0, [6]);
            Assert.Equal([6], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);
            Assert.Equal(1, t1[1]);
            Assert.Equal(2, t1[2]);
            Assert.Equal(3, t1[3]);
            Assert.Equal(4, t1[4]);
            Assert.Equal(5, t1[5]);

            t1 = Tensor.Resize(t0, [10]);
            Assert.Equal([10], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);
            Assert.Equal(1, t1[1]);
            Assert.Equal(2, t1[2]);
            Assert.Equal(3, t1[3]);
            Assert.Equal(4, t1[4]);
            Assert.Equal(5, t1[5]);
            Assert.Equal(6, t1[6]);
            Assert.Equal(7, t1[7]);
            Assert.Equal(0, t1[8]);
            Assert.Equal(0, t1[9]);

            t1 = Tensor.Resize(t0, [2, 5]);
            Assert.Equal([2, 5], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(1, t1[0, 1]);
            Assert.Equal(2, t1[0, 2]);
            Assert.Equal(3, t1[0, 3]);
            Assert.Equal(4, t1[0, 4]);
            Assert.Equal(5, t1[1, 0]);
            Assert.Equal(6, t1[1, 1]);
            Assert.Equal(7, t1[1, 2]);
            Assert.Equal(0, t1[1, 3]);
            Assert.Equal(0, t1[1, 4]);
        }

        [Fact]
        public static void TensorSplitTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8), [2, 2, 2]);
            var t1 = Tensor.Split<int>(t0, 2, 0);
            Assert.Equal([1, 2, 2], t1[0].Lengths.ToArray());
            Assert.Equal([1, 2, 2], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(3, t1[0][0, 1, 1]);
            Assert.Equal(4, t1[1][0, 0, 0]);
            Assert.Equal(5, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][0, 1, 0]);
            Assert.Equal(7, t1[1][0, 1, 1]);

            t1 = Tensor.Split<int>(t0, 2, 1);
            Assert.Equal([2, 1, 2], t1[0].Lengths.ToArray());
            Assert.Equal([2, 1, 2], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(5, t1[0][1, 0, 1]);
            Assert.Equal(2, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 0, 1]);

            t1 = Tensor.Split<int>(t0, 2, 2);
            Assert.Equal([2, 2, 1], t1[0].Lengths.ToArray());
            Assert.Equal([2, 2, 1], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(6, t1[0][1, 1, 0]);
            Assert.Equal(1, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 1, 0]);
            Assert.Equal(5, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 1, 0]);
        }

        [Fact]
        public static void TensorReverseTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8), [2, 2, 2]);
            var t1 = Tensor.Reverse<int>(t0);
            Assert.Equal(7, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(5, t1[0, 1, 0]);
            Assert.Equal(4, t1[0, 1, 1]);
            Assert.Equal(3, t1[1, 0, 0]);
            Assert.Equal(2, t1[1, 0, 1]);
            Assert.Equal(1, t1[1, 1, 0]);
            Assert.Equal(0, t1[1, 1, 1]);

            t1 = Tensor.Reverse<int>(t0, 0);
            Assert.Equal(4, t1[0, 0, 0]);
            Assert.Equal(5, t1[0, 0, 1]);
            Assert.Equal(6, t1[0, 1, 0]);
            Assert.Equal(7, t1[0, 1, 1]);
            Assert.Equal(0, t1[1, 0, 0]);
            Assert.Equal(1, t1[1, 0, 1]);
            Assert.Equal(2, t1[1, 1, 0]);
            Assert.Equal(3, t1[1, 1, 1]);

            t1 = Tensor.Reverse<int>(t0, 1);
            Assert.Equal(2, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 0, 1]);
            Assert.Equal(0, t1[0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 1]);
            Assert.Equal(6, t1[1, 0, 0]);
            Assert.Equal(7, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(5, t1[1, 1, 1]);

            t1 = Tensor.Reverse<int>(t0, 2);
            Assert.Equal(1, t1[0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 1]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(2, t1[0, 1, 1]);
            Assert.Equal(5, t1[1, 0, 0]);
            Assert.Equal(4, t1[1, 0, 1]);
            Assert.Equal(7, t1[1, 1, 0]);
            Assert.Equal(6, t1[1, 1, 1]);
        }

        [Fact]
        public static void TensorSetSliceTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 10), [2, 5]);
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(10, 10), [2, 5]);
            Tensor.SetSlice(t0, t1);

            Assert.Equal(10, t0[0, 0]);
            Assert.Equal(11, t0[0, 1]);
            Assert.Equal(12, t0[0, 2]);
            Assert.Equal(13, t0[0, 3]);
            Assert.Equal(14, t0[0, 4]);
            Assert.Equal(15, t0[1, 0]);
            Assert.Equal(16, t0[1, 1]);
            Assert.Equal(17, t0[1, 2]);
            Assert.Equal(18, t0[1, 3]);
            Assert.Equal(19, t0[1, 4]);

            t0 = Tensor.Create(Enumerable.Range(0, 10), [2, 5]);
            t1 = Tensor.Create(Enumerable.Range(10, 5), [1, 5]);
            t0.SetSlice(t1, 0..1, ..);

            Assert.Equal(10, t0[0, 0]);
            Assert.Equal(11, t0[0, 1]);
            Assert.Equal(12, t0[0, 2]);
            Assert.Equal(13, t0[0, 3]);
            Assert.Equal(14, t0[0, 4]);
            Assert.Equal(5, t0[1, 0]);
            Assert.Equal(6, t0[1, 1]);
            Assert.Equal(7, t0[1, 2]);
            Assert.Equal(8, t0[1, 3]);
            Assert.Equal(9, t0[1, 4]);

            t0 = Tensor.Create(Enumerable.Range(0, 10), [2, 5]);
            t1 = Tensor.Create(Enumerable.Range(10, 5), [1, 5]);
            Tensor.SetSlice(t0, t1, 1..2, ..);

            Assert.Equal(0, t0[0, 0]);
            Assert.Equal(1, t0[0, 1]);
            Assert.Equal(2, t0[0, 2]);
            Assert.Equal(3, t0[0, 3]);
            Assert.Equal(4, t0[0, 4]);
            Assert.Equal(10, t0[1, 0]);
            Assert.Equal(11, t0[1, 1]);
            Assert.Equal(12, t0[1, 2]);
            Assert.Equal(13, t0[1, 3]);
            Assert.Equal(14, t0[1, 4]);
        }
        [Fact]
        public static void TensorStackTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 10), [2, 5]);
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 10), [2, 5]);

            var resultTensor = Tensor.Stack([t0, t1]);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(5, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(1, resultTensor[0, 0, 1]);
            Assert.Equal(2, resultTensor[0, 0, 2]);
            Assert.Equal(3, resultTensor[0, 0, 3]);
            Assert.Equal(4, resultTensor[0, 0, 4]);
            Assert.Equal(5, resultTensor[0, 1, 0]);
            Assert.Equal(6, resultTensor[0, 1, 1]);
            Assert.Equal(7, resultTensor[0, 1, 2]);
            Assert.Equal(8, resultTensor[0, 1, 3]);
            Assert.Equal(9, resultTensor[0, 1, 4]);
            Assert.Equal(0, resultTensor[1, 0, 0]);
            Assert.Equal(1, resultTensor[1, 0, 1]);
            Assert.Equal(2, resultTensor[1, 0, 2]);
            Assert.Equal(3, resultTensor[1, 0, 3]);
            Assert.Equal(4, resultTensor[1, 0, 4]);
            Assert.Equal(5, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 1, 2]);
            Assert.Equal(8, resultTensor[1, 1, 3]);
            Assert.Equal(9, resultTensor[1, 1, 4]);

            resultTensor = Tensor.Stack([t0, t1], axis:1);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(5, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(1, resultTensor[0, 0, 1]);
            Assert.Equal(2, resultTensor[0, 0, 2]);
            Assert.Equal(3, resultTensor[0, 0, 3]);
            Assert.Equal(4, resultTensor[0, 0, 4]);
            Assert.Equal(0, resultTensor[0, 1, 0]);
            Assert.Equal(1, resultTensor[0, 1, 1]);
            Assert.Equal(2, resultTensor[0, 1, 2]);
            Assert.Equal(3, resultTensor[0, 1, 3]);
            Assert.Equal(4, resultTensor[0, 1, 4]);
            Assert.Equal(5, resultTensor[1, 0, 0]);
            Assert.Equal(6, resultTensor[1, 0, 1]);
            Assert.Equal(7, resultTensor[1, 0, 2]);
            Assert.Equal(8, resultTensor[1, 0, 3]);
            Assert.Equal(9, resultTensor[1, 0, 4]);
            Assert.Equal(5, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 1, 2]);
            Assert.Equal(8, resultTensor[1, 1, 3]);
            Assert.Equal(9, resultTensor[1, 1, 4]);

            resultTensor = Tensor.Stack([t0, t1], axis: 2);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(5, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(0, resultTensor[0, 0, 1]);
            Assert.Equal(1, resultTensor[0, 1, 0]);
            Assert.Equal(1, resultTensor[0, 1, 1]);
            Assert.Equal(2, resultTensor[0, 2, 0]);
            Assert.Equal(2, resultTensor[0, 2, 1]);
            Assert.Equal(3, resultTensor[0, 3, 0]);
            Assert.Equal(3, resultTensor[0, 3, 1]);
            Assert.Equal(4, resultTensor[0, 4, 0]);
            Assert.Equal(4, resultTensor[0, 4, 1]);
            Assert.Equal(5, resultTensor[1, 0, 0]);
            Assert.Equal(5, resultTensor[1, 0, 1]);
            Assert.Equal(6, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 2, 0]);
            Assert.Equal(7, resultTensor[1, 2, 1]);
            Assert.Equal(8, resultTensor[1, 3, 0]);
            Assert.Equal(8, resultTensor[1, 3, 1]);
            Assert.Equal(9, resultTensor[1, 4, 0]);
            Assert.Equal(9, resultTensor[1, 4, 1]);
        }

        [Fact]
        public static void TensorStdDevTests()
        {
            Tensor<float> t0 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);

            Assert.Equal(StdDev([0, 1, 2, 3]), Tensor.StdDev<float>(t0), .1);
        }

        public static float StdDev(float[] values)
        {
            float mean = Mean(values);
            float sum = 0;
            for(int i = 0; i < values.Length; i++)
            {
                sum += MathF.Pow(values[i] - mean, 2);
            }
            return sum / values.Length;
        }

        [Fact]
        public static void TensorMeanTests()
        {
            Tensor<float> t0 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);

            Assert.Equal(Mean([0, 1, 2, 3]), Tensor.Mean<float>(t0), .1);
        }

        public static float Mean(float[] values)
        {
            float sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum/values.Length;
        }

        [Fact]
        public static void TensorConcatenateTests()
        {
            Tensor<float> t0 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);
            Tensor<float> t1 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);
            var resultTensor = Tensor.Concatenate([t0, t1]);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(4, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(0, resultTensor[2, 0]);
            Assert.Equal(1, resultTensor[2, 1]);
            Assert.Equal(2, resultTensor[3, 0]);
            Assert.Equal(3, resultTensor[3, 1]);

            resultTensor = Tensor.Concatenate([t0, t1], 1);
            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(4, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(0, resultTensor[0, 2]);
            Assert.Equal(1, resultTensor[0, 3]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(2, resultTensor[1, 2]);
            Assert.Equal(3, resultTensor[1, 3]);

            resultTensor = Tensor.Concatenate([t0, t1], -1);
            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(8, resultTensor.Lengths[0]);
            Assert.Equal(0, resultTensor[0]);
            Assert.Equal(1, resultTensor[1]);
            Assert.Equal(2, resultTensor[2]);
            Assert.Equal(3, resultTensor[3]);
            Assert.Equal(0, resultTensor[4]);
            Assert.Equal(1, resultTensor[5]);
            Assert.Equal(2, resultTensor[6]);
            Assert.Equal(3, resultTensor[7]);

            Tensor<float> t2 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);
            resultTensor = Tensor.Concatenate([t0, t1, t2]);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(6, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(0, resultTensor[2, 0]);
            Assert.Equal(1, resultTensor[2, 1]);
            Assert.Equal(2, resultTensor[3, 0]);
            Assert.Equal(3, resultTensor[3, 1]);
            Assert.Equal(0, resultTensor[4, 0]);
            Assert.Equal(1, resultTensor[4, 1]);
            Assert.Equal(2, resultTensor[5, 0]);
            Assert.Equal(3, resultTensor[5, 1]);

            resultTensor = Tensor.Concatenate([t0, t1, t2], -1);

            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(12, resultTensor.Lengths[0]);
            Assert.Equal(0, resultTensor[0]);
            Assert.Equal(1, resultTensor[1]);
            Assert.Equal(2, resultTensor[2]);
            Assert.Equal(3, resultTensor[3]);
            Assert.Equal(0, resultTensor[4]);
            Assert.Equal(1, resultTensor[5]);
            Assert.Equal(2, resultTensor[6]);
            Assert.Equal(3, resultTensor[7]);
            Assert.Equal(0, resultTensor[8]);
            Assert.Equal(1, resultTensor[9]);
            Assert.Equal(2, resultTensor[10]);
            Assert.Equal(3, resultTensor[11]);

            resultTensor = Tensor.Concatenate([t0, t1, t2], 1);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(6, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(0, resultTensor[0, 2]);
            Assert.Equal(1, resultTensor[0, 3]);
            Assert.Equal(0, resultTensor[0, 4]);
            Assert.Equal(1, resultTensor[0, 5]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(2, resultTensor[1, 2]);
            Assert.Equal(3, resultTensor[1, 3]);
            Assert.Equal(2, resultTensor[1, 4]);
            Assert.Equal(3, resultTensor[1, 5]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 3, 2]);
            t1 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 3, 2]);
            t2 = Tensor.Create<float>((Enumerable.Range(0, 8).Select(i => (float)i)), [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 2));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 5));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], -2));
            resultTensor = Tensor.Concatenate([t0, t1, t2], -1);
            float[] result = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7];
            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(32, resultTensor.Lengths[0]);
            Assert.Equal(result, resultTensor.ToArray());

            resultTensor = Tensor.Concatenate([t0, t1, t2], 1);
            result = [0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 6, 7, 8, 9, 10, 11, 6, 7, 8, 9, 10, 11, 4, 5, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(8, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            nint[] indices = new nint[resultTensor.Rank];
            for(int i  = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 2, 3]);
            t1 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 2, 3]);
            t2 = Tensor.Create<float>((Enumerable.Range(0, 8).Select(i => (float)i)), [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 0));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 1));
            resultTensor = Tensor.Concatenate([t0, t1, t2], 2);
            result = [0, 1, 2, 0, 1, 2, 0, 1, 3, 4, 5, 3, 4, 5, 2, 3, 6, 7, 8, 6, 7, 8, 4, 5, 9, 10, 11, 9, 10, 11, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(8, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            indices = new nint[resultTensor.Rank];
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [3, 2, 2]);
            t1 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [3, 2, 2]);
            t2 = Tensor.Create<float>((Enumerable.Range(0, 8).Select(i => (float)i)), [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 1));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 2));
            resultTensor = Tensor.Concatenate([t0, t1, t2]);
            result = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(8, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            indices = new nint[resultTensor.Rank];
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }
        }

        [Fact]
        public static void TensorTransposeTests()
        {
            Tensor<float> t0 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);
            var t1 = Tensor.Permute(t0);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 6).Select(i => (float)i)), [2, 3]);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(3, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(4, t1[1, 1]);
            Assert.Equal(2, t1[2, 0]);
            Assert.Equal(5, t1[2, 1]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 6).Select(i => (float)i)), [1, 2, 3]);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(1, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(1, t1[1, 0, 0]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(2, t1[2, 0, 0]);
            Assert.Equal(5, t1[2, 1, 0]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 2, 3]);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(9, t1[0, 1, 1]);
            Assert.Equal(1, t1[1, 0, 0]);
            Assert.Equal(7, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(2, t1[2, 0, 0]);
            Assert.Equal(8, t1[2, 0, 1]);
            Assert.Equal(5, t1[2, 1, 0]);
            Assert.Equal(11, t1[2, 1, 1]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 2, 3]);
            t1 = Tensor.Permute(t0, 1, 2, 0);

            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(3, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(1, t1[0, 1, 0]);
            Assert.Equal(7, t1[0, 1, 1]);
            Assert.Equal(2, t1[0, 2, 0]);
            Assert.Equal(8, t1[0, 2, 1]);
            Assert.Equal(3, t1[1, 0, 0]);
            Assert.Equal(9, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(5, t1[1, 2, 0]);
            Assert.Equal(11, t1[1, 2, 1]);
        }

        [Fact]
        public static void TensorPermuteTests()
        {
            Tensor<float> t0 = Tensor.Create<float>((Enumerable.Range(0, 4).Select(i => (float)i)), [2, 2]);
            var t1 = Tensor.Transpose(t0);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.Create<float>((Enumerable.Range(0, 12).Select(i => (float)i)), [2, 2, 3]);
            t1 = Tensor.Transpose(t0);

            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(3, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 0, 1]);
            Assert.Equal(1, t1[0, 1, 0]);
            Assert.Equal(4, t1[0, 1, 1]);
            Assert.Equal(2, t1[0, 2, 0]);
            Assert.Equal(5, t1[0, 2, 1]);
            Assert.Equal(6, t1[1, 0, 0]);
            Assert.Equal(9, t1[1, 0, 1]);
            Assert.Equal(7, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(8, t1[1, 2, 0]);
            Assert.Equal(11, t1[1, 2, 1]);
        }

        [Fact]
        public static void IntArrayAsTensor()
        {
            int[] a = [91, 92, -93, 94];
            TensorSpan<int> t1 = a.AsTensorSpan(4);
            nint[] dims = [4];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            t1.CopyTo(tensor);
            Assert.Equal(1, tensor.Rank);

            Assert.Equal(1, tensor.Lengths.Length);
            Assert.Equal(4, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Strides.Length);
            Assert.Equal(1, tensor.Strides[0]);
            Assert.Equal(91, tensor[0]);
            Assert.Equal(92, tensor[1]);
            Assert.Equal(-93, tensor[2]);
            Assert.Equal(94, tensor[3]);
            Assert.Equal(a, tensor.ToArray());
            tensor[0] = 100;
            tensor[1] = 101;
            tensor[2] = -102;
            tensor[3] = 103;

            Assert.Equal(100, tensor[0]);
            Assert.Equal(101, tensor[1]);
            Assert.Equal(-102, tensor[2]);
            Assert.Equal(103, tensor[3]);

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            t1 = a.AsTensorSpan(2, 2);
            dims = [2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            t1.CopyTo(tensor);
            Assert.Equal(a, tensor.ToArray());
            Assert.Equal(2, tensor.Rank);
            //Assert.Equal(4, t1.FlattenedLength);
            Assert.Equal(2, tensor.Lengths.Length);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Strides.Length);
            Assert.Equal(2, tensor.Strides[0]);
            Assert.Equal(1, tensor.Strides[1]);
            Assert.Equal(91, tensor[0, 0]);
            Assert.Equal(92, tensor[0, 1]);
            Assert.Equal(-93, tensor[1, 0]);
            Assert.Equal(94, tensor[1, 1]);

            tensor[0, 0] = 100;
            tensor[0, 1] = 101;
            tensor[1, 0] = -102;
            tensor[1, 1] = 103;

            Assert.Equal(100, tensor[0, 0]);
            Assert.Equal(101, tensor[0, 1]);
            Assert.Equal(-102, tensor[1, 0]);
            Assert.Equal(103, tensor[1, 1]);
        }

        [Fact]
        public static void TensorFillTest()
        {
            nint[] dims = [3, 3];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            var enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            tensor.Fill(int.MinValue);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MinValue, enumerator.Current);
            }

            tensor.Fill(int.MaxValue);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MaxValue, enumerator.Current);
            }

            dims = [9];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 3, 3];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [2, 2, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }
        }

        [Fact]
        public static void TensorClearTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            TensorSpan<int> t1 = a.AsTensorSpan(3, 3);
            var tensor = Tensor.CreateUninitialized<int>([3, 3], false);
            t1.CopyTo(tensor);
            var slice = tensor.Slice(0..2, 0..2);
            slice.Clear();
            Assert.Equal(0, slice[0, 0]);
            Assert.Equal(0, slice[0, 1]);
            Assert.Equal(0, slice[1, 0]);
            Assert.Equal(0, slice[1, 1]);

            // Since Tensor.Slice does a copy the original tensor shouldn't be modified
            Assert.Equal(1, tensor[0, 0]);
            Assert.Equal(2, tensor[0, 1]);
            Assert.Equal(3, tensor[0, 2]);
            Assert.Equal(4, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);
            Assert.Equal(6, tensor[1, 2]);
            Assert.Equal(7, tensor[2, 0]);
            Assert.Equal(8, tensor[2, 1]);
            Assert.Equal(9, tensor[2, 2]);


            tensor.Clear();
            var enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            t1 = a.AsTensorSpan(9);
            tensor = Tensor.CreateUninitialized<int>([9], false);
            t1.CopyTo(tensor);
            slice = tensor.Slice(0..1);
            slice.Clear();
            Assert.Equal(0, slice[0]);

            // Since Tensor.Slice does a copy the original tensor shouldn't be modified
            Assert.Equal(1, tensor[0]);
            Assert.Equal(2, tensor[1]);
            Assert.Equal(3, tensor[2]);
            Assert.Equal(4, tensor[3]);
            Assert.Equal(5, tensor[4]);
            Assert.Equal(6, tensor[5]);
            Assert.Equal(7, tensor[6]);
            Assert.Equal(8, tensor[7]);
            Assert.Equal(9, tensor[8]);


            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            t1 = a.AsTensorSpan(3, 3, 3);
            tensor = Tensor.CreateUninitialized<int>([3, 3, 3], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            t1 = a.AsTensorSpan(3, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([3, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            t1 = a.AsTensorSpan(2, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([2, 2, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            t1 = a.AsTensorSpan(3, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([3, 2, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            // Make sure clearing a slice of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            t1 = a.AsTensorSpan(3, 3);
            var spanSlice = t1.Slice(0..1, 0..3);
            spanSlice.Clear();
            var spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(0, t1[0, 1]);
            Assert.Equal(0, t1[0, 2]);
            Assert.Equal(3, t1[1, 0]);
            Assert.Equal(4, t1[1, 1]);
            Assert.Equal(5, t1[1, 2]);
            Assert.Equal(6, t1[2, 0]);
            Assert.Equal(7, t1[2, 1]);
            Assert.Equal(8, t1[2, 2]);

            // Make sure clearing a slice from the middle of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            t1 = a.AsTensorSpan(3, 3);
            spanSlice = t1.Slice(1..2, 0..3);
            spanSlice.Clear();
            spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(1, t1[0, 1]);
            Assert.Equal(2, t1[0, 2]);
            Assert.Equal(0, t1[1, 0]);
            Assert.Equal(0, t1[1, 1]);
            Assert.Equal(0, t1[1, 2]);
            Assert.Equal(6, t1[2, 0]);
            Assert.Equal(7, t1[2, 1]);
            Assert.Equal(8, t1[2, 2]);

            // Make sure clearing a slice from the end of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            t1 = a.AsTensorSpan(3, 3);
            spanSlice = t1.Slice(2..3, 0..3);
            spanSlice.Clear();
            spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(1, t1[0, 1]);
            Assert.Equal(2, t1[0, 2]);
            Assert.Equal(3, t1[1, 0]);
            Assert.Equal(4, t1[1, 1]);
            Assert.Equal(5, t1[1, 2]);
            Assert.Equal(0, t1[2, 0]);
            Assert.Equal(0, t1[2, 1]);
            Assert.Equal(0, t1[2, 2]);

            // Make sure it works with reference types.
            object[] o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            TensorSpan<object> spanObj = o.AsTensorSpan(3, 3);
            spanObj.Clear();

            var oSpanEnumerator = spanObj.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            var oSpanSlice = spanObj.Slice(0..1, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.Null(spanObj[0, 0]);
            Assert.Null(spanObj[0, 1]);
            Assert.Null(spanObj[0, 2]);
            Assert.NotNull(spanObj[1, 0]);
            Assert.NotNull(spanObj[1, 1]);
            Assert.NotNull(spanObj[1, 2]);
            Assert.NotNull(spanObj[2, 0]);
            Assert.NotNull(spanObj[2, 1]);
            Assert.NotNull(spanObj[2, 2]);

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            oSpanSlice = spanObj.Slice(1..2, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.NotNull(spanObj[0, 0]);
            Assert.NotNull(spanObj[0, 1]);
            Assert.NotNull(spanObj[0, 2]);
            Assert.Null(spanObj[1, 0]);
            Assert.Null(spanObj[1, 1]);
            Assert.Null(spanObj[1, 2]);
            Assert.NotNull(spanObj[2, 0]);
            Assert.NotNull(spanObj[2, 1]);
            Assert.NotNull(spanObj[2, 2]);

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            oSpanSlice = spanObj.Slice(2..3, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.NotNull(spanObj[0, 0]);
            Assert.NotNull(spanObj[0, 1]);
            Assert.NotNull(spanObj[0, 2]);
            Assert.NotNull(spanObj[1, 0]);
            Assert.NotNull(spanObj[1, 1]);
            Assert.NotNull(spanObj[1, 2]);
            Assert.Null(spanObj[2, 0]);
            Assert.Null(spanObj[2, 1]);
            Assert.Null(spanObj[2, 2]);
        }

        [Fact]
        public static void TensorCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            nint[] dims = [3, 3];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            leftSpan.CopyTo(tensor);
            var leftEnum = leftSpan.GetEnumerator();
            var tensorEnum = tensor.GetEnumerator();
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            tensor.CopyTo(rightSpan);
            var rightEnum = rightSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            while (rightEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(rightEnum.Current, tensorEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);
            Assert.NotEqual(leftSpan[0, 0], tensor[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            dims = [15];
            leftSpan = leftData.AsTensorSpan(9);
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            leftSpan.CopyTo(tensor);
            leftEnum = leftSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (tensorEnum.MoveNext())
            {
                Assert.Equal(0, tensorEnum.Current);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                var l = leftData.AsTensorSpan(3, 3, 3);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void TensorTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            nint[] dims = [3, 3];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            var success = leftSpan.TryCopyTo(tensor);
            Assert.True(success);
            success = tensor.TryCopyTo(rightSpan);
            Assert.True(success);

            var leftEnum = leftSpan.GetEnumerator();
            var tensorEnum = tensor.GetEnumerator();
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);
            Assert.NotEqual(leftSpan[0, 0], tensor[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            dims = [15];
            leftSpan = leftData.AsTensorSpan(9);
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            success = leftSpan.TryCopyTo(tensor);
            leftEnum = leftSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            Assert.True(success);
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (tensorEnum.MoveNext())
            {
                Assert.Equal(0, tensorEnum.Current);
            }

            leftData = [.. Enumerable.Range(0, 27)];
            var l = leftData.AsTensorSpan(3, 3, 3);
            dims = [2, 2];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(tensor);
            Assert.False(success);
            success = tensor.TryCopyTo(r);
            Assert.False(success);
        }

        [Fact]
        public static void TensorSliceTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            var tensor = Tensor.CreateUninitialized<int>([3, 3], false);

            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(1..2));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1, 5..6));
            var intSpan = a.AsTensorSpan(3, 3);
            intSpan.CopyTo(tensor.AsTensorSpan());

            var sp = tensor.Slice(1..3, 1..3);
            Assert.Equal(5, sp[0, 0]);
            Assert.Equal(6, sp[0, 1]);
            Assert.Equal(8, sp[1, 0]);
            Assert.Equal(9, sp[1, 1]);
            int[] slice = [5, 6, 8, 9];
            Assert.Equal(slice, sp.ToArray());
            var enumerator = sp.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..3, 0..3);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(3, sp[0, 2]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            Assert.Equal(6, sp[1, 2]);
            Assert.Equal(7, sp[2, 0]);
            Assert.Equal(8, sp[2, 1]);
            Assert.Equal(9, sp[2, 2]);
            Assert.Equal(a, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(a[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan(3, 3).Slice(0..1, 0..1)[0, 1]);
            slice = [1];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..2, 0..2);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            slice = [1, 2, 4, 5];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            int[] numbers = [.. Enumerable.Range(0, 27)];
            intSpan = numbers.AsTensorSpan(3, 3, 3);
            tensor = Tensor.CreateUninitialized<int>([3, 3, 3], false);
            intSpan.CopyTo(tensor.AsTensorSpan());
            sp = tensor.Slice(1..2, 1..2, 1..2);
            Assert.Equal(13, sp[0, 0, 0]);
            slice = [13];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(1..3, 1..3, 1..3);
            Assert.Equal(13, sp[0, 0, 0]);
            Assert.Equal(14, sp[0, 0, 1]);
            Assert.Equal(16, sp[0, 1, 0]);
            Assert.Equal(17, sp[0, 1, 1]);
            Assert.Equal(22, sp[1, 0, 0]);
            Assert.Equal(23, sp[1, 0, 1]);
            Assert.Equal(25, sp[1, 1, 0]);
            Assert.Equal(26, sp[1, 1, 1]);
            slice = [13, 14, 16, 17, 22, 23, 25, 26];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            numbers = [.. Enumerable.Range(0, 16)];
            intSpan = numbers.AsTensorSpan(2, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([2, 2, 2, 2], false);
            intSpan.CopyTo(tensor.AsTensorSpan());
            sp = tensor.Slice(1..2, 0..2, 1..2, 0..2);
            Assert.Equal(10, sp[0, 0, 0, 0]);
            Assert.Equal(11, sp[0, 0, 0, 1]);
            Assert.Equal(14, sp[0, 1, 0, 0]);
            Assert.Equal(15, sp[0, 1, 0, 1]);
            slice = [10, 11, 14, 15];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
        }

        [Fact]
        public static void TensorReshapeTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            nint[] dims = [9];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            var span = a.AsTensorSpan(dims);
            span.CopyTo(tensor);

            Assert.Equal(1, tensor.Rank);
            Assert.Equal(9, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Strides.Length);
            Assert.Equal(1, tensor.Strides[0]);
            Assert.Equal(1, tensor[0]);
            Assert.Equal(2, tensor[1]);
            Assert.Equal(3, tensor[2]);
            Assert.Equal(4, tensor[3]);
            Assert.Equal(5, tensor[4]);
            Assert.Equal(6, tensor[5]);
            Assert.Equal(7, tensor[6]);
            Assert.Equal(8, tensor[7]);
            Assert.Equal(9, tensor[8]);

            dims = [3, 3];
            tensor = Tensor.Reshape(tensor, dims);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(3, tensor.Lengths[0]);
            Assert.Equal(3, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Strides.Length);
            Assert.Equal(3, tensor.Strides[0]);
            Assert.Equal(1, tensor.Strides[1]);
            Assert.Equal(1, tensor[0, 0]);
            Assert.Equal(2, tensor[0, 1]);
            Assert.Equal(3, tensor[0, 2]);
            Assert.Equal(4, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);
            Assert.Equal(6, tensor[1, 2]);
            Assert.Equal(7, tensor[2, 0]);
            Assert.Equal(8, tensor[2, 1]);
            Assert.Equal(9, tensor[2, 2]);

            Assert.Throws<ArgumentException>(() => Tensor.Reshape(tensor, [1, 2, 3, 4, 5]));
        }

        [Fact]
        public static void TensorSqueezeTest()
        {
            nint[] dims = [1, 2];
            var tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor, 2);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            Assert.Throws<ArgumentException>(() => tensor = Tensor.Squeeze(tensor, 1));
            Assert.Throws<ArgumentException>(() => tensor = Tensor.Squeeze(tensor, 3));
        }

        [Fact]
        public static void TensorUnsqueezeTest()
        {
            var tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            tensor = Tensor.Unsqueeze(tensor, 1);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Unsqueeze<int>(tensor, -1));
            Assert.Throws<ArgumentException>(() => Tensor.Unsqueeze<int>(tensor, 2));

            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 2), default);
            t0 = Tensor.Unsqueeze(t0, 1);
            Assert.Equal(0, t0[0, 0]);
            Assert.Equal(1, t0[1, 0]);
        }
    }
}
