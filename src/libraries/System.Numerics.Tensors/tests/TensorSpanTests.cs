﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorSpanTests
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

        public delegate void TensorPrimitivesSpanInSpanOut<T>(ReadOnlySpan<T> input, Span<T> output);
        public delegate TensorSpan<T> TensorSpanInSpanOut<T>(TensorSpan<T> input);

        public static IEnumerable<object[]> SpanInSpanOutData()
        {
            yield return Create<float>(TensorPrimitives.Abs<float>, TensorSpan.Abs);
            yield return Create<float>(TensorPrimitives.Abs, TensorSpan.AbsInPlace);
            yield return Create<float>(TensorPrimitives.Acos, TensorSpan.Acos);
            yield return Create<float>(TensorPrimitives.Acos, TensorSpan.AcosInPlace);
            yield return Create<float>(TensorPrimitives.Acosh, TensorSpan.Acosh);
            yield return Create<float>(TensorPrimitives.Acosh, TensorSpan.AcoshInPlace);
            yield return Create<float>(TensorPrimitives.AcosPi, TensorSpan.AcosPi);
            yield return Create<float>(TensorPrimitives.AcosPi, TensorSpan.AcosPiInPlace);
            yield return Create<float>(TensorPrimitives.Asin, TensorSpan.Asin);
            yield return Create<float>(TensorPrimitives.Asin, TensorSpan.AsinInPlace);
            yield return Create<float>(TensorPrimitives.Asinh, TensorSpan.Asinh);
            yield return Create<float>(TensorPrimitives.Asinh, TensorSpan.AsinhInPlace);
            yield return Create<float>(TensorPrimitives.AsinPi, TensorSpan.AsinPi);
            yield return Create<float>(TensorPrimitives.AsinPi, TensorSpan.AsinPiInPlace);
            yield return Create<float>(TensorPrimitives.Atan, TensorSpan.Atan);
            yield return Create<float>(TensorPrimitives.Atan, TensorSpan.AtanInPlace);
            yield return Create<float>(TensorPrimitives.Atanh, TensorSpan.Atanh);
            yield return Create<float>(TensorPrimitives.Atanh, TensorSpan.AtanhInPlace);
            yield return Create<float>(TensorPrimitives.AtanPi, TensorSpan.AtanPi);
            yield return Create<float>(TensorPrimitives.AtanPi, TensorSpan.AtanPiInPlace);
            yield return Create<float>(TensorPrimitives.Cbrt, TensorSpan.CubeRoot);
            yield return Create<float>(TensorPrimitives.Cbrt, TensorSpan.CubeRootInPlace);
            yield return Create<float>(TensorPrimitives.Ceiling, TensorSpan.Ceiling);
            yield return Create<float>(TensorPrimitives.Ceiling, TensorSpan.CeilingInPlace);
            yield return Create<float>(TensorPrimitives.Cos, TensorSpan.Cos);
            yield return Create<float>(TensorPrimitives.Cos, TensorSpan.CosInPlace);
            yield return Create<float>(TensorPrimitives.Cosh, TensorSpan.Cosh);
            yield return Create<float>(TensorPrimitives.Cosh, TensorSpan.CoshInPlace);
            yield return Create<float>(TensorPrimitives.CosPi, TensorSpan.CosPi);
            yield return Create<float>(TensorPrimitives.CosPi, TensorSpan.CosPiInPlace);
            yield return Create<float>(TensorPrimitives.DegreesToRadians, TensorSpan.DegreesToRadians);
            yield return Create<float>(TensorPrimitives.DegreesToRadians, TensorSpan.DegreesToRadiansInPlace);
            yield return Create<float>(TensorPrimitives.Exp, TensorSpan.Exp);
            yield return Create<float>(TensorPrimitives.Exp, TensorSpan.ExpInPlace);
            yield return Create<float>(TensorPrimitives.Exp10, TensorSpan.Exp10);
            yield return Create<float>(TensorPrimitives.Exp10, TensorSpan.Exp10InPlace);
            yield return Create<float>(TensorPrimitives.Exp10M1, TensorSpan.Exp10M1);
            yield return Create<float>(TensorPrimitives.Exp10M1, TensorSpan.Exp10M1InPlace);
            yield return Create<float>(TensorPrimitives.Exp2, TensorSpan.Exp2);
            yield return Create<float>(TensorPrimitives.Exp2, TensorSpan.Exp2InPlace);
            yield return Create<float>(TensorPrimitives.Exp2M1, TensorSpan.Exp2M1);
            yield return Create<float>(TensorPrimitives.Exp2M1, TensorSpan.Exp2M1InPlace);
            yield return Create<float>(TensorPrimitives.ExpM1, TensorSpan.ExpM1);
            yield return Create<float>(TensorPrimitives.ExpM1, TensorSpan.ExpM1InPlace);
            yield return Create<float>(TensorPrimitives.Floor, TensorSpan.Floor);
            yield return Create<float>(TensorPrimitives.Floor, TensorSpan.FloorInPlace);
            yield return Create<int>(TensorPrimitives.LeadingZeroCount, TensorSpan.LeadingZeroCount);
            yield return Create<int>(TensorPrimitives.LeadingZeroCount, TensorSpan.LeadingZeroCount);
            yield return Create<float>(TensorPrimitives.Log, TensorSpan.Log);
            yield return Create<float>(TensorPrimitives.Log, TensorSpan.LogInPlace);
            yield return Create<float>(TensorPrimitives.Log10, TensorSpan.Log10);
            yield return Create<float>(TensorPrimitives.Log10, TensorSpan.Log10InPlace);
            yield return Create<float>(TensorPrimitives.Log10P1, TensorSpan.Log10P1);
            yield return Create<float>(TensorPrimitives.Log10P1, TensorSpan.Log10P1InPlace);
            yield return Create<float>(TensorPrimitives.Log2, TensorSpan.Log2);
            yield return Create<float>(TensorPrimitives.Log2, TensorSpan.Log2InPlace);
            yield return Create<float>(TensorPrimitives.Log2P1, TensorSpan.Log2P1);
            yield return Create<float>(TensorPrimitives.Log2P1, TensorSpan.Log2P1InPlace);
            yield return Create<float>(TensorPrimitives.LogP1, TensorSpan.LogP1);
            yield return Create<float>(TensorPrimitives.LogP1, TensorSpan.LogP1InPlace);
            yield return Create<float>(TensorPrimitives.Negate, TensorSpan.Negate);
            yield return Create<float>(TensorPrimitives.Negate, TensorSpan.NegateInPlace);
            yield return Create<float>(TensorPrimitives.OnesComplement, TensorSpan.OnesComplement);
            yield return Create<float>(TensorPrimitives.OnesComplement, TensorSpan.OnesComplementInPlace);
            yield return Create<int>(TensorPrimitives.PopCount, TensorSpan.PopCount);
            yield return Create<int>(TensorPrimitives.PopCount, TensorSpan.PopCountInPlace);
            yield return Create<float>(TensorPrimitives.RadiansToDegrees, TensorSpan.RadiansToDegrees);
            yield return Create<float>(TensorPrimitives.RadiansToDegrees, TensorSpan.RadiansToDegreesInPlace);
            yield return Create<float>(TensorPrimitives.Reciprocal, TensorSpan.Reciprocal);
            yield return Create<float>(TensorPrimitives.Reciprocal, TensorSpan.ReciprocalInPlace);
            yield return Create<float>(TensorPrimitives.Round, TensorSpan.Round);
            yield return Create<float>(TensorPrimitives.Round, TensorSpan.RoundInPlace);
            yield return Create<float>(TensorPrimitives.Sigmoid, TensorSpan.Sigmoid);
            yield return Create<float>(TensorPrimitives.Sigmoid, TensorSpan.SigmoidInPlace);
            yield return Create<float>(TensorPrimitives.Sin, TensorSpan.Sin);
            yield return Create<float>(TensorPrimitives.Sin, TensorSpan.SinInPlace);
            yield return Create<float>(TensorPrimitives.Sinh, TensorSpan.Sinh);
            yield return Create<float>(TensorPrimitives.Sinh, TensorSpan.SinhInPlace);
            yield return Create<float>(TensorPrimitives.SinPi, TensorSpan.SinPi);
            yield return Create<float>(TensorPrimitives.SinPi, TensorSpan.SinPiInPlace);
            yield return Create<float>(TensorPrimitives.SoftMax, TensorSpan.SoftMax);
            yield return Create<float>(TensorPrimitives.SoftMax, TensorSpan.SoftMaxInPlace);
            yield return Create<float>(TensorPrimitives.Sqrt, TensorSpan.Sqrt);
            yield return Create<float>(TensorPrimitives.Sqrt, TensorSpan.SqrtInPlace);
            yield return Create<float>(TensorPrimitives.Tan, TensorSpan.Tan);
            yield return Create<float>(TensorPrimitives.Tan, TensorSpan.TanInPlace);
            yield return Create<float>(TensorPrimitives.Tanh, TensorSpan.Tanh);
            yield return Create<float>(TensorPrimitives.Tanh, TensorSpan.TanhInPlace);
            yield return Create<float>(TensorPrimitives.TanPi, TensorSpan.TanPi);
            yield return Create<float>(TensorPrimitives.TanPi, TensorSpan.TanPiInPlace);
            yield return Create<float>(TensorPrimitives.Truncate, TensorSpan.Truncate);
            yield return Create<float>(TensorPrimitives.Truncate, TensorSpan.TruncateInPlace);

            static object[] Create<T>(TensorPrimitivesSpanInSpanOut<T> tensorPrimitivesMethod, TensorSpanInSpanOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(SpanInSpanOutData))]
        public void TensorExtensionsSpanInSpanOut<T>(TensorPrimitivesSpanInSpanOut<T> tensorPrimitivesOperation, TensorSpanInSpanOut<T> tensorOperation)
            where T : INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data = new T[length];
                T[] expectedOutput = new T[length];

                FillTensor<T>(data);
                TensorSpan<T> x = Tensor.Create<T>(data, tensorLength, []);
                tensorPrimitivesOperation((ReadOnlySpan<T>)data, expectedOutput);
                TensorSpan<T> results = tensorOperation(x);

                Assert.Equal(tensorLength, results.Lengths);
                nint[] startingIndex = new nint[tensorLength.Length];
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref results[startingIndex], (int)length);

                for (int i = 0; i < data.Length; i++)
                {
                    Assert.Equal(expectedOutput[i], span[i]);
                }
            });
        }

        public delegate T TensorPrimitivesSpanInTOut<T>(ReadOnlySpan<T> input);
        public delegate T TensorSpanInTOut<T>(TensorSpan<T> input);
        public static IEnumerable<object[]> SpanInFloatOutData()
        {
            yield return Create<float>(TensorPrimitives.Max, TensorSpan.Max);
            yield return Create<float>(TensorPrimitives.MaxMagnitude, TensorSpan.MaxMagnitude);
            yield return Create<float>(TensorPrimitives.MaxNumber, TensorSpan.MaxNumber);
            yield return Create<float>(TensorPrimitives.Min, TensorSpan.Min);
            yield return Create<float>(TensorPrimitives.MinMagnitude, TensorSpan.MinMagnitude);
            yield return Create<float>(TensorPrimitives.MinNumber, TensorSpan.MinNumber);
            yield return Create<float>(TensorPrimitives.Norm, TensorSpan.Norm);
            yield return Create<float>(TensorPrimitives.Product, TensorSpan.Product);
            yield return Create<float>(TensorPrimitives.Sum, TensorSpan.Sum);

            static object[] Create<T>(TensorPrimitivesSpanInTOut<T> tensorPrimitivesMethod, TensorSpanInTOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(SpanInFloatOutData))]
        public void TensorExtensionsSpanInTOut<T>(TensorPrimitivesSpanInTOut<T> tensorPrimitivesOperation, TensorSpanInTOut<T> tensorOperation)
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

        public delegate void TensorPrimitivesTwoSpanInSpanOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output);
        public delegate TensorSpan<T> TensorTwoSpanInSpanOut<T>(TensorSpan<T> input, TensorSpan<T> inputTwo);
        public static IEnumerable<object[]> TwoSpanInSpanOutData()
        {
            yield return Create<float>(TensorPrimitives.Add, TensorSpan.Add);
            yield return Create<float>(TensorPrimitives.Add, TensorSpan.AddInPlace);
            yield return Create<float>(TensorPrimitives.Atan2, TensorSpan.Atan2);
            yield return Create<float>(TensorPrimitives.Atan2, TensorSpan.Atan2InPlace);
            yield return Create<float>(TensorPrimitives.Atan2Pi, TensorSpan.Atan2Pi);
            yield return Create<float>(TensorPrimitives.Atan2Pi, TensorSpan.Atan2PiInPlace);
            yield return Create<float>(TensorPrimitives.CopySign, TensorSpan.CopySign);
            yield return Create<float>(TensorPrimitives.CopySign, TensorSpan.CopySignInPlace);
            yield return Create<float>(TensorPrimitives.Divide, TensorSpan.Divide);
            yield return Create<float>(TensorPrimitives.Divide, TensorSpan.DivideInPlace);
            yield return Create<float>(TensorPrimitives.Hypot, TensorSpan.Hypotenuse);
            yield return Create<float>(TensorPrimitives.Hypot, TensorSpan.HypotenuseInPlace);
            yield return Create<float>(TensorPrimitives.Ieee754Remainder, TensorSpan.Ieee754Remainder);
            yield return Create<float>(TensorPrimitives.Ieee754Remainder, TensorSpan.Ieee754RemainderInPlace);
            yield return Create<float>(TensorPrimitives.Multiply, TensorSpan.Multiply);
            yield return Create<float>(TensorPrimitives.Multiply, TensorSpan.MultiplyInPlace);
            yield return Create<float>(TensorPrimitives.Pow, TensorSpan.Pow);
            yield return Create<float>(TensorPrimitives.Pow, TensorSpan.PowInPlace);
            yield return Create<float>(TensorPrimitives.Subtract, TensorSpan.Subtract);
            yield return Create<float>(TensorPrimitives.Subtract, TensorSpan.SubtractInPlace);

            static object[] Create<T>(TensorPrimitivesTwoSpanInSpanOut<T> tensorPrimitivesMethod, TensorTwoSpanInSpanOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInSpanOutData))]
        public void TensorExtensionsTwoSpanInSpanOut<T>(TensorPrimitivesTwoSpanInSpanOut<T> tensorPrimitivesOperation, TensorTwoSpanInSpanOut<T> tensorOperation)
            where T : INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data1 = new T[length];
                T[] data2 = new T[length];
                T[] expectedOutput = new T[length];

                FillTensor<T>(data1);
                FillTensor<T>(data2);
                TensorSpan<T> x = Tensor.Create<T>(data1, tensorLength, []);
                TensorSpan<T> y = Tensor.Create<T>(data2, tensorLength, []);
                tensorPrimitivesOperation((ReadOnlySpan<T>)data1, data2, expectedOutput);
                TensorSpan<T> results = tensorOperation(x, y);

                Assert.Equal(tensorLength, results.Lengths);
                nint[] startingIndex = new nint[tensorLength.Length];
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref results[startingIndex], (int)length);

                for (int i = 0; i < data1.Length; i++)
                {
                    Assert.Equal(expectedOutput[i], span[i]);
                }
            });
        }

        public delegate T TensorPrimitivesTwoSpanInTOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo);
        public delegate T TensorTwoSpanInTOut<T>(TensorSpan<T> input, TensorSpan<T> inputTwo);
        public static IEnumerable<object[]> TwoSpanInFloatOutData()
        {
            yield return Create<float>(TensorPrimitives.Distance, TensorSpan.Distance);
            yield return Create<float>(TensorPrimitives.Dot, TensorSpan.Dot);

            static object[] Create<T>(TensorPrimitivesTwoSpanInTOut<T> tensorPrimitivesMethod, TensorTwoSpanInTOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInFloatOutData))]
        public void TensorExtensionsTwoSpanInFloatOut<T>(TensorPrimitivesTwoSpanInTOut<T> tensorPrimitivesOperation, TensorTwoSpanInTOut<T> tensorOperation)
            where T : INumberBase<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data1 = new T[length];
                T[] data2 = new T[length];

                FillTensor<T>(data1);
                FillTensor<T>(data2);
                TensorSpan<T> x = Tensor.Create<T>(data1, tensorLength, []);
                TensorSpan<T> y = Tensor.Create<T>(data2, tensorLength, []);
                T expectedOutput = tensorPrimitivesOperation((ReadOnlySpan<T>)data1, data2);
                T results = tensorOperation(x, y);

                Assert.Equal(expectedOutput, results);
            });
        }

        #endregion

        [Fact]
        public static void TensorSpanSystemArrayConstructorTests()
        {
            // Make sure basic T[,] constructor works
            int[,] a = new int[,] { { 91, 92, -93, 94 } };
            scoped TensorSpan<int> spanInt = new TensorSpan<int>(a);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(4, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[0, 2]);
            Assert.Equal(94, spanInt[0, 3]);

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            int[,] n = null;
            spanInt = new TensorSpan<int>(n);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 2 with dimension 0 length 0
            int[,] b = { { } };
            spanInt = new TensorSpan<int>(b);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.Lengths[1]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            Assert.Equal(0, spanInt.Strides[1]);
            // Make sure it still throws on index 0, 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(b);
                var x = spanInt[0, 0];
            });

            // Make sure 2D array works
            spanInt = new TensorSpan<int>(a, (int[])[0, 0], [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure can use only some of the array
            spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[0, -1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[-1, 0];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a, (int[])[0, 1], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a, (int[])[0, 2], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new TensorSpan<int>(a, (int[])[0, 3], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 3], [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new TensorSpan<int>(a, (int[])[0, 0], [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new TensorSpan<int>(a, (int[])[0, 0], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new TensorSpan<int>(a, (int[])[0, 2], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[0, 0], [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new TensorSpan<int>(a, (int[])[], [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new TensorSpan<int>(a, (int[])[], [2, 2], [1, 1]);
            });

            a = new int[,] { { 91, 92 }, { -93, 94 } };
            spanInt = new TensorSpan<int>(a, (int[])[1, 1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            //Make sure it works with NIndex
            spanInt = new TensorSpan<int>(a, (NIndex[])[1, 1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            //Make sure it works with NIndex
            spanInt = new TensorSpan<int>(a, (NIndex[])[^1, ^1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
        }

        [Fact]
        public static void TensorSpanArrayConstructorTests()
        {
            // Make sure basic T[] constructor works
            int[] a = { 91, 92, -93, 94 };
            scoped TensorSpan<int> spanInt = new TensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            spanInt = new TensorSpan<int>(null);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            int[] b = { };
            spanInt = new TensorSpan<int>(b);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            // Make sure it still throws on index 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(b);
                var x = spanInt[0];
            });

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            spanInt = new TensorSpan<int>(b, 0, [], default);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);

            // Make sure 2D array works
            spanInt = new TensorSpan<int>(a, new Index(0), [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure can use only some of the array
            spanInt = new TensorSpan<int>(a, new Index(0), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(0), [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(0), [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a, new Index(1), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a, new Index(2), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(3), [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new TensorSpan<int>(a, new Index(0), [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new TensorSpan<int>(a, new Index(0), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new TensorSpan<int>(a, new Index(2), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new TensorSpan<int>(a, new Index(3), [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(3), [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(3), [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(3), [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, new Index(3), [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new TensorSpan<int>(a, 0, [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new TensorSpan<int>(a, 0, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static void TensorSpanSpanConstructorTests()
        {
            // Make sure basic T[] constructor works
            Span<int> a = [91, 92, -93, 94];
            scoped TensorSpan<int> spanInt = new TensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            spanInt = new TensorSpan<int>(b);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            // Make sure it still throws on index 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> b = [];
                var spanInt = new TensorSpan<int>(b);
                var x = spanInt[0];
            });

            // Make sure 2D array works
            spanInt = new TensorSpan<int>(a, [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure can use only some of the array
            spanInt = new TensorSpan<int>(a, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a.Slice(1), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new TensorSpan<int>(a.Slice(2), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a.Slice(3), [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new TensorSpan<int>(a, [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new TensorSpan<int>(a, [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new TensorSpan<int>(a.Slice(2), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new TensorSpan<int>(a.Slice(3), [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new TensorSpan<int>(a, [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new TensorSpan<int>(a, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static unsafe void TensorSpanPointerConstructorTests()
        {
            // Make sure basic T[] constructor works
            Span<int> a = [91, 92, -93, 94];
            TensorSpan<int> spanInt;
            fixed (int* p = a)
            {
                spanInt = new TensorSpan<int>(p, 4);
                Assert.Equal(1, spanInt.Rank);
                Assert.Equal(4, spanInt.Lengths[0]);
                Assert.Equal(91, spanInt[0]);
                Assert.Equal(92, spanInt[1]);
                Assert.Equal(-93, spanInt[2]);
                Assert.Equal(94, spanInt[3]);
            }

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            fixed (int* p = b)
            {
                spanInt = new TensorSpan<int>(p, 0);
                Assert.Equal(1, spanInt.Rank);
                Assert.Equal(0, spanInt.Lengths[0]);
                Assert.Equal(0, spanInt.FlattenedLength);
                Assert.Equal(0, spanInt.Strides[0]);
                // Make sure it still throws on index 0
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> b = [];
                    fixed (int* p = b)
                    {
                        var spanInt = new TensorSpan<int>(p, 0);
                        var x = spanInt[0];
                    }
                });
            }

            // Make sure 2D array works
            fixed (int* p = a)
            {
                spanInt = new TensorSpan<int>(p, 4, [2, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

                // Make sure can use only some of the array
                spanInt = new TensorSpan<int>(p, 4, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [1, 2], default);
                        var x = spanInt[1, 1];
                    }
                });

                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [1, 2], default);
                        var x = spanInt[1, 0];
                    }
                });

                // Make sure Index offset works correctly
                spanInt = new TensorSpan<int>(p + 1, 3, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(92, spanInt[0, 0]);
                Assert.Equal(-93, spanInt[0, 1]);

                // Make sure Index offset works correctly
                spanInt = new TensorSpan<int>(p + 2, 2, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);

                // Make sure we catch that there aren't enough elements in the array for the lengths
                Assert.Throws<ArgumentException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p + 3, 1, [1, 2], default);
                    }
                });

                // Make sure 2D array works with basic strides
                spanInt = new TensorSpan<int>(p, 4, [2, 2], [2, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

                // Make sure 2D array works with stride of 0 to loop over first 2 elements again
                spanInt = new TensorSpan<int>(p, 4, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(91, spanInt[1, 0]);
                Assert.Equal(92, spanInt[1, 1]);

                // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
                spanInt = new TensorSpan<int>(p + 2, 2, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

                // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
                spanInt = new TensorSpan<int>(p + 3, 1, [2, 2], [0, 0]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(94, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(94, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

                // Make sure strides can't be negative
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [1, 2], [-1, 0]);
                    }
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [1, 2], [0, -1]);
                    }
                });

                // Make sure lengths can't be negative
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [-1, 2], []);
                    }
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [1, -2], []);
                    }
                });

                // Make sure can't use negative data length amount
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, -1, [1, -2], []);
                    }
                });

                // Make sure 2D array works with strides to hit element 0,0,2,2
                spanInt = new TensorSpan<int>(p, 4, [2, 2], [2, 0]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(91, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(-93, spanInt[1, 1]);

                // Make sure you can't overlap elements using strides
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new TensorSpan<int>(p, 4, [2, 2], [1, 1]);
                    }
                });
            }
        }

        [Fact]
        public static void IntArrayAsTensorSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            int[] results = new int[4];
            TensorSpan<int> spanInt = a.AsTensorSpan(4);
            Assert.Equal(1, spanInt.Rank);

            Assert.Equal(1, spanInt.Lengths.Length);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(1, spanInt.Strides.Length);
            Assert.Equal(1, spanInt.Strides[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            spanInt[0] = 100;
            spanInt[1] = 101;
            spanInt[2] = -102;
            spanInt[3] = 103;

            Assert.Equal(100, spanInt[0]);
            Assert.Equal(101, spanInt[1]);
            Assert.Equal(-102, spanInt[2]);
            Assert.Equal(103, spanInt[3]);

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            spanInt = a.AsTensorSpan(2, 2);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths.Length);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(2, spanInt.Strides.Length);
            Assert.Equal(2, spanInt.Strides[0]);
            Assert.Equal(1, spanInt.Strides[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            spanInt[0, 0] = 100;
            spanInt[0, 1] = 101;
            spanInt[1, 0] = -102;
            spanInt[1, 1] = 103;

            Assert.Equal(100, spanInt[0, 0]);
            Assert.Equal(101, spanInt[0, 1]);
            Assert.Equal(-102, spanInt[1, 0]);
            Assert.Equal(103, spanInt[1, 1]);
        }

        [Fact]
        public static void TensorSpanFillTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            TensorSpan<int> spanInt = a.AsTensorSpan(3, 3);
            spanInt.Fill(-1);
            var enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            spanInt.Fill(int.MinValue);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MinValue, enumerator.Current);
            }

            spanInt.Fill(int.MaxValue);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MaxValue, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            spanInt = a.AsTensorSpan(9);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            spanInt = a.AsTensorSpan(3,3,3);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            spanInt = a.AsTensorSpan(3, 2, 2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            spanInt = a.AsTensorSpan(2,2,2,2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            spanInt = a.AsTensorSpan(3, 2, 2, 2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }
        }

        [Fact]
        public static void TensorSpanClearTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            TensorSpan<int> spanInt = a.AsTensorSpan(3, 3);

            var slice = spanInt.Slice(0..2, 0..2);
            slice.Clear();
            Assert.Equal(0, slice[0, 0]);
            Assert.Equal(0, slice[0, 1]);
            Assert.Equal(0, slice[1, 0]);
            Assert.Equal(0, slice[1, 1]);
            //First values of original span should be cleared.
            Assert.Equal(0, spanInt[0, 0]);
            Assert.Equal(0, spanInt[0, 1]);
            Assert.Equal(0, spanInt[1, 0]);
            Assert.Equal(0, spanInt[1, 1]);
            //Make sure the rest of the values from the original span didn't get cleared.
            Assert.Equal(3, spanInt[0, 2]);
            Assert.Equal(6, spanInt[1, 2]);
            Assert.Equal(7, spanInt[2, 0]);
            Assert.Equal(8, spanInt[2, 1]);
            Assert.Equal(9, spanInt[2, 2]);


            spanInt.Clear();
            var enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            spanInt = a.AsTensorSpan(9);
            slice = spanInt.Slice(0..1);
            slice.Clear();
            Assert.Equal(0, slice[0]);
            //First value of original span should be cleared.
            Assert.Equal(0, spanInt[0]);
            //Make sure the rest of the values from the original span didn't get cleared.
            Assert.Equal(2, spanInt[1]);
            Assert.Equal(3, spanInt[2]);
            Assert.Equal(4, spanInt[3]);
            Assert.Equal(5, spanInt[4]);
            Assert.Equal(6, spanInt[5]);
            Assert.Equal(7, spanInt[6]);
            Assert.Equal(8, spanInt[7]);
            Assert.Equal(9, spanInt[8]);


            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            spanInt = a.AsTensorSpan(3, 3, 3);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            spanInt = a.AsTensorSpan(3, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            spanInt = a.AsTensorSpan(2, 2, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            spanInt = a.AsTensorSpan(3, 2, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }
        }

        [Fact]
        public static void TensorSpanCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            leftSpan.CopyTo(rightSpan);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while(leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsTensorSpan(9);
            rightSpan = rightData.AsTensorSpan(15);
            leftSpan.CopyTo(rightSpan);
            leftEnum = leftSpan.GetEnumerator();
            rightEnum = rightSpan.GetEnumerator();
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while(rightEnum.MoveNext())
            {
                Assert.Equal(0, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0] = 100;
            Assert.NotEqual(leftSpan[0], rightSpan[0]);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsTensorSpan(3, 3, 3);
            rightSpan = rightData.AsTensorSpan(3, 3, 3);
            leftSpan.CopyTo(rightSpan);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                var l = leftData.AsTensorSpan(3, 3, 3);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void TensorSpanTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            var success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsTensorSpan(9);
            rightSpan = rightData.AsTensorSpan(15);
            success = leftSpan.TryCopyTo(rightSpan);
            leftEnum = leftSpan.GetEnumerator();
            rightEnum = rightSpan.GetEnumerator();
            Assert.True(success);
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (rightEnum.MoveNext())
            {
                Assert.Equal(0, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0] = 100;
            Assert.NotEqual(leftSpan[0], rightSpan[0]);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsTensorSpan(3, 3, 3);
            rightSpan = rightData.AsTensorSpan(3, 3, 3);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            var l = leftData.AsTensorSpan(3, 3, 3);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(r);
            Assert.False(success);
        }

        [Fact]
        public static void TensorSpanSliceTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] results = new int[9];
            TensorSpan<int> spanInt = a.AsTensorSpan(3, 3);

            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan(2, 3).Slice(0..1));
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan(2, 3).Slice(1..2));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsTensorSpan(2, 3).Slice(0..1, 5..6));

            var sp = spanInt.Slice(1..3, 1..3);
            Assert.Equal(5, sp[0, 0]);
            Assert.Equal(6, sp[0, 1]);
            Assert.Equal(8, sp[1, 0]);
            Assert.Equal(9, sp[1, 1]);
            int[] slice = [5, 6, 8, 9];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            var enumerator = sp.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..3, 0..3);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(3, sp[0, 2]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            Assert.Equal(6, sp[1, 2]);
            Assert.Equal(7, sp[2, 0]);
            Assert.Equal(8, sp[2, 1]);
            Assert.Equal(9, sp[2, 2]);
            results = new int[9];
            sp.FlattenTo(results);
            Assert.Equal(a, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(a[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan(3, 3).Slice(0..1, 0..1)[0, 1]);
            slice = [1];
            results = new int[1];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..2, 0..2);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            slice = [1, 2, 4, 5];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            int[] numbers = [.. Enumerable.Range(0, 27)];
            spanInt = numbers.AsTensorSpan(3, 3, 3);
            sp = spanInt.Slice(1..2, 1..2, 1..2);
            Assert.Equal(13, sp[0, 0, 0]);
            slice = [13];
            results = new int[1];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(1..3, 1..3, 1..3);
            Assert.Equal(13, sp[0, 0, 0]);
            Assert.Equal(14, sp[0, 0, 1]);
            Assert.Equal(16, sp[0, 1, 0]);
            Assert.Equal(17, sp[0, 1, 1]);
            Assert.Equal(22, sp[1, 0, 0]);
            Assert.Equal(23, sp[1, 0, 1]);
            Assert.Equal(25, sp[1, 1, 0]);
            Assert.Equal(26, sp[1, 1, 1]);
            slice = [13, 14, 16, 17, 22, 23, 25, 26];
            results = new int[8];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            numbers = [.. Enumerable.Range(0, 16)];
            spanInt = numbers.AsTensorSpan(2, 2, 2, 2);
            sp = spanInt.Slice(1..2, 0..2, 1..2, 0..2);
            Assert.Equal(10, sp[0,0,0,0]);
            Assert.Equal(11, sp[0,0,0,1]);
            Assert.Equal(14, sp[0,1,0,0]);
            Assert.Equal(15, sp[0,1,0,1]);
            slice = [10, 11, 14, 15];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
        }

        [Fact]
        public static void LongArrayAsTensorSpan()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            TensorSpan<long> spanLong = b.AsTensorSpan(5);
            Assert.Equal(91, spanLong[0]);
            Assert.Equal(-92, spanLong[1]);
            Assert.Equal(93, spanLong[2]);
            Assert.Equal(94, spanLong[3]);
            Assert.Equal(-95, spanLong[4]);
        }

        [Fact]
        public static void NullArrayAsTensorSpan()
        {
            int[] a = null;
            TensorSpan<int> span = a.AsTensorSpan();
            Assert.True(span == default);
        }
    }
}
