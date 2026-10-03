// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorTests
    {
        #region TensorPrimitivesForwardsTests
        private void FillTensor<T>(Span<T> span)
            where T : INumberBase<T>, IComparisonOperators<T, T, bool>
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

        public delegate Tensor<T> PerformSpanInSpanOut<T>(in ReadOnlyTensorSpan<T> input);
        public delegate T PerformCalculationSpanInSpanOut<T>(T input);

        public static IEnumerable<object[]> SpanInSpanOutData()
        {
            const float TrigTolerance = 1e-4f;

            yield return Create<float>(float.Abs, Tensor.Abs);
            yield return Create<float>(float.Acos, Tensor.Acos);
            yield return Create<float>(float.Acosh, Tensor.Acosh);
            yield return Create<float>(float.AcosPi, Tensor.AcosPi);
            yield return Create<float>(float.Asin, Tensor.Asin, TrigTolerance);
            yield return Create<float>(float.Asinh, Tensor.Asinh);
            yield return Create<float>(float.AsinPi, Tensor.AsinPi);
            yield return Create<float>(float.Atan, Tensor.Atan);
            yield return Create<float>(float.Atanh, Tensor.Atanh);
            yield return Create<float>(float.AtanPi, Tensor.AtanPi);
            yield return Create<float>(float.Cbrt, Tensor.Cbrt);
            yield return Create<float>(float.Ceiling, Tensor.Ceiling);
            yield return Create<float>(float.Cos, Tensor.Cos, TrigTolerance);
            yield return Create<float>(float.Cosh, Tensor.Cosh);
            yield return Create<float>(float.CosPi, Tensor.CosPi, TrigTolerance);
            yield return Create<float>(float.DegreesToRadians, Tensor.DegreesToRadians);
            yield return Create<float>(float.Exp, Tensor.Exp);
            yield return Create<float>(float.Exp10, Tensor.Exp10, 1e-5f);
            yield return Create<float>(float.Exp10M1, Tensor.Exp10M1, 1e-5f);
            yield return Create<float>(float.Exp2, Tensor.Exp2, 1e-5f);
            yield return Create<float>(float.Exp2M1, Tensor.Exp2M1, 1e-5f);
            yield return Create<float>(float.ExpM1, Tensor.ExpM1);
            yield return Create<float>(float.Floor, Tensor.Floor);
            yield return Create<int>(int.LeadingZeroCount, Tensor.LeadingZeroCount);
            yield return Create<int>(int.LeadingZeroCount, Tensor.LeadingZeroCount);
            yield return Create<float>(float.Log, Tensor.Log);
            yield return Create<float>(float.Log10, Tensor.Log10);
            yield return Create<float>(float.Log10P1, Tensor.Log10P1);
            yield return Create<float>(float.Log2, Tensor.Log2);
            yield return Create<float>(float.Log2P1, Tensor.Log2P1);
            yield return Create<float>(float.LogP1, Tensor.LogP1);
            yield return Create<float>(f => -f, Tensor.Negate);
            yield return Create<int>(f => ~f, Tensor.OnesComplement);
            yield return Create<int>(int.PopCount, Tensor.PopCount);
            yield return Create<float>(float.RadiansToDegrees, Tensor.RadiansToDegrees);
            yield return Create<float>( f => 1 / f, Tensor.Reciprocal);
            yield return Create<float>(float.Round, Tensor.Round);
            //yield return Create<float>(float.Sigmoid, Tensor.Sigmoid);
            yield return Create<float>(float.Sin, Tensor.Sin, TrigTolerance);
            yield return Create<float>(float.Sinh, Tensor.Sinh);
            yield return Create<float>(float.SinPi, Tensor.SinPi, TrigTolerance);
            //yield return Create<float>(float.SoftMax, Tensor.SoftMax);
            yield return Create<float>(float.Sqrt, Tensor.Sqrt);
            yield return Create<float>(float.Tan, Tensor.Tan, TrigTolerance);
            yield return Create<float>(float.Tanh, Tensor.Tanh);
            yield return Create<float>(float.TanPi, Tensor.TanPi);
            yield return Create<float>(float.Truncate, Tensor.Truncate);

            static object[] Create<T>(PerformCalculationSpanInSpanOut<T> tensorPrimitivesMethod, PerformSpanInSpanOut<T> tensorOperation, float? tolerance = null)
                => new object[] { tensorPrimitivesMethod, tensorOperation, tolerance };
        }

        [Theory, MemberData(nameof(SpanInSpanOutData))]
        public void TensorExtensionsSpanInSpanOut<T>(PerformCalculationSpanInSpanOut<T> tensorPrimitivesOperation, PerformSpanInSpanOut<T> tensorOperation, float? tolerance)
            where T : unmanaged, INumber<T>
        {
            Assert.All(Helpers.TensorShapes, tensorLength =>
            {
                nint length = CalculateTotalLength(tensorLength);
                T[] data = new T[length];
                T[] expectedOutput = new T[length];

                FillTensor<T>(data);
                Tensor<T> x = Tensor.Create<T>(data, tensorLength, []);

                Tensor<T> results = tensorOperation(x);

                Assert.Equal(tensorLength, results.Lengths);
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref results.GetPinnableReference(), (int)length);

                for (int i = 0; i < data.Length; i++)
                {
                    Helpers.AssertEqualWithTolerance(tensorPrimitivesOperation(data[i]), span[i],
                        tolerance.HasValue ? T.CreateTruncating(tolerance.Value) : null);
                }
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 3)]
        [InlineData(0, 9)]
        [InlineData(0, 17)]
        [InlineData(0, 33)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(1, 3)]
        [InlineData(1, 9)]
        [InlineData(1, 17)]
        [InlineData(1, 33)]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(2, 3)]
        [InlineData(2, 9)]
        [InlineData(2, 17)]
        [InlineData(2, 33)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 3)]
        [InlineData(3, 9)]
        [InlineData(3, 17)]
        [InlineData(3, 33)]
        public static void TensorTanPreservesScalarAccuracy(int layout, int columns)
        {
            Test<float>(layout, columns);
            Test<double>(layout, columns);
            Test<Half>(layout, columns);

            static void Test<T>(int layout, int columns)
                where T : unmanaged, IFloatingPointIeee754<T>
            {
                nint[] lengths = [2, columns];
                nint[] strides = layout switch
                {
                    1 => [columns + 3, columns > 1 ? 1 : 0],
                    2 => [1, columns > 1 ? 2 : 0],
                    3 => [0, columns > 1 ? 1 : 0],
                    _ => [],
                };
                T[] storage = new T[2 * (columns + 3)];
                T[] expected = new T[2 * columns];
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        T pole = (T.CreateChecked(column % 16) + T.CreateChecked(0.5)) * T.Pi;
                        T value = (column % 4) switch
                        {
                            0 => T.BitDecrement(pole),
                            1 => T.BitIncrement(pole),
                            2 => -T.BitDecrement(pole),
                            _ => -T.BitIncrement(pole),
                        };
                        if (column == 0)
                        {
                            value = T.CreateChecked(-32.986717f);
                        }
                        else if (column >= 24)
                        {
                            value = (column % 5) switch
                            {
                                0 => T.Zero,
                                1 => T.NegativeZero,
                                2 => T.PositiveInfinity,
                                3 => T.NegativeInfinity,
                                _ => T.NaN,
                            };
                        }
                        int offset = layout switch
                        {
                            1 => row * (columns + 3) + column,
                            2 => row + 2 * column,
                            3 => column,
                            _ => row * columns + column,
                        };
                        storage[offset] = value;
                        expected[row * columns + column] = T.Tan(value);
                    }
                }

                Tensor<T> source = Tensor.Create(storage, lengths, strides);
                AssertResult(Tensor.Tan<T>(source).ToArray());

                Tensor<T> destination = Tensor.CreateFromShape<T>(lengths);
                Tensor.Tan<T>(source.AsReadOnlyTensorSpan(), destination.AsTensorSpan());
                AssertResult(destination.ToArray());

                if (layout != 3)
                {
                    Tensor.Tan<T>(source.AsReadOnlyTensorSpan(), source.AsTensorSpan());
                    AssertResult(source.ToArray());
                }

                void AssertResult(T[] actual)
                {
                    Assert.Equal(expected.Length, actual.Length);
                    for (int i = 0; i < actual.Length; i++)
                    {
                        Helpers.AssertEqualWithTolerance(expected[i], actual[i],
                            Helpers.DetermineTolerance<T>(doubleTolerance: 3e-13, floatTolerance: 1e-4f));
                    }
                }
            }
        }

        public delegate T PerformSpanInTOut<T>(scoped in ReadOnlyTensorSpan<T> input);
        public delegate T PerformCalculationSpanInTOut<T>(ReadOnlySpan<T> input);
        public static IEnumerable<object[]> SpanInFloatOutData()
        {
            yield return Create<float>(TensorPrimitives.Max, Tensor.Max);
            yield return Create<float>(TensorPrimitives.MaxMagnitude, Tensor.MaxMagnitude);
            yield return Create<float>(TensorPrimitives.MaxNumber, Tensor.MaxNumber);
            yield return Create<float>(TensorPrimitives.Min, Tensor.Min);
            yield return Create<float>(TensorPrimitives.MinMagnitude, Tensor.MinMagnitude);
            yield return Create<float>(TensorPrimitives.MinNumber, Tensor.MinNumber);
            yield return Create<float>(x =>
            {
                float sum = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    sum += x[i] * x[i];
                }
                return float.Sqrt(sum);
            }, Tensor.Norm);
            yield return Create<float>(x =>
            {
                float sum = 1;
                for (int i = 0; i < x.Length; i++)
                {
                    sum *= x[i];
                }
                return sum;
            }, Tensor.Product);
            yield return Create<float>(x =>
            {
                float sum = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    sum+= x[i];
                }
                return sum;
            }, Tensor.Sum);

            static object[] Create<T>(PerformCalculationSpanInTOut<T> tensorPrimitivesMethod, PerformSpanInTOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(SpanInFloatOutData))]
        public void TensorExtensionsSpanInTOut<T>(PerformCalculationSpanInTOut<T> tensorPrimitivesOperation, PerformSpanInTOut<T> tensorOperation)
            where T : INumberBase<T>, IComparisonOperators<T, T, bool>
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

        public delegate Tensor<T> PerformTwoSpanInSpanOut<T>(in ReadOnlyTensorSpan<T> input, in ReadOnlyTensorSpan<T> input2);
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

            static object[] Create<T>(PerformCalculationTwoSpanInSpanOut<T> tensorPrimitivesMethod, PerformTwoSpanInSpanOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInSpanOutData))]
        public void TensorExtensionsTwoSpanInSpanOut<T>(PerformCalculationTwoSpanInSpanOut<T> tensorPrimitivesOperation, PerformTwoSpanInSpanOut<T> tensorOperation)
            where T: INumberBase<T>, IComparisonOperators<T, T, bool>
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

        public delegate T PerformTwoSpanInFloatOut<T>(in ReadOnlyTensorSpan<T> input, in ReadOnlyTensorSpan<T> input2);
        public delegate T PerformCalculationTwoSpanInFloatOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo);
        public static IEnumerable<object[]> TwoSpanInFloatOutData()
        {
            yield return Create<float>((x, y) =>
            {
                float sum = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    sum += (x[i] - y[i]) * (x[i] - y[i]);
                }
                return float.Sqrt(sum);
            }, Tensor.Distance);
            yield return Create<float>((x, y) =>
            {
                float sum = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    sum += x[i] * y[i];
                }
                return sum;
            }, Tensor.Dot);

            static object[] Create<T>(PerformCalculationTwoSpanInFloatOut<T> tensorPrimitivesMethod, PerformTwoSpanInFloatOut<T> tensorOperation)
                => new object[] { tensorPrimitivesMethod, tensorOperation };
        }

        [Theory, MemberData(nameof(TwoSpanInFloatOutData))]
        public void TensorExtensionsTwoSpanInFloatOut<T>(PerformCalculationTwoSpanInFloatOut<T> tensorPrimitivesOperation, PerformTwoSpanInFloatOut<T> tensorOperation)
            where T: INumberBase<T>, IComparisonOperators<T, T, bool>
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
            Tensor<int> tensor = Tensor.Create<int>(a, lengths: [1, 1, 1, 1, 1, 6]);
            Assert.Equal(6, tensor.Rank);

            Assert.Equal(6, tensor.Lengths.Length);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);
            Assert.Equal(1, tensor.Lengths[3]);
            Assert.Equal(1, tensor.Lengths[4]);
            Assert.Equal(6, tensor.Lengths[5]);
            Assert.Equal(6, tensor.Strides.Length);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);
            Assert.Equal(0, tensor.Strides[2]);
            Assert.Equal(0, tensor.Strides[3]);
            Assert.Equal(0, tensor.Strides[4]);
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
            tensor = Tensor.Create<int>(a, lengths: [1, 2, 2, 1, 1, 3]);
            Assert.Equal(6, tensor.Lengths.Length);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Lengths[2]);
            Assert.Equal(1, tensor.Lengths[3]);
            Assert.Equal(1, tensor.Lengths[4]);
            Assert.Equal(3, tensor.Lengths[5]);
            Assert.Equal(6, tensor.Strides.Length);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(6, tensor.Strides[1]);
            Assert.Equal(3, tensor.Strides[2]);
            Assert.Equal(0, tensor.Strides[3]);
            Assert.Equal(0, tensor.Strides[4]);
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
            Tensor<int> t1 = Tensor.CreateFromShapeUninitialized<int>([1]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Make sure can't index too many dimensions
            Assert.Throws<ArgumentOutOfRangeException>(() =>
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
                Tensor<int> t1 = Tensor.CreateFromShapeUninitialized<int>([-1]);
            });

            t1 = Tensor.CreateFromShapeUninitialized<int>([0]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            t1 = Tensor.CreateFromShapeUninitialized<int>([]);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Null should behave like empty array since there is no "null" span.
            t1 = Tensor.CreateFromShapeUninitialized<int>(null);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Make sure pinned works
            t1 = Tensor.CreateFromShapeUninitialized<int>([1], true);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.True(t1.IsPinned);

            // Make sure 2D array works with basic strides
            t1 = Tensor.CreateFromShapeUninitialized<int>([2, 2], [2, 1]);
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
            t1 = Tensor.CreateFromShapeUninitialized<int>([2, 2], [0, 1]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            // Can't validate actual values since it's uninitialized
            // But since it loops over the first 2 elements we can assert the results are the same for those.
            Assert.Equal(t1[0, 0], t1[1, 0]);
            Assert.Equal(t1[0, 1], t1[1, 1]);

            // Make sure 2D array works with strides of all 0 to loop over first element again
            t1 = Tensor.CreateFromShapeUninitialized<int>([2, 2], [0, 0]);
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
                var t1 = Tensor.CreateFromShapeUninitialized<int>([1, 2], [-1, 0], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateFromShapeUninitialized<int>([1, 2], [0, -1], false);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateFromShapeUninitialized<int>([-1, 2], [], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateFromShapeUninitialized<int>([1, -2], [], false);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            t1 = Tensor.CreateFromShapeUninitialized<int>([2, 2], [2, 0]);
            Assert.Equal(2, t1.Rank);
            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(t1[0, 0], t1[0, 1]);
            Assert.Equal(t1[1, 0], t1[1, 1]);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentException>(() => {
                var t1 = Tensor.CreateFromShapeUninitialized<int>([2, 2], [1, 1], false);
            });
        }

        [Fact]
        public static void TensorFactoryCreateTests()
        {
            // Basic tensor creation
            Tensor<int> t1 = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)([1]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);
            Assert.Equal(0, t1[0]);

            // Make sure can't index too many dimensions
            Assert.Throws<ArgumentOutOfRangeException>(() =>
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
                Tensor<int> t1 = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)([-1]));
            });

            t1 = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)([0]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            t1 = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)([]));
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(0, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
            Assert.False(t1.IsPinned);

            // Null should behave like empty array since there is no "null" span.
            t1 = Tensor.Create<int>(null);
            Assert.Equal(0, t1.Rank);
            Assert.Equal(0, t1.Lengths.Length);
            Assert.Equal(0, t1.Strides.Length);
            Assert.False(t1.IsPinned);

            // Make sure pinned works
            t1 = Tensor.CreateFromShape<int>([(nint)1], true);
            Assert.Equal(1, t1.Rank);
            Assert.Equal(1, t1.Lengths.Length);
            Assert.Equal(1, t1.Lengths[0]);
            Assert.Equal(1, t1.Strides.Length);
            Assert.Equal(0, t1.Strides[0]);
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
                var t1 = Tensor.CreateFromShape<int>((Span<nint>)[1, 2], [-1, 0], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var t1 = Tensor.CreateFromShape<int>((Span<nint>)[1, 2], [0, -1], false);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateFromShape<int>([-1, (nint)2], false);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var t1 = Tensor.CreateFromShape<int>([(nint)1, -2], false);
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
            Assert.Throws<ArgumentException>(() => {
                var t1 = Tensor.CreateFromShape<int>((Span<nint>)[2, 2], [1, 1], false);
            });
        }

        [Fact]
        public static void TensorCreateSingleElementTests()
        {
            // Tensor.Create with a single-element array should have stride 0
            Tensor<double> src = Tensor.Create([1.0]);
            Assert.Equal(1, src.Rank);
            Assert.Equal(1, src.Lengths[0]);
            Assert.Equal(0, src.Strides[0]);
            Assert.Equal(1, src.FlattenedLength);
            Assert.Equal(1.0, src[0]);

            // CreateFromShapeUninitialized without strides should work
            Tensor<double> dst = Tensor.CreateFromShapeUninitialized<double>(src.Lengths);
            Assert.Equal(1, dst.Rank);
            Assert.Equal(1, dst.Lengths[0]);
            Assert.Equal(0, dst.Strides[0]);
            Assert.Equal(1, dst.FlattenedLength);

            // CopyTo should succeed
            src.CopyTo(dst);
            Assert.Equal(1.0, dst[0]);

            // CreateFromShapeUninitialized with explicit strides should work
            dst = Tensor.CreateFromShapeUninitialized<double>(src.Lengths, src.Strides);
            Assert.Equal(1, dst.Rank);
            Assert.Equal(1, dst.Lengths[0]);
            Assert.Equal(0, dst.Strides[0]);
            Assert.Equal(1, dst.FlattenedLength);

            src.CopyTo(dst);
            Assert.Equal(1.0, dst[0]);

            // CreateFromShape without strides should also work
            dst = Tensor.CreateFromShape<double>(src.Lengths);
            Assert.Equal(1, dst.Rank);
            Assert.Equal(1, dst.Lengths[0]);
            Assert.Equal(0, dst.Strides[0]);
            Assert.Equal(1, dst.FlattenedLength);

            src.CopyTo(dst);
            Assert.Equal(1.0, dst[0]);

            // CreateFromShape with explicit strides should also work
            dst = Tensor.CreateFromShape<double>(src.Lengths, src.Strides);
            Assert.Equal(1, dst.Rank);
            Assert.Equal(1, dst.Lengths[0]);
            Assert.Equal(0, dst.Strides[0]);
            Assert.Equal(1, dst.FlattenedLength);

            src.CopyTo(dst);
            Assert.Equal(1.0, dst[0]);

            // TensorSpan from single-element span should also have stride 0
            Span<double> span = [42.0];
            TensorSpan<double> tensorSpan = new TensorSpan<double>(span);
            Assert.Equal(1, tensorSpan.Rank);
            Assert.Equal(1, tensorSpan.Lengths[0]);
            Assert.Equal(0, tensorSpan.Strides[0]);
            Assert.Equal(1, tensorSpan.FlattenedLength);
            Assert.Equal(42.0, tensorSpan[0]);

            // ReadOnlyTensorSpan from single-element span should also have stride 0
            ReadOnlySpan<double> roSpan = [42.0];
            ReadOnlyTensorSpan<double> roTensorSpan = new ReadOnlyTensorSpan<double>(roSpan);
            Assert.Equal(1, roTensorSpan.Rank);
            Assert.Equal(1, roTensorSpan.Lengths[0]);
            Assert.Equal(0, roTensorSpan.Strides[0]);
            Assert.Equal(1, roTensorSpan.FlattenedLength);
            Assert.Equal(42.0, roTensorSpan[0]);
        }

        [Fact]
        public static void TensorCosineSimilarityTests()
        {
            float[] a = [0, 0, 0, 1, 1, 1];
            float[] b = [1, 0, 0, 1, 1, 0];

            Tensor<float> left = Tensor.Create<float>(a, lengths: [2,3]);
            Tensor<float> right = Tensor.Create<float>(b, lengths: [2,3]);

            Tensor<float> result = Tensor.Create<float>(a, lengths: [2, 1]);

            result[0, 0] = Tensor.CosineSimilarity(left.AsReadOnlyTensorSpan([0..1, 0.. ]), right[0..1, 0..]);
            result[1, 0] = Tensor.CosineSimilarity(left.AsReadOnlyTensorSpan([1.., 0..]), right[1.., 0..]);

            Assert.Equal(2, result.Rank);
            Assert.Equal(2, result.Lengths[0]);
            Assert.Equal(1, result.Lengths[1]);

            Assert.Equal(float.NaN, result[0, 0]);
            Assert.Equal(0.81649, result[1, 0], .00001);
        }

        //[Fact]
        //public static void TensorSequenceEqualTests()
        //{
        //    Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    Tensor<bool> equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([3], equal.Lengths.ToArray());
        //    Assert.True(equal[0]);
        //    Assert.True(equal[1]);
        //    Assert.True(equal[2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.True(equal[0, 1]);
        //    Assert.True(equal[0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [1, 1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0, 0]);
        //    Assert.True(equal[0, 0, 1]);
        //    Assert.True(equal[0, 0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [1, 3]);
        //    equal = Tensor.SequenceEqual(t0, t1);

        //    Assert.Equal([1, 3], equal.Lengths.ToArray());
        //    Assert.True(equal[0, 0]);
        //    Assert.True(equal[0, 1]);
        //    Assert.True(equal[0, 2]);

        //    t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [3, 1]);
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

        //    t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [1, 3]);
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), [3, 1]);
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
        //    t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), default);
        //    Assert.Throws<Exception>(() => Tensor.SequenceEqual(t0, t1));
        //}

        /// <summary>
        /// Provides test cases of (dataArray, shape, strides) for creating non-dense tensor spans.
        /// Each case has known logical elements that differ from the raw buffer layout.
        /// </summary>
        public static IEnumerable<object[]> NonDenseTensorData()
        {
            // 2x2 from 1D array with stride gap: logical elements are [10, 20, 30, 40]
            yield return new object[] { new int[] { 10, 20, 99, 99, 30, 40, 99, 99 }, new nint[] { 2, 2 }, new nint[] { 4, 1 }, new int[] { 10, 20, 30, 40 } };
            // 2x3 from 1D array with stride gap: logical elements are [1, 2, 3, 4, 5, 6]
            yield return new object[] { new int[] { 1, 2, 3, 99, 4, 5, 6, 99 }, new nint[] { 2, 3 }, new nint[] { 4, 1 }, new int[] { 1, 2, 3, 4, 5, 6 } };
            // 3x2 from 1D array with stride gap: logical elements are [1, 2, 3, 4, 5, 6]
            yield return new object[] { new int[] { 1, 2, 99, 3, 4, 99, 5, 6, 99 }, new nint[] { 3, 2 }, new nint[] { 3, 1 }, new int[] { 1, 2, 3, 4, 5, 6 } };
            // 1x2 from 1D array with larger stride gap
            yield return new object[] { new int[] { 42, 99, 99, 99, 7, 99, 99, 99 }, new nint[] { 2 }, new nint[] { 4 }, new int[] { 42, 7 } };
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorSequenceEqualNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            var ts1 = new ReadOnlyTensorSpan<int>(data, shape, strides);
            Assert.False(ts1.IsDense);

            // Non-dense vs non-dense with same logical elements but different gap values
            int[] data2 = (int[])data.Clone();
            for (int i = 0; i < data2.Length; i++)
            {
                if (data2[i] == 99)
                {
                    data2[i] = 77;
                }
            }
            var ts2 = new ReadOnlyTensorSpan<int>(data2, shape, strides);
            Assert.False(ts2.IsDense);
            Assert.True(ts1.SequenceEqual(ts2));

            // Non-dense vs dense with same logical elements
            var tsDense = new ReadOnlyTensorSpan<int>(expectedLogical, shape);
            Assert.True(tsDense.IsDense);
            Assert.True(ts1.SequenceEqual(tsDense));
            Assert.True(tsDense.SequenceEqual(ts1));

            // TensorSpan overload also works
            var tspan1 = new TensorSpan<int>(data, shape, strides);
            Assert.True(tspan1.SequenceEqual(ts2));

            // Differing logical element should return false
            int[] data3 = (int[])data.Clone();
            data3[0] = data3[0] + 1;
            var ts3 = new ReadOnlyTensorSpan<int>(data3, shape, strides);
            Assert.False(ts1.SequenceEqual(ts3));
        }

        [Theory]
        [InlineData(4, 16)]
        [InlineData(8, 8)]
        [InlineData(2, 32)]
        public static void TensorFlattenAndCompareContiguousRows(int rows, int columns)
        {
            int rowStride = columns + 2;
            int[] backing = Enumerable.Repeat(-11, rows * rowStride).ToArray();
            int[] expected = Enumerable.Range(0, rows * columns).ToArray();
            nint[] lengths = [rows, columns];
            nint[] strides = [rowStride, 1];

            for (int row = 0; row < rows; row++)
            {
                expected.AsSpan(row * columns, columns).CopyTo(backing.AsSpan(row * rowStride, columns));
            }

            ReadOnlyTensorSpan<int> source = new ReadOnlyTensorSpan<int>(backing, lengths, strides);
            ReadOnlyTensorSpan<int> dense = new ReadOnlyTensorSpan<int>(expected, lengths);
            int[] destination = Enumerable.Repeat(-7, expected.Length + 2).ToArray();
            source.FlattenTo(destination);
            Assert.Equal(expected.Concat([-7, -7]), destination);
            Assert.True(source.SequenceEqual(dense));
            Assert.True(dense.SequenceEqual(source));

            backing[(rows - 1) * rowStride + columns - 1] = -99;
            Assert.False(source.SequenceEqual(dense));
        }

        /// <summary>
        /// Computes the set of buffer offsets that correspond to logical elements in a non-dense tensor.
        /// </summary>
        private static HashSet<int> ComputeLogicalOffsets(nint[] shape, nint[] strides, nint flattenedLength)
        {
            HashSet<int> logicalOffsets = new HashSet<int>();
            for (nint i = 0; i < flattenedLength; i++)
            {
                nint offset = 0;
                nint remaining = i;
                for (int d = shape.Length - 1; d >= 0; d--)
                {
                    nint dimIndex = remaining % shape[d];
                    remaining /= shape[d];
                    offset += dimIndex * strides[d];
                }
                logicalOffsets.Add((int)offset);
            }
            return logicalOffsets;
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorFillGaussianNormalDistributionNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            double[] dblData = new double[data.Length];
            var ts = new TensorSpan<double>(dblData, shape, strides);
            Assert.False(ts.IsDense);

            Tensor.FillGaussianNormalDistribution(ts, new Random(42));

            // All logical elements should be filled
            foreach (double val in ts)
            {
                Assert.NotEqual(0.0, val);
            }

            // Gap positions should remain untouched (zero)
            HashSet<int> logicalOffsets = ComputeLogicalOffsets(shape, strides, ts.FlattenedLength);
            for (int i = 0; i < dblData.Length; i++)
            {
                if (!logicalOffsets.Contains(i))
                {
                    Assert.Equal(0.0, dblData[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorFillUniformDistributionNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            double[] dblData = new double[data.Length];
            var ts = new TensorSpan<double>(dblData, shape, strides);
            Assert.False(ts.IsDense);

            Tensor.FillUniformDistribution(ts, new Random(42));

            // All logical elements should be filled with values in [0, 1)
            foreach (double val in ts)
            {
                Assert.InRange(val, 0.0, 1.0);
                Assert.NotEqual(0.0, val);
            }

            // Gap positions should remain untouched (zero)
            HashSet<int> logicalOffsets = ComputeLogicalOffsets(shape, strides, ts.FlattenedLength);
            for (int i = 0; i < dblData.Length; i++)
            {
                if (!logicalOffsets.Contains(i))
                {
                    Assert.Equal(0.0, dblData[i]);
                }
            }
        }

        [Theory]
        [InlineData(false, 2, 4)]
        [InlineData(false, 8, 16)]
        [InlineData(true, 2, 4)]
        [InlineData(true, 8, 16)]
        public static void TensorFillNonDensePreservesLogicalRandomOrder(bool gaussian, int rows, int columns)
        {
            double[] backing = new double[rows * (columns + 3)];
            double[] expected = new double[rows * columns];
            TensorSpan<double> strided = new TensorSpan<double>(backing, [rows, columns], [columns + 3, 1]);
            TensorSpan<double> dense = new TensorSpan<double>(expected, [rows, columns]);

            if (gaussian)
            {
                Tensor.FillGaussianNormalDistribution(strided, new Random(42));
                Tensor.FillGaussianNormalDistribution(dense, new Random(42));
            }
            else
            {
                Tensor.FillUniformDistribution(strided, new Random(42));
                Tensor.FillUniformDistribution(dense, new Random(42));
            }

            double[] actual = new double[expected.Length];
            strided.FlattenTo(actual);
            Assert.Equal(expected, actual);
            for (int row = 0; row < rows; row++)
            {
                Assert.Equal([0.0, 0.0, 0.0], backing.AsSpan(row * (columns + 3) + columns, 3).ToArray());
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public static void TensorCopyToIndependentContiguousRuns(int layout, bool highRank)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                T[] sourceData = Enumerable.Range(0, 220).Select(createValue).ToArray();
                nint[] lengths = [2, 3, 16];
                nint[] sourceLengths = layout == 2 ? [16] : lengths;
                nint[] sourceStrides = layout switch
                {
                    0 => [],
                    1 => [55, 16, 1],
                    2 => [1],
                    3 => [17, 36, 1],
                    _ => [110, 34, 2],
                };
                nint[] destinationStrides = [64, 19, 1];
                if (highRank)
                {
                    lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                    destinationStrides = [0, 0, 0, 0, 0, 0, 0, 0, .. destinationStrides];
                }
                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, 1, sourceLengths, sourceStrides);
                T[] destinationData = new T[130];
                Array.Fill(destinationData, sentinel);
                TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, lengths, destinationStrides);

                Assert.True(source.TryCopyTo(destination));

                T[] expected = new T[destinationData.Length];
                Array.Fill(expected, sentinel);
                for (int i = 0; i < 96; i++)
                {
                    int sourceOffset = layout switch
                    {
                        0 => i,
                        1 => i / 48 * 55 + i % 48,
                        2 => i % 16,
                        3 => i / 48 * 17 + i / 16 % 3 * 36 + i % 16,
                        _ => i / 48 * 110 + i / 16 % 3 * 34 + i % 16 * 2,
                    };
                    expected[1 + i / 48 * 64 + i / 16 % 3 * 19 + i % 16] = sourceData[1 + sourceOffset];
                }
                Assert.Equal(expected, destinationData);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public static void TensorValueReductionsPreserveScalarOrder(int layout)
        {
            Verify([1e16, 1, -1e16, 3, -2, 0.5]);
            Verify([double.NegativeInfinity, double.PositiveInfinity, -0.0, 0.0, double.NaN, -3]);
            Verify([-0.0, 0.0]);

            void Verify(double[] values)
            {
                nint[] lengths = [3, 32];
                nint[] strides = layout switch
                {
                    0 => [32, 1],
                    1 or 5 => [35, 1],
                    2 => [70, 2],
                    3 => [0, 1],
                    _ => [1, 3],
                };
                double[] backing = new double[220];
                for (int i = 0; i < 96; i++)
                {
                    backing[1 + i / 32 * (int)strides[0] + i % 32 * (int)strides[1]] = values[i % values.Length];
                }
                if (layout == 5)
                {
                    lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                    strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                }
                ReadOnlyTensorSpan<double> source = new ReadOnlyTensorSpan<double>(backing, 1, lengths, strides);
                double[] logical = new double[96];
                source.FlattenTo(logical);
                double sum = 0;
                double product = 1;
                double squares = 0;
                double[] extrema = Enumerable.Repeat(logical[0], 8).ToArray();
                foreach (double value in logical)
                {
                    sum += value;
                    product *= value;
                    squares += value * value;
                    extrema[0] = double.Max(value, extrema[0]);
                    extrema[1] = double.Min(value, extrema[1]);
                    extrema[2] = double.MaxMagnitude(value, extrema[2]);
                    extrema[3] = double.MinMagnitude(value, extrema[3]);
                    extrema[4] = double.MaxNumber(value, extrema[4]);
                    extrema[5] = double.MinNumber(value, extrema[5]);
                    extrema[6] = double.MaxMagnitudeNumber(value, extrema[6]);
                    extrema[7] = double.MinMagnitudeNumber(value, extrema[7]);
                }
                AssertBits(sum, Tensor.Sum(source));
                AssertBits(product, Tensor.Product(source));
                AssertBits(Math.Sqrt(squares), Tensor.Norm(source));
                AssertBits(sum / logical.Length, Tensor.Average(source));
                AssertBits(extrema[0], Tensor.Max(source));
                AssertBits(extrema[1], Tensor.Min(source));
                AssertBits(extrema[2], Tensor.MaxMagnitude(source));
                AssertBits(extrema[3], Tensor.MinMagnitude(source));
                AssertBits(extrema[4], Tensor.MaxNumber(source));
                AssertBits(extrema[5], Tensor.MinNumber(source));
                AssertBits(extrema[6], Tensor.MaxMagnitudeNumber(source));
                AssertBits(extrema[7], Tensor.MinMagnitudeNumber(source));
                double mean = sum / logical.Length;
                double variance = 0;
                foreach (double value in logical)
                {
                    double difference = double.Abs(value - mean);
                    variance += difference * difference;
                }
                AssertBits(Math.Sqrt(variance / logical.Length), Tensor.StdDev(source));

                foreach (bool broadcast in new[] { false, true })
                {
                    double[] otherData = Enumerable.Range(0, broadcast ? 32 : 96).Select(i => (double)(i % 7 - 3)).ToArray();
                    ReadOnlyTensorSpan<double> other = new ReadOnlyTensorSpan<double>(otherData, broadcast ? [32] : lengths);
                    double dot = 0;
                    double otherSquares = 0;
                    double differences = 0;
                    for (int i = 0; i < logical.Length; i++)
                    {
                        double x = logical[i];
                        double y = otherData[i % otherData.Length];
                        dot += x * y;
                        otherSquares += y * y;
                        differences += (x - y) * (x - y);
                    }
                    AssertBits(dot, Tensor.Dot(source, other));
                    AssertBits(dot, Tensor.Dot(other, source));
                    AssertBits(Math.Sqrt(differences), Tensor.Distance(source, other));
                    AssertBits(dot / (Math.Sqrt(squares) * Math.Sqrt(otherSquares)), Tensor.CosineSimilarity(source, other));
                }
            }

            static void AssertBits(double expected, double actual)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 96)]
        [InlineData(1, 96)]
        [InlineData(2, 96)]
        [InlineData(3, 96)]
        [InlineData(4, 96)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        [InlineData(5, 96)]
        public static void TensorPredicatesPreserveOrderAndShortCircuit(int layout, int count)
        {
            int rows = count == 96 ? 3 : count;
            int columns = count == 96 ? 32 : 1;
            nint[] lengths = [rows, columns];
            nint[] strides = layout switch
            {
                0 => [columns, 1],
                1 or 5 => [columns + 3, 1],
                2 => [(columns + 3) * 2, 2],
                3 => [0, 1],
                _ => [1, rows],
            };
            if (count <= 1)
            {
                Array.Clear(strides);
            }
            if (layout == 5)
            {
                lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
            }

            for (int operation = 0; operation < 10; operation++)
            {
                for (int operand = 0; operand < 3; operand++)
                {
                    List<(int, int)> trace = [];
                    PredicateValue[] backing = new PredicateValue[220];
                    int normal = operation switch { 0 or 4 or 8 => 3, 1 or 2 or 7 or 9 => 4, _ => 2 };
                    int decisive = operation switch { 0 or 3 or 6 or 8 => 4, 1 or 5 or 9 => 3, _ => 2 };
                    for (int i = 0; i < count; i++)
                    {
                        nint offset = 1 + i / columns * strides[^2] + i % columns * strides[^1];
                        backing[offset] = new PredicateValue(i == Math.Min(count - 1, 65) ? decisive : normal, i, trace);
                    }
                    ReadOnlyTensorSpan<PredicateValue> source = new ReadOnlyTensorSpan<PredicateValue>(backing, 1, lengths, strides);
                    PredicateValue[] logical = new PredicateValue[count];
                    source.FlattenTo(logical);
                    PredicateValue[] rightData = Enumerable.Range(0, operand == 1 ? columns : count)
                        .Select(i => new PredicateValue(3, 1000 + i, trace)).ToArray();
                    ReadOnlyTensorSpan<PredicateValue> right = new ReadOnlyTensorSpan<PredicateValue>(rightData, operand == 1 ? [columns] : lengths);
                    PredicateValue scalar = new PredicateValue(3, -1, trace);
                    List<(int, int)> expectedTrace = [];
                    bool all = operation % 2 == 0;
                    bool expected = all;
                    for (int i = 0; i < count; i++)
                    {
                        PredicateValue left = logical[i];
                        PredicateValue other = operand == 2 ? scalar : rightData[i % rightData.Length];
                        expectedTrace.Add((left.Index, other.Index));
                        bool condition = operation switch
                        {
                            0 or 1 => left.Value == other.Value,
                            2 or 3 => left.Value > other.Value,
                            4 or 5 => left.Value >= other.Value,
                            6 or 7 => left.Value < other.Value,
                            _ => left.Value <= other.Value,
                        };
                        if (condition != all)
                        {
                            expected = condition;
                            break;
                        }
                    }
                    bool actual = operand == 2 ? operation switch
                    {
                        0 => Tensor.EqualsAll(source, scalar),
                        1 => Tensor.EqualsAny(source, scalar),
                        2 => Tensor.GreaterThanAll(source, scalar),
                        3 => Tensor.GreaterThanAny(source, scalar),
                        4 => Tensor.GreaterThanOrEqualAll(source, scalar),
                        5 => Tensor.GreaterThanOrEqualAny(source, scalar),
                        6 => Tensor.LessThanAll(source, scalar),
                        7 => Tensor.LessThanAny(source, scalar),
                        8 => Tensor.LessThanOrEqualAll(source, scalar),
                        _ => Tensor.LessThanOrEqualAny(source, scalar),
                    } : operation switch
                    {
                        0 => Tensor.EqualsAll(source, right),
                        1 => Tensor.EqualsAny(source, right),
                        2 => Tensor.GreaterThanAll(source, right),
                        3 => Tensor.GreaterThanAny(source, right),
                        4 => Tensor.GreaterThanOrEqualAll(source, right),
                        5 => Tensor.GreaterThanOrEqualAny(source, right),
                        6 => Tensor.LessThanAll(source, right),
                        7 => Tensor.LessThanAny(source, right),
                        8 => Tensor.LessThanOrEqualAll(source, right),
                        _ => Tensor.LessThanOrEqualAny(source, right),
                    };
                    Assert.Equal(expected, actual);
                    Assert.Equal(expectedTrace, trace);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorPredicatesPreserveNaNComparisons(bool padded)
        {
            double[] samples = [-0.0, 0.0, double.NaN, -1, 1, double.PositiveInfinity, double.NegativeInfinity];
            double[] backing = new double[105];
            int stride = padded ? 35 : 32;
            double[] logical = new double[96];
            for (int i = 0; i < logical.Length; i++)
            {
                logical[i] = samples[i % samples.Length];
                backing[i / 32 * stride + i % 32] = logical[i];
            }
            ReadOnlyTensorSpan<double> source = new ReadOnlyTensorSpan<double>(backing, [3, 32], [stride, 1]);
            foreach (double value in samples)
            {
                double[] row = new double[32];
                Array.Fill(row, value);
                ReadOnlyTensorSpan<double> other = new ReadOnlyTensorSpan<double>(row);
                Assert.Equal(logical.All(x => x == value), Tensor.EqualsAll(source, other));
                Assert.Equal(logical.Any(x => x == value), Tensor.EqualsAny(source, other));
                Assert.Equal(logical.All(x => x > value), Tensor.GreaterThanAll(source, other));
                Assert.Equal(logical.Any(x => x > value), Tensor.GreaterThanAny(source, other));
                Assert.Equal(logical.All(x => x >= value), Tensor.GreaterThanOrEqualAll(source, other));
                Assert.Equal(logical.Any(x => x >= value), Tensor.GreaterThanOrEqualAny(source, other));
                Assert.Equal(logical.All(x => x < value), Tensor.LessThanAll(source, other));
                Assert.Equal(logical.Any(x => x < value), Tensor.LessThanAny(source, other));
                Assert.Equal(logical.All(x => x <= value), Tensor.LessThanOrEqualAll(source, other));
                Assert.Equal(logical.Any(x => x <= value), Tensor.LessThanOrEqualAny(source, other));
                Assert.Equal(logical.All(x => x == value), Tensor.EqualsAll(source, value));
                Assert.Equal(logical.Any(x => x == value), Tensor.EqualsAny(source, value));
                Assert.Equal(logical.All(x => x > value), Tensor.GreaterThanAll(source, value));
                Assert.Equal(logical.Any(x => x > value), Tensor.GreaterThanAny(source, value));
                Assert.Equal(logical.All(x => x >= value), Tensor.GreaterThanOrEqualAll(source, value));
                Assert.Equal(logical.Any(x => x >= value), Tensor.GreaterThanOrEqualAny(source, value));
                Assert.Equal(logical.All(x => x < value), Tensor.LessThanAll(source, value));
                Assert.Equal(logical.Any(x => x < value), Tensor.LessThanAny(source, value));
                Assert.Equal(logical.All(x => x <= value), Tensor.LessThanOrEqualAll(source, value));
                Assert.Equal(logical.Any(x => x <= value), Tensor.LessThanOrEqualAny(source, value));
                Assert.Equal(logical.All(x => value > x), Tensor.GreaterThanAll(value, source));
                Assert.Equal(logical.Any(x => value > x), Tensor.GreaterThanAny(value, source));
            }
        }

        private readonly struct PredicateValue(int value, int index, List<(int, int)> trace) : IComparisonOperators<PredicateValue, PredicateValue, bool>
        {
            public int Value => value;
            public int Index => index;

            private List<(int, int)> Trace => trace;
            private static void Track(PredicateValue left, PredicateValue right) => left.Trace.Add((left.Index, right.Index));

            public static bool operator ==(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value == right.Value;
            }
            public static bool operator !=(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value != right.Value;
            }
            public static bool operator >(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value > right.Value;
            }
            public static bool operator >=(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value >= right.Value;
            }
            public static bool operator <(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value < right.Value;
            }
            public static bool operator <=(PredicateValue left, PredicateValue right)
            {
                Track(left, right);
                return left.Value <= right.Value;
            }
            public override bool Equals(object? obj) => obj is PredicateValue other && Value == other.Value;
            public override int GetHashCode() => Value;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public static void TensorReverseIndependentContiguousRuns(int layout)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                nint[] lengths = [2, 3, 16];
                nint[] sourceLengths = layout is 4 or 8 ? [16] : lengths;
                nint[] sourceStrides = layout switch
                {
                    0 or 7 => [],
                    1 or 5 => [55, 16, 1],
                    2 => [60, 19, 1],
                    3 => [17, 36, 1],
                    4 or 8 => [1],
                    _ => [110, 34, 2],
                };
                nint[] destinationStrides = layout switch
                {
                    0 => [64, 19, 1],
                    1 => [],
                    7 => [110, 34, 2],
                    8 => [0, 0, 1],
                    _ => [55, 16, 1],
                };
                if (layout == 5)
                {
                    lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                    sourceLengths = lengths;
                    sourceStrides = [0, 0, 0, 0, 0, 0, 0, 0, .. sourceStrides];
                    destinationStrides = [0, 0, 0, 0, 0, 0, 0, 0, .. destinationStrides];
                }
                T[] sourceData = Enumerable.Range(0, 220).Select(createValue).ToArray();
                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, 1, sourceLengths, sourceStrides);
                T[] destinationData = new T[220];
                Array.Fill(destinationData, sentinel);
                TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, lengths, destinationStrides);
                Tensor.Reverse(source, destination);

                T[] expected = new T[destinationData.Length];
                Array.Fill(expected, sentinel);
                for (int i = 0; i < 96; i++)
                {
                    int reversed = 95 - i;
                    int sourceOffset = layout switch
                    {
                        0 or 7 => reversed,
                        1 or 5 => reversed / 48 * 55 + reversed % 48,
                        2 => reversed / 48 * 60 + reversed / 16 % 3 * 19 + reversed % 16,
                        3 => reversed / 48 * 17 + reversed / 16 % 3 * 36 + reversed % 16,
                        4 or 8 => reversed % 16,
                        _ => reversed / 48 * 110 + reversed / 16 % 3 * 34 + reversed % 16 * 2,
                    };
                    int targetOffset = layout switch
                    {
                        0 => i / 48 * 64 + i / 16 % 3 * 19 + i % 16,
                        1 => i,
                        7 => i / 48 * 110 + i / 16 % 3 * 34 + i % 16 * 2,
                        8 => i % 16,
                        _ => i / 48 * 55 + i % 48,
                    };
                    expected[1 + targetOffset] = sourceData[1 + sourceOffset];
                }
                Assert.Equal(expected, destinationData);
                Assert.Equal(Enumerable.Range(0, 220).Select(createValue), sourceData);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(16)]
        [InlineData(65)]
        [InlineData(256)]
        public static void TensorDenseReversePreservesExtentAndReferences(int columns)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                int count = 3 * columns;
                T[] sourceData = Enumerable.Range(0, count + 2).Select(createValue).ToArray();
                T[] destinationData = new T[count + 2];
                Array.Fill(destinationData, sentinel);
                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, 1, [3, columns], []);
                TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, [3, columns], []);
                Tensor.Reverse(source, destination);
                Assert.Equal(sourceData.Skip(1).Take(count).Reverse(), destinationData.Skip(1).Take(count));
                Assert.Equal(sentinel, destinationData[0]);
                Assert.Equal(sentinel, destinationData[count + 1]);

                Tensor.Reverse(destination.AsReadOnlyTensorSpan(), destination);
                Assert.Equal(sourceData.Skip(1).Take(count), destinationData.Skip(1).Take(count));
                Tensor.ReverseDimension(destination.AsReadOnlyTensorSpan(), destination, 1);
                for (int row = 0; row < 3; row++)
                {
                    Assert.Equal(sourceData.Skip(1 + row * columns).Take(columns).Reverse(),
                        destinationData.Skip(1 + row * columns).Take(columns));
                }
                Assert.Equal(sentinel, destinationData[0]);
                Assert.Equal(sentinel, destinationData[count + 1]);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(16, 0)]
        [InlineData(31, 0)]
        [InlineData(32, 0)]
        [InlineData(33, 0)]
        [InlineData(255, 0)]
        [InlineData(256, 0)]
        [InlineData(257, 0)]
        [InlineData(1025, 0)]
        [InlineData(8193, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(16, 1)]
        [InlineData(31, 1)]
        [InlineData(32, 1)]
        [InlineData(33, 1)]
        [InlineData(255, 1)]
        [InlineData(256, 1)]
        [InlineData(257, 1)]
        [InlineData(1025, 1)]
        [InlineData(8193, 1)]
        public static void TensorDenseReverseBlocksPreservesExtentAndReferences(int columns, int dimension)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                int count = 4 * 5 * columns;
                T[] backing = new T[count + 2];
                Array.Fill(backing, sentinel);
                for (int i = 0; i < count; i++)
                {
                    backing[i + 1] = createValue(i);
                }
                TensorSpan<T> tensor = new TensorSpan<T>(backing, 1, [4, 5, columns], []);
                Tensor.ReverseDimension(tensor.AsReadOnlyTensorSpan(), tensor, dimension);
                for (int i = 0; i < count; i++)
                {
                    int row = i / (5 * columns);
                    int middle = i / columns % 5;
                    int column = i % columns;
                    int source = dimension == 0
                        ? ((3 - row) * 5 + middle) * columns + column
                        : (row * 5 + 4 - middle) * columns + column;
                    Assert.Equal(createValue(source), backing[i + 1]);
                }
                Tensor.ReverseDimension(tensor.AsReadOnlyTensorSpan(), tensor, dimension);
                Assert.Equal(Enumerable.Range(0, count).Select(createValue), backing.Skip(1).Take(count));
                Assert.Equal(sentinel, backing[0]);
                Assert.Equal(sentinel, backing[count + 1]);
            }
        }

        [Fact]
        public static void TensorDenseReverseBlocksWithLargeElements()
        {
            LargeReverseValue[] values = new LargeReverseValue[514];
            for (int i = 0; i < values.Length; i++)
            {
                values[i].Value = i;
            }
            TensorSpan<LargeReverseValue> tensor = new TensorSpan<LargeReverseValue>(values, 1, [2, 256], []);
            Tensor.ReverseDimension(tensor.AsReadOnlyTensorSpan(), tensor, 0);
            for (int i = 0; i < 512; i++)
            {
                Assert.Equal((i + 256) % 512 + 1, values[i + 1].Value);
            }
            Assert.Equal(0, values[0].Value);
            Assert.Equal(513, values[513].Value);
        }

        [StructLayout(LayoutKind.Sequential, Size = 8192)]
        private struct LargeReverseValue
        {
            public int Value;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorOrderedReductionsPreserveDecimalOverflow(bool padded)
        {
            nint[] lengths = [2, 32];
            nint[] strides = [padded ? 35 : 32, 1];
            decimal[] backing = new decimal[70];
            backing[31] = decimal.MaxValue;
            backing[padded ? 35 : 32] = -decimal.MaxValue;
            backing[padded ? 36 : 33] = 1;
            Assert.Equal(1, Tensor.Sum(new ReadOnlyTensorSpan<decimal>(backing, lengths, strides)));
            backing[padded ? 35 : 32] = 1;
            Assert.Throws<OverflowException>(() => Tensor.Sum(new ReadOnlyTensorSpan<decimal>(backing, lengths, strides)));

            Array.Fill(backing, 1);
            backing[31] = decimal.MaxValue;
            backing[padded ? 35 : 32] = 0;
            Assert.Equal(0, Tensor.Product(new ReadOnlyTensorSpan<decimal>(backing, lengths, strides)));
            backing[padded ? 35 : 32] = 2;
            backing[padded ? 36 : 33] = 0;
            Assert.Throws<OverflowException>(() => Tensor.Product(new ReadOnlyTensorSpan<decimal>(backing, lengths, strides)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void TensorStreamingIndexReductionsPreservePrimitiveSemantics(int layout)
        {
            Verify<double>([0.0, -0.0]);
            Verify<double>([-0.0, 0.0]);
            Verify<double>([double.NegativeInfinity, double.PositiveInfinity, -1, 1]);
            Verify<double>([1, -1, 1, -1]);
            Verify<double>([double.NaN, 3, double.NaN]);
            Verify<double>([3, double.NaN, 4, double.NaN]);
            Verify<double>([3, 3, 3]);
            Verify<double>([]);
            Verify<int>([int.MinValue, int.MaxValue, 0, -1, 1]);
            Verify<long>([long.MinValue, long.MaxValue, 0, -1, 1]);
            Verify<decimal>([decimal.MinValue, decimal.MaxValue, 0, -1, 1]);
            Verify<Half>([Half.NegativeZero, Half.Zero, Half.NaN]);
            double[] acrossRuns = new double[96];
            Array.Fill(acrossRuns, -0.0);
            acrossRuns[48] = 0.0;
            acrossRuns[64] = 0.0;
            Verify(acrossRuns);
            Array.Fill(acrossRuns, 0.0);
            acrossRuns[48] = -0.0;
            acrossRuns[64] = -0.0;
            Verify(acrossRuns);
            Array.Fill(acrossRuns, -3.0);
            acrossRuns[48] = 3.0;
            acrossRuns[64] = 3.0;
            Verify(acrossRuns);
            Array.Fill(acrossRuns, 1.0);
            acrossRuns[48] = double.NaN;
            acrossRuns[64] = double.NaN;
            Verify(acrossRuns);

            void Verify<T>(T[] values) where T : INumber<T>
            {
                int count = values.Length == 0 ? 0 : 96;
                T[] logical = new T[count];
                T[] backing = new T[250];
                nint[] lengths = layout == 0 ? [count] : layout is 1 or 4 ? [2, count / 2] : layout == 2 ? [3, count / 3] : [count];
                nint[] strides = count == 0 ? [] : layout switch
                {
                    0 => [2],
                    1 => [55, 1],
                    2 => [36, 1],
                    4 => [55, 1],
                    _ => [0],
                };
                for (int i = 0; i < count; i++)
                {
                    T value = values[i % values.Length];
                    int offset = layout switch
                    {
                        0 => i * 2,
                        1 or 4 => i / 48 * 55 + i % 48,
                        2 => i / 32 * 36 + i % 32,
                        _ => 0,
                    };
                    backing[offset + 1] = value;
                    logical[i] = layout == 3 ? values[(count - 1) % values.Length] : value;
                }
                if (layout == 4)
                {
                    lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                    strides = count == 0 ? [] : [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                }
                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(backing, 1, lengths, strides);

                Assert.Equal((nint)TensorPrimitives.IndexOfMax<T>(logical), Tensor.IndexOfMax(source));
                Assert.Equal((nint)TensorPrimitives.IndexOfMin<T>(logical), Tensor.IndexOfMin(source));
                Assert.Equal((nint)TensorPrimitives.IndexOfMaxMagnitude<T>(logical), Tensor.IndexOfMaxMagnitude(source));
                Assert.Equal((nint)TensorPrimitives.IndexOfMinMagnitude<T>(logical), Tensor.IndexOfMinMagnitude(source));
            }
        }

        [Fact]
        public static void TensorCopyToBroadcastDestinationPreservesCompatibility()
        {
            int[] sourceData = Enumerable.Range(1, 96).ToArray();
            int[] backing = new int[59];
            Array.Fill(backing, -1);
            ReadOnlyTensorSpan<int> source = new ReadOnlyTensorSpan<int>(sourceData, [2, 3, 16]);
            TensorSpan<int> destination = new TensorSpan<int>(backing, 1, [2, 3, 16], [0, 19, 1]);

            Assert.False(source.TryCopyTo(destination));
            ReadOnlyTensorSpan<int> broadcastSource = new ReadOnlyTensorSpan<int>(sourceData, [1, 3, 16], [0, 16, 1]);
            broadcastSource.CopyTo(destination);

            int[] expected = new int[backing.Length];
            Array.Fill(expected, -1);
            for (int i = 0; i < 48; i++)
            {
                expected[1 + i / 16 * 19 + i % 16] = sourceData[i];
            }
            Assert.Equal(expected, backing);
        }

        private const int LargeMemoryOutOfMemoryExitCode = 3;
        private const int LargeMemorySigKillExitCode = 128 + 9;
        private static bool s_isLargeMemoryTestChild;

        private static bool RunLargeMemoryTest(long peakMemoryBytes, string testName, params string[] arguments)
        {
            if (s_isLargeMemoryTestChild)
            {
                return false;
            }

            if (IntPtr.Size != sizeof(long))
            {
                throw new SkipTestException("Unable to allocate enough memory in a 32-bit process.");
            }

            long availableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            // Match the established large-allocation test policy of requiring memory for the peak plus equal headroom.
            long requiredMemoryBytes = checked(peakMemoryBytes * 2);
            if (availableMemoryBytes < requiredMemoryBytes)
            {
                throw new SkipTestException($"Prone to OOM killer. {availableMemoryBytes} bytes are available; {requiredMemoryBytes} bytes are required.");
            }

            string serializedArguments = string.Join(",", arguments);
            using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static (string testName, string serializedArguments) =>
            {
                // The child re-enters the existing body so test assertions and cleanup are not duplicated.
                s_isLargeMemoryTestChild = true;
                try
                {
                    MethodInfo? method = typeof(TensorTests).GetMethod(testName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (method is null)
                    {
                        throw new InvalidOperationException($"Could not find large-memory test '{testName}'.");
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    string[] values = serializedArguments.Length == 0 ? [] : serializedArguments.Split(',');
                    if (values.Length != parameters.Length)
                    {
                        throw new InvalidOperationException($"Invalid arguments for large-memory test '{testName}'.");
                    }

                    object?[] invokeArguments = new object?[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        invokeArguments[i] = parameters[i].ParameterType == typeof(bool)
                            ? bool.Parse(values[i])
                            : parameters[i].ParameterType == typeof(int)
                                ? int.Parse(values[i], CultureInfo.InvariantCulture)
                                : throw new InvalidOperationException($"Unsupported argument type for large-memory test '{testName}'.");
                    }

                    method.Invoke(null, invokeArguments);
                    return RemoteExecutor.SuccessExitCode;
                }
                catch (OutOfMemoryException)
                {
                    return LargeMemoryOutOfMemoryExitCode;
                }
                catch (TargetInvocationException exception) when (exception.InnerException is OutOfMemoryException)
                {
                    return LargeMemoryOutOfMemoryExitCode;
                }
            }, testName, serializedArguments, new RemoteInvokeOptions { CheckExitCode = false });

            handle.Process.WaitForExit();
            int exitCode = handle.Process.ExitCode;
            if (exitCode is LargeMemoryOutOfMemoryExitCode or LargeMemorySigKillExitCode)
            {
                throw new SkipTestException($"Ran out of memory allocating the large tensor test buffers. Exit code {exitCode}.");
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitCode);
            return true;
        }

        private static unsafe void* AllocateNativeMemory(nuint elementCount, nuint elementSize = 1, bool zeroInitialize = false)
        {
            void* allocation = zeroInitialize
                ? NativeMemory.AllocZeroed(elementCount, elementSize)
                : NativeMemory.Alloc(elementCount, elementSize);
            if (allocation is null)
            {
                throw new OutOfMemoryException();
            }

            return allocation;
        }

        internal static unsafe void TensorLargeDenseQueries(bool singletonDimensions, int query)
        {
            nint length = int.MaxValue;
            length += 65;
            long peakMemoryBytes = (long)(length + 130) * (query is 0 or 3 ? 2 : 1);
            if (RunLargeMemoryTest(peakMemoryBytes, nameof(TensorLargeDenseQueries), singletonDimensions.ToString(), query.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 130), zeroInitialize: true);
            byte* equalBacking = query is 0 or 3 ? (byte*)AllocateNativeMemory((nuint)(length + 130), zeroInitialize: true) : null;
            try
            {
                if (query == 2)
                {
                    NativeMemory.Fill(backing, (nuint)(length + 130), 7);
                }

                backing[0] = 7;
                backing[length + 1] = 7;
                nint[] lengths = singletonDimensions ? [1, length, 1] : [length];
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(backing + 1, length, lengths);
                nint extremeIndex = length - 33;
                if (query == 0)
                {
                    backing[1] = 7;
                    equalBacking[1] = 7;
                    // Different trailing guards ensure comparison stops at the logical extent.
                    equalBacking[length + 1] = 8;
                    ReadOnlyTensorSpan<byte> equal = new ReadOnlyTensorSpan<byte>(equalBacking + 1, length, lengths);
                    Assert.True(source.SequenceEqual(equal));
                    ReadOnlyTensorSpan<byte> different = new ReadOnlyTensorSpan<byte>(backing, length, lengths);
                    backing[length] = 8;
                    Assert.False(source.SequenceEqual(different));
                }
                else if (query == 1)
                {
                    backing[extremeIndex + 1] = 9;
                    Assert.Equal(extremeIndex, Tensor.IndexOfMax(source));
                    Assert.Equal(extremeIndex, Tensor.IndexOfMaxMagnitude(source));
                }
                else if (query == 2)
                {
                    backing[extremeIndex + 1] = 0;
                    Assert.Equal(extremeIndex, Tensor.IndexOfMin(source));
                    Assert.Equal(extremeIndex, Tensor.IndexOfMinMagnitude(source));
                }
                else
                {
                    backing[1] = 7;
                    equalBacking[1] = 7;
                    ReadOnlyTensorSpan<EqualityByte> customSource = new ReadOnlyTensorSpan<EqualityByte>((EqualityByte*)(backing + 1), length, lengths);
                    ReadOnlyTensorSpan<EqualityByte> equal = new ReadOnlyTensorSpan<EqualityByte>((EqualityByte*)(equalBacking + 1), length, lengths);
                    Assert.True(customSource.SequenceEqual(equal));
                    equalBacking[extremeIndex + 1] = 8;
                    Assert.False(customSource.SequenceEqual(equal));
                }
                Assert.Equal(7, backing[0]);
                Assert.Equal(7, backing[length + 1]);
            }
            finally
            {
                NativeMemory.Free(backing);
                NativeMemory.Free(equalBacking);
            }
        }

        internal static unsafe void TensorCopyToLargeDenseOverlap(int shift)
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest((long)length + 128, nameof(TensorCopyToLargeDenseOverlap), shift.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 128), zeroInitialize: true);
            try
            {
                backing[0] = byte.MaxValue;
                backing[length + 127] = byte.MaxValue;
                byte* sourceData = backing + 64;
                sourceData[0] = 1;
                sourceData[int.MaxValue] = 2;
                sourceData[length - 1] = 3;
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData, length);
                TensorSpan<byte> destination = new TensorSpan<byte>(sourceData + shift, length);

                source.CopyTo(destination);

                Assert.Equal(1, sourceData[shift]);
                Assert.Equal(0, sourceData[shift + 1]);
                Assert.Equal(2, sourceData[shift + (nint)int.MaxValue]);
                Assert.Equal(0, sourceData[shift + (nint)int.MaxValue + 1]);
                Assert.Equal(3, sourceData[shift + length - 1]);
                Assert.Equal(byte.MaxValue, backing[0]);
                Assert.Equal(byte.MaxValue, backing[length + 127]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        internal static unsafe void TensorLargeDenseElementwiseOperations(bool singletonDimensions)
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest((long)length + 2, nameof(TensorLargeDenseElementwiseOperations), singletonDimensions.ToString()))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                backing[0] = 7;
                backing[length + 1] = 7;
                TensorSpan<byte> destination = new TensorSpan<byte>(backing + 1, length, singletonDimensions ? [1, length, 1] : [length]);

                Tensor.OnesComplement(destination.AsReadOnlyTensorSpan(), destination);
                Verify(255);
                Tensor.Add(destination.AsReadOnlyTensorSpan(), (byte)2, destination);
                Verify(1);
                Tensor.Subtract((byte)20, destination.AsReadOnlyTensorSpan(), destination);
                Verify(19);
                Tensor.Add(destination.AsReadOnlyTensorSpan(), destination.AsReadOnlyTensorSpan(), destination);
                Verify(38);

                void Verify(byte expected)
                {
                    Assert.Equal(expected, backing[1]);
                    Assert.Equal(expected, backing[int.MaxValue]);
                    Assert.Equal(expected, backing[unchecked((nint)int.MaxValue + 1)]);
                    Assert.Equal(expected, backing[length]);
                    Assert.Equal(7, backing[0]);
                    Assert.Equal(7, backing[length + 1]);
                }
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        internal static unsafe void TensorLargeDenseConversionPreservesElementOffsets()
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest(((long)(length + 2) * (1 + sizeof(short))), nameof(TensorLargeDenseConversionPreservesElementOffsets)))
            {
                return;
            }

            byte* sourceData = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            short* destinationData = (short*)AllocateNativeMemory((nuint)(length + 2), (nuint)sizeof(short), zeroInitialize: true);
            try
            {
                sourceData[0] = 7;
                sourceData[length + 1] = 7;
                sourceData[length - 64] = 8;
                destinationData[0] = -1;
                destinationData[length + 1] = -1;
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData + 1, length, [1, length, 1]);
                TensorSpan<short> destination = new TensorSpan<short>(destinationData + 1, length, [1, length, 1]);

                Tensor.ConvertChecked<byte, short>(source, destination);

                Assert.Equal((short)0, destinationData[1]);
                Assert.Equal((short)0, destinationData[int.MaxValue]);
                Assert.Equal(8, destinationData[length - 64]);
                Assert.Equal((short)0, destinationData[length - 63]);
                Assert.Equal(-1, destinationData[0]);
                Assert.Equal(-1, destinationData[length + 1]);
                Assert.Equal(7, sourceData[0]);
                Assert.Equal(7, sourceData[length + 1]);
            }
            finally
            {
                NativeMemory.Free(sourceData);
                NativeMemory.Free(destinationData);
            }
        }

        internal static unsafe void TensorLargeDenseReductionAndReversal(bool singletonDimensions)
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest(((long)(length + 2) * 2), nameof(TensorLargeDenseReductionAndReversal), singletonDimensions.ToString()))
            {
                return;
            }

            byte* sourceData = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            byte* destinationData = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                destinationData[0] = 99;
                destinationData[length + 1] = 99;
                sourceData[0] = 7;
                sourceData[length + 1] = 7;
                sourceData[1] = 1;
                sourceData[length - 64] = 2;
                sourceData[length] = 3;
                nint[] lengths = singletonDimensions ? [1, length, 1] : [length];
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData + 1, length, lengths);
                TensorSpan<byte> destination = new TensorSpan<byte>(destinationData + 1, length, lengths);
                Assert.Equal((byte)6, Tensor.Sum(source));
                Assert.Equal((byte)0, Tensor.Product(source));
                Assert.Equal((byte)14, Tensor.Dot(source, source));
                Assert.Equal((byte)3, Tensor.Max(source));
                Assert.Equal((byte)0, Tensor.Min(source));
                Assert.True(Tensor.EqualsAll(source, source));
                Assert.False(Tensor.GreaterThanAll(source, (byte)0));
                Assert.True(Tensor.EqualsAny(source, (byte)0));

                Assert.True(Tensor.EqualsAny(source, (byte)3));
                Tensor.Reverse(source, destination);
                Assert.Equal(3, destinationData[1]);
                Assert.Equal(0, destinationData[2]);
                Assert.Equal(2, destinationData[65]);
                Assert.Equal(0, destinationData[66]);
                Assert.Equal(1, destinationData[length]);

                Tensor.Reverse(destination.AsReadOnlyTensorSpan(), destination);
                Assert.Equal(1, destinationData[1]);
                Assert.Equal(2, destinationData[length - 64]);
                Assert.Equal(3, destinationData[length]);
                Assert.Equal(99, destinationData[0]);
                Assert.Equal(99, destinationData[length + 1]);
                Assert.Equal(7, sourceData[0]);
                Assert.Equal(7, sourceData[length + 1]);
            }
            finally
            {
                NativeMemory.Free(sourceData);
                NativeMemory.Free(destinationData);
            }
        }

        internal static unsafe void TensorLargeDenseReverseIndependentRuns(bool sourcePadded)
        {
            nint length = int.MaxValue;
            length += 65;
            nint columns = length / 4;
            nint sourceRowStride = 2 * columns + (sourcePadded ? 3 : 0);
            nint destinationRowStride = columns + 5;
            nint sourceExtent = sourceRowStride + 2 * columns;
            nint destinationExtent = 3 * destinationRowStride + columns;
            if (RunLargeMemoryTest((long)(sourceExtent + destinationExtent + 4), nameof(TensorLargeDenseReverseIndependentRuns), sourcePadded.ToString()))
            {
                return;
            }

            byte* sourceData = (byte*)AllocateNativeMemory((nuint)(sourceExtent + 2), zeroInitialize: true);
            byte* destinationData = (byte*)AllocateNativeMemory((nuint)(destinationExtent + 2), zeroInitialize: true);
            try
            {
                sourceData[0] = 7;
                sourceData[sourceExtent + 1] = 7;
                destinationData[0] = 99;
                destinationData[destinationExtent + 1] = 99;
                sourceData[1] = 1;
                sourceData[1 + sourceRowStride] = 2;
                sourceData[sourceExtent] = 3;
                for (int row = 0; row < 3; row++)
                {
                    new Span<byte>(destinationData + 1 + row * destinationRowStride + columns, 5).Fill(99);
                }

                if (sourcePadded)
                {
                    new Span<byte>(sourceData + 1 + 2 * columns, 3).Fill(7);
                }

                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData + 1, sourceExtent,
                    [2, 2, columns], [sourceRowStride, columns, 1]);
                TensorSpan<byte> destination = new TensorSpan<byte>(destinationData + 1, destinationExtent,
                    [2, 2, columns], [2 * destinationRowStride, destinationRowStride, 1]);

                Tensor.Reverse(source, destination);

                for (int row = 0; row < 4; row++)
                {
                    byte* target = destinationData + 1 + row * destinationRowStride;
                    if (row == 0)
                    {
                        Assert.Equal(3, target[0]);
                    }
                    if (row is 1 or 3)
                    {
                        Assert.Equal(row == 1 ? 2 : 1, target[columns - 1]);
                    }
                    Assert.Equal(0, target[columns / 2]);
                    if (row != 3)
                    {
                        Assert.Equal(-1, new ReadOnlySpan<byte>(target + columns, 5).IndexOfAnyExcept((byte)99));
                    }
                }
                Assert.Equal(99, destinationData[0]);
                Assert.Equal(99, destinationData[destinationExtent + 1]);
                Assert.Equal(7, sourceData[0]);
                Assert.Equal(7, sourceData[sourceExtent + 1]);
                if (sourcePadded)
                {
                    Assert.Equal(-1, new ReadOnlySpan<byte>(sourceData + 1 + 2 * columns, 3).IndexOfAnyExcept((byte)7));
                }
            }
            finally
            {
                NativeMemory.Free(sourceData);
                NativeMemory.Free(destinationData);
            }
        }

        internal static unsafe void TensorLargeDenseInPlaceReverseChunks(int layout)
        {
            nint columns = int.MaxValue;
            columns += 66;
            int rows = layout == 0 ? 1 : layout == 1 ? 2 : 3;
            nint length = rows * columns;
            if (RunLargeMemoryTest((long)length + 2, nameof(TensorLargeDenseInPlaceReverseChunks), layout.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                backing[0] = 99;
                backing[length + 1] = 99;
                nint[] markers = [0, 4095, 4096, columns / 2, columns - 4097, columns - 4096, columns - 1];
                for (int row = 0; row < rows; row++)
                {
                    byte* data = backing + 1 + row * columns;
                    for (int i = 0; i < markers.Length; i++)
                    {
                        data[markers[i]] = (byte)(10 * (row + 1) + i);
                    }
                }
                TensorSpan<byte> tensor = new TensorSpan<byte>(backing + 1, length, [rows, columns]);
                int dimension = layout == 2 ? 0 : 1;
                Tensor.ReverseDimension(tensor.AsReadOnlyTensorSpan(), tensor, dimension);
                for (int row = 0; row < rows; row++)
                {
                    int sourceRow = layout == 2 ? rows - row - 1 : row;
                    byte* data = backing + 1 + row * columns;
                    for (int i = 0; i < markers.Length; i++)
                    {
                        int sourceMarker = layout == 2 ? i : markers.Length - i - 1;
                        nint marker = layout == 2 ? markers[i] : columns - markers[sourceMarker] - 1;
                        Assert.Equal((byte)(10 * (sourceRow + 1) + sourceMarker), data[marker]);
                    }
                    Assert.Equal(0, data[2048]);
                    Assert.Equal(0, data[columns / 3]);
                }
                Assert.Equal(99, backing[0]);
                Assert.Equal(99, backing[length + 1]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }

        }

        internal static unsafe void TensorLargeDenseRandomFillPreservesDrawOrder(bool gaussian)
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest(((long)(length + 2) * sizeof(Half)), nameof(TensorLargeDenseRandomFillPreservesDrawOrder), gaussian.ToString()))
            {
                return;
            }

            Half* backing = (Half*)AllocateNativeMemory((nuint)(length + 2), (nuint)sizeof(Half), zeroInitialize: true);
            try
            {
                backing[0] = (Half)99;
                backing[length + 1] = (Half)99;
                TensorSpan<Half> destination = new TensorSpan<Half>(backing + 1, length);
                BoundaryRandom random = new BoundaryRandom((long)int.MaxValue * (gaussian ? 2 : 1));
                Half expected;
                Half boundaryExpected;
                if (gaussian)
                {
                    Tensor.FillGaussianNormalDistribution(destination, random);
                    expected = (Half)(Math.Sqrt(-2.0 * Math.Log(0.75)) * Math.Sin(2.0 * Math.PI * 0.75));
                    boundaryExpected = (Half)(Math.Sqrt(-2.0 * Math.Log(0.25)) * Math.Sin(2.0 * Math.PI * 0.75));
                }
                else
                {
                    Tensor.FillUniformDistribution(destination, random);
                    expected = (Half)0.25;
                    boundaryExpected = (Half)0.75;
                }

                Assert.Equal((long)length * (gaussian ? 2 : 1), random.Calls);
                Assert.Equal(expected, backing[1]);
                Assert.Equal(expected, backing[int.MaxValue]);
                Assert.Equal(boundaryExpected, backing[length - 64]);
                Assert.Equal(expected, backing[unchecked((nint)int.MaxValue + 2)]);
                Assert.Equal(expected, backing[length]);
                Assert.Equal((Half)99, backing[0]);
                Assert.Equal((Half)99, backing[length + 1]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public static void TensorLargeBroadcastIndexOfNaN()
        {
            nint length = int.MaxValue;
            length += 65;
            ReadOnlyTensorSpan<double> source = new ReadOnlyTensorSpan<double>(new double[] { double.NaN }, [length], [0]);

            Assert.Equal(0, Tensor.IndexOfMax(source));
            Assert.Equal(0, Tensor.IndexOfMin(source));
            Assert.Equal(0, Tensor.IndexOfMaxMagnitude(source));
            Assert.Equal(0, Tensor.IndexOfMinMagnitude(source));
        }

        private readonly struct EqualityByte(byte value) : IEquatable<EqualityByte>
        {
            private readonly byte _value = value;

            public bool Equals(EqualityByte other) => _value == other._value;
        }

        private sealed class BoundaryRandom(long boundary) : Random
        {
            public long Calls { get; private set; }

            public override double NextDouble() => Calls++ == boundary ? 0.75 : 0.25;
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorIndexOfMaxNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            // Set a known max in the data at logical position
            int[] testData = (int[])data.Clone();
            // First, set all logical elements to small values
            nint flatLen = 1;
            foreach (nint s in shape)
            {
                flatLen *= s;
            }

            // Place the maximum value at the last logical position
            nint lastOffset = 0;
            nint rem = flatLen - 1;
            for (int d = shape.Length - 1; d >= 0; d--)
            {
                nint dimIndex = rem % shape[d];
                rem /= shape[d];
                lastOffset += dimIndex * strides[d];
            }
            testData[(int)lastOffset] = 9999;

            var ts = new ReadOnlyTensorSpan<int>(testData, shape, strides);
            Assert.False(ts.IsDense);

            nint idx = Tensor.IndexOfMax(ts);
            Assert.Equal(flatLen - 1, idx);
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorIndexOfMinNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            // Set all logical elements to positive values, then put the minimum at the last logical position
            int[] testData = new int[data.Length];
            Array.Fill(testData, 50);

            nint flatLen = 1;
            foreach (nint s in shape)
            {
                flatLen *= s;
            }

            // Set all logical positions to values > 0
            for (nint i = 0; i < flatLen; i++)
            {
                nint offset = 0;
                nint remaining = i;
                for (int d = shape.Length - 1; d >= 0; d--)
                {
                    nint dimIndex = remaining % shape[d];
                    remaining /= shape[d];
                    offset += dimIndex * strides[d];
                }
                testData[(int)offset] = (int)(i + 10);
            }

            // Place the minimum at the last logical position
            nint lastOffset = 0;
            nint rem = flatLen - 1;
            for (int d = shape.Length - 1; d >= 0; d--)
            {
                nint dimIndex = rem % shape[d];
                rem /= shape[d];
                lastOffset += dimIndex * strides[d];
            }
            testData[(int)lastOffset] = -1;

            var ts = new ReadOnlyTensorSpan<int>(testData, shape, strides);
            Assert.False(ts.IsDense);

            nint idx = Tensor.IndexOfMin(ts);
            Assert.Equal(flatLen - 1, idx);
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorIndexOfMaxMagnitudeNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            int[] testData = new int[data.Length];
            Array.Fill(testData, 1);

            nint flatLen = 1;
            foreach (nint s in shape)
            {
                flatLen *= s;
            }

            // Set all logical positions to small values
            for (nint i = 0; i < flatLen; i++)
            {
                nint offset = 0;
                nint remaining = i;
                for (int d = shape.Length - 1; d >= 0; d--)
                {
                    nint dimIndex = remaining % shape[d];
                    remaining /= shape[d];
                    offset += dimIndex * strides[d];
                }
                testData[(int)offset] = (int)(i + 1);
            }

            // Place the max magnitude at the last logical position
            nint lastOffset = 0;
            nint rem = flatLen - 1;
            for (int d = shape.Length - 1; d >= 0; d--)
            {
                nint dimIndex = rem % shape[d];
                rem /= shape[d];
                lastOffset += dimIndex * strides[d];
            }
            testData[(int)lastOffset] = -9999;

            var ts = new ReadOnlyTensorSpan<int>(testData, shape, strides);
            Assert.False(ts.IsDense);

            nint idx = Tensor.IndexOfMaxMagnitude(ts);
            Assert.Equal(flatLen - 1, idx);
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorIndexOfMinMagnitudeNonDenseTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            _ = expectedLogical;
            int[] testData = new int[data.Length];
            Array.Fill(testData, 100);

            nint flatLen = 1;
            foreach (nint s in shape)
            {
                flatLen *= s;
            }

            // Set all logical positions to large magnitude values
            for (nint i = 0; i < flatLen; i++)
            {
                nint offset = 0;
                nint remaining = i;
                for (int d = shape.Length - 1; d >= 0; d--)
                {
                    nint dimIndex = remaining % shape[d];
                    remaining /= shape[d];
                    offset += dimIndex * strides[d];
                }
                testData[(int)offset] = (int)(i + 100);
            }

            // Place the min magnitude at the first logical position
            testData[0] = 0;

            var ts = new ReadOnlyTensorSpan<int>(testData, shape, strides);
            Assert.False(ts.IsDense);

            nint idx = Tensor.IndexOfMinMagnitude(ts);
            Assert.Equal(0, idx);
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorResizeToNonDenseSourceTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            var src = new ReadOnlyTensorSpan<int>(data, shape, strides);
            Assert.False(src.IsDense);

            // Resize to a larger dense destination
            nint srcFlatLen = src.FlattenedLength;
            int[] dstData = new int[(int)srcFlatLen + 2];
            var dst = new TensorSpan<int>(dstData, [(nint)dstData.Length], [1]);
            Assert.True(dst.IsDense);

            Tensor.ResizeTo(src, dst);

            for (int i = 0; i < expectedLogical.Length; i++)
            {
                Assert.Equal(expectedLogical[i], dstData[i]);
            }
            // Extra positions should be zero-filled
            for (int i = expectedLogical.Length; i < dstData.Length; i++)
            {
                Assert.Equal(0, dstData[i]);
            }
        }

        [Theory]
        [MemberData(nameof(NonDenseTensorData))]
        public static void TensorResizeToNonDenseDestinationTests(int[] data, nint[] shape, nint[] strides, int[] expectedLogical)
        {
            // Create a dense source with known values
            var src = new ReadOnlyTensorSpan<int>(expectedLogical, [(nint)expectedLogical.Length], [1]);
            Assert.True(src.IsDense);

            // Create a non-dense destination
            int[] dstData = new int[data.Length];
            var dst = new TensorSpan<int>(dstData, shape, strides);
            Assert.False(dst.IsDense);

            nint copyLength = Math.Min(src.FlattenedLength, dst.FlattenedLength);
            Tensor.ResizeTo(src, dst);

            // Verify logical elements were written correctly
            int logicalIdx = 0;
            foreach (int val in dst)
            {
                if (logicalIdx < expectedLogical.Length)
                {
                    Assert.Equal(expectedLogical[logicalIdx], val);
                }
                logicalIdx++;
            }
        }

        [Fact]
        public static void TensorResizeNonDenseTests()
        {
            // 2x3 tensor, slice to 2x2 (non-dense), then resize
            Tensor<int> tensor = Tensor.Create([10, 20, 30, 40, 50, 60], [2, 3]);
            Tensor<int> sliced = tensor.Slice(0..2, 0..2);
            Assert.False(sliced.IsDense);

            Tensor<int> resized = Tensor.Resize(sliced, [3]);
            Assert.Equal(3, resized.FlattenedLength);
            Assert.Equal(10, resized[0]);
            Assert.Equal(20, resized[1]);
            Assert.Equal(40, resized[2]);

            // 3x4 tensor, slice to 2x2 (non-dense), then resize larger
            Tensor<int> tensor2 = Tensor.Create(Enumerable.Range(1, 12).ToArray(), [3, 4]);
            Tensor<int> sliced2 = tensor2.Slice(0..2, 0..2);
            Assert.False(sliced2.IsDense);

            Tensor<int> resized2 = Tensor.Resize(sliced2, [6]);
            Assert.Equal(6, resized2.FlattenedLength);
            Assert.Equal(1, resized2[0]);
            Assert.Equal(2, resized2[1]);
            Assert.Equal(5, resized2[2]);
            Assert.Equal(6, resized2[3]);
            Assert.Equal(0, resized2[4]);
            Assert.Equal(0, resized2[5]);
        }

        [Theory]
        [InlineData(31)]
        [InlineData(33)]
        [InlineData(64)]
        [InlineData(130)]
        public static void TensorResizeNonDenseContiguousRows(int newLength)
        {
            int[] backing = Enumerable.Repeat(-1, 8 * 19).ToArray();
            int[] expected = Enumerable.Range(1, 8 * 16).ToArray();
            for (int row = 0; row < 8; row++)
            {
                expected.AsSpan(row * 16, 16).CopyTo(backing.AsSpan(row * 19, 16));
            }

            Tensor<int> source = Tensor.Create(backing, [8, 16], [19, 1]);
            Tensor<int> result = Tensor.Resize(source, [newLength]);
            int[] actual = new int[newLength];
            result.FlattenTo(actual);
            Assert.Equal(expected.Take(newLength).Concat(Enumerable.Repeat(0, Math.Max(0, newLength - expected.Length))), actual);
        }

        [Theory]
        [InlineData(8, 4, 1)]
        [InlineData(12, 4, 1)]
        [InlineData(13, 6, 1)]
        [InlineData(16, 8, 2)]
        public static void TensorOperationsOnContiguousSlices(int outerStride, int rowStride, int columnStride)
        {
            nint[] lengths = [2, 2, 4];
            nint[] strides = [outerStride, rowStride, columnStride];
            int[] xData = new int[32];
            int[] yData = new int[32];
            int[] resultData = Enumerable.Repeat(-99, 32).ToArray();
            int[] expected = Enumerable.Repeat(-99, 32).ToArray();
            bool[] comparisonData = new bool[32];
            bool[] expectedComparison = new bool[32];

            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                xData[offset] = -i - 1;
                yData[offset] = i + 1;
                expectedComparison[offset] = true;
            }

            ReadOnlyTensorSpan<int> x = new ReadOnlyTensorSpan<int>(xData, lengths, strides);
            ReadOnlyTensorSpan<int> y = new ReadOnlyTensorSpan<int>(yData, lengths, strides);
            TensorSpan<int> result = new TensorSpan<int>(resultData, lengths, strides);
            TensorSpan<bool> comparison = new TensorSpan<bool>(comparisonData, lengths, strides);

            int[] flattened = Enumerable.Repeat(-99, 18).ToArray();
            x.FlattenTo(flattened);
            Assert.Equal(Enumerable.Range(1, 16).Select(i => -i).Concat([-99, -99]), flattened);

            ReadOnlyTensorSpan<int> dense = new ReadOnlyTensorSpan<int>(
                Enumerable.Range(1, 16).Select(i => -i).ToArray(), lengths);
            Assert.True(x.SequenceEqual(dense));
            Assert.True(dense.SequenceEqual(x));
            Assert.False(y.SequenceEqual(dense));

            int[] differing = (int[])xData.Clone();
            differing[outerStride + rowStride + 3 * columnStride] = -99;
            Assert.False(x.SequenceEqual(new ReadOnlyTensorSpan<int>(differing, lengths, strides)));

            Tensor.Abs(x, result);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = i + 1;
            }
            Assert.Equal(expected, resultData);

            Tensor.Add(x, y, result);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = 0;
            }
            Assert.Equal(expected, resultData);

            Tensor.Multiply(x, 2, result);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = -2 * (i + 1);
            }
            Assert.Equal(expected, resultData);

            Tensor.Subtract(50, x, result);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = 51 + i;
            }
            Assert.Equal(expected, resultData);

            Tensor.LessThan(x, y, comparison);
            Assert.Equal(expectedComparison, comparisonData);

            Tensor.Multiply(result.AsReadOnlyTensorSpan(), 2, result);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] *= 2;
            }
            Assert.Equal(expected, resultData);

            result.Clear();
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = 0;
            }
            Assert.Equal(expected, resultData);

            result.Fill(7);
            for (int i = 0; i < 16; i++)
            {
                int offset = (i / 8 * outerStride) + (i / 4 % 2 * rowStride) + (i % 4 * columnStride);
                expected[offset] = 7;
            }
            Assert.Equal(expected, resultData);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(64)]
        public static void TensorGappedOperationsAcrossSliceThreshold(int columns)
        {
            int rowStride = columns + 3;
            nint[] lengths = [2, columns];
            nint[] strides = [rowStride, 1];
            int[] input = Enumerable.Repeat(-99, 2 * rowStride).ToArray();
            int[] output = Enumerable.Repeat(-99, input.Length).ToArray();
            int[] expected = (int[])output.Clone();
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    input[row * rowStride + column] = -(row * columns + column + 1);
                }
            }

            ReadOnlyTensorSpan<int> source = new ReadOnlyTensorSpan<int>(input, lengths, strides);
            TensorSpan<int> destination = new TensorSpan<int>(output, lengths, strides);

            Tensor.Abs(source, destination);
            Validate(1, 0);
            Tensor.Add(source, source, destination);
            Validate(-2, 0);
            Tensor.Multiply(source, 3, destination);
            Validate(-3, 0);
            Tensor.Subtract(50, source, destination);
            Validate(1, 50);
            destination.Clear();
            Validate(0, 0);
            destination.Fill(7);
            Validate(0, 7);

            void Validate(int multiplier, int addend)
            {
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        expected[row * rowStride + column] = multiplier * (row * columns + column + 1) + addend;
                    }
                }
                Assert.Equal(expected, output);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorOperationsOnBroadcastContiguousSlices(bool paddedRank)
        {
            int[] sourceData = [-1, -2, -3, -4];
            ReadOnlyTensorSpan<int> source = paddedRank
                ? new ReadOnlyTensorSpan<int>(sourceData, [1, 4])
                : new ReadOnlyTensorSpan<int>(sourceData, [4]);
            ReadOnlyTensorSpan<int> other = new ReadOnlyTensorSpan<int>(
                [10, 11, 12, 13, 14, 15, 16, 17], [2, 4]);
            int[] resultData = Enumerable.Repeat(-99, 12).ToArray();
            TensorSpan<int> result = new TensorSpan<int>(resultData, [2, 4], [6, 1]);

            Tensor.Abs(source, result);
            Assert.Equal([1, 2, 3, 4, -99, -99, 1, 2, 3, 4, -99, -99], resultData);

            Tensor.Add(source, other, result);
            Assert.Equal([9, 9, 9, 9, -99, -99, 13, 13, 13, 13, -99, -99], resultData);

            Tensor.Add(source, 3, result);
            Assert.Equal([2, 1, 0, -1, -99, -99, 2, 1, 0, -1, -99, -99], resultData);

            Tensor.Subtract(10, source, result);
            Assert.Equal([11, 12, 13, 14, -99, -99, 11, 12, 13, 14, -99, -99], resultData);
        }

        [Theory]
        [InlineData(3, 2)]
        [InlineData(6, 2)]
        [InlineData(6, 8)]
        [InlineData(6, 16)]
        public static void TensorGappedOperationsPropagateExceptions(int rank, int columns)
        {
            nint[] lengths = Enumerable.Repeat((nint)1, rank).ToArray();
            nint[] strides = new nint[rank];
            lengths[rank - 2] = 2;
            lengths[rank - 1] = columns;
            strides[rank - 2] = columns + 3;
            strides[rank - 1] = 1;
            int[] data = Enumerable.Repeat(1, 2 * (columns + 3)).ToArray();
            data[0] = int.MinValue;
            int[] output = Enumerable.Repeat(-99, data.Length).ToArray();
            Tensor<int> source = Tensor.Create(data, lengths, strides);
            Tensor<int> zero = Tensor.Create(new int[data.Length], lengths, strides);
            Tensor<int> destination = Tensor.Create(output, lengths, strides);

            Assert.Throws<OverflowException>(() => Tensor.Abs<int>(source.AsReadOnlyTensorSpan(), destination.AsTensorSpan()));
            Assert.Throws<DivideByZeroException>(() => Tensor.Divide<int>(source.AsReadOnlyTensorSpan(), zero.AsReadOnlyTensorSpan(), destination.AsTensorSpan()));
            Assert.Throws<DivideByZeroException>(() => Tensor.Divide<int>(source.AsReadOnlyTensorSpan(), 0, destination.AsTensorSpan()));
            Assert.Throws<DivideByZeroException>(() => Tensor.Divide<int>(1, zero.AsReadOnlyTensorSpan(), destination.AsTensorSpan()));
            Assert.All(output, value => Assert.Equal(-99, value));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(6)]
        public static void TensorComparisonBroadcastShapeOverflow(int rank)
        {
            nint[] xLengths = Enumerable.Repeat((nint)1, rank).ToArray();
            nint[] yLengths = (nint[])xLengths.Clone();
            xLengths[0] = nint.MaxValue;
            yLengths[1] = 2;
            Tensor<int> x = Tensor.Create([1], xLengths, new nint[rank]);
            Tensor<int> y = Tensor.Create([1], yLengths, new nint[rank]);

            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.EqualsAny<int>(x.AsReadOnlyTensorSpan(), y.AsReadOnlyTensorSpan()));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.EqualsAll<int>(x.AsReadOnlyTensorSpan(), y.AsReadOnlyTensorSpan()));
        }

        [Fact]
        public static void TensorMultiplyTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray());
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), lengths: [3, 1]);

            Tensor<int> t2 = Tensor.Multiply(t0.AsReadOnlyTensorSpan(), t1);
            Assert.Equal([3, 3], t2.Lengths);
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            t2 = Tensor.Multiply(t1.AsReadOnlyTensorSpan(), t0);

            Assert.Equal([3, 3], t2.Lengths);
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            // Same rank with broadcasting: [2,3] * [2,1] should broadcast to [2,3]
            Tensor<int> t3 = Tensor.Create([2, 3, 5, 7, 11, 13], [2, 3]);
            Tensor<int> t4 = Tensor.Create([-2, -3], [2, 1]);
            t2 = Tensor.Multiply(t3.AsReadOnlyTensorSpan(), t4);

            Assert.Equal([2, 3], t2.Lengths);
            Assert.Equal(-4, t2[0, 0]);
            Assert.Equal(-6, t2[0, 1]);
            Assert.Equal(-10, t2[0, 2]);
            Assert.Equal(-21, t2[1, 0]);
            Assert.Equal(-33, t2[1, 1]);
            Assert.Equal(-39, t2[1, 2]);

            t1 = Tensor.Create(Enumerable.Range(0, 9).ToArray(), lengths: [3, 3]);
            t2 = Tensor.Multiply(t0.AsReadOnlyTensorSpan(), t1);

            Assert.Equal([3, 3], t2.Lengths);
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
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), lengths: [1, 3, 1, 1, 1]);
            Tensor<int> t1 = Tensor.Broadcast<int>(t0, [1, 3, 1, 2, 1]);

            Assert.Equal([1, 3, 1, 2, 1], t1.Lengths);

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 1, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 1, 0]);

            t1 = Tensor.Broadcast<int>(t0, [1, 3, 2, 1, 1]);
            Assert.Equal([1, 3, 2, 1, 1], t1.Lengths);

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 1, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 1, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 1, 0, 0]);

            t0 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), lengths: [1, 3]);
            t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), lengths: [3, 1]);
            var t2 = Tensor.Broadcast<int>(t0, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths);

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(2, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(1, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);

            t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray(), lengths: [3, 1]);
            t2 = Tensor.Broadcast<int>(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths);

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
            Assert.Equal([1, 3], t3.Lengths);

            t1 = Tensor.Create(Enumerable.Range(0, 3).ToArray());
            t2 = Tensor.Broadcast<int>(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths);

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
        public static void TensorBroadcastEmptyDimensionsTests()
        {
            Tensor<int> source = Tensor.Create([1, 2, 3], [1, 3]);
            Tensor<int> empty = Tensor.Broadcast<int>(source, (ReadOnlySpan<nint>)[0, 3]);
            Assert.Equal([0, 3], empty.Lengths);
            Assert.Empty(empty.ToArray());

            Tensor<int> result = Tensor.Add<int>(empty, source);
            Assert.Equal([0, 3], result.Lengths);
            Assert.False(source.TryCopyTo(empty.AsTensorSpan()));

            Tensor<int> mismatched = Tensor.CreateFromShape<int>([0, 4]);
            Assert.Throws<ArgumentException>(() => Tensor.Add<int>(empty, mismatched));
        }

        [Fact]
        public static void TensorDefaultEmptyDoesNotBroadcastToNonempty()
        {
            Tensor<int> nonempty = Tensor.Create([1, 2]);

            Assert.Throws<ArgumentException>(() => Tensor.Add<int>(Tensor<int>.Empty, nonempty));
            Assert.Throws<ArgumentException>(() => Tensor.Broadcast<int>(Tensor<int>.Empty, (ReadOnlySpan<nint>)[2]));
        }

        [Theory]
        [InlineData(new int[] { }, new int[] { }, true)]
        [InlineData(new int[] { }, new int[] { 0 }, true)]
        [InlineData(new int[] { }, new int[] { 2, 0 }, true)]
        [InlineData(new int[] { }, new int[] { 0, 0 }, true)]
        [InlineData(new int[] { }, new int[] { 1, 2, 3, 4, 5, 0 }, true)]
        [InlineData(new int[] { }, new int[] { 0, 2 }, false)]
        [InlineData(new int[] { }, new int[] { 0, 1 }, false)]
        [InlineData(new int[] { }, new int[] { 1 }, false)]
        [InlineData(new int[] { }, new int[] { 2 }, false)]
        [InlineData(new int[] { 0 }, new int[] { }, true)]
        [InlineData(new int[] { 1, 0 }, new int[] { }, true)]
        [InlineData(new int[] { 1, 1, 0 }, new int[] { 0 }, true)]
        [InlineData(new int[] { 2, 0 }, new int[] { }, false)]
        [InlineData(new int[] { 0, 0 }, new int[] { }, false)]
        [InlineData(new int[] { 1 }, new int[] { }, true)]
        [InlineData(new int[] { 1, 1 }, new int[] { }, true)]
        [InlineData(new int[] { 2 }, new int[] { }, false)]
        [InlineData(new int[] { 2, 0 }, new int[] { 2, 0 }, true)]
        [InlineData(new int[] { 2, 0 }, new int[] { 3, 0 }, false)]
        [InlineData(new int[] { 2, 0 }, new int[] { 0, 0 }, false)]
        [InlineData(new int[] { 1, 0 }, new int[] { 2, 0 }, true)]
        [InlineData(new int[] { 2, 1 }, new int[] { 2, 0 }, true)]
        public static void TensorEmptyBroadcastCompatibility(int[] sourceLengths, int[] destinationLengths, bool compatible)
        {
            nint[] sourceShape = Array.ConvertAll(sourceLengths, static length => (nint)length);
            nint[] destinationShape = Array.ConvertAll(destinationLengths, static length => (nint)length);
            Tensor<int> source = sourceShape.Length == 0 ? Tensor<int>.Empty : Tensor.CreateFromShape<int>(sourceShape);
            Tensor<int> destination = destinationShape.Length == 0 ? Tensor<int>.Empty : Tensor.CreateFromShape<int>(destinationShape);

            Assert.Equal(compatible, source.TryBroadcastTo(destination.AsTensorSpan()));
            Assert.Equal(compatible, source.AsTensorSpan().TryBroadcastTo(destination.AsTensorSpan()));
            Assert.Equal(compatible, source.AsReadOnlyTensorSpan().TryBroadcastTo(destination.AsTensorSpan()));
            Assert.Equal(compatible && source.FlattenedLength <= destination.FlattenedLength, source.TryCopyTo(destination.AsTensorSpan()));

            if (compatible)
            {
                nint[] expectedShape = destinationShape.Length == 0 ? [0] : destinationShape;
                Assert.Equal(expectedShape, Tensor.Broadcast<int>(source, destinationShape).Lengths);
                Assert.Equal(expectedShape, Tensor.Broadcast<int>(source, destination).Lengths);
                source.BroadcastTo(destination.AsTensorSpan());
                source.AsTensorSpan().BroadcastTo(destination.AsTensorSpan());
                source.AsReadOnlyTensorSpan().BroadcastTo(destination.AsTensorSpan());
            }
            else
            {
                Assert.Throws<ArgumentException>(() => Tensor.Broadcast<int>(source, destinationShape));
                Assert.Throws<ArgumentException>(() => Tensor.Broadcast<int>(source, destination));
                Assert.Throws<ArgumentException>(() => source.BroadcastTo(destination.AsTensorSpan()));
                Assert.Throws<ArgumentException>(() => source.AsTensorSpan().BroadcastTo(destination.AsTensorSpan()));
                Assert.Throws<ArgumentException>(() => source.AsReadOnlyTensorSpan().BroadcastTo(destination.AsTensorSpan()));
            }

            Assert.Equal(sourceShape, source.Lengths);
            Assert.Equal(destinationShape, destination.Lengths);
        }

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(1, 3, false)]
        [InlineData(1, 3, true)]
        [InlineData(5, 3, true)]
        [InlineData(5, 64, false)]
        [InlineData(5, 64, true)]
        public static void TensorBroadcastIgnoresLeadingSingletonDimensions(int padding, int length, bool strided)
        {
            nint[] lengths = new nint[padding + 1];
            Array.Fill(lengths, (nint)1);
            lengths[^1] = length;
            nint[] strides = new nint[lengths.Length];
            strides[^1] = length == 1 ? 0 : strided ? 2 : 1;
            int[] values = new int[strided ? (2 * length) - 1 : length];
            int[] expected = Enumerable.Range(1, length).ToArray();
            for (int i = 0; i < length; i++)
            {
                values[strided ? 2 * i : i] = expected[i];
            }
            Tensor<int> source = Tensor.Create(values, lengths, strides);
            Tensor<int> destination = Tensor.Create(new int[values.Length], [length], [strides[^1]]);
            Tensor<int> singleton = Tensor.Create([5], Enumerable.Repeat((nint)1, padding + 1).ToArray());

            Assert.Equal(expected, Tensor.Broadcast<int>(source, (ReadOnlySpan<nint>)[length]).ToArray());
            Assert.Equal(expected, Tensor.Broadcast<int>(source, destination).ToArray());
            Assert.True(source.TryBroadcastTo(destination));
            Assert.Equal(expected, destination.ToArray());
            Assert.True(source.TryCopyTo(destination));
            source.AsTensorSpan().BroadcastTo(destination);
            source.AsReadOnlyTensorSpan().BroadcastTo(destination);
            Assert.Equal(expected, destination.ToArray());

            Tensor.Abs<int>(source, destination);
            Assert.Equal(expected, destination.ToArray());
            Tensor.Add<int>(source, 5, destination);
            Assert.Equal(expected.Select(static value => value + 5), destination.ToArray());
            Tensor.Subtract<int>(100, source, destination);
            Assert.Equal(expected.Select(static value => 100 - value), destination.ToArray());
            Tensor.Add<int>(source, singleton, destination);
            Assert.Equal(expected.Select(static value => value + 5), destination.ToArray());
            Tensor.Add<int>(singleton, source, destination);
            Assert.Equal(expected.Select(static value => value + 5), destination.ToArray());
            Tensor.Reverse<int>(source, destination);
            Assert.Equal(Enumerable.Reverse(expected), destination.ToArray());
            Tensor.ReverseDimension<int>(source, destination, padding);
            Assert.Equal(Enumerable.Reverse(expected), destination.ToArray());

            Tensor<int> vector = Tensor.Create(expected);
            Tensor<int> scalarResult = Tensor.Broadcast<int>(singleton, vector);
            Assert.Equal(Enumerable.Repeat(5, length), scalarResult.ToArray());
            Tensor.Add<int>(vector, singleton, destination);
            Assert.Equal(expected.Select(static value => value + 5), destination.ToArray());
            TensorSpan<int> equivalentDestination = new TensorSpan<int>(values, [length], [strides[^1]]);
            Tensor.Add<int>(source, 1, equivalentDestination);
            Assert.Equal(expected.Select(static value => value + 1), source.ToArray());
            if (!strided)
            {
                Tensor.ReverseDimension<int>(source, equivalentDestination, padding);
                Assert.Equal(expected.Select(static value => value + 1).Reverse(), source.ToArray());
            }
            Assert.Equal(lengths, source.Lengths);
        }

        [Theory]
        [InlineData(new int[] { 2, 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2 }, new int[] { 1 })]
        public static void TensorBroadcastRetainsSignificantDimensions(int[] sourceLengths, int[] destinationLengths)
        {
            Tensor<int> source = Tensor.CreateFromShape<int>(Array.ConvertAll(sourceLengths, static length => (nint)length));
            Tensor<int> destination = Tensor.CreateFromShape<int>(Array.ConvertAll(destinationLengths, static length => (nint)length));

            Assert.False(source.TryBroadcastTo(destination));
            Assert.Throws<ArgumentException>(() => Tensor.Broadcast<int>(source, destination));
            Assert.Throws<ArgumentException>(() => Tensor.Add<int>(source, 1, destination));
        }

        [Fact]
        public static void TensorDefaultEmptyOperationsUseCanonicalShape()
        {
            Tensor<int> empty = Tensor<int>.Empty;
            Tensor<int> vector = Tensor.CreateFromShape<int>([0]);

            Assert.Equal([0], Tensor.Broadcast<int>(empty, ReadOnlySpan<nint>.Empty).Lengths);
            Assert.Equal([0], Tensor.Broadcast<int>(empty, empty).Lengths);
            Assert.Equal([0], empty.ToDenseTensor().Lengths);
            Assert.Equal([0], Tensor.Reverse<int>(empty).Lengths);
            Assert.Equal([0], Tensor.ReverseDimension<int>(empty, -1).Lengths);
            Assert.Equal([0], Tensor.Abs<int>(empty).Lengths);
            Assert.Equal([0], Tensor.Add<int>(empty, 1).Lengths);
            Assert.Equal([0], Tensor.Add<int>(empty, empty).Lengths);
            Assert.Equal([0], Tensor.Add<int>(empty, vector).Lengths);
            Assert.Equal([0], Tensor.Add<int>(vector, empty).Lengths);
            Assert.Equal([0], Tensor.Equals<int>(empty, 0).Lengths);
            Assert.Equal([0], Tensor.Equals<int>(empty, vector).Lengths);
            Assert.Equal([0], Tensor.Equals<int>(vector, empty).Lengths);
            CheckDense(TensorSpan<int>.Empty);
            CheckDense(ReadOnlyTensorSpan<int>.Empty);

            Assert.Equal(0, empty.Rank);
            Assert.True(empty.Lengths.IsEmpty);
            Assert.Equal(0, TensorSpan<int>.Empty.Rank);
            Assert.Equal(0, ReadOnlyTensorSpan<int>.Empty.Rank);

            static void CheckDense<TTensor>(TTensor tensor)
                where TTensor : IReadOnlyTensor<TTensor, int>, allows ref struct
            {
                TTensor dense = tensor.ToDenseTensor();
                Assert.True(dense.IsDense);
                Assert.True(dense.IsEmpty);
                Assert.Equal([0], dense.Lengths);
            }
        }

        [Theory]
        [InlineData(new int[] { 1 }, new int[] { 0 })]
        [InlineData(new int[] { 0 }, new int[] { 0 })]
        [InlineData(new int[] { 0, 1 }, new int[] { 0, 0 })]
        [InlineData(new int[] { 2, 0 }, new int[] { 2, 0 })]
        [InlineData(new int[] { 1, 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 1, 0 })]
        [InlineData(new int[] { 1, 2, 3, 4, 5, 0 }, new int[] { 1, 2, 3, 4, 5, 0 })]
        public static void TensorDefaultEmptyBinaryOperationsUseBroadcastShape(int[] otherLengths, int[] expectedLengths)
        {
            Tensor<int> other = Tensor.CreateFromShape<int>(Array.ConvertAll(otherLengths, static length => (nint)length));
            nint[] expectedShape = Array.ConvertAll(expectedLengths, static length => (nint)length);
            Tensor<int> destination = Tensor.CreateFromShape<int>(expectedShape);
            ReadOnlyTensorSpan<int> empty = ReadOnlyTensorSpan<int>.Empty;

            Assert.Equal(expectedShape, Tensor.Add<int>(empty, other).Lengths);
            Assert.Equal(expectedShape, Tensor.Add<int>(other, empty).Lengths);
            Assert.Equal(expectedShape, Tensor.Equals<int>(empty, other).Lengths);
            Assert.Equal(expectedShape, Tensor.Equals<int>(other, empty).Lengths);
            Tensor.Add<int>(empty, other, destination);
            Tensor.Add<int>(other, empty, destination);
            Assert.False(Tensor.EqualsAny<int>(empty, other));
            Assert.False(Tensor.EqualsAny<int>(other, empty));
            Assert.True(Tensor.EqualsAll<int>(empty, other));
            Assert.True(Tensor.EqualsAll<int>(other, empty));
            Assert.Equal(0, Tensor.Dot<int>(empty, other));
            Assert.Equal(0, Tensor.Dot<int>(other, empty));
            Assert.True(destination.IsEmpty);
        }

        [Theory]
        [InlineData(new int[] { 2 })]
        [InlineData(new int[] { 0, 2 })]
        [InlineData(new int[] { 1, 0, 2 })]
        public static void TensorDefaultEmptyBinaryOperationsRejectIncompatibleShapes(int[] otherLengths)
        {
            Tensor<int> other = Tensor.CreateFromShape<int>(Array.ConvertAll(otherLengths, static length => (nint)length));

            Assert.Throws<ArgumentException>(() => Tensor.Add<int>(Tensor<int>.Empty, other));
            Assert.Throws<ArgumentException>(() => Tensor.Add<int>(other, Tensor<int>.Empty));
            Assert.Throws<ArgumentException>(() => Tensor.EqualsAny<int>(Tensor<int>.Empty, other));
            Assert.Throws<ArgumentException>(() => Tensor.EqualsAny<int>(other, Tensor<int>.Empty));
            Assert.Throws<ArgumentException>(() => Tensor.Dot<int>(Tensor<int>.Empty, other));
            Assert.Throws<ArgumentException>(() => Tensor.Dot<int>(other, Tensor<int>.Empty));
        }

        [Theory]
        [InlineData(new int[] { }, new int[] { })]
        [InlineData(new int[] { -1 }, new int[] { 0 })]
        [InlineData(new int[] { 0 }, new int[] { 0 })]
        [InlineData(new int[] { 1 }, new int[] { 0 })]
        [InlineData(new int[] { -1, 0, 1 }, new int[] { 0, 0, 0 })]
        [InlineData(new int[] { 1, 0, -1 }, new int[] { 0, 0, 0 })]
        [InlineData(new int[] { -1, -1, -1 }, new int[] { 0, 0, 0 })]
        [InlineData(new int[] { 1, 1, 1 }, new int[] { 0, 0, 0 })]
        [InlineData(new int[] { 1, 1, 1 }, new int[] { 0, 1, 2 })]
        [InlineData(new int[] { -1, -1, -1 }, new int[] { 0, -1, -2 })]
        public static void TensorAnySpanKernels(int[] left, int[] right)
        {
            Check<TensorOperation.EqualsAny<int>>(left, right, static (x, y) => x == y);
            Check<TensorOperation.GreaterThanAny<int>>(left, right, static (x, y) => x > y);
            Check<TensorOperation.GreaterThanOrEqualAny<int>>(left, right, static (x, y) => x >= y);
            Check<TensorOperation.LessThanAny<int>>(left, right, static (x, y) => x < y);
            Check<TensorOperation.LessThanOrEqualAny<int>>(left, right, static (x, y) => x <= y);

            static void Check<TOperation>(int[] left, int[] right, Func<int, int, bool> comparison)
                where TOperation : TensorOperation.IBinaryOperation_Tensor_Scalar<int, bool>,
                                   TensorOperation.IBinaryOperation_Tensor_Tensor<int, bool>
            {
                Span<bool> destination = stackalloc bool[1];
                bool expectedScalar = !left.Any(value => comparison(value, 0));
                bool expectedPairwise = !Enumerable.Range(0, left.Length).Any(i => comparison(left[i], right[i]));

                destination[0] = !expectedScalar;
                TOperation.Invoke(left, 0, destination);
                Assert.Equal(expectedScalar, destination[0]);

                destination[0] = !expectedPairwise;
                TOperation.Invoke(left, right, destination);
                Assert.Equal(expectedPairwise, destination[0]);
            }
        }

        [Fact]
        public static void TensorPairwiseReductionsUseBroadcastShape()
        {
            ReadOnlyTensorSpan<int> column = new ReadOnlyTensorSpan<int>([1, 2, 3], [3, 1], []);
            ReadOnlyTensorSpan<int> row = new ReadOnlyTensorSpan<int>([9, 9, 9, 3], [1, 4], []);

            Assert.True(Tensor.EqualsAny(column, row));
            Assert.True(Tensor.EqualsAny(row, column));
            Assert.False(Tensor.LessThanAll(column, row));
            Assert.False(Tensor.GreaterThanAll(row, column));

            ReadOnlyTensorSpan<int> highRank = new ReadOnlyTensorSpan<int>([1, 2, 3], [1, 1, 1, 1, 3, 1], []);
            ReadOnlyTensorSpan<int> vector = new ReadOnlyTensorSpan<int>([9, 9, 9, 3]);
            Assert.True(Tensor.EqualsAny(vector, highRank));
            Assert.False(Tensor.LessThanAll(highRank, vector));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorAnyComparisonOfEmptyIsFalse(bool ranked)
        {
            ReadOnlyTensorSpan<int> empty = ranked ? new ReadOnlyTensorSpan<int>(new int[4], [2, 0], []) : default;
            Assert.False(Tensor.EqualsAny(empty, empty));
            Assert.False(Tensor.EqualsAny(empty, 0));
            Assert.False(Tensor.GreaterThanAny(empty, empty));
            Assert.False(Tensor.GreaterThanAny(empty, 0));
            Assert.False(Tensor.GreaterThanAny(0, empty));
            Assert.False(Tensor.GreaterThanOrEqualAny(empty, empty));
            Assert.False(Tensor.GreaterThanOrEqualAny(empty, 0));
            Assert.False(Tensor.GreaterThanOrEqualAny(0, empty));
            Assert.False(Tensor.LessThanAny(empty, empty));
            Assert.False(Tensor.LessThanAny(empty, 0));
            Assert.False(Tensor.LessThanAny(0, empty));
            Assert.False(Tensor.LessThanOrEqualAny(empty, empty));
            Assert.False(Tensor.LessThanOrEqualAny(empty, 0));
            Assert.False(Tensor.LessThanOrEqualAny(0, empty));
            Assert.True(Tensor.EqualsAll(empty, empty));
            Assert.True(Tensor.EqualsAll(empty, 0));
            Assert.True(Tensor.GreaterThanAll(empty, empty));
            Assert.True(Tensor.GreaterThanAll(empty, 0));
            Assert.True(Tensor.GreaterThanAll(0, empty));
            Assert.True(Tensor.GreaterThanOrEqualAll(empty, empty));
            Assert.True(Tensor.GreaterThanOrEqualAll(empty, 0));
            Assert.True(Tensor.GreaterThanOrEqualAll(0, empty));
            Assert.True(Tensor.LessThanAll(empty, empty));
            Assert.True(Tensor.LessThanAll(empty, 0));
            Assert.True(Tensor.LessThanAll(0, empty));
            Assert.True(Tensor.LessThanOrEqualAll(empty, empty));
            Assert.True(Tensor.LessThanOrEqualAll(empty, 0));
            Assert.True(Tensor.LessThanOrEqualAll(0, empty));

            ReadOnlyTensorSpan<int> emptyColumn = new ReadOnlyTensorSpan<int>(new int[1], [0, 1], []);
            ReadOnlyTensorSpan<int> row = new ReadOnlyTensorSpan<int>([1, 2, 3], [1, 3], []);
            Assert.False(Tensor.EqualsAny(emptyColumn, row));
            Assert.True(Tensor.EqualsAll(emptyColumn, row));
        }

        [Fact]
        public static void TensorNumericReductionsUseBroadcastShape()
        {
            ReadOnlyTensorSpan<int> column = new ReadOnlyTensorSpan<int>([1, 2], [2, 1], []);
            ReadOnlyTensorSpan<int> row = new ReadOnlyTensorSpan<int>([10, 20], [1, 2], []);
            Assert.Equal(90, Tensor.Dot(column, row));
            Assert.Equal(90, Tensor.Dot(row, column));

            ReadOnlyTensorSpan<int> empty = new ReadOnlyTensorSpan<int>(new int[1], [0, 1], []);
            Assert.Equal(0, Tensor.Dot(empty, row));
            ReadOnlyTensorSpan<double> emptyDoubles = new ReadOnlyTensorSpan<double>(new double[1], [0, 1], []);
            ReadOnlyTensorSpan<double> doubleRow = new ReadOnlyTensorSpan<double>([10.0, 20.0], [1, 2], []);
            Assert.Equal(0.0, Tensor.Distance(emptyDoubles, doubleRow));
            Assert.Equal(0, Tensor.Dot(ReadOnlyTensorSpan<int>.Empty, ReadOnlyTensorSpan<int>.Empty));
        }

        [Fact]
        public static void AllocatingBinaryOperationsValidateAndBroadcast()
        {
            ReadOnlyTensorSpan<float> column = new ReadOnlyTensorSpan<float>([1f, 2f], [2, 1], []);
            ReadOnlyTensorSpan<float> row = new ReadOnlyTensorSpan<float>([-1f, -1f], [1, 2], []);
            Assert.Equal([2, 2], Tensor.Atan2(column, row).Lengths);
            Assert.Equal([2, 2], Tensor.Atan2Pi(column, row).Lengths);
            Assert.Equal([2, 2], Tensor.CopySign(column, row).Lengths);

            ReadOnlyTensorSpan<int> bitColumn = new ReadOnlyTensorSpan<int>([1, 2], [2, 1], []);
            ReadOnlyTensorSpan<int> bitRow = new ReadOnlyTensorSpan<int>([3, 3], [1, 2], []);
            Assert.Equal([2, 2], Tensor.BitwiseAnd(bitColumn, bitRow).Lengths);
            Assert.Equal([2, 2], Tensor.BitwiseOr(bitColumn, bitRow).Lengths);
            Assert.Equal([2, 2], Tensor.Xor(bitColumn, bitRow).Lengths);

            Assert.Throws<ArgumentException>(() => Tensor.Atan2(
                new ReadOnlyTensorSpan<float>([1, 2, 3]),
                new ReadOnlyTensorSpan<float>([-1, -1], [2])));
            Assert.Throws<ArgumentException>(() => Tensor.CopySign(
                new ReadOnlyTensorSpan<float>([1, 2, 3]),
                new ReadOnlyTensorSpan<float>([-1, -1], [2])));
            Assert.Throws<ArgumentException>(() => Tensor.BitwiseAnd(
                new ReadOnlyTensorSpan<int>([1, 2, 3]),
                new ReadOnlyTensorSpan<int>([1, 2], [2])));
        }

        [Theory]
        [InlineData(0, -8)]
        [InlineData(1, -8)]
        [InlineData(2, 3)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        [InlineData(5, -8)]
        public static void ElementwiseMinMaxUseSecondOperand(int operation, int expected)
        {
            ReadOnlyTensorSpan<int> x = new int[] { 3 };
            ReadOnlyTensorSpan<int> y = new int[] { -8 };
            Tensor<int> result = operation switch
            {
                0 => Tensor.MaxMagnitude(x, y),
                1 => Tensor.MaxMagnitudeNumber(x, y),
                2 => Tensor.MaxNumber(x, y),
                3 => Tensor.MinMagnitude(x, y),
                4 => Tensor.MinMagnitudeNumber(x, y),
                _ => Tensor.MinNumber(x, y),
            };
            Assert.Equal(expected, result[0]);

            int[] values = [99];
            TensorSpan<int> destination = new TensorSpan<int>(values);
            switch (operation)
            {
                case 0: Tensor.MaxMagnitude(x, -8, destination); break;
                case 1: Tensor.MaxMagnitudeNumber(x, -8, destination); break;
                case 2: Tensor.MaxNumber(x, -8, destination); break;
                case 3: Tensor.MinMagnitude(x, -8, destination); break;
                case 4: Tensor.MinMagnitudeNumber(x, -8, destination); break;
                default: Tensor.MinNumber(x, -8, destination); break;
            }
            Assert.Equal(expected, values[0]);
        }

        [Fact]
        public static void TensorRejectsCovariantArrays()
        {
            object[] data = new string[2];
            Assert.Throws<ArrayTypeMismatchException>(() => Tensor.Create(data));
            Assert.Throws<ArrayTypeMismatchException>(() => Tensor.Create(data, [2]));
            Assert.Throws<ArrayTypeMismatchException>(() => Tensor.Create(data, 0, [2], []));
        }

        [Fact]
        public static void TensorViewOperationsPreservePinning()
        {
            Tensor<int> tensor = Tensor.CreateFromShape<int>([2, 2], pinned: true);
            Assert.True(tensor.Reshape([4]).IsPinned);
            Assert.True(tensor.PermuteDimensions([1, 0]).IsPinned);
            Assert.True(Tensor.Transpose(tensor).IsPinned);
            Assert.True(tensor.Unsqueeze(0).IsPinned);
            Assert.True(tensor.Slice((ReadOnlySpan<nint>)[1, 0]).Squeeze().IsPinned);
        }

        [Fact]
        public static void TensorRejectsDuplicatePermutationAxes()
        {
            Tensor<int> tensor = Tensor.Create([1, 2], [1, 2]);
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions([0, 0]));
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions([1, 1]));
            Assert.Equal([2, 1], tensor.PermuteDimensions([1, 0]).Lengths);
            Assert.Throws<ArgumentException>(() => Tensor.Create([42]).PermuteDimensions([1]));
        }

        [Fact]
        public static void TensorRankZeroPermutationUsesEffectiveEmptyVector()
        {
            Tensor<int> tensor = Tensor<int>.Empty;
            Assert.Same(tensor, tensor.PermuteDimensions([]));
            Assert.Same(tensor, tensor.PermuteDimensions([0]));
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions([1]));
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions([-1]));
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions([0, 1]));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(321)]
        public static void TensorPermuteDimensionsAcrossBufferSizes(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            lengths[0] = 2;
            Tensor<int> tensor = Tensor.Create([1, 2], lengths);
            int[] dimensions = Enumerable.Range(0, rank).Reverse().ToArray();

            Tensor<int> permuted = tensor.PermuteDimensions(dimensions);
            Assert.Equal(2, permuted.Lengths[^1]);
            Assert.Equal([1, 2], permuted.ToArray());

            dimensions[0] = dimensions[^1];
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions(dimensions));

            dimensions[0] = rank;
            Assert.Throws<ArgumentException>(() => tensor.PermuteDimensions(dimensions));

            dimensions[0] = rank - 1;
            Assert.Equal([1, 2], tensor.PermuteDimensions(dimensions).ToArray());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        public static void TensorBroadcastResultAcrossBufferSizes(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            lengths[^1] = 2;
            Tensor<int> tensor = Tensor.Create([1, 2], lengths);

            Tensor<int> result = Tensor.Add<int>(tensor, Tensor.Create([10, 20]));

            Assert.Equal(lengths, result.Lengths);
            Assert.Equal([11, 22], result.ToArray());
        }

        [Fact]
        public static void TensorConcatenateEmptyLeadingDimensionsAndValidateDestination()
        {
            Tensor<int> empty = Tensor.CreateFromShape<int>([0, 2]);
            Tensor<int> result = Tensor.ConcatenateOnDimension(1, [empty, empty]);
            Assert.Equal([0, 4], result.Lengths);
            Assert.Equal(0, result.FlattenedLength);

            Tensor<int> first = Tensor.Create([1]);
            Tensor<int> second = Tensor.Create([2, 3]);
            int[] values = [7, 7, 7];
            Assert.Throws<ArgumentException>(() =>
                Tensor.ConcatenateOnDimension(-1, [first, second], new TensorSpan<int>(values, [2], [2])));
            Assert.Equal([7, 7, 7], values);
        }

        [Fact]
        public static void TensorConcatenateRejectsOverlappingInputs()
        {
            int[] data = [1, 2, 3, 4];
            Tensor<int> first = Tensor.Create(data, 2, [2], []);
            Tensor<int> second = Tensor.Create(data, 0, [2], []);

            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(-1, [first, second], new TensorSpan<int>(data)));

            Assert.Equal([1, 2, 3, 4], data);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public static void TensorConcatenateRejectsAliasedDestination(int dimension)
        {
            int[] backing = [99];
            Tensor<int> first = Tensor.Create([1]);
            Tensor<int> second = Tensor.Create([2]);

            Assert.Throws<ArgumentException>(() =>
                Tensor.ConcatenateOnDimension(dimension, [first, second], new TensorSpan<int>(backing, [2], [0])));
            Assert.Equal([99], backing);

            int[] gappedBacking = [99, 99, 99];
            TensorSpan<int> gappedDestination = new TensorSpan<int>(gappedBacking, [2], [2]);
            Tensor.ConcatenateOnDimension(dimension, [first, second], gappedDestination);
            Assert.Equal([1, 99, 2], gappedBacking);
        }

        [Fact]
        public static void TensorResizeToRejectsAliasedDestination()
        {
            int[] backing = [99];

            Assert.Throws<ArgumentException>(() =>
                Tensor.ResizeTo(new ReadOnlyTensorSpan<int>([42]), new TensorSpan<int>(backing, [2], [0])));
            Assert.Equal([99], backing);

            int[] gappedBacking = [99, 99, 99];
            Tensor.ResizeTo(new ReadOnlyTensorSpan<int>([42]), new TensorSpan<int>(gappedBacking, [2], [2]));
            Assert.Equal([42, 99, 0], gappedBacking);
        }

        [Fact]
        public static void TensorAuditCopiedEnumeratorKeepsIndependentPosition()
        {
            Tensor<int> tensor = Tensor.Create([10, 20, 99, 30, 40], [2, 2], [3, 1]);
            Tensor<int>.Enumerator first = tensor.GetEnumerator();
            Assert.True(first.MoveNext());
            Tensor<int>.Enumerator second = first;

            for (int i = 0; i < 3; i++)
            {
                second.Reset();
                Assert.True(second.MoveNext());
                Assert.True(second.MoveNext());
                Assert.True(first.MoveNext());
            }

            Assert.Equal(40, first.Current);
        }

        [Fact]
        public static void TensorConversionRejectsOverlappingDifferentElementTypes()
        {
            byte[] data = [1, 2, 0, 0, 0, 0, 0, 0];

            Assert.Throws<ArgumentException>(() =>
                Tensor.ConvertChecked<byte, int>(new ReadOnlyTensorSpan<byte>(data.AsSpan(0, 2)),
                    new TensorSpan<int>(MemoryMarshal.Cast<byte, int>(data.AsSpan()))));
            Assert.Equal([1, 2, 0, 0, 0, 0, 0, 0], data);
        }

        [Fact]
        public static void TensorAuditReverseBroadcastsAndHandlesOverlap()
        {
            int[] repeated = [7];
            TensorSpan<int> view = new TensorSpan<int>(repeated, [3], [0]);
            Tensor.Reverse<int>(view, view);
            Assert.Equal([7], repeated);

            int[] destination = [99, 99, 99];
            Tensor.ReverseDimension<int>(new ReadOnlyTensorSpan<int>([7]), new TensorSpan<int>(destination), 0);
            Assert.Equal([7, 7, 7], destination);
        }

        [Theory]
        [InlineData(new int[] { 7 }, new int[] { 2, 3 }, new int[] { 7, 7, 7, 7, 7, 7 })]
        [InlineData(new int[] { 1, 2 }, new int[] { 3, 2 }, new int[] { 2, 1, 2, 1, 2, 1 })]
        public static void TensorAuditReverseWithRankExpansion(int[] sourceValues, int[] destinationLengths, int[] expected)
        {
            int[] output = new int[expected.Length];
            Tensor.ReverseDimension<int>(
                new ReadOnlyTensorSpan<int>(sourceValues),
                new TensorSpan<int>(output, [destinationLengths[0], destinationLengths[1]]),
                0);
            Assert.Equal(expected, output);
        }

        [Fact]
        public static void TensorAuditResizeToEmptyShape()
        {
            Tensor<int> resized = Tensor.Resize(Tensor.Create([42]), ReadOnlySpan<nint>.Empty);

            Assert.Equal([0], resized.Lengths);
            Assert.Equal(0, resized.FlattenedLength);
        }

        [Fact]
        public static void TensorAuditSoftMaxShiftIsRepresentable()
        {
            double low = 1e16;
            double high = low + 708;
            Tensor<double> result = Tensor.SoftMax<double>(new ReadOnlyTensorSpan<double>([low, high, high, high, high, high, high]));

            Assert.Equal(0, result[0], 12);
            for (int i = 1; i < 7; i++)
            {
                Assert.Equal(1.0 / 6, result[i], 12);
            }
        }

        [Theory]
        [InlineData(false, 1.0, 0.0, Math.PI / 2)]
        [InlineData(true, 1.0, 0.0, 0.5)]
        [InlineData(false, -1.0, 0.0, -Math.PI / 2)]
        [InlineData(true, -1.0, 0.0, -0.5)]
        public static void TensorScalarLeftAtan2PreservesOperandOrder(bool divideByPi, double x, double y, double expected)
        {
            ReadOnlyTensorSpan<double> source = new ReadOnlyTensorSpan<double>([y]);
            Tensor<double> result = divideByPi ? Tensor.Atan2Pi(x, source) : Tensor.Atan2(x, source);
            Assert.Equal(expected, result[0], 12);

            double[] destination = [double.NaN];
            if (divideByPi)
            {
                Tensor.Atan2Pi(x, source, new TensorSpan<double>(destination));
            }
            else
            {
                Tensor.Atan2(x, source, new TensorSpan<double>(destination));
            }
            Assert.Equal(expected, destination[0], 12);
        }

        [Fact]
        public static void TensorOperationsRejectUnsafeOverlap()
        {
            int[] broadcastBacking = [1];
            Assert.Throws<ArgumentException>(() =>
                Tensor.Add<int>(new TensorSpan<int>(broadcastBacking, [3], [0]), 1,
                    new TensorSpan<int>(broadcastBacking, [3], [0])));
            Assert.Equal(1, broadcastBacking[0]);

            int[] gappedBacking = [1, 99, 99, 99, 2];
            Assert.Throws<ArgumentException>(() =>
                Tensor.Add<int>(new TensorSpan<int>(gappedBacking, [2, 2], [0, 4]), 1,
                    new TensorSpan<int>(gappedBacking, [2, 2], [0, 4])));
            Assert.Equal([1, 99, 99, 99, 2], gappedBacking);

            int[] shifted = [1, 2, 3, 4];
            Assert.Throws<ArgumentException>(() =>
                Tensor.Add<int>(new ReadOnlyTensorSpan<int>(shifted, 0, [3], []), 10,
                    new TensorSpan<int>(shifted, 1, [3], [])));
            Assert.Equal([1, 2, 3, 4], shifted);

            shifted = [1, 2, 3, 4];
            Assert.Throws<ArgumentException>(() =>
                Tensor.Add<int>(new ReadOnlyTensorSpan<int>(shifted, 0, [3], []),
                    new ReadOnlyTensorSpan<int>([10, 10, 10]), new TensorSpan<int>(shifted, 1, [3], [])));
            Assert.Equal([1, 2, 3, 4], shifted);

            int[] reversed = [1, 2, 3];
            Tensor.Reverse<int>(new ReadOnlyTensorSpan<int>(reversed), new TensorSpan<int>(reversed));
            Assert.Equal([3, 2, 1], reversed);
        }

        [Theory]
        [InlineData(-1, new int[] { 6, 5, 4, 3, 2, 1 })]
        [InlineData(0, new int[] { 4, 5, 6, 1, 2, 3 })]
        [InlineData(1, new int[] { 3, 2, 1, 6, 5, 4 })]
        public static void TensorReverseDenseInPlaceUsesNoSnapshot(int dimension, int[] expected)
        {
            int[] data = [1, 2, 3, 4, 5, 6];
            Tensor.ReverseDimension<int>(new ReadOnlyTensorSpan<int>(data, [2, 3]),
                new TensorSpan<int>(data, [2, 3]), dimension);
            Assert.Equal(expected, data);
        }

        [Fact]
        public static void TensorReverseBroadcastWithDistinctValuesRejectsInPlace()
        {
            int[] data = [1, 2];
            Assert.Throws<ArgumentException>(() =>
                Tensor.ReverseDimension<int>(new ReadOnlyTensorSpan<int>(data, [2, 2], [0, 1]),
                    new TensorSpan<int>(data, [2, 2], [0, 1]), 1));
            Assert.Equal([1, 2], data);
        }

        [Fact]
        public static void TensorReverseMiddleAxisDenseInPlace()
        {
            int[] data = [1, 2, 3, 4, 5, 6, 7, 8];
            Tensor.ReverseDimension<int>(new ReadOnlyTensorSpan<int>(data, [2, 2, 2]),
                new TensorSpan<int>(data, [2, 2, 2]), 1);
            Assert.Equal([3, 4, 1, 2, 7, 8, 5, 6], data);
        }

        [Theory]
        [InlineData(1000.0, 1000.0, 0.5, 0.5)]
        [InlineData(-1000.0, -1000.0, 0.5, 0.5)]
        [InlineData(-1000.0, 1000.0, 0.0, 1.0)]
        [InlineData(1000.0, -1000.0, 1.0, 0.0)]
        public static void TensorSoftMaxRemainsFiniteForFiniteInputs(double first, double second, double firstExpected, double secondExpected)
        {
            double[] data = [first, second];
            Tensor<double> output = Tensor.SoftMax<double>(new ReadOnlyTensorSpan<double>(data));
            Assert.Equal(firstExpected, output[0], 12);
            Assert.Equal(secondExpected, output[1], 12);

            Tensor.SoftMax<double>(new ReadOnlyTensorSpan<double>(data), new TensorSpan<double>(data));
            Assert.Equal(firstExpected, data[0], 12);
            Assert.Equal(secondExpected, data[1], 12);
        }

        [Theory]
        [InlineData(new double[] { double.NegativeInfinity, double.NegativeInfinity, 0 }, new double[] { 0, 0, 1 })]
        [InlineData(new double[] { 0, double.NegativeInfinity, double.NegativeInfinity }, new double[] { 1, 0, 0 })]
        [InlineData(new double[] { double.NegativeInfinity, double.NegativeInfinity, 0, 0 }, new double[] { 0, 0, 0.5, 0.5 })]
        public static void TensorSoftMaxHandlesNegativeInfinityWithFiniteValues(double[] input, double[] expected)
        {
            Tensor<double> result = Tensor.SoftMax<double>(new ReadOnlyTensorSpan<double>(input));
            Assert.Equal(expected, result.ToArray());

            double[] inPlace = (double[])input.Clone();
            Tensor.SoftMax<double>(new ReadOnlyTensorSpan<double>(inPlace), new TensorSpan<double>(inPlace));
            Assert.Equal(expected, inPlace);
        }

        [Fact]
        public static void TensorBroadcastToRejectsOverlappingViews()
        {
            int[] data = [1, 2, 3, 4];
            Assert.Throws<ArgumentException>(() =>
                Tensor.BroadcastTo(new ReadOnlyTensorSpan<int>(data, 1, [1, 2], []), new TensorSpan<int>(data, [2, 2])));
            Assert.Equal([1, 2, 3, 4], data);
        }

        [Fact]
        public static void TensorEmptyEnumerationAndCurrentPosition()
        {
            Tensor<int>.Enumerator empty = Tensor<int>.Empty.GetEnumerator();
            Assert.False(empty.MoveNext());
            Assert.Throws<InvalidOperationException>(() => _ = empty.Current);
            Assert.Equal(0, Tensor.Sum<int>(ReadOnlyTensorSpan<int>.Empty));

            Tensor<int>.Enumerator enumerator = Tensor.Create([7]).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(7, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
        }

        [Fact]
        public static void TensorEnumeratorsApplyRankOneStrides()
        {
            Tensor<int> tensor = Tensor.Create([10, 99, 20, 99, 30], [3], [2]);
            Assert.Equal(30, tensor.AsTensorSpan().GetDimensionSpan(0)[2][0]);
            Assert.Equal(30, tensor.AsReadOnlyTensorSpan().GetDimensionSpan(0)[2][0]);

            Tensor<int>.Enumerator tensorEnumerator = tensor.GetEnumerator();
            TensorSpan<int>.Enumerator spanEnumerator = tensor.AsTensorSpan().GetEnumerator();
            ReadOnlyTensorSpan<int>.Enumerator readOnlySpanEnumerator = tensor.AsReadOnlyTensorSpan().GetEnumerator();

            for (int i = 0; i < 3; i++)
            {
                int expected = (i + 1) * 10;
                Assert.True(tensorEnumerator.MoveNext());
                Assert.Equal(expected, tensorEnumerator.Current);
                Assert.True(spanEnumerator.MoveNext());
                Assert.Equal(expected, spanEnumerator.Current);
                Assert.True(readOnlySpanEnumerator.MoveNext());
                Assert.Equal(expected, readOnlySpanEnumerator.Current);
            }

            Assert.False(tensorEnumerator.MoveNext());
            Assert.False(spanEnumerator.MoveNext());
            Assert.False(readOnlySpanEnumerator.MoveNext());

            ReadOnlyTensorSpan<int> broadcast = Tensor.Create([42], [3], [0]).AsReadOnlyTensorSpan();
            ReadOnlyTensorSpan<int>.Enumerator broadcastEnumerator = broadcast.GetEnumerator();
            for (int i = 0; i < 3; i++)
            {
                Assert.True(broadcastEnumerator.MoveNext());
                Assert.Equal(42, broadcastEnumerator.Current);
            }
            Assert.False(broadcastEnumerator.MoveNext());
        }

        [Fact]
        public static void TensorSqueezeAllSingletonDimensionsRetainsElement()
        {
            Tensor<int> tensor = Tensor.Create([42], [1, 1]);
            Tensor<int> squeezed = tensor.Squeeze();
            Assert.Equal(1, squeezed.FlattenedLength);
            Assert.Equal([42], squeezed.ToArray());

            TensorSpan<int> mutable = tensor.AsTensorSpan().Squeeze();
            Assert.Equal(1, mutable.FlattenedLength);
            Assert.Equal(42, mutable[0]);

            ReadOnlyTensorSpan<int> readonlyView = tensor.AsReadOnlyTensorSpan().SqueezeDimension(0);
            readonlyView = readonlyView.SqueezeDimension(0);
            Assert.Equal(1, readonlyView.FlattenedLength);
            Assert.Equal(42, readonlyView[0]);
        }

        [Fact]
        public static void TensorInvalidShapeAndSliceThrowDocumentedExceptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.CreateFromShape<int>([nint.MaxValue, 2]));
            Tensor<int> tensor = Tensor.Create(new int[16], [4, 4]);
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice((ReadOnlySpan<nint>)[5, 0]));
        }

        [Fact]
        public static void TensorResizeTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8).ToArray(), lengths: [2, 2, 2]);
            var t1 = Tensor.Resize(t0, [1]);
            Assert.Equal([1], t1.Lengths);
            Assert.Equal(0, t1[0]);

            t1 = Tensor.Resize(t0, [1, 1]);
            Assert.Equal([1, 1], t1.Lengths);
            Assert.Equal(0, t1[0, 0]);

            t1 = Tensor.Resize(t0, [6]);
            Assert.Equal([6], t1.Lengths);
            Assert.Equal(0, t1[0]);
            Assert.Equal(1, t1[1]);
            Assert.Equal(2, t1[2]);
            Assert.Equal(3, t1[3]);
            Assert.Equal(4, t1[4]);
            Assert.Equal(5, t1[5]);

            t1 = Tensor.Resize(t0, [10]);
            Assert.Equal([10], t1.Lengths);
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
            Assert.Equal([2, 5], t1.Lengths);
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
        public static void TensorResizePreservesPinning()
        {
            Tensor<int> pinned = Tensor.CreateFromShape<int>([2], pinned: true);
            pinned[0] = 7;

            Tensor<int> resized = Tensor.Resize(pinned, [3]);

            Assert.True(resized.IsPinned);
            Assert.Equal([7, 0, 0], resized.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public static void TensorResizeToClearsUnwrittenElements(int sourceLength)
        {
            ReadOnlyTensorSpan<int> source = new ReadOnlyTensorSpan<int>(new int[] { 1, 2 }.AsSpan(0, sourceLength));
            int[] dense = [9, 9, 9, 9];
            Tensor.ResizeTo(source, new TensorSpan<int>(dense));
            Assert.Equal(sourceLength == 0 ? [0, 0, 0, 0] : [1, 2, 0, 0], dense);

            int[] strided = [9, 9, 9, 9, 9, 9];
            TensorSpan<int> destination = new TensorSpan<int>(strided, [2, 2], [3, 1]);
            Tensor.ResizeTo(source, destination);
            Assert.Equal(sourceLength == 0 ? [0, 0, 9, 0, 0, 9] : [1, 2, 9, 0, 0, 9], strided);
        }

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(0, false, true)]
        [InlineData(0, true, false)]
        [InlineData(0, true, true)]
        [InlineData(2, false, false)]
        [InlineData(2, false, true)]
        [InlineData(2, true, false)]
        [InlineData(2, true, true)]
        [InlineData(4, false, false)]
        [InlineData(4, false, true)]
        [InlineData(4, true, false)]
        [InlineData(4, true, true)]
        [InlineData(7, false, false)]
        [InlineData(7, false, true)]
        [InlineData(7, true, false)]
        [InlineData(7, true, true)]
        [InlineData(64, false, false)]
        [InlineData(64, false, true)]
        [InlineData(64, true, false)]
        [InlineData(64, true, true)]
        public static void TensorResizeToNonDenseSourceClearsUnwrittenElements(int destinationLength, bool denseDestination, bool broadcastSource)
        {
            Verify<int>([1, 2, 9, 3, 4, 9], 9);
            Verify<string>(["one", "two", "sentinel", "three", "four", "sentinel"], "sentinel");
            Verify<(int, string)>([(1, "one"), (2, "two"), (9, "sentinel"), (3, "three"), (4, "four"), (9, "sentinel")], (9, "sentinel"));

            void Verify<T>(T[] sourceData, T sentinel)
            {
                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, [2, 2], [broadcastSource ? 0 : 3, 1]);
                Assert.False(source.IsDense);

                int stride = denseDestination ? 1 : 2;
                T[] destinationData = new T[(destinationLength * stride) + 2];
                Array.Fill(destinationData, sentinel);
                TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, [1, destinationLength, 1], destinationLength == 0 ? [] : [0, stride, 0]);
                Assert.Equal(denseDestination || destinationLength == 0, destination.IsDense);

                Tensor.ResizeTo(source, destination);

                T[] expected = new T[destinationData.Length];
                Array.Fill(expected, sentinel);
                for (int i = 0; i < destinationLength; i++)
                {
                    expected[1 + (i * stride)] = i < 4 ? sourceData[broadcastSource ? i % 2 : (i / 2 * 3) + (i % 2)] : default!;
                }
                Assert.Equal(expected, destinationData);
            }
        }

        [Fact]
        public static void TensorResizeToRejectsOverlappingStridedViews()
        {
            int[] data = [1, 2, 3, 4, 5, 6];

            Assert.Throws<ArgumentException>(() =>
                Tensor.ResizeTo(new ReadOnlyTensorSpan<int>(data, [2, 2], [3, 1]), new TensorSpan<int>(data, 2, [2, 2], [])));
            Assert.Equal([1, 2, 3, 4, 5, 6], data);

            data = [1, 2, 3, 4, 5, 6];

            Assert.Throws<ArgumentException>(() =>
                Tensor.ResizeTo(new ReadOnlyTensorSpan<int>(data.AsSpan(0, 2)), new TensorSpan<int>(data, 1, [2, 2], [3, 1])));
            Assert.Equal([1, 2, 3, 4, 5, 6], data);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(47)]
        [InlineData(48)]
        [InlineData(49)]
        [InlineData(80)]
        [InlineData(96)]
        [InlineData(97)]
        [InlineData(160)]
        public static void TensorResizeToCopiesContiguousSourceRuns(int destinationLength)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                T[] sourceData = Enumerable.Range(0, 110).Select(createValue).ToArray();
                for (int layout = 0; layout < 4; layout++)
                {
                    nint[] lengths = layout == 2 ? [3, 2, 16] : [2, 3, 16];
                    nint[] strides = layout == 2 ? [16, 53, 1] : [layout == 1 ? 0 : 53, 16, 1];
                    if (layout == 3)
                    {
                        lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                        strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                    }
                    ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, 1, lengths, strides);
                    Assert.False(source.IsDense);
                    T[] destinationData = new T[destinationLength + 2];
                    Array.Fill(destinationData, sentinel);
                    TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, [destinationLength], []);

                    Tensor.ResizeTo(source, destination);

                    T[] expected = new T[destinationData.Length];
                    Array.Fill(expected, sentinel);
                    for (int i = 0; i < destinationLength; i++)
                    {
                        int offset = layout == 2
                            ? (i / 32 * 16) + (i / 16 % 2 * 53) + (i % 16)
                            : (layout == 1 ? 0 : i / 48 * 53) + (i % 48);
                        expected[i + 1] = i < 96 ? sourceData[offset + 1] : default!;
                    }
                    Assert.Equal(expected, destinationData);

                    Tensor<T> resized = Tensor.Resize(Tensor.Create(sourceData, 1, lengths, strides), [destinationLength]);
                    Assert.Equal(expected.AsSpan(1, destinationLength).ToArray(), resized.ToArray());
                }
            }
        }

        [Theory]
        [InlineData(1, 16)]
        [InlineData(1, 32)]
        [InlineData(1, 65)]
        [InlineData(2, 16)]
        [InlineData(2, 17)]
        [InlineData(2, 24)]
        [InlineData(2, 31)]
        [InlineData(2, 32)]
        [InlineData(3, 16)]
        [InlineData(3, 24)]
        [InlineData(3, 32)]
        [InlineData(3, 33)]
        [InlineData(6, 32)]
        [InlineData(32, 1)]
        [InlineData(64, 1)]
        [InlineData(65, 1)]
        public static void TensorResizeToCopiesIntoContiguousDestinationRuns(int rows, int columns)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                T[] sourceData = Enumerable.Range(0, 130).Select(createValue).ToArray();
                for (int layout = 0; layout < 6; layout++)
                {
                    nint[] lengths = layout is 0 or 5 ? [64] : layout == 3 ? [2, 2, 16] : [4, 16];
                    nint[] strides = layout == 0 ? [] : layout == 5 ? [2] : layout == 3 ? [19, 38, 1] : [layout == 2 ? 0 : 19, 1];
                    if (layout == 4)
                    {
                        lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                        strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                    }
                    ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, 1, lengths, strides);
                    T[] destinationData = new T[rows * (columns + 3) + 2];
                    Array.Fill(destinationData, sentinel);
                    TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, [rows, columns], [rows == 1 ? 0 : columns + 3, columns == 1 ? 0 : 1]);

                    Tensor.ResizeTo(source, destination);

                    T[] expected = new T[destinationData.Length];
                    Array.Fill(expected, sentinel);
                    for (int i = 0; i < rows * columns; i++)
                    {
                        int sourceOffset = layout switch
                        {
                            0 => i,
                            2 => i % 16,
                            3 => i / 32 * 19 + i / 16 % 2 * 38 + i % 16,
                            5 => i * 2,
                            _ => i / 16 * 19 + i % 16,
                        };
                        expected[1 + i / columns * (columns + 3) + i % columns] = i < 64 ? sourceData[sourceOffset + 1] : default!;
                    }
                    Assert.Equal(expected, destinationData);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(95)]
        [InlineData(96)]
        [InlineData(97)]
        [InlineData(160)]
        [InlineData(161)]
        [InlineData(191)]
        [InlineData(192)]
        [InlineData(193)]
        public static void TensorResizeToClearsContiguousDestinationRuns(int sourceLength)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                T[] sourceData = Enumerable.Range(0, sourceLength * 2).Select(createValue).ToArray();
                for (int layout = 0; layout < 6; layout++)
                {
                    ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(sourceData, [sourceLength], [sourceLength <= 1 ? 0 : layout % 2 + 1]);
                    nint[] lengths = layout >= 4 ? [3, 2, 32, 1] : [2, 3, 32, 1];
                    nint[] strides = layout >= 4 ? [35, 110, 1, 0] : [110, 35, 1, 0];
                    if (layout is 2 or 3)
                    {
                        lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                        strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                    }
                    T[] destinationData = new T[215];
                    Array.Fill(destinationData, sentinel);
                    TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, lengths, strides);
                    Assert.False(destination.IsDense);

                    Tensor.ResizeTo(source, destination);

                    T[] expected = new T[destinationData.Length];
                    Array.Fill(expected, sentinel);
                    for (int i = 0; i < 192; i++)
                    {
                        int offset = layout >= 4
                            ? (i / 64 * 35) + (i / 32 % 2 * 110) + (i % 32)
                            : (i / 96 * 110) + (i / 32 % 3 * 35) + (i % 32);
                        expected[offset + 1] = i < sourceLength ? sourceData[i * (layout % 2 + 1)] : default!;
                    }
                    Assert.Equal(expected, destinationData);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorResizeToRejectsOverlappingContiguousRuns(bool denseDestination)
        {
            int[] data = Enumerable.Range(1, 160).ToArray();
            int[] expected = (int[])data.Clone();

            Assert.Throws<ArgumentException>(() =>
                Tensor.ResizeTo(
                    new ReadOnlyTensorSpan<int>(data, [2, 32], [40, 1]),
                    new TensorSpan<int>(data, 16, [3, 32], denseDestination ? [] : [40, 1])));

            Assert.Equal(expected, data);
        }

        internal static unsafe void TensorResizeToDenseDestinationLongerThanSpan(int sourceLayout)
        {
            nint destinationLength = int.MaxValue;
            destinationLength += 65;
            nint rowLength = (int.MaxValue / 2) + 2;
            nint sourceStorageLength = rowLength * 2 + 2;
            long peakMemoryBytes = (long)(destinationLength + 2) + (sourceLayout is 1 or 3 ? (long)sourceStorageLength : 0);
            if (RunLargeMemoryTest(peakMemoryBytes, nameof(TensorResizeToDenseDestinationLongerThanSpan), sourceLayout.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(destinationLength + 2), zeroInitialize: true);
            byte* sourceBacking = null;
            try
            {
                backing[0] = byte.MaxValue;
                backing[destinationLength + 1] = byte.MaxValue;
                ReadOnlyTensorSpan<byte> source = sourceLayout == 4
                    ? new ReadOnlyTensorSpan<byte>(Array.Empty<byte>())
                    : sourceLayout == 2
                    ? new ReadOnlyTensorSpan<byte>(new byte[] { 1, 2 })
                    : new ReadOnlyTensorSpan<byte>(new byte[] { 1, 99, 2 }, [2], [2]);
                if (sourceLayout is 1 or 3)
                {
                    sourceBacking = (byte*)AllocateNativeMemory((nuint)sourceStorageLength, zeroInitialize: true);
                    sourceBacking[0] = 1;
                    sourceBacking[rowLength / 2] = 1;
                    sourceBacking[rowLength - 1] = 1;
                    int gap = sourceLayout == 1 ? 1 : 0;
                    sourceBacking[rowLength + gap] = 2;
                    sourceBacking[rowLength + gap + rowLength / 2] = 2;
                    sourceBacking[rowLength + gap + rowLength - 1] = 2;
                    if (gap != 0)
                    {
                        sourceBacking[rowLength] = 99;
                    }

                    source = new ReadOnlyTensorSpan<byte>(sourceBacking, sourceStorageLength, [2, rowLength], sourceLayout == 1 ? [rowLength + 1, 1] : []);
                }

                nint tailStart = source.FlattenedLength;
                if (tailStart < destinationLength)
                {
                    backing[tailStart + 1] = byte.MaxValue;
                    if (int.MaxValue > tailStart && int.MaxValue < destinationLength)
                    {
                        backing[unchecked((nint)int.MaxValue + 1)] = byte.MaxValue;
                    }
                    backing[destinationLength] = byte.MaxValue;
                }

                TensorSpan<byte> destination = new TensorSpan<byte>(backing + 1, destinationLength);

                Tensor.ResizeTo(source, destination);

                Assert.Equal(byte.MaxValue, backing[0]);
                if (sourceLayout is 1 or 3)
                {
                    Assert.Equal(1, backing[1]);
                    Assert.Equal(1, backing[rowLength / 2 + 1]);
                    Assert.Equal(1, backing[rowLength]);
                    Assert.Equal(2, backing[rowLength + 1]);
                    Assert.Equal(2, backing[rowLength + rowLength / 2 + 1]);
                    Assert.Equal(2, backing[2 * rowLength]);
                    if (sourceLayout == 1)
                    {
                        Assert.Equal(99, sourceBacking[rowLength]);
                    }
                }
                else
                {
                    Assert.Equal(sourceLayout == 4 ? 0 : 1, backing[1]);
                    Assert.Equal(sourceLayout == 4 ? 0 : 2, backing[2]);
                }
                if (tailStart < destinationLength)
                {
                    Assert.Equal(0, backing[tailStart + 1]);
                    if (int.MaxValue > tailStart && int.MaxValue < destinationLength)
                    {
                        Assert.Equal(0, backing[unchecked((nint)int.MaxValue + 1)]);
                    }
                    Assert.Equal(0, backing[destinationLength]);
                }
                Assert.Equal(byte.MaxValue, backing[destinationLength + 1]);
            }
            finally
            {
                NativeMemory.Free(sourceBacking);
                NativeMemory.Free(backing);
            }
        }

        internal static unsafe void TensorResizeToLargeDenseSourceSmallDestination()
        {
            nint sourceLength = int.MaxValue;
            sourceLength += 65;
            if (RunLargeMemoryTest((long)sourceLength + 66, nameof(TensorResizeToLargeDenseSourceSmallDestination)))
            {
                return;
            }

            byte* sourceData = (byte*)AllocateNativeMemory((nuint)sourceLength);
            try
            {
                new Span<byte>(sourceData, 64).Fill(7);
                byte[] destinationData = new byte[66];
                Array.Fill(destinationData, byte.MaxValue);
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData, sourceLength);
                TensorSpan<byte> destination = new TensorSpan<byte>(destinationData, 1, [64], []);

                Tensor.ResizeTo(source, destination);

                Assert.Equal(byte.MaxValue, destinationData[0]);
                Assert.Equal(-1, destinationData.AsSpan(1, 64).IndexOfAnyExcept((byte)7));
                Assert.Equal(byte.MaxValue, destinationData[^1]);
            }
            finally
            {
                NativeMemory.Free(sourceData);
            }
        }

        internal static unsafe void TensorResizeToLargeDenseOverlap(int shift, int lengthDifference)
        {
            nint sourceLength = int.MaxValue;
            sourceLength += 257;
            nint destinationLength = sourceLength + lengthDifference;
            if (RunLargeMemoryTest((long)sourceLength + 128, nameof(TensorResizeToLargeDenseOverlap), shift.ToString(CultureInfo.InvariantCulture), lengthDifference.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(sourceLength + 128), zeroInitialize: true);
            try
            {
                backing[0] = byte.MaxValue;
                backing[sourceLength + 127] = byte.MaxValue;
                byte* sourceData = backing + 64;
                nint boundary = (nint)int.MaxValue - 32;
                sourceData[0] = 1;
                sourceData[boundary] = 2;
                sourceData[unchecked((nint)int.MaxValue + 1)] = 4;
                sourceData[sourceLength - 64] = 3;
                byte* destinationData = sourceData + shift;
                ReadOnlyTensorSpan<byte> source = new ReadOnlyTensorSpan<byte>(sourceData, sourceLength);
                TensorSpan<byte> destination = new TensorSpan<byte>(destinationData, destinationLength);
                if (destinationLength > sourceLength)
                {
                    destinationData[destinationLength - 1] = byte.MaxValue;
                }

                Tensor.ResizeTo(source, destination);

                Assert.Equal(1, destinationData[0]);
                Assert.Equal(0, destinationData[1]);
                Assert.Equal(2, destinationData[boundary]);
                Assert.Equal(0, destinationData[boundary + 1]);
                Assert.Equal(4, destinationData[unchecked((nint)int.MaxValue + 1)]);
                Assert.Equal(3, destinationData[sourceLength - 64]);
                if (destinationLength > sourceLength)
                {
                    Assert.Equal(0, destinationData[sourceLength]);
                    Assert.Equal(0, destinationData[destinationLength - 1]);
                }
                Assert.Equal(byte.MaxValue, backing[0]);
                Assert.Equal(byte.MaxValue, backing[sourceLength + 127]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }

        }

        internal static unsafe void TensorClearAndFillDenseLongerThanSpan(bool singletonDimensions)
        {
            nint length = int.MaxValue;
            length += 65;
            if (RunLargeMemoryTest((long)length + 2, nameof(TensorClearAndFillDenseLongerThanSpan), singletonDimensions.ToString()))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                backing[0] = byte.MaxValue;
                backing[length + 1] = byte.MaxValue;
                nint[] markers = [0, 4095, 4096, int.MaxValue - 1, int.MaxValue, length - 1];
                foreach (nint marker in markers)
                {
                    backing[marker + 1] = byte.MaxValue;
                }

                TensorSpan<byte> destination = new TensorSpan<byte>(backing + 1, length, singletonDimensions ? [1, length, 1] : [length]);

                destination.Fill(7);
                Verify(7);
                destination.Clear();
                Verify(0);

                void Verify(byte expected)
                {
                    foreach (nint marker in markers)
                    {
                        Assert.Equal(expected, backing[marker + 1]);
                    }

                    Assert.Equal(byte.MaxValue, backing[0]);
                    Assert.Equal(byte.MaxValue, backing[length + 1]);
                }
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        [Fact]
        public static void TensorResizeWithStartTests()
        {
            Tensor<int> t0 = Tensor.Create([1, 2, 3, 4, 5, 6], start: 2, lengths: [4], strides: []);
            Tensor<int> t1 = Tensor.Resize(t0, [4]);
            Assert.Equal([4], t1.Lengths);
            Assert.Equal(3, t1[0]);
            Assert.Equal(4, t1[1]);
            Assert.Equal(5, t1[2]);
            Assert.Equal(6, t1[3]);

            t1 = Tensor.Resize(t0, [6]);
            Assert.Equal([6], t1.Lengths);
            Assert.Equal(3, t1[0]);
            Assert.Equal(4, t1[1]);
            Assert.Equal(5, t1[2]);
            Assert.Equal(6, t1[3]);
            Assert.Equal(0, t1[4]);
            Assert.Equal(0, t1[5]);

            t1 = Tensor.Resize(t0, [2]);
            Assert.Equal([2], t1.Lengths);
            Assert.Equal(3, t1[0]);
            Assert.Equal(4, t1[1]);

            t1 = Tensor.Resize(t0, [1, 1]);
            Assert.Equal([1, 1], t1.Lengths);
            Assert.Equal(3, t1[0, 0]);

            t1 = Tensor.Resize(t0, [2, 3]);
            Assert.Equal([2, 3], t1.Lengths);
            Assert.Equal(3, t1[0, 0]);
            Assert.Equal(4, t1[0, 1]);
            Assert.Equal(5, t1[0, 2]);
            Assert.Equal(6, t1[1, 0]);
            Assert.Equal(0, t1[1, 1]);
            Assert.Equal(0, t1[1, 2]);
        }

        [Fact]
        public static void TensorSplitTests()
        {
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8).ToArray(), lengths: [2, 2, 2]);
            var t1 = Tensor.Split<int>(t0, 2, 0);
            Assert.Equal([1, 2, 2], t1[0].Lengths);
            Assert.Equal([1, 2, 2], t1[1].Lengths);
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(3, t1[0][0, 1, 1]);
            Assert.Equal(4, t1[1][0, 0, 0]);
            Assert.Equal(5, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][0, 1, 0]);
            Assert.Equal(7, t1[1][0, 1, 1]);

            t1 = Tensor.Split<int>(t0, 2, 1);
            Assert.Equal([2, 1, 2], t1[0].Lengths);
            Assert.Equal([2, 1, 2], t1[1].Lengths);
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(5, t1[0][1, 0, 1]);
            Assert.Equal(2, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 0, 1]);

            t1 = Tensor.Split<int>(t0, 2, 2);
            Assert.Equal([2, 2, 1], t1[0].Lengths);
            Assert.Equal([2, 2, 1], t1[1].Lengths);
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(6, t1[0][1, 1, 0]);
            Assert.Equal(1, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 1, 0]);
            Assert.Equal(5, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 1, 0]);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public static void TensorSplitPreservesOutputShapesAfterBufferReuse(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            lengths[0] = 2;
            Tensor<int> source = Tensor.Create([3, 5], lengths);

            Tensor<int>[] result = Tensor.Split<int>(source, 2, 0);
            Tensor.Split<int>(source, 2, 0);

            Assert.Equal(1, result[0].Lengths[0]);
            Assert.Equal(1, result[1].Lengths[0]);
            Assert.Equal(3, result[0].ToArray()[0]);
            Assert.Equal(5, result[1].ToArray()[0]);
        }

        [Fact]
        public static void TensorReverseTests()
        {
            // Helper: verify Reverse correctness for any shape
            static void AssertReverseCorrect(nint[] shape)
            {
                nint totalLengthNative = 1;
                foreach (var s in shape)
                    totalLengthNative = checked(totalLengthNative * s);

                int totalLength = checked((int)totalLengthNative);

                // Use 1-based values so 0 (default) is never a valid value - makes bugs obvious
                int[] data = new int[totalLength];
                for (int i = 0; i < totalLength; i++)
                    data[i] = i + 1;

                var tensor = Tensor.Create<int>(data, shape);
                var reversed = Tensor.Reverse<int>(tensor);

                // Shape must be preserved
                Assert.Equal(tensor.Lengths.ToArray(), reversed.Lengths.ToArray());

                // Flattened elements should be in reverse order
                var flatOriginal = new int[totalLength];
                tensor.FlattenTo(flatOriginal);
                Array.Reverse(flatOriginal);

                var actualFlat = new int[totalLength];
                reversed.FlattenTo(actualFlat);

                Assert.Equal(flatOriginal, actualFlat);
            }

            // Test the reproduction case from issue #124105
            AssertReverseCorrect([1, 3]);

            // Test 1D tensors
            AssertReverseCorrect([1]);
            AssertReverseCorrect([3]);
            AssertReverseCorrect([5]);

            // Test 2D tensors with length-1 dimensions
            AssertReverseCorrect([3, 1]);
            AssertReverseCorrect([1, 1]);
            AssertReverseCorrect([1, 4]);
            AssertReverseCorrect([4, 1]);

            // Test 2D tensors asymmetric
            AssertReverseCorrect([2, 3]);
            AssertReverseCorrect([3, 2]);

            // Test 3D tensors with length-1 dimensions
            AssertReverseCorrect([1, 2, 3]);
            AssertReverseCorrect([2, 1, 3]);
            AssertReverseCorrect([2, 3, 1]);
            AssertReverseCorrect([1, 1, 3]);
            AssertReverseCorrect([1, 3, 1]);
            AssertReverseCorrect([3, 1, 1]);
            AssertReverseCorrect([1, 1, 1]);

            // Test larger tensors
            AssertReverseCorrect([2, 3, 4]);
            AssertReverseCorrect([1, 2, 3, 4]);
            AssertReverseCorrect([2, 1, 3, 2]);

            // Keep existing explicit test case
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 8).ToArray(), lengths: [2, 2, 2]);
            var t1 = Tensor.Reverse<int>(t0);
            Assert.Equal(7, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(5, t1[0, 1, 0]);
            Assert.Equal(4, t1[0, 1, 1]);
            Assert.Equal(3, t1[1, 0, 0]);
            Assert.Equal(2, t1[1, 0, 1]);
            Assert.Equal(1, t1[1, 1, 0]);
            Assert.Equal(0, t1[1, 1, 1]);

            t1 = Tensor.ReverseDimension<int>(t0, 0);
            Assert.Equal(4, t1[0, 0, 0]);
            Assert.Equal(5, t1[0, 0, 1]);
            Assert.Equal(6, t1[0, 1, 0]);
            Assert.Equal(7, t1[0, 1, 1]);
            Assert.Equal(0, t1[1, 0, 0]);
            Assert.Equal(1, t1[1, 0, 1]);
            Assert.Equal(2, t1[1, 1, 0]);
            Assert.Equal(3, t1[1, 1, 1]);

            t1 = Tensor.ReverseDimension<int>(t0, 1);
            Assert.Equal(2, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 0, 1]);
            Assert.Equal(0, t1[0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 1]);
            Assert.Equal(6, t1[1, 0, 0]);
            Assert.Equal(7, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(5, t1[1, 1, 1]);

            t1 = Tensor.ReverseDimension<int>(t0, 2);
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
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 10).ToArray(), lengths: [2, 5]);
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(10, 10).ToArray(), lengths: [2, 5]);
            Tensor.SetSlice(t0, t1, .., ..);

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

            t0 = Tensor.Create(Enumerable.Range(0, 10).ToArray(), lengths: [2, 5]);
            t1 = Tensor.Create(Enumerable.Range(10, 5).ToArray(), lengths: [1, 5]);
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

            t0 = Tensor.Create(Enumerable.Range(0, 10).ToArray(), lengths: [2, 5]);
            t1 = Tensor.Create(Enumerable.Range(10, 5).ToArray(), lengths: [1, 5]);
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
            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 10).ToArray(), lengths: [2, 5]);
            Tensor<int> t1 = Tensor.Create(Enumerable.Range(0, 10).ToArray(), lengths: [2, 5]);

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

            resultTensor = Tensor.StackAlongDimension(1, [t0, t1]);
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

            resultTensor = Tensor.StackAlongDimension(2, [t0, t1]);
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

            // stacking 2x2 tensors along dimension 1
            Tensor<int> v1 = Tensor.Create([1, 2, 3, 4], lengths: [2, 2]);
            Tensor<int> v2 = Tensor.Create([10, 20, 30, 40], lengths: [2, 2]);

            resultTensor = Tensor.StackAlongDimension(1, [v1, v2]);

            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);

            Assert.Equal(1, resultTensor[0, 0, 0]);
            Assert.Equal(2, resultTensor[0, 0, 1]);
            Assert.Equal(10, resultTensor[0, 1, 0]);
            Assert.Equal(20, resultTensor[0, 1, 1]);

            Assert.Equal(3, resultTensor[1, 0, 0]);
            Assert.Equal(4, resultTensor[1, 0, 1]);
            Assert.Equal(30, resultTensor[1, 1, 0]);
            Assert.Equal(40, resultTensor[1, 1, 1]);

            Tensor<int> resultTensor2 = Tensor.CreateFromShape<int>([(nint)2, 2, 2]);
            Tensor.StackAlongDimension([v1, v2], resultTensor2, 1);

            Assert.Equal(3, resultTensor2.Rank);
            Assert.Equal(2, resultTensor2.Lengths[0]);
            Assert.Equal(2, resultTensor2.Lengths[1]);
            Assert.Equal(2, resultTensor2.Lengths[2]);

            Assert.Equal(1, resultTensor2[0, 0, 0]);
            Assert.Equal(2, resultTensor2[0, 0, 1]);
            Assert.Equal(10, resultTensor2[0, 1, 0]);
            Assert.Equal(20, resultTensor2[0, 1, 1]);

            Assert.Equal(3, resultTensor2[1, 0, 0]);
            Assert.Equal(4, resultTensor2[1, 0, 1]);
            Assert.Equal(30, resultTensor2[1, 1, 0]);
            Assert.Equal(40, resultTensor2[1, 1, 1]);
        }

        public static IEnumerable<object[]> TensorStackLayoutsData()
        {
            foreach (int rank in new[] { 2, 6 })
            {
                for (int dimension = 0; dimension <= rank; dimension++)
                {
                    for (int layout = 0; layout < 4; layout++)
                    {
                        yield return new object[] { rank, dimension, layout, false };
                        yield return new object[] { rank, dimension, layout, true };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TensorStackLayoutsData))]
        public static void TensorStackPreservesLayoutsAndGuards(int rank, int dimension, int layout, bool gappedDestination)
        {
            Verify<int>(i => i, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                nint[] lengths = Enumerable.Repeat((nint)1, rank).ToArray();
                lengths[^2] = 2;
                lengths[^1] = 3;
                nint[] strides = new nint[rank];
                strides[^2] = layout switch { 1 => 4, 2 => 6, 3 => 0, _ => 3 };
                strides[^1] = layout == 2 ? 2 : 1;
                Tensor<T>[] sources = new Tensor<T>[3];
                T[][] logical = new T[3][];
                for (int s = 0; s < sources.Length; s++)
                {
                    T[] data = Enumerable.Range(s * 100, 15).Select(createValue).ToArray();
                    sources[s] = Tensor.Create(data, 1, lengths, strides);
                    logical[s] = Enumerable.Range(0, 6)
                        .Select(i => data[1 + i / 3 * (int)strides[^2] + i % 3 * (int)strides[^1]]).ToArray();
                }

                nint[] outputLengths = [.. lengths[..dimension], sources.Length, .. lengths[dimension..]];
                int blockLength = (int)CalculateTotalLength(lengths[dimension..]);
                if (dimension == rank)
                {
                    blockLength = 1;
                }
                T[] expected = new T[18];
                for (int i = 0; i < expected.Length; i++)
                {
                    int outer = i / (sources.Length * blockLength);
                    int source = i / blockLength % sources.Length;
                    expected[i] = logical[source][outer * blockLength + i % blockLength];
                }

                Tensor<T> allocated = Tensor.StackAlongDimension(dimension, sources);
                Assert.Equal(outputLengths, allocated.Lengths);
                Assert.Equal(expected, allocated.ToArray());

                T[] backing = new T[expected.Length * 2 + 2];
                Array.Fill(backing, sentinel);
                nint[] outputStrides = new nint[outputLengths.Length];
                nint stride = gappedDestination ? 2 : 1;
                for (int i = outputLengths.Length - 1; i >= 0; i--)
                {
                    outputStrides[i] = outputLengths[i] == 1 ? 0 : stride;
                    stride *= outputLengths[i];
                }
                TensorSpan<T> destination = new TensorSpan<T>(backing, 1, outputLengths, outputStrides);
                Tensor.StackAlongDimension(sources, destination, dimension);

                T[] expectedBacking = new T[backing.Length];
                Array.Fill(expectedBacking, sentinel);
                for (int i = 0; i < expected.Length; i++)
                {
                    expectedBacking[1 + i * (gappedDestination ? 2 : 1)] = expected[i];
                }
                Assert.Equal(expectedBacking, backing);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 2)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        public static void TensorStackEmptyInputs(int emptyDimension, int dimension)
        {
            nint[] lengths = [2, 3];
            lengths[emptyDimension] = 0;
            Tensor<int> empty = Tensor.Create(Array.Empty<int>(), lengths);
            Tensor<int> output = Tensor.StackAlongDimension(dimension, [empty, empty]);
            Assert.Equal([.. lengths[..dimension], (nint)2, .. lengths[dimension..]], output.Lengths);
            Assert.Equal(0, output.FlattenedLength);
            int[] backing = [-1];
            Tensor.StackAlongDimension([empty, empty],
                new TensorSpan<int>(backing, output.Lengths, []), dimension);
            Assert.Equal([-1], backing);
        }

        [Fact]
        public static void TensorStackValidatesBeforeWriting()
        {
            Tensor<int> first = Tensor.Create([1, 2], [2]);
            int[] backing = [9, 9, 9, 9, 9];
            Tensor<int> overlapping = Tensor.Create(backing, 1, [2], []);

            Assert.Throws<ArgumentException>(() =>
                Tensor.StackAlongDimension([first, overlapping], new TensorSpan<int>(backing, [2, 2]), 1));
            Assert.Throws<ArgumentException>(() =>
                Tensor.StackAlongDimension([first, first], new TensorSpan<int>(backing, [2, 2], [0, 1]), 0));
            Assert.Throws<ArgumentException>(() =>
                Tensor.StackAlongDimension([first, first], new TensorSpan<int>(backing, [4]), 0));
            Assert.Throws<ArgumentException>(() =>
                Tensor.StackAlongDimension([first, Tensor.Create([3])], new TensorSpan<int>(backing, [2, 2]), 0));
            Assert.Throws<ArgumentException>(() => Tensor.StackAlongDimension(-1, [first, first]));
            Assert.Throws<ArgumentException>(() => Tensor.StackAlongDimension(2, [first, first]));
            Assert.Throws<ArgumentException>(() => Tensor.StackAlongDimension(0, [first]));
            Assert.Equal([9, 9, 9, 9, 9], backing);
        }

        public static IEnumerable<object[]> StdDevFloatTestData()
        {
            // Test case where Abs doesn't change the result (real numbers, positive values)
            yield return new object[] 
            { 
                new float[] { 0f, 1f, 2f, 3f },
                StdDev([0f, 1f, 2f, 3f])
            };
            
            // Test case with all same values (should be 0)
            yield return new object[]
            {
                new float[] { 1f, 1f, 1f, 1f },
                0f
            };
            
            // Test case with negative values where Abs could matter (but doesn't for standard deviation)
            yield return new object[]
            {
                new float[] { -2f, -1f, 1f, 2f },
                StdDev([-2f, -1f, 1f, 2f])
            };
        }

        public static IEnumerable<object[]> StdDevComplexTestData()
        {
            // Test case where Abs is critical (complex numbers)
            yield return new object[]
            {
                new TestComplex[] { new(new(1, 2)), new(new(3, 4)) }
            };
            
            // Test case with purely imaginary numbers
            yield return new object[]
            {
                new TestComplex[] { new(new(0, 1)), new(new(0, 2)), new(new(0, 3)) }
            };
            
            // Test case with purely real numbers (should behave like floats)
            yield return new object[]
            {
                new TestComplex[] { new(new(1, 0)), new(new(2, 0)), new(new(3, 0)) }
            };
        }

        [Theory, MemberData(nameof(StdDevFloatTestData))]
        public static void TensorStdDevFloatTests(float[] data, float expectedStdDev)
        {
            var tensor = Tensor.Create(data);
            
            var tensorPrimitivesResult = TensorPrimitives.StdDev<float>(data);
            var tensorResult = Tensor.StdDev(tensor.AsReadOnlyTensorSpan());
            
            // Both should produce the same result
            Assert.Equal(tensorPrimitivesResult, tensorResult, precision: 5);
            Assert.Equal(expectedStdDev, tensorResult, precision: 5);
            
            // Test that non-contiguous calculations work with reshaped tensor
            if (data.Length >= 4)
            {
                var reshapedTensor = Tensor.Create(data, lengths: [2, 2]);
                var reshapedResult = Tensor.StdDev(reshapedTensor.AsReadOnlyTensorSpan());
                Assert.Equal(expectedStdDev, reshapedResult, precision: 5);
            }
        }

        [Theory, MemberData(nameof(StdDevComplexTestData))]
        public static void TensorStdDevComplexTests(TestComplex[] data)
        {
            var tensor = Tensor.Create(data);
            
            var tensorPrimitivesResult = TensorPrimitives.StdDev<TestComplex>(data);
            var tensorResult = Tensor.StdDev(tensor.AsReadOnlyTensorSpan());
            
            // Both should produce the same result - this is the key test for the fix
            Assert.Equal(tensorPrimitivesResult.Real, tensorResult.Real, precision: 10);
            Assert.Equal(tensorPrimitivesResult.Imaginary, tensorResult.Imaginary, precision: 10);
        }

        [Fact]
        public static void TensorStdDevNonContiguousTests()
        {
            // Test that non-contiguous calculations work for float tensors
            Tensor<float> fourByFour = Tensor.CreateFromShape<float>([4, 4]);
            fourByFour[[0, 0]] = 1f;
            fourByFour[[0, 1]] = 1f;
            fourByFour[[1, 0]] = 1f;
            fourByFour[[1, 1]] = 1f;
            ReadOnlyTensorSpan<float> upperLeft = fourByFour.AsReadOnlyTensorSpan().Slice([0..2, 0..2]);
            Assert.Equal(0f, Tensor.StdDev(upperLeft));
        }

        [Fact]
        public static void TensorSumTests()
        {
            float[] values = new float[] { 1, 2, 3, 4, 5, 6 };
            Tensor<float> t0 = Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, lengths: [2, 3]);
            float sum = Tensor.Sum<float>(t0);
            Assert.Equal(21, sum);

            // Slice first row of 2 x 2 for sum
            Tensor<float> t1 = t0.Slice(new NRange(new NIndex(0), new NIndex(1)), new NRange(new NIndex(0), new NIndex(0, true)));
            sum = Tensor.Sum<float>(t1);
            Assert.Equal(6, sum);

            // Slice second row of 2 x 2 for sum.
            t1 = t0.Slice(new NRange(new NIndex(1), new NIndex(2)), new NRange(new NIndex(0), new NIndex(0, true)));
            sum = Tensor.Sum<float>(t1);
            Assert.Equal(15, sum);

            // Slice first column of 2 x 2 for sum.
            t1 = t0.Slice(new NRange(new NIndex(0), new NIndex(2)), new NRange(new NIndex(0), new NIndex(1)));
            sum = Tensor.Sum<float>(t1);
            Assert.Equal(5, sum);

            // Slice second column of 2 x 2 for sum.
            t1 = t0.Slice(new NRange(new NIndex(0), new NIndex(2)), new NRange(new NIndex(1), new NIndex(2)));
            sum = Tensor.Sum<float>(t1);
            Assert.Equal(7, sum);

            // Slice Third column of 2 x 2 for sum.
            t1 = t0.Slice(new NRange(new NIndex(0), new NIndex(2)), new NRange(new NIndex(2), new NIndex(3)));
            sum = Tensor.Sum<float>(t1);
            Assert.Equal(9, sum);

            Assert.Throws<ArgumentOutOfRangeException>(()=> Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, start: -1, lengths: [2, 3], strides: []));
            Assert.Throws<ArgumentOutOfRangeException>(()=> Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, start: 100, lengths: [2, 3], strides: []));
            Assert.Throws<ArgumentOutOfRangeException>(()=> Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, start: int.MinValue, lengths: [2, 3], strides: []));
            Assert.Throws<ArgumentOutOfRangeException>(()=> Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, start: int.MaxValue, lengths: [2, 3], strides: []));
            Assert.Throws<ArgumentOutOfRangeException>(()=> Tensor.Create<float>(new float[] { 1, 2, 3, 4, 5, 6 }, start: 2, lengths: [2, 3], strides: []));
        }

        public static float StdDev(float[] values)
        {
            float mean = Mean(values);
            float sum = 0;
            for(int i = 0; i < values.Length; i++)
            {
                sum += MathF.Pow(values[i] - mean, 2);
            }
            return MathF.Sqrt(sum / values.Length);
        }

        [Fact]
        public static void TensorMeanTests()
        {
            Tensor<float> t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);

            Assert.Equal(Mean([0, 1, 2, 3]), Tensor.Average<float>(t0), .1);
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
            Tensor<float> t0 = Tensor.Create(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);
            Tensor<float> t1 = Tensor.Create(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);
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

            resultTensor = Tensor.ConcatenateOnDimension(1, [t0, t1]);
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

            resultTensor = Tensor.ConcatenateOnDimension(-1, [t0, t1]);
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

            Tensor<float> t2 = Tensor.Create(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);
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

            resultTensor = Tensor.ConcatenateOnDimension(-1, [t0, t1, t2]);

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

            resultTensor = Tensor.ConcatenateOnDimension(1, [t0, t1, t2]);

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

            t0 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 3, 2]);
            t1 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 3, 2]);
            t2 = Tensor.Create(Enumerable.Sequence<float>(0, 8, 1).ToArray(), lengths: [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(0, [t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(2, [t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(5, [t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(-2, [t0, t1, t2]));
            resultTensor = Tensor.ConcatenateOnDimension(-1, [t0, t1, t2]);
            float[] result = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7];
            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(32, resultTensor.Lengths[0]);
            Assert.Equal(result, resultTensor.ToArray());

            resultTensor = Tensor.ConcatenateOnDimension(1, [t0, t1, t2]);
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

            t0 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 2, 3]);
            t1 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 2, 3]);
            t2 = Tensor.Create(Enumerable.Sequence<float>(0, 8, 1).ToArray(), lengths: [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(1, [t0, t1, t2]));
            resultTensor = Tensor.ConcatenateOnDimension(2, [t0, t1, t2]);
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

            t0 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [3, 2, 2]);
            t1 = Tensor.Create(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [3, 2, 2]);
            t2 = Tensor.Create(Enumerable.Sequence<float>(0, 8, 1).ToArray(), lengths: [2, 2, 2]);
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(1, [t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(2, [t0, t1, t2]));
            resultTensor = Tensor.ConcatenateOnDimension(0, [t0, t1, t2]);
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

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public static void TensorConcatenateValidatesDestinationWithoutShapeBuffer(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            lengths[0] = 2;
            Tensor<int> first = Tensor.Create([1, 2], lengths);
            Tensor<int> second = Tensor.Create([3, 4], lengths);
            nint[] resultLengths = [.. lengths];
            resultLengths[^1] = 2;

            Tensor<int> result = Tensor.ConcatenateOnDimension(rank - 1, [first, second]);
            Assert.Equal(resultLengths, result.Lengths);
            Assert.Equal([1, 3, 2, 4], result.ToArray());

            TensorSpan<int> destination = new TensorSpan<int>(new int[4], resultLengths, []);
            Tensor.ConcatenateOnDimension(rank - 1, [first, second], destination);
            int[] actual = new int[4];
            destination.FlattenTo(actual);
            Assert.Equal([1, 3, 2, 4], actual);

            resultLengths[0] = 4;
            Assert.Throws<ArgumentException>(() =>
                Tensor.ConcatenateOnDimension(rank - 1, [first, second],
                    new TensorSpan<int>(new int[8], resultLengths, [])));
        }

        [Fact]
        public static void TensorConcatenateNonDenseDestinationTests()
        {
            Tensor<int> backing = Tensor.CreateFromShape<int>([2, 3]);
            TensorSpan<int> destination = backing.AsTensorSpan().Slice([0..2, 1..3]);
            Assert.False(destination.IsDense);

            Tensor<int> t0 = Tensor.Create([1, 2]);
            Tensor<int> t1 = Tensor.Create([3, 4]);

            Tensor.ConcatenateOnDimension(-1, [t0, t1], destination);

            Assert.Equal(0, backing[0, 0]);
            Assert.Equal(1, backing[0, 1]);
            Assert.Equal(2, backing[0, 2]);
            Assert.Equal(0, backing[1, 0]);
            Assert.Equal(3, backing[1, 1]);
            Assert.Equal(4, backing[1, 2]);
        }

        [Theory]
        [InlineData(2, 1, false)]
        [InlineData(2, 1, true)]
        [InlineData(3, 1, false)]
        [InlineData(3, 2, false)]
        [InlineData(6, 1, false)]
        [InlineData(6, 5, false)]
        [InlineData(6, 5, true)]
        public static void TensorConcatenateDenseInnerBlocks(int rank, int dimension, bool emptySource)
        {
            Verify<int>(i => i, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                nint[] lengths = Enumerable.Repeat((nint)2, rank).ToArray();
                int innerLength = 1 << (rank - dimension - 1);
                int outerLength = 1 << dimension;
                int[] axisLengths = [1, emptySource ? 0 : 3, 2];
                Tensor<T>[] sources = new Tensor<T>[axisLengths.Length];
                T[][] logical = new T[sources.Length][];
                for (int s = 0; s < sources.Length; s++)
                {
                    lengths[dimension] = axisLengths[s];
                    logical[s] = Enumerable.Range(s * 1000, outerLength * axisLengths[s] * innerLength)
                        .Select(createValue).ToArray();
                    T[] data = [sentinel, .. logical[s], sentinel];
                    sources[s] = Tensor.Create(data, 1, lengths, []);
                }
                lengths[dimension] = axisLengths.Sum();
                T[] expected = new T[outerLength * axisLengths.Sum() * innerLength];
                int copied = 0;
                for (int outer = 0; outer < outerLength; outer++)
                {
                    for (int s = 0; s < sources.Length; s++)
                    {
                        int count = axisLengths[s] * innerLength;
                        logical[s].AsSpan(outer * count, count).CopyTo(expected.AsSpan(copied));
                        copied += count;
                    }
                }
                Tensor<T> allocated = Tensor.ConcatenateOnDimension(dimension, sources);
                Assert.Equal(expected, allocated.ToArray());
                T[] backing = new T[expected.Length + 2];
                Array.Fill(backing, sentinel);
                Tensor.ConcatenateOnDimension(dimension, sources, new TensorSpan<T>(backing, 1, lengths, []));
                Assert.Equal([sentinel, .. expected, sentinel], backing);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public static void TensorConcatenateNonDenseDestinationAlongAxis(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            Tensor<int> first = Tensor.Create([1], lengths);
            Tensor<int> second = Tensor.Create([2], lengths);

            lengths[^1] = 2;
            nint[] strides = new nint[rank];
            strides[^1] = 2;
            int[] backing = [0, 0, 0];
            TensorSpan<int> destination = new TensorSpan<int>(backing, lengths, strides);

            Tensor.ConcatenateOnDimension(rank - 1, [first, second], destination);

            Assert.Equal([1, 0, 2], backing);
        }

        [Fact]
        public static void TensorConcatenateNonDenseReferenceDestinationTests()
        {
            Tensor<string> backing = Tensor.Create(["_", "_", "_", "_", "_", "_"], [2, 3]);
            TensorSpan<string> destination = backing.AsTensorSpan().Slice([0..2, 1..3]);
            Tensor<string> first = Tensor.Create(["a", "b"]);
            Tensor<string> second = Tensor.Create(["c", "d"]);

            Tensor.ConcatenateOnDimension(-1, [first, second], destination);

            Assert.Equal(["_", "a", "b", "_", "c", "d"], backing.ToArray());
        }

        [Theory]
        [InlineData(16, false)]
        [InlineData(17, false)]
        [InlineData(32, false)]
        [InlineData(17, true)]
        public static void TensorConcatenateContiguousDestinationRuns(int columns, bool highRank)
        {
            Verify<int>(i => i + 1, -1);
            Verify<string>(i => i.ToString(CultureInfo.InvariantCulture), "sentinel");
            Verify<(int, string)>(i => (i + 1, i.ToString(CultureInfo.InvariantCulture)), (-1, "sentinel"));

            void Verify<T>(Func<int, T> createValue, T sentinel)
            {
                int[] sourceLengths = [0, 1, 31, 32, 33, 8 * columns - 97, 0];
                Tensor<T>[] sources = new Tensor<T>[sourceLengths.Length];
                T[] logical = new T[8 * columns];
                int copied = 0;
                for (int s = 0; s < sources.Length; s++)
                {
                    int length = sourceLengths[s];
                    T[] data = Enumerable.Range(s * 100, Math.Max(length * 2, 40)).Select(createValue).ToArray();
                    if (s == 3)
                    {
                        sources[s] = Tensor.Create(data, [2, 16], [highRank ? 0 : 19, 1]);
                    }
                    else if (s == 4)
                    {
                        sources[s] = Tensor.Create(data, [length], [2]);
                    }
                    else
                    {
                        sources[s] = Tensor.Create(data.AsSpan(0, length).ToArray());
                    }
                    for (int i = 0; i < length; i++)
                    {
                        int offset = s == 3 ? (highRank ? 0 : i / 16 * 19) + i % 16 : s == 4 ? i * 2 : i;
                        logical[copied++] = data[offset];
                    }
                }

                nint[] lengths = [8, columns];
                nint[] strides = [columns + 3, 1];
                if (highRank)
                {
                    lengths = [1, 1, 1, 1, 1, 1, 1, 1, .. lengths];
                    strides = [0, 0, 0, 0, 0, 0, 0, 0, .. strides];
                }
                T[] destinationData = new T[8 * (columns + 3) + 2];
                Array.Fill(destinationData, sentinel);
                TensorSpan<T> destination = new TensorSpan<T>(destinationData, 1, lengths, strides);

                Tensor.ConcatenateOnDimension(-1, sources, destination);

                T[] expected = new T[destinationData.Length];
                Array.Fill(expected, sentinel);
                for (int i = 0; i < logical.Length; i++)
                {
                    expected[1 + i / columns * (columns + 3) + i % columns] = logical[i];
                }
                Assert.Equal(expected, destinationData);
            }
        }

        internal static unsafe void TensorConcatenateDenseDestinationLongerThanSpan(int dimension)
        {
            int rowLength = int.MaxValue / 2 + 33;
            nint length = (nint)rowLength * 2 + (dimension == -1 ? 34 : 0);
            if (RunLargeMemoryTest((long)(length + 2 + (nint)rowLength * 2), nameof(TensorConcatenateDenseDestinationLongerThanSpan), dimension.ToString(CultureInfo.InvariantCulture)))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                byte[] firstData = GC.AllocateUninitializedArray<byte>(rowLength);
                byte[] secondData = GC.AllocateUninitializedArray<byte>(rowLength);
                firstData[0] = 1;
                firstData[rowLength / 2] = 11;
                firstData[^1] = 12;
                secondData[0] = 2;
                secondData[rowLength / 2] = 22;
                secondData[^1] = 23;
                Tensor<byte> first = Tensor.Create(firstData, [1, rowLength]);
                Tensor<byte> second = Tensor.Create(secondData, [1, rowLength]);
                backing[0] = byte.MaxValue;
                backing[length + 1] = byte.MaxValue;
                TensorSpan<byte> destination = new TensorSpan<byte>(backing + 1, length, dimension == -1 ? [length] : [2, rowLength]);
                byte[] thirdData = new byte[33];
                thirdData.AsSpan().Fill(3);
                Tensor<byte>[] sources = dimension == -1
                    ? [first, second, Tensor.Create(thirdData), Tensor.Create<byte>(new byte[] { 4 })]
                    : [first, second];

                Tensor.ConcatenateOnDimension(dimension, sources, destination);

                Assert.Equal(1, backing[1]);
                Assert.Equal(11, backing[rowLength / 2 + 1]);
                Assert.Equal(12, backing[rowLength]);
                Assert.Equal(2, backing[rowLength + 1]);
                Assert.Equal(22, backing[rowLength + rowLength / 2 + 1]);
                Assert.Equal(23, backing[(nint)rowLength * 2]);
                if (dimension == -1)
                {
                    Assert.Equal(3, backing[(nint)rowLength * 2 + 1]);
                    Assert.Equal(3, backing[(nint)rowLength * 2 + 33]);
                    Assert.Equal(4, backing[length]);
                }
                Assert.Equal(byte.MaxValue, backing[0]);
                Assert.Equal(byte.MaxValue, backing[length + 1]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        internal static unsafe void TensorLargeDenseJoinInnerBlocks(bool stack)
        {
            int rows = int.MaxValue / 2 + 33;
            nint length = (nint)rows * 2;
            if (RunLargeMemoryTest((long)(length + 2 + (nint)rows * 2), nameof(TensorLargeDenseJoinInnerBlocks), stack.ToString()))
            {
                return;
            }

            byte* backing = (byte*)AllocateNativeMemory((nuint)(length + 2), zeroInitialize: true);
            try
            {
                byte[] firstData = GC.AllocateUninitializedArray<byte>(rows);
                byte[] secondData = GC.AllocateUninitializedArray<byte>(rows);
                firstData[0] = 1;
                firstData[rows / 2] = 11;
                firstData[^1] = 12;
                secondData[0] = 2;
                secondData[rows / 2] = 22;
                secondData[^1] = 23;
                Tensor<byte>[] sources =
                [
                    Tensor.Create(firstData, stack ? [rows] : [rows, 1]),
                    Tensor.Create(secondData, stack ? [rows] : [rows, 1]),
                ];
                backing[0] = byte.MaxValue;
                backing[length + 1] = byte.MaxValue;
                TensorSpan<byte> destination = new TensorSpan<byte>(backing + 1, length, [rows, 2]);
                if (stack)
                {
                    Tensor.StackAlongDimension(sources, destination, 1);
                }
                else
                {
                    Tensor.ConcatenateOnDimension(1, sources, destination);
                }
                Assert.Equal(1, backing[1]);
                Assert.Equal(2, backing[2]);
                Assert.Equal(11, backing[(nint)rows + 1]);
                Assert.Equal(22, backing[(nint)rows + 2]);
                Assert.Equal(12, backing[length - 1]);
                Assert.Equal(23, backing[length]);
                Assert.Equal(byte.MaxValue, backing[0]);
                Assert.Equal(byte.MaxValue, backing[length + 1]);
            }
            finally
            {
                NativeMemory.Free(backing);
            }
        }

        [Fact]
        public static void TensorTransposeTests()
        {
            Tensor<float> t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);
            var t1 = t0.PermuteDimensions([]);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 6, 1).ToArray(), lengths: [2, 3]);
            t1 = t0.PermuteDimensions([]);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(3, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(4, t1[1, 1]);
            Assert.Equal(2, t1[2, 0]);
            Assert.Equal(5, t1[2, 1]);

            t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 6, 1).ToArray(), lengths: [1, 2, 3]);
            t1 = t0.PermuteDimensions([]);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(1, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(1, t1[1, 0, 0]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(2, t1[2, 0, 0]);
            Assert.Equal(5, t1[2, 1, 0]);

            t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 2, 3]);
            t1 = t0.PermuteDimensions([]);

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

            t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 2, 3]);
            t1 = t0.PermuteDimensions([1, 2, 0]);

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
            Tensor<float> t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 4, 1).ToArray(), lengths: [2, 2]);
            var t1 = Tensor.Transpose(t0);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.Create<float>(Enumerable.Sequence<float>(0, 12, 1).ToArray(), lengths: [2, 2, 3]);
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
            TensorSpan<int> t1 = a.AsTensorSpan();
            nint[] dims = [4];
            var tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
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
            t1 = a.AsTensorSpan([2, 2]);
            dims = [2, 2];
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
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
            var tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
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
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 3, 3];
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2];
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [2, 2, 2, 2];
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2, 2];
            tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
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
            TensorSpan<int> t1 = a.AsTensorSpan([3, 3]);
            var tensor = Tensor.CreateFromShapeUninitialized<int>([3, 3], false);
            t1.CopyTo(tensor);
            var slice = tensor.Slice(0..2, 0..2);
            slice.Clear();
            Assert.Equal(0, slice[0, 0]);
            Assert.Equal(0, slice[0, 1]);
            Assert.Equal(0, slice[1, 0]);
            Assert.Equal(0, slice[1, 1]);

            // Since Tensor.Slice does do a copy the original tensor should be modified but only in the slice we took.
            Assert.Equal(0, tensor[0, 0]);
            Assert.Equal(0, tensor[0, 1]);
            Assert.Equal(3, tensor[0, 2]);
            Assert.Equal(0, tensor[1, 0]);
            Assert.Equal(0, tensor[1, 1]);
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
            t1 = a.AsTensorSpan([9]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([9], false);
            t1.CopyTo(tensor);
            slice = tensor.Slice(0..1);
            slice.Clear();
            Assert.Equal(0, slice[0]);

            // Since Tensor.Slice does do a copy the original tensor should be modified but only in the slice we took.
            Assert.Equal(0, tensor[0]);
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
            t1 = a.AsTensorSpan([3, 3, 3]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([3, 3, 3], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            t1 = a.AsTensorSpan([3, 2, 2]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([3, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            t1 = a.AsTensorSpan([2, 2, 2, 2]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([2, 2, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            t1 = a.AsTensorSpan([3, 2, 2, 2]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([3, 2, 2, 2], false);
            t1.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            // Make sure clearing a slice of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            t1 = a.AsTensorSpan([3, 3]);
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
            t1 = a.AsTensorSpan([3, 3]);
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
            t1 = a.AsTensorSpan([3, 3]);
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
            TensorSpan<object> spanObj = o.AsTensorSpan([3, 3]);
            spanObj.Clear();

            var oSpanEnumerator = spanObj.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan([3, 3]);
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
            spanObj = o.AsTensorSpan([3, 3]);
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
            spanObj = o.AsTensorSpan([3, 3]);
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
            TensorSpan<int> leftSpan = leftData.AsTensorSpan([3, 3]);
            var tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan([3, 3]);
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

            // Can't copy if data is not same shape or broadcastable to.
            Assert.Throws<ArgumentException>(() =>
                {
                    leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
                    dims = [15];
                    TensorSpan<int> leftSpan = leftData.AsTensorSpan([9]);
                    tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
                    leftSpan.CopyTo(tensor);
                }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = leftData.AsTensorSpan([3, 3, 3]);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void TensorTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan([3, 3]);
            nint[] dims = [3, 3];
            var tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan([3, 3]);
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
            leftSpan = leftData.AsTensorSpan([9]);
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            success = leftSpan.TryCopyTo(tensor);
            Assert.False(success);

            leftData = [.. Enumerable.Range(0, 27)];
            var l = leftData.AsTensorSpan([3, 3, 3]);
            dims = [2, 2];
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(tensor);
            Assert.False(success);
            success = tensor.TryCopyTo(r);
            Assert.False(success);

            success = new TensorSpan<double>(new double[1]).TryCopyTo(Array.Empty<double>());
            Assert.False(success);
        }

        [Fact]
        public static void TensorSliceTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            var tensor = Tensor.CreateFromShapeUninitialized<int>([3, 3], false);

            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(1..2));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1, 5..6));
            var intSpan = a.AsTensorSpan([3, 3]);
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
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan([3, 3]).Slice(0..1, 0..1)[0, 1]);
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
            intSpan = numbers.AsTensorSpan([3, 3, 3]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([3, 3, 3], false);
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
            intSpan = numbers.AsTensorSpan([2, 2, 2, 2]);
            tensor = Tensor.CreateFromShapeUninitialized<int>([2, 2, 2, 2], false);
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
            var tensor = Tensor.CreateFromShapeUninitialized<int>(dims.AsSpan(), false);
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

            dims = [-1];
            tensor = Tensor.Reshape(tensor, dims);
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

            dims = [3, -1];
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

            Assert.Throws<ArgumentException>(() => Tensor.Reshape(tensor, [-1, -1]));

            Assert.Throws<ArgumentException>(() => Tensor.Reshape(tensor, [1, 2, 3, 4, 5]));

            // Make sure reshape works correctly with 0 strides.
            tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], [0], false);
            tensor = Tensor.Reshape(tensor, [1, 2]);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);

            tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], [0], false);
            tensor = Tensor.Reshape(tensor, [2, 1]);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);

            tensor = Tensor.Reshape(tensor, [1, 2, 1]);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);
            Assert.Equal(0, tensor.Strides[2]);

            tensor = Tensor.Reshape(tensor, [1, 1, -1, 1]);
            Assert.Equal(4, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Lengths[2]);
            Assert.Equal(1, tensor.Lengths[3]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);
            Assert.Equal(0, tensor.Strides[2]);
            Assert.Equal(0, tensor.Strides[3]);
        }

        [Theory]
        [InlineData(new int[] { 0 }, new int[] { 0 })]
        [InlineData(new int[] { 2, 0 }, new int[] { 2, 0 })]
        [InlineData(new int[] { 0, 2 }, new int[] { 0, 2 })]
        [InlineData(new int[] { -1 }, new int[] { 0 })]
        [InlineData(new int[] { 2, -1 }, new int[] { 2, 0 })]
        [InlineData(new int[] { 1, 1, 1, 1, 1, 0 }, new int[] { 1, 1, 1, 1, 1, 0 })]
        public static void TensorDefaultEmptyReshape(int[] requestedLengths, int[] expectedLengths)
        {
            nint[] lengths = Array.ConvertAll(requestedLengths, static length => (nint)length);
            nint[] expected = Array.ConvertAll(expectedLengths, static length => (nint)length);
            Tensor<int> empty = Tensor<int>.Empty;
            Tensor<int> result = empty.Reshape(lengths);
            TensorSpan<int> span = TensorSpan<int>.Empty.Reshape(lengths);
            ReadOnlyTensorSpan<int> readOnlySpan = ReadOnlyTensorSpan<int>.Empty.Reshape(lengths);

            Assert.Equal(expected, result.Lengths);
            Assert.Equal(expected, span.Lengths);
            Assert.Equal(expected, readOnlySpan.Lengths);
            Assert.Equal(expected, empty.AsTensorSpan().Reshape(lengths).Lengths);
            Assert.Equal(expected, empty.AsReadOnlyTensorSpan().Reshape(lengths).Lengths);
            Assert.Equal(new nint[expected.Length], result.Strides);
            Assert.Equal(result.Strides, span.Strides);
            Assert.Equal(result.Strides, readOnlySpan.Strides);
            Assert.Equal(0, result.FlattenedLength);
            Assert.Equal(0, span.FlattenedLength);
            Assert.Equal(0, readOnlySpan.FlattenedLength);
            Assert.Equal(0, empty.Rank);
            Assert.Empty(empty.Lengths.ToArray());
        }

        [Theory]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 0, -1 })]
        [InlineData(new int[] { -1, -1 })]
        [InlineData(new int[] { 0, -2 })]
        public static void TensorDefaultEmptyReshapeRejectsInvalidShape(int[] requestedLengths)
        {
            nint[] lengths = Array.ConvertAll(requestedLengths, static length => (nint)length);

            Assert.Throws<ArgumentException>(() => Tensor<int>.Empty.Reshape(lengths));
            Assert.Throws<ArgumentException>(() => TensorSpan<int>.Empty.Reshape(lengths));
            Assert.Throws<ArgumentException>(() => ReadOnlyTensorSpan<int>.Empty.Reshape(lengths));
        }

        [Theory]
        [InlineData(0, new int[] { 1, 0 })]
        [InlineData(1, new int[] { 0, 1 })]
        public static void TensorDefaultEmptyUnsqueeze(int dimension, int[] expectedLengths)
        {
            nint[] expected = Array.ConvertAll(expectedLengths, static length => (nint)length);
            Tensor<int> empty = Tensor<int>.Empty;
            Tensor<int> result = empty.Unsqueeze(dimension);
            TensorSpan<int> span = TensorSpan<int>.Empty.Unsqueeze(dimension);
            ReadOnlyTensorSpan<int> readOnlySpan = ReadOnlyTensorSpan<int>.Empty.Unsqueeze(dimension);

            Assert.Equal(expected, result.Lengths);
            Assert.Equal(expected, span.Lengths);
            Assert.Equal(expected, readOnlySpan.Lengths);
            Assert.Equal(expected, empty.AsTensorSpan().Unsqueeze(dimension).Lengths);
            Assert.Equal(expected, empty.AsReadOnlyTensorSpan().Unsqueeze(dimension).Lengths);
            Assert.Equal(new nint[2], result.Strides);
            Assert.Equal(result.Strides, span.Strides);
            Assert.Equal(result.Strides, readOnlySpan.Strides);
            Assert.Equal(0, result.FlattenedLength);
            Assert.Equal(0, span.FlattenedLength);
            Assert.Equal(0, readOnlySpan.FlattenedLength);
            Assert.Throws<ArgumentException>(() => empty.Unsqueeze(2));
            Assert.Throws<ArgumentException>(() => TensorSpan<int>.Empty.Unsqueeze(2));
            Assert.Throws<ArgumentException>(() => ReadOnlyTensorSpan<int>.Empty.Unsqueeze(2));
            Assert.Equal(0, empty.Rank);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public static void TensorDefaultEmptyStack(int dimension, bool mixedShapes)
        {
            Tensor<int> other = mixedShapes ? Tensor.CreateFromShape<int>([0]) : Tensor<int>.Empty;
            nint[] expected = dimension == 0 ? [2, 0] : [0, 2];
            Tensor<int> destination = Tensor.CreateFromShape<int>(expected);
            Tensor<int>[] inputs = [Tensor<int>.Empty, other];

            Assert.Equal(expected, Tensor.StackAlongDimension(dimension, inputs).Lengths);
            Tensor.StackAlongDimension<int>(inputs, destination, dimension);
            Array.Reverse(inputs);
            Assert.Equal(expected, Tensor.StackAlongDimension(dimension, inputs).Lengths);
            Tensor.StackAlongDimension<int>(inputs, destination, dimension);
            Assert.Equal([2, 0], Tensor.Stack<int>(inputs).Lengths);
            Tensor.Stack<int>(inputs, Tensor.CreateFromShape<int>([2, 0]));
            Assert.Equal(0, destination.FlattenedLength);
            Assert.Throws<ArgumentException>(() => Tensor.StackAlongDimension(2, inputs));
            Assert.Equal(0, Tensor<int>.Empty.Rank);
        }

        [Theory]
        [InlineData(new int[] { 2 }, new int[] { 1, 2 }, true)]
        [InlineData(new int[] { 1 }, new int[] { 1, 1, 1, 1, 1, 1 }, true)]
        [InlineData(new int[] { }, new int[] { 0 }, true)]
        [InlineData(new int[] { }, new int[] { 1, 1, 1, 1, 1, 0 }, true)]
        [InlineData(new int[] { 2, 1 }, new int[] { 1, 2 }, false)]
        [InlineData(new int[] { 0 }, new int[] { 2, 0 }, false)]
        [InlineData(new int[] { 0 }, new int[] { 0, 2 }, false)]
        public static void TensorSequenceEqualEquivalentShapes(int[] firstLengths, int[] secondLengths, bool equivalent)
        {
            foreach (int firstSpacing in new int[] { 1, 2 })
            {
                foreach (int secondSpacing in new int[] { 1, 2 })
                {
                    Tensor<int> first = CreateModelTensor(firstLengths, firstSpacing, 0);
                    Tensor<int> second = CreateModelTensor(secondLengths, secondSpacing, 0);

                    Assert.Equal(equivalent, first.AsReadOnlyTensorSpan().SequenceEqual(second));
                    Assert.Equal(equivalent, second.AsReadOnlyTensorSpan().SequenceEqual(first));
                    Assert.Equal(equivalent, first.AsTensorSpan().SequenceEqual(second));
                    Assert.Equal(equivalent, second.AsTensorSpan().SequenceEqual(first));
                    Assert.Equal(Array.ConvertAll(firstLengths, static length => (nint)length), first.Lengths);
                    Assert.Equal(Array.ConvertAll(secondLengths, static length => (nint)length), second.Lengths);

                    if (equivalent && second.FlattenedLength != 0)
                    {
                        second.AsTensorSpan().Fill(-1);
                        Assert.False(first.AsReadOnlyTensorSpan().SequenceEqual(second));
                        Assert.False(second.AsReadOnlyTensorSpan().SequenceEqual(first));
                    }
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public static void TensorSequenceEqualEquivalentContiguousRows(int padding)
        {
            int[] values = Enumerable.Range(0, 64).ToArray();
            int[] backing = Enumerable.Repeat(-99, 72).ToArray();
            for (int row = 0; row < 4; row++)
            {
                values.AsSpan(row * 16, 16).CopyTo(backing.AsSpan(row * 18, 16));
            }

            nint[] lengths = new nint[padding + 2];
            Array.Fill(lengths, (nint)1);
            lengths[^2] = 4;
            lengths[^1] = 16;
            nint[] strides = new nint[lengths.Length];
            strides[^2] = 18;
            strides[^1] = 1;
            ReadOnlyTensorSpan<int> padded = new ReadOnlyTensorSpan<int>(backing, lengths, strides);
            ReadOnlyTensorSpan<int> dense = new ReadOnlyTensorSpan<int>(values, [4, 16]);

            Assert.True(padded.SequenceEqual(dense));
            Assert.True(dense.SequenceEqual(padded));
            backing[69] = -1;
            Assert.False(padded.SequenceEqual(dense));
            Assert.False(dense.SequenceEqual(padded));
        }

        [Theory]
        [InlineData(new int[] { 2 }, new int[] { 1, 2 }, 0)]
        [InlineData(new int[] { 2 }, new int[] { 1, 1, 1, 1, 1, 2 }, 1)]
        [InlineData(new int[] { 1, 2 }, new int[] { 2 }, 0)]
        [InlineData(new int[] { 1, 2 }, new int[] { 2 }, 1)]
        [InlineData(new int[] { 1, 1, 1, 1, 1, 2 }, new int[] { 2 }, 6)]
        [InlineData(new int[] { }, new int[] { 1, 0 }, 0)]
        [InlineData(new int[] { 0 }, new int[] { 1, 0 }, 1)]
        [InlineData(new int[] { 1, 0 }, new int[] { }, 1)]
        public static void TensorStackEquivalentShapes(int[] firstLengths, int[] secondLengths, int dimension)
        {
            foreach (int spacing in new int[] { 1, 2 })
            {
                Tensor<int> first = CreateModelTensor(firstLengths, spacing, 0);
                Tensor<int> second = CreateModelTensor(secondLengths, spacing, 100);
                nint[] referenceLengths = firstLengths.Length == 0 ? [0] : Array.ConvertAll(firstLengths, static length => (nint)length);
                Tensor<int> expected = Tensor.StackAlongDimension(dimension,
                    [Tensor.Create(first.ToArray(), referenceLengths), Tensor.Create(second.ToArray(), referenceLengths)]);
                Tensor<int>[] inputs = [first, second];
                Tensor<int> actual = Tensor.StackAlongDimension(dimension, inputs);
                Assert.Equal(expected.Lengths, actual.Lengths);
                Assert.Equal(expected.ToArray(), actual.ToArray());

                nint[] destinationLengths = [1, .. expected.Lengths];
                nint[] destinationStrides = [.. Tensor.CreateFromShape<int>(destinationLengths).Strides];
                for (int i = 0; i < destinationStrides.Length; i++)
                {
                    destinationStrides[i] *= spacing;
                }
                int[] backing = Enumerable.Repeat(-99, checked((int)expected.FlattenedLength * spacing + 1)).ToArray();
                TensorSpan<int> destination = new TensorSpan<int>(backing, destinationLengths, destinationStrides);
                Tensor.StackAlongDimension<int>(inputs, destination, dimension);
                int[] values = new int[(int)expected.FlattenedLength];
                destination.FlattenTo(values);
                Assert.Equal(expected.ToArray(), values);
                Assert.Equal(-99, backing[^1]);
                if (spacing == 2)
                {
                    Assert.All(backing.Where((_, index) => index % 2 == 1), value => Assert.Equal(-99, value));
                }

                Assert.Equal(Array.ConvertAll(firstLengths, static length => (nint)length), first.Lengths);
                Assert.Equal(Array.ConvertAll(secondLengths, static length => (nint)length), second.Lengths);
            }
        }

        [Theory]
        [InlineData(new int[] { 2, 1 }, new int[] { 1, 2 })]
        [InlineData(new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 0 }, new int[] { 2, 0 })]
        public static void TensorStackRejectsInequivalentShapes(int[] firstLengths, int[] secondLengths)
        {
            Tensor<int>[] inputs = [CreateModelTensor(firstLengths, 1, 0), CreateModelTensor(secondLengths, 1, 100)];
            Assert.Throws<ArgumentException>(() => Tensor.Stack<int>(inputs));
            Assert.Throws<ArgumentException>(() => Tensor.Stack<int>(inputs, Tensor.CreateFromShape<int>([4])));
        }

        [Theory]
        [InlineData(new int[] { 2 }, new int[] { 1, 3 }, 0, new int[] { 5 }, new int[] { 0, 1, 100, 101, 102 })]
        [InlineData(new int[] { 1, 2 }, new int[] { 3 }, 1, new int[] { 1, 5 }, new int[] { 0, 1, 100, 101, 102 })]
        [InlineData(new int[] { 2, 2 }, new int[] { 1, 2, 3 }, 1, new int[] { 2, 5 }, new int[] { 0, 1, 100, 101, 102, 2, 3, 103, 104, 105 })]
        [InlineData(new int[] { 1, 1, 1, 1, 2, 2 }, new int[] { 2, 3 }, 5, new int[] { 1, 1, 1, 1, 2, 5 }, new int[] { 0, 1, 100, 101, 102, 2, 3, 103, 104, 105 })]
        [InlineData(new int[] { 1, 1, 2 }, new int[] { 2 }, 0, new int[] { 2, 1, 2 }, new int[] { 0, 1, 100, 101 })]
        [InlineData(new int[] { 1, 1, 2 }, new int[] { 2 }, 1, new int[] { 1, 2, 2 }, new int[] { 0, 1, 100, 101 })]
        [InlineData(new int[] { 1, 1, 0 }, new int[] { }, 1, new int[] { 1, 2, 0 }, new int[] { })]
        [InlineData(new int[] { }, new int[] { 1, 0 }, 0, new int[] { 0 }, new int[] { })]
        [InlineData(new int[] { 1, 0 }, new int[] { }, 1, new int[] { 1, 0 }, new int[] { })]
        [InlineData(new int[] { }, new int[] { 1, 2 }, 0, new int[] { 2 }, new int[] { 100, 101 })]
        public static void TensorConcatenateEquivalentAxes(int[] firstLengths, int[] secondLengths, int dimension, int[] expectedLengths, int[] expected)
        {
            foreach (int spacing in new int[] { 1, 2 })
            {
                Tensor<int>[] inputs = [CreateModelTensor(firstLengths, spacing, 0), CreateModelTensor(secondLengths, spacing, 100)];
                nint[] lengths = Array.ConvertAll(expectedLengths, static length => (nint)length);
                Tensor<int> actual = Tensor.ConcatenateOnDimension(dimension, inputs);
                Assert.Equal(lengths, actual.Lengths);
                Assert.Equal(expected, actual.ToArray());

                foreach (nint[] destinationLengths in new nint[][] { lengths, [1, .. lengths] })
                {
                    nint[] strides = [.. Tensor.CreateFromShape<int>(destinationLengths).Strides];
                    for (int i = 0; i < strides.Length; i++)
                    {
                        strides[i] *= spacing;
                    }
                    int[] backing = Enumerable.Repeat(-99, expected.Length * spacing + 1).ToArray();
                    TensorSpan<int> destination = new TensorSpan<int>(backing, destinationLengths, strides);
                    Tensor.ConcatenateOnDimension(dimension, inputs, destination);
                    int[] values = new int[expected.Length];
                    destination.FlattenTo(values);
                    Assert.Equal(expected, values);
                    Assert.Equal(-99, backing[^1]);
                    if (spacing == 2)
                    {
                        Assert.All(backing.Where((_, index) => index % 2 == 1), value => Assert.Equal(-99, value));
                    }
                }
                Assert.Equal(Array.ConvertAll(firstLengths, static length => (nint)length), inputs[0].Lengths);
                Assert.Equal(Array.ConvertAll(secondLengths, static length => (nint)length), inputs[1].Lengths);
            }
        }

        [Fact]
        public static void TensorConcatenateEquivalentDestinationDropsPadding()
        {
            Tensor<int>[] inputs = [Tensor.Create([1, 2], [1, 2]), Tensor.Create([3, 4], [2])];
            int[] backing = [-99, -99, -99, -99, -99, -99, -99, -99];
            Tensor.ConcatenateOnDimension(1, inputs, new TensorSpan<int>(backing, [4], [2]));
            Assert.Equal([1, -99, 2, -99, 3, -99, 4, -99], backing);

            Tensor.StackAlongDimension<int>(inputs, new TensorSpan<int>(backing, [2, 2], [4, 2]), 1);
            Assert.Equal([1, -99, 2, -99, 3, -99, 4, -99], backing);
        }

        [Theory]
        [InlineData(0, new int[] { 2, 1, 3 })]
        [InlineData(1, new int[] { 1, 2, 3 })]
        public static void TensorCombineEquivalentSlicedInputs(int dimension, int[] expectedLengths)
        {
            Tensor<int> first = Tensor.Create([-99, -99, -99, 1, 2, 3], [2, 3]).Slice(1..2, NRange.All);
            Tensor<int> second = Tensor.Create([-99, 4, 5, 6, -99]).Slice(1..4);
            Tensor<int>[] inputs = [first, second];
            Tensor<int> stacked = Tensor.StackAlongDimension(dimension, inputs);
            Assert.Equal(Array.ConvertAll(expectedLengths, static length => (nint)length), stacked.Lengths);
            Assert.Equal([1, 2, 3, 4, 5, 6], stacked.ToArray());

            Tensor<int> concatenated = Tensor.ConcatenateOnDimension(1, inputs);
            Assert.Equal([1, 6], concatenated.Lengths);
            Assert.Equal([1, 2, 3, 4, 5, 6], concatenated.ToArray());
            int[] backing = Enumerable.Repeat(-99, 13).ToArray();
            Tensor.ConcatenateOnDimension(1, inputs, new TensorSpan<int>(backing, [6], [2]));
            Assert.Equal([1, -99, 2, -99, 3, -99, 4, -99, 5, -99, 6, -99, -99], backing);
            Assert.Equal([1, 2, 3], first.ToArray());
            Assert.Equal([4, 5, 6], second.ToArray());
        }

        [Theory]
        [InlineData(new int[] { 2 }, new int[] { 2, 2 }, 0)]
        [InlineData(new int[] { 2, 2 }, new int[] { 1, 3, 2 }, 1)]
        [InlineData(new int[] { 2, 2 }, new int[] { 2, 3 }, 0)]
        [InlineData(new int[] { 2, 1, 2 }, new int[] { 2 }, 1)]
        [InlineData(new int[] { 1, 2, 1 }, new int[] { 2 }, 0)]
        [InlineData(new int[] { 0 }, new int[] { 2, 0 }, 0)]
        public static void TensorConcatenateRejectsIncompatibleAxes(int[] firstLengths, int[] secondLengths, int dimension)
        {
            Tensor<int>[] inputs = [CreateModelTensor(firstLengths, 1, 0), CreateModelTensor(secondLengths, 1, 100)];
            int[] backing = Enumerable.Repeat(-99, 16).ToArray();
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(dimension, inputs));
            Assert.Throws<ArgumentException>(() => Tensor.ConcatenateOnDimension(dimension, inputs, new TensorSpan<int>(backing)));
            Assert.All(backing, value => Assert.Equal(-99, value));
        }

        [Theory]
        [InlineData(false, new int[] { 2, 1, 2 })]
        [InlineData(false, new int[] { 1, 4, 1 })]
        [InlineData(false, new int[] { 4 })]
        [InlineData(true, new int[] { 2, 1, 1, 2 })]
        [InlineData(true, new int[] { 1, 4, 1, 1 })]
        [InlineData(true, new int[] { 2, 2 })]
        public static void TensorCombineRejectsDestinationBeforeWriting(bool stack, int[] destinationLengths)
        {
            Tensor<int>[] inputs = [Tensor.Create([1, 2], [1, 1, 2]), Tensor.Create([3, 4], [2])];
            int[] backing = [-99, -99, -99, -99];
            nint[] lengths = Array.ConvertAll(destinationLengths, static length => (nint)length);
            Assert.Throws<ArgumentException>("destination", () =>
            {
                TensorSpan<int> destination = new TensorSpan<int>(backing, lengths);
                if (stack)
                {
                    Tensor.StackAlongDimension<int>(inputs, destination, 1);
                }
                else
                {
                    Tensor.ConcatenateOnDimension(1, inputs, destination);
                }
            });
            Assert.Equal([-99, -99, -99, -99], backing);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorCombineRejectsOverlappingStorageBeforeWriting(bool selfOverlapping)
        {
            int[] backing = [1, 2, -99, -99];
            Tensor<int> first = Tensor.Create(backing, [2]);
            Tensor<int>[] inputs = [first, Tensor.Create([3, 4], [2])];
            Assert.Throws<ArgumentException>(() =>
                Tensor.StackAlongDimension<int>(inputs, new TensorSpan<int>(backing, [2, 2], selfOverlapping ? [0, 1] : [2, 1]), 0));
            Assert.Equal([1, 2, -99, -99], backing);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public static void TensorDefaultEmptyAxisOperations(int splitCount)
        {
            Tensor<int>[] outputs = Tensor.Split<int>(ReadOnlyTensorSpan<int>.Empty, splitCount, 0);
            Assert.Equal(splitCount, outputs.Length);
            Assert.All(outputs, output =>
            {
                Assert.Equal([0], output.Lengths);
                Assert.Empty(output.ToArray());
            });

            Tensor<int>[] inputs = [Tensor<int>.Empty, Tensor.CreateFromShape<int>([0])];
            Assert.Equal([0], Tensor.Concatenate<int>(inputs).Lengths);
            Tensor.Concatenate<int>(inputs, TensorSpan<int>.Empty);
            Array.Reverse(inputs);
            Assert.Equal([0], Tensor.Concatenate<int>(inputs).Lengths);
            Tensor.Concatenate<int>(inputs, TensorSpan<int>.Empty);

            Assert.Equal(0, Tensor<int>.Empty.PermuteDimensions([0]).FlattenedLength);
            Assert.Equal([0], Tensor.ReverseDimension<int>(ReadOnlyTensorSpan<int>.Empty, 0).Lengths);
            Tensor.ReverseDimension<int>(ReadOnlyTensorSpan<int>.Empty, TensorSpan<int>.Empty, 0);
            Assert.Equal([0], Tensor<int>.Empty.Slice(NRange.All).Lengths);
            Assert.Equal([0], TensorSpan<int>.Empty.Slice(NRange.All).Lengths);
            Assert.Equal([0], ReadOnlyTensorSpan<int>.Empty.Slice(NRange.All).Lengths);
            Assert.Throws<ArgumentOutOfRangeException>(() => TensorSpan<int>.Empty.Slice((nint)0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<int>(Array.Empty<int>(), [0]).Slice((nint)0));
            Assert.Throws<IndexOutOfRangeException>(() => ReadOnlyTensorSpan<int>.Empty.Slice(NIndex.End));
            Assert.Throws<IndexOutOfRangeException>(() => new ReadOnlyTensorSpan<int>(Array.Empty<int>(), [0]).Slice(NIndex.End));
            Assert.Equal(0, Tensor<int>.Empty.GetDimensionSpan(0).Length);
            Assert.Equal(0, TensorSpan<int>.Empty.GetDimensionSpan(0).Length);
            Assert.Equal(0, ReadOnlyTensorSpan<int>.Empty.GetDimensionSpan(0).Length);
            Assert.Equal(0, ((ReadOnlyTensorDimensionSpan<int>)TensorSpan<int>.Empty.GetDimensionSpan(0)).Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = TensorSpan<int>.Empty.GetDimensionSpan(0)[0]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = ReadOnlyTensorSpan<int>.Empty.GetDimensionSpan(0)[0]; });
            Assert.Throws<ArgumentException>(() => Tensor.Split<int>(ReadOnlyTensorSpan<int>.Empty, splitCount, 1));
            Assert.Throws<ArgumentException>(() => Tensor.ReverseDimension<int>(ReadOnlyTensorSpan<int>.Empty, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => TensorSpan<int>.Empty.GetDimensionSpan(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReadOnlyTensorSpan<int>.Empty.Slice((nint)1));
            Assert.Equal(0, Tensor<int>.Empty.Rank);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public static void TensorSplitRejectsNonpositiveCount(int splitCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>("splitCount", () => Tensor.Split<int>(Tensor.CreateFromShape<int>([0]), splitCount, 0));
            Assert.Throws<ArgumentOutOfRangeException>("splitCount", () => Tensor.Split<int>(ReadOnlyTensorSpan<int>.Empty, splitCount, 0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorEmptySlicesPreserveOrigin(bool slicedSource)
        {
            Tensor<int> source = Tensor.Create(Enumerable.Range(0, 9).ToArray(), [3, 3]);
            if (slicedSource)
            {
                source = source.Slice(1..3, NRange.All);
            }

            nint rows = source.Lengths[0];
            NRange[] ranges = [new NRange(rows, rows), 3..3];
            nint[] indexes = [rows, 3];
            NIndex[] nindexes = [NIndex.End, NIndex.End];
            Tensor<int> slice = source.Slice(ranges);
            Assert.Equal([0, 0], slice.Lengths);
            Assert.Equal([1, 0, 0], slice.Unsqueeze(0).Lengths);
            Assert.Equal([0], slice.Reshape([0]).Lengths);
            Assert.Empty(slice.ToArray());

            TensorSpan<int> span = source.AsTensorSpan();
            TensorSpan<int> expected = span.Slice(0..0, 0..0);
            Assert.True(span.Slice(ranges) == expected);
            ReadOnlyTensorSpan<int> readOnly = source.AsReadOnlyTensorSpan();
            ReadOnlyTensorSpan<int> readOnlyExpected = readOnly.Slice(0..0, 0..0);
            Assert.True(readOnly.Slice(ranges) == readOnlyExpected);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Slice(indexes));
            Assert.Throws<IndexOutOfRangeException>(() => source.Slice(nindexes));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.AsTensorSpan().Slice(indexes));
            Assert.Throws<IndexOutOfRangeException>(() => source.AsReadOnlyTensorSpan().Slice(nindexes));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Slice(new NRange(rows + 1, rows + 1), 3..3));
        }

        [Fact]
        public static void TensorEmptyStridedViewsPreserveOrigin()
        {
            Tensor<int> source = Tensor.Create(Array.Empty<int>(), [2, 0], [1, 0]);
            Tensor<int> slice = source.Slice(1..2, NRange.All);
            Assert.Equal([0], slice.Squeeze().Lengths);
            Assert.Equal([1, 1, 0], slice.Unsqueeze(0).Lengths);
            TensorDimensionSpan<int> dimensions = source.GetDimensionSpan(0);
            Assert.Equal(2, dimensions.Length);
            Assert.True(dimensions[0] == dimensions[1]);
            Assert.Equal([0], dimensions[1].Lengths);
            ReadOnlyTensorDimensionSpan<int> readOnlyDimensions = source.AsReadOnlyTensorSpan().GetDimensionSpan(0);
            Assert.True(readOnlyDimensions[0] == readOnlyDimensions[1]);
            Assert.Equal([0], readOnlyDimensions[1].Lengths);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public static void TensorReverseScalarIntoDefaultEmpty(int dimension)
        {
            Tensor<int> source = Tensor.Create([7]);
            Tensor.ReverseDimension<int>(source, TensorSpan<int>.Empty, dimension);
            Tensor.ReverseDimension<int>(source, Tensor.CreateFromShape<int>([0]), dimension);
            Tensor.ReverseDimension<int>(source, Tensor.CreateFromShape<int>([2, 0]), dimension);
            Assert.Throws<ArgumentException>(() => Tensor.ReverseDimension<int>(Tensor.Create([7, 8]), TensorSpan<int>.Empty, dimension));
            Assert.Throws<ArgumentException>(() => Tensor.ReverseDimension<int>(source, TensorSpan<int>.Empty, 1));
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(6, 0)]
        [InlineData(6, 3)]
        [InlineData(6, 5)]
        public static void TensorExplicitEmptyStridesIgnoreProductOverflow(int rank, int zeroDimension)
        {
            nint[] lengths = Enumerable.Repeat(nint.MaxValue, rank).ToArray();
            lengths[zeroDimension] = 0;
            nint[] strides = new nint[rank];
            Tensor<int> tensor = Tensor.Create(Array.Empty<int>(), lengths, strides);
            TensorSpan<int> span = new TensorSpan<int>(Array.Empty<int>(), lengths, strides);
            ReadOnlyTensorSpan<int> readOnly = new ReadOnlyTensorSpan<int>(Array.Empty<int>(), lengths, strides);
            Assert.Equal(lengths, tensor.Lengths);
            Assert.Equal(0, tensor.FlattenedLength);
            Assert.Equal(0, span.FlattenedLength);
            Assert.Equal(0, readOnly.FlattenedLength);
            Assert.Equal(0, tensor.Unsqueeze(0).FlattenedLength);
            Assert.Equal(0, span.Unsqueeze(0).FlattenedLength);
            Assert.Equal(0, readOnly.Unsqueeze(0).FlattenedLength);
            lengths[(zeroDimension + 1) % rank] = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Create(Array.Empty<int>(), lengths, strides));
            lengths[(zeroDimension + 1) % rank] = 2;
            strides[zeroDimension] = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Create(Array.Empty<int>(), lengths, strides));
            strides[zeroDimension] = 1;
            Assert.Throws<ArgumentException>(() => Tensor.Create(Array.Empty<int>(), lengths, strides));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(16)]
        [InlineData(33)]
        public static void TensorIndexOfMinMaxDispatch(int maximumChunkLength)
        {
            List<float[]> floatingPointValues =
            [
                [0, float.NegativeZero],
                [float.NegativeZero, 0],
                [2, -2, 1, -1, 2, -2],
                [float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, -float.Epsilon],
                [1, float.NaN, 2, float.NaN],
                [float.NaN, 1]
            ];
            foreach (int position in new int[] { 15, 16, 31, 32, 67, 95 })
            {
                foreach (float value in new float[] { float.NaN, 2, -2, 0, float.NegativeZero })
                {
                    float[] values = new float[96];
                    Array.Fill(values, float.IsNaN(value) ? 1 : -value);
                    values[position] = value;
                    floatingPointValues.Add(values);
                }
            }
            float[] multipleNaNs = new float[96];
            multipleNaNs[32] = float.NaN;
            multipleNaNs[95] = float.NaN;
            floatingPointValues.Add(multipleNaNs);

            foreach ((int rowStride, int columnStride) in new (int, int)[] { (32, 1), (35, 1), (70, 2), (1, 3), (0, 1), (1, 0) })
            {
                foreach (float[] values in floatingPointValues)
                {
                    CheckLayout(values, rowStride, columnStride, maximumChunkLength);
                }
                foreach (int[] values in new int[][] { [1, 1], [2, -2, 1, -1], [int.MinValue, int.MaxValue, 0, 1, -1] })
                {
                    CheckLayout(values, rowStride, columnStride, maximumChunkLength);
                }
            }

            Check(default(ReadOnlyTensorSpan<float>), Array.Empty<float>(), maximumChunkLength);
            Check(new ReadOnlyTensorSpan<float>(Array.Empty<float>(), [2, 0]), Array.Empty<float>(), maximumChunkLength);

            static void CheckLayout<T>(T[] values, int rowStride, int columnStride, int maximumChunkLength)
                where T : INumber<T>
            {
                T[] data = new T[2 * rowStride + 31 * columnStride + 1];
                Array.Fill(data, T.CreateChecked(9999));
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 32; column++)
                    {
                        data[row * rowStride + column * columnStride] = values[(row * 32 + column) % values.Length];
                    }
                }

                T[] expected = new T[96];
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 32; column++)
                    {
                        expected[row * 32 + column] = data[row * rowStride + column * columnStride];
                    }
                }

                ReadOnlyTensorSpan<T> source = new ReadOnlyTensorSpan<T>(data, [3, 32], [rowStride, columnStride]);
                Check(source, expected, maximumChunkLength);
                source = new ReadOnlyTensorSpan<T>(data, [1, 1, 1, 1, 1, 3, 32], [0, 0, 0, 0, 0, rowStride, columnStride]);
                Check(source, expected, maximumChunkLength);
            }

            static void Check<T>(ReadOnlyTensorSpan<T> source, T[] expected, int maximumChunkLength)
                where T : INumber<T>
            {
                Assert.Equal(TensorPrimitives.IndexOfMax<T>(expected), Tensor.IndexOfMax(source));
                Assert.Equal(TensorPrimitives.IndexOfMin<T>(expected), Tensor.IndexOfMin(source));
                Assert.Equal(TensorPrimitives.IndexOfMaxMagnitude<T>(expected), Tensor.IndexOfMaxMagnitude(source));
                Assert.Equal(TensorPrimitives.IndexOfMinMagnitude<T>(expected), Tensor.IndexOfMinMagnitude(source));
                Assert.Equal(TensorPrimitives.IndexOfMax<T>(expected),
                    TensorOperation.IndexOfMinMax<T, TensorPrimitives.IndexOfMaxOperator<T>>(source, maximumChunkLength));
                Assert.Equal(TensorPrimitives.IndexOfMin<T>(expected),
                    TensorOperation.IndexOfMinMax<T, TensorPrimitives.IndexOfMinOperator<T>>(source, maximumChunkLength));
                Assert.Equal(TensorPrimitives.IndexOfMaxMagnitude<T>(expected),
                    TensorOperation.IndexOfMinMax<T, TensorPrimitives.IndexOfMaxMagnitudeOperator<T>>(source, maximumChunkLength));
                Assert.Equal(TensorPrimitives.IndexOfMinMagnitude<T>(expected),
                    TensorOperation.IndexOfMinMax<T, TensorPrimitives.IndexOfMinMagnitudeOperator<T>>(source, maximumChunkLength));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(65)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1025)]
        [InlineData(4097)]
        public static void TensorIndexOfMinMaxBoundedMemory(int length)
        {
            using BoundedMemory<float> storage = BoundedMemory.Allocate<float>(2 * length + 3);
            storage.Span.Fill(9999);
            float[] expected = new float[2 * length];
            foreach ((int rowStride, int columnStride) in new (int, int)[] { (length, 1), (length + 3, 1), (1, 2) })
            {
                int actualRowStride = length == 0 ? 0 : rowStride;
                int actualColumnStride = length <= 1 ? 0 : columnStride;
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < length; column++)
                    {
                        float value = (row * length + column) % 17 - 8;
                        storage.Span[row * actualRowStride + column * actualColumnStride] = value;
                        expected[row * length + column] = value;
                    }
                }

                ReadOnlyTensorSpan<float> source = new ReadOnlyTensorSpan<float>(storage.Span, [2, length], [actualRowStride, actualColumnStride]);
                Assert.Equal(TensorPrimitives.IndexOfMax<float>(expected), Tensor.IndexOfMax(source));
                Assert.Equal(TensorPrimitives.IndexOfMin<float>(expected), Tensor.IndexOfMin(source));
                Assert.Equal(TensorPrimitives.IndexOfMaxMagnitude<float>(expected), Tensor.IndexOfMaxMagnitude(source));
                Assert.Equal(TensorPrimitives.IndexOfMinMagnitude<float>(expected), Tensor.IndexOfMinMagnitude(source));
                Assert.Equal(TensorPrimitives.IndexOfMax<float>(expected),
                    TensorOperation.IndexOfMinMax<float, TensorPrimitives.IndexOfMaxOperator<float>>(source, 17));
                Assert.Equal(TensorPrimitives.IndexOfMin<float>(expected),
                    TensorOperation.IndexOfMinMax<float, TensorPrimitives.IndexOfMinOperator<float>>(source, 17));
                Assert.Equal(TensorPrimitives.IndexOfMaxMagnitude<float>(expected),
                    TensorOperation.IndexOfMinMax<float, TensorPrimitives.IndexOfMaxMagnitudeOperator<float>>(source, 17));
                Assert.Equal(TensorPrimitives.IndexOfMinMagnitude<float>(expected),
                    TensorOperation.IndexOfMinMax<float, TensorPrimitives.IndexOfMinMagnitudeOperator<float>>(source, 17));
            }
        }

        [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorIndexOfMinMaxWithNativeWidthLength(bool maximumLength)
        {
            ulong wideLength = (ulong)uint.MaxValue + 3;
            nint length = maximumLength ? nint.MaxValue : checked((nint)wideLength);
            ReadOnlyTensorSpan<float> source = new ReadOnlyTensorSpan<float>([float.NaN], [length], [0]);
            Assert.Equal(0, Tensor.IndexOfMax(source));
            Assert.Equal(0, Tensor.IndexOfMin(source));
            Assert.Equal(0, Tensor.IndexOfMaxMagnitude(source));
            Assert.Equal(0, Tensor.IndexOfMinMagnitude(source));
            source = new ReadOnlyTensorSpan<float>([float.NegativeZero, 0, float.NaN], [length / 3, 3], [0, 1]);
            Assert.Equal(2, Tensor.IndexOfMax(source));
            Assert.Equal(2, Tensor.IndexOfMin(source));
            Assert.Equal(2, Tensor.IndexOfMaxMagnitude(source));
            Assert.Equal(2, Tensor.IndexOfMinMagnitude(source));
        }

        [Theory]
        [InlineData(new int[] { 2, 0 }, new int[] { 1, 0 })]
        [InlineData(new int[] { 0, 2 }, new int[] { 0, 1 })]
        [InlineData(new int[] { 2, 0, 3 }, new int[] { 3, 0, 1 })]
        public static void TensorEmptyStridedReshape(int[] lengths, int[] strides)
        {
            Tensor<int> source = Tensor.Create(Array.Empty<int>(),
                Array.ConvertAll(lengths, static length => (nint)length),
                Array.ConvertAll(strides, static stride => (nint)stride));
            foreach (nint[] target in new nint[][] { [0], [1, 0, 1], [0, 2], [3, 0, 2] })
            {
                Assert.Equal(target, source.Reshape(target).Lengths);
                Assert.Equal(target, source.AsTensorSpan().Reshape(target).Lengths);
                Assert.Equal(target, source.AsReadOnlyTensorSpan().Reshape(target).Lengths);
                Assert.Equal(target, source.Slice(Enumerable.Repeat(NRange.All, source.Rank).ToArray()).Reshape(target).Lengths);
            }
            Assert.Throws<ArgumentException>(() => source.Reshape([2]));
            Assert.Throws<ArgumentException>(() => source.Reshape([-1, 0]));
            Assert.Throws<ArgumentException>(() => source.AsTensorSpan().Reshape([2]));
            Assert.Throws<ArgumentException>(() => source.AsReadOnlyTensorSpan().Reshape([-1, 0]));
        }

        [Theory]
        [InlineData(0, 0, 0, 3)]
        [InlineData(0, 0, 17, 1)]
        [InlineData(0, 3, 15, 4)]
        [InlineData(3, 0, 15, 4)]
        [InlineData(0, 3, 15, 16)]
        [InlineData(3, 0, 15, 16)]
        [InlineData(0, 10, 7, 3)]
        public static void TensorDenseCopyChunksPreserveOverlap(int sourceOffset, int destinationOffset, int length, int chunkLength)
        {
            int[] actual = Enumerable.Range(0, 20).ToArray();
            int[] expected = (int[])actual.Clone();
            expected.AsSpan(sourceOffset, length).CopyTo(expected.AsSpan(destinationOffset, length));
            TensorOperation.CopyDense(in actual[sourceOffset], ref actual[destinationOffset], length, chunkLength);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TensorMultidimensionalEmptyEndpointConstruction(bool explicitLengths)
        {
            Array source = new int[2, 3];
            TensorSpan<int> span = new TensorSpan<int>(source, [2, 3], explicitLengths ? [0, 0] : [], []);
            ReadOnlyTensorSpan<int> readOnly = new ReadOnlyTensorSpan<int>(source, [2, 3], explicitLengths ? [0, 0] : [], []);
            Assert.Equal([0, 0], span.Lengths);
            Assert.Equal([0, 0], readOnly.Lengths);
            Assert.Equal(0, span.Unsqueeze(0).FlattenedLength);
            Assert.Equal(0, readOnly.Reshape([0]).FlattenedLength);
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<int>(source, [3, 3], [0, 0], []));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyTensorSpan<int>(source, [2, -1], [0, 0], []));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<int>(source, [2, 3], [1], []));
        }

        [Theory]
        [InlineData(new int[] { 2, 0 }, new int[] { 1, 0 })]
        [InlineData(new int[] { 2, 2, 0 }, new int[] { 2, 1, 0 })]
        [InlineData(new int[] { 2, 0, 2 }, new int[] { 2, 0, 1 })]
        public static void TensorEmptyStridedFormatting(int[] lengths, int[] strides)
        {
            nint[] shape = Array.ConvertAll(lengths, static length => (nint)length);
            Tensor<int> dense = Tensor.Create(Array.Empty<int>(), shape);
            Tensor<int> strided = Tensor.Create(Array.Empty<int>(), shape,
                Array.ConvertAll(strides, static stride => (nint)stride));
            Assert.Equal(dense.ToString(shape), strided.ToString(shape));
            Assert.Equal(dense.AsTensorSpan().ToString(shape), strided.AsTensorSpan().ToString(shape));
            Assert.Equal(dense.AsReadOnlyTensorSpan().ToString(shape), strided.AsReadOnlyTensorSpan().ToString(shape));
        }

        private static Tensor<int> CreateModelTensor(int[] lengths, int spacing, int firstValue)
        {
            if (lengths.Length == 0)
            {
                return Tensor<int>.Empty;
            }

            Tensor<int> shape = Tensor.CreateFromShape<int>(Array.ConvertAll(lengths, static length => (nint)length));
            nint[] strides = [.. shape.Strides];
            for (int i = 0; i < strides.Length; i++)
            {
                strides[i] *= spacing;
            }
            int[] values = Enumerable.Repeat(-99, checked((int)shape.FlattenedLength * spacing)).ToArray();
            for (int i = 0; i < shape.FlattenedLength; i++)
            {
                values[i * spacing] = firstValue + i;
            }

            return Tensor.Create(values, shape.Lengths, strides);
        }

        [Fact]
        public static void TensorReshapeHandlesZeroDimensions()
        {
            Tensor<int> tensor = Tensor.Create([1, 2, 3, 4], [4]);
            Assert.Throws<ArgumentException>(() => tensor.Reshape([-1, 0]));
            Assert.Throws<ArgumentException>(() => tensor.Reshape([2, -2]));
            Assert.Throws<ArgumentException>(() => tensor.Reshape([]));

            Tensor<int> empty = Tensor.CreateFromShape<int>([0, 0, 7]);
            Tensor<int> reshaped = empty.Reshape([0, 1, 0, 1]);
            Assert.Equal([0, 1, 0, 1], reshaped.Lengths);
            Assert.Equal(0, reshaped.FlattenedLength);
            Assert.Throws<ArgumentException>(() => empty.Reshape([0, -1]));
            Assert.Equal([0], empty.Reshape([-1]).Lengths);

            Tensor<int> broadcast = Tensor.Create([1, 2], [2, 2], [0, 1]);
            Assert.Equal([1, 2, 1, 2], broadcast.Reshape([2, 1, 2]).ToArray());
            Assert.Equal([1, 2, 1, 2], broadcast.Reshape([1, 2, 2]).Reshape([2, 2]).ToArray());
            Assert.Equal([1, 2, 3, 4], tensor.Reshape([1, 4]).Reshape([4]).ToArray());
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(6, 0)]
        [InlineData(6, 1)]
        [InlineData(6, 2)]
        [InlineData(6, 3)]
        [InlineData(6, 4)]
        [InlineData(6, 5)]
        public static void TensorEmptyReshapeIgnoresProductOverflow(int rank, int zeroDimension)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            int largeDimension = zeroDimension == 0 ? 1 : 0;
            int otherDimension = zeroDimension <= 1 ? 2 : 1;
            lengths[largeDimension] = nint.MaxValue;
            lengths[otherDimension] = 2;
            lengths[zeroDimension] = 0;
            Tensor<int> empty = Tensor.CreateFromShape<int>([0]);
            Tensor<int> expected = Tensor.CreateFromShape<int>(lengths);
            Tensor<int> uninitialized = Tensor.CreateFromShapeUninitialized<int>(lengths);
            Tensor<int> fromArray = Tensor.Create(Array.Empty<int>(), lengths);
            TensorSpan<int> constructedSpan = new TensorSpan<int>(Array.Empty<int>(), lengths);
            ReadOnlyTensorSpan<int> constructedReadOnlySpan = new ReadOnlyTensorSpan<int>(Array.Empty<int>(), lengths);

            Tensor<int> reshaped = empty.Reshape(lengths);
            TensorSpan<int> span = empty.AsTensorSpan().Reshape(lengths);
            ReadOnlyTensorSpan<int> readOnlySpan = empty.AsReadOnlyTensorSpan().Reshape(lengths);
            Assert.Equal(new nint[rank], expected.Strides);
            Assert.Equal(expected.Strides, uninitialized.Strides);
            Assert.Equal(expected.Strides, fromArray.Strides);
            Assert.Equal(expected.Strides, constructedSpan.Strides);
            Assert.Equal(expected.Strides, constructedReadOnlySpan.Strides);
            Assert.Equal(lengths, reshaped.Lengths);
            Assert.Equal(lengths, span.Lengths);
            Assert.Equal(lengths, readOnlySpan.Lengths);
            Assert.Equal(expected.Strides, reshaped.Strides);
            Assert.Equal(expected.Strides, span.Strides);
            Assert.Equal(expected.Strides, readOnlySpan.Strides);
            Assert.Equal(0, reshaped.FlattenedLength);
            Assert.Equal(0, span.FlattenedLength);
            Assert.Equal(0, readOnlySpan.FlattenedLength);

            lengths[largeDimension] = -1;
            Assert.Throws<ArgumentException>(() => empty.Reshape(lengths));
            Assert.Throws<ArgumentException>(() => empty.AsTensorSpan().Reshape(lengths));
            Assert.Throws<ArgumentException>(() => empty.AsReadOnlyTensorSpan().Reshape(lengths));
            lengths[largeDimension] = -2;
            Assert.Throws<ArgumentException>(() => empty.Reshape(lengths));
            Assert.Throws<ArgumentException>(() => empty.AsTensorSpan().Reshape(lengths));
            Assert.Throws<ArgumentException>(() => empty.AsReadOnlyTensorSpan().Reshape(lengths));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.CreateFromShape<int>(lengths));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.CreateFromShapeUninitialized<int>(lengths));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Create(Array.Empty<int>(), lengths));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<int>(Array.Empty<int>(), lengths));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyTensorSpan<int>(Array.Empty<int>(), lengths));
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(4, true)]
        [InlineData(6, false)]
        [InlineData(6, true)]
        public static void TensorReshapeRejectsInvalidShapeAcrossBufferSizes(int rank, bool isBroadcast)
        {
            Tensor<int> tensor = isBroadcast
                ? Tensor.Create([1, 2], [2, 2], [0, 1])
                : Tensor.Create([1, 2, 3, 4]);
            nint[] invalidLengths = new nint[rank];
            Array.Fill(invalidLengths, (nint)1);
            invalidLengths[^1] = isBroadcast ? 4 : -2;

            Assert.Throws<ArgumentException>(() => tensor.Reshape(invalidLengths));
            Assert.Throws<ArgumentException>(() => tensor.AsTensorSpan().Reshape(invalidLengths));
            Assert.Throws<ArgumentException>(() => tensor.AsReadOnlyTensorSpan().Reshape(invalidLengths));
            Assert.Equal(isBroadcast ? [1, 2, 1, 2] : [1, 2, 3, 4], tensor.ToArray());
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(4, true)]
        [InlineData(6, false)]
        [InlineData(6, true)]
        public static void TensorReshapeUsesIndependentShapeStorage(int rank, bool isBroadcast)
        {
            nint[] sourceLengths = new nint[rank];
            Array.Fill(sourceLengths, (nint)1);
            sourceLengths[0] = 2;
            int[] values = isBroadcast ? [42] : [1, 2];
            nint[] sourceStrides = isBroadcast ? new nint[rank] : [];
            Tensor<int> source = Tensor.Create(values, sourceLengths, sourceStrides);

            nint[] requestedLengths = new nint[rank];
            Array.Fill(requestedLengths, (nint)1);
            requestedLengths[^1] = -1;
            nint[] expectedLengths = [.. requestedLengths];
            expectedLengths[^1] = 2;
            int[] expectedValues = isBroadcast ? [42, 42] : [1, 2];

            Tensor<int> reshaped = source.Reshape(requestedLengths);
            TensorSpan<int> span = source.AsTensorSpan().Reshape(requestedLengths);
            ReadOnlyTensorSpan<int> readOnlySpan = source.AsReadOnlyTensorSpan().Reshape(requestedLengths);

            nint[] alternateLengths = [.. expectedLengths];
            alternateLengths[1] = 2;
            alternateLengths[^1] = 1;
            source.Reshape(alternateLengths);

            Assert.Equal(-1, requestedLengths[^1]);
            Assert.Equal(expectedLengths, reshaped.Lengths);
            Assert.Equal(expectedLengths, span.Lengths);
            Assert.Equal(expectedLengths, readOnlySpan.Lengths);
            Assert.Equal(reshaped.Strides, span.Strides);
            Assert.Equal(reshaped.Strides, readOnlySpan.Strides);
            Assert.Equal(expectedValues, reshaped.ToArray());
            int[] flattened = new int[2];
            span.FlattenTo(flattened);
            Assert.Equal(expectedValues, flattened);
            readOnlySpan.FlattenTo(flattened);
            Assert.Equal(expectedValues, flattened);
        }

        [Fact]
        public static void TensorSqueezeTest()
        {
            nint[] dims = [1, 2];
            var tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.SqueezeDimension(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.SqueezeDimension(tensor, 2);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.CreateFromShape<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            Assert.Throws<ArgumentException>(() => tensor = Tensor.SqueezeDimension(tensor, 1));
            Assert.Throws<ArgumentException>(() => tensor = Tensor.SqueezeDimension(tensor, 3));
        }

        [Fact]
        public static void TensorUnsqueezeTest()
        {
            var tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], [0], false);
            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);

            tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Strides[0]);

            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(1, tensor.Strides[1]);

            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Lengths[2]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);
            Assert.Equal(1, tensor.Strides[2]);

            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(4, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);
            Assert.Equal(2, tensor.Lengths[3]);
            Assert.Equal(0, tensor.Strides[0]);
            Assert.Equal(0, tensor.Strides[1]);
            Assert.Equal(0, tensor.Strides[2]);
            Assert.Equal(1, tensor.Strides[3]);

            tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            tensor = Tensor.Unsqueeze(tensor, 1);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            tensor = Tensor.CreateFromShape<int>((ReadOnlySpan<nint>)[2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Unsqueeze<int>(tensor, -1));
            Assert.Throws<ArgumentException>(() => Tensor.Unsqueeze<int>(tensor, 2));

            Tensor<int> t0 = Tensor.Create(Enumerable.Range(0, 2).ToArray());
            t0 = Tensor.Unsqueeze(t0, 1);
            Assert.Equal(0, t0[0, 0]);
            Assert.Equal(1, t0[1, 0]);
        }

        [Fact]
        public void TensorGreaterThanTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            Tensor<bool> result = Tensor.GreaterThan(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { false, false, true }, result.ToArray());

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThan(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { true, false, true, true }, result.ToArray());

            result = Tensor.GreaterThan(tensor1.AsReadOnlyTensorSpan(), 2);

            Assert.Equal(new bool[] { false, false, true, true }, result.ToArray());

            result = Tensor.GreaterThan(2, tensor1.AsReadOnlyTensorSpan());

            Assert.Equal(new bool[] { true, false, false, false }, result.ToArray());
        }

        [Fact]
        public void TensorGreaterThanOrEqualTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            Tensor<bool> result = Tensor.GreaterThanOrEqual(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { false, true, true }, result.ToArray());

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThanOrEqual(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { true, true, true, true }, result.ToArray());

            result = Tensor.GreaterThanOrEqual(tensor1.AsReadOnlyTensorSpan(), 2);

            Assert.Equal(new bool[] { false, true, true, true }, result.ToArray());

            result = Tensor.GreaterThanOrEqual(2, tensor1.AsReadOnlyTensorSpan());

            Assert.Equal(new bool[] { true, true, false, false }, result.ToArray());
        }

        [Fact]
        public void TensorGreaterThanAllTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.GreaterThanAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThanAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            result = Tensor.GreaterThanAll(tensor1.AsReadOnlyTensorSpan(), 2);
            Assert.False(result);

            result = Tensor.GreaterThanAll(tensor1.AsReadOnlyTensorSpan(), 0);
            Assert.True(result);

            result = Tensor.GreaterThanAll(2, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.GreaterThanAll(5, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorGreaterThanOrEqualAllTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.GreaterThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            result = Tensor.GreaterThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), 2);
            Assert.False(result);

            result = Tensor.GreaterThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), 1);
            Assert.True(result);

            result = Tensor.GreaterThanOrEqualAll(2, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.GreaterThanOrEqualAll(4, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorGreaterThanAnyTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.GreaterThanAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThanAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            result = Tensor.GreaterThanAny(tensor1.AsReadOnlyTensorSpan(), 2);
            Assert.True(result);

            result = Tensor.GreaterThanAny(tensor1.AsReadOnlyTensorSpan(), 5);
            Assert.False(result);

            result = Tensor.GreaterThanAny(2, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);

            result = Tensor.GreaterThanAny(0, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.GreaterThanAny(3, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorGreaterThanOrEqualAnyTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.GreaterThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.GreaterThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            result = Tensor.GreaterThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), 1);
            Assert.True(result);

            result = Tensor.GreaterThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), 5);
            Assert.False(result);

            result = Tensor.GreaterThanOrEqualAny(2, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);

            result = Tensor.GreaterThanOrEqualAny(0, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.GreaterThanOrEqualAny(1, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorLessThanTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            Tensor<bool> result = Tensor.LessThan(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { true, false, false }, result.ToArray());

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThan(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { false, false, false, false }, result.ToArray());

            result = Tensor.LessThan(tensor1.AsReadOnlyTensorSpan(), 2);

            Assert.Equal(new bool[] { true, false, false, false }, result.ToArray());

            result = Tensor.LessThan(2, tensor1.AsReadOnlyTensorSpan());

            Assert.Equal(new bool[] { false, false, true, true }, result.ToArray());
        }

        [Fact]
        public void TensorLessThanOrEqualTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            Tensor<bool> result = Tensor.LessThanOrEqual(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { true, true, false }, result.ToArray());

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThanOrEqual(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { false, true, false, false }, result.ToArray());

            result = Tensor.LessThanOrEqual(tensor1.AsReadOnlyTensorSpan(), 2);

            Assert.Equal(new bool[] { true, true, false, false }, result.ToArray());

            result = Tensor.LessThanOrEqual(2, tensor1.AsReadOnlyTensorSpan());

            Assert.Equal(new bool[] { false, true, true, true }, result.ToArray());
        }

        [Fact]
        public void TensorLessThanAllTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.LessThanAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThanAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            result = Tensor.LessThanAll(tensor1.AsReadOnlyTensorSpan(), 4);
            Assert.False(result);

            result = Tensor.LessThanAll(tensor1.AsReadOnlyTensorSpan(), 5);
            Assert.True(result);

            result = Tensor.LessThanAll(2, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.LessThanAll(0, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorLessThanAnyTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.LessThanAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThanAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            result = Tensor.LessThanAny(tensor1.AsReadOnlyTensorSpan(), 2);
            Assert.True(result);

            result = Tensor.LessThanAny(tensor1.AsReadOnlyTensorSpan(), 5);
            Assert.True(result);

            result = Tensor.LessThanAny(2, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);

            result = Tensor.LessThanAny(5, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.LessThanAny(3, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorLessThanOrEqualAllTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.LessThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            result = Tensor.LessThanOrEqualAll(tensor2, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);

            result = Tensor.LessThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), 4);
            Assert.True(result);

            result = Tensor.LessThanOrEqualAll(tensor1.AsReadOnlyTensorSpan(), 3);
            Assert.False(result);

            result = Tensor.LessThanOrEqualAll(2, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.LessThanOrEqualAll(1, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorLessThanOrEqualAnyTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 2, 2, 2 }, lengths: [3]);

            bool result = Tensor.LessThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, lengths: [2, 2]);
            tensor2 = Tensor.Create<int>(new int[] { 0, 2, 2, 3 }, lengths: [2, 2]);

            result = Tensor.LessThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            result = Tensor.LessThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), 1);
            Assert.True(result);

            result = Tensor.LessThanOrEqualAny(tensor1.AsReadOnlyTensorSpan(), 0);
            Assert.False(result);

            result = Tensor.LessThanOrEqualAny(2, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);

            result = Tensor.LessThanOrEqualAny(5, tensor1.AsReadOnlyTensorSpan());
            Assert.False(result);

            result = Tensor.LessThanOrEqualAny(4, tensor1.AsReadOnlyTensorSpan());
            Assert.True(result);
        }

        [Fact]
        public void TensorEqualsTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);

            Tensor<bool> result = Tensor.Equals(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { true, true, true }, result.ToArray());

            result = Tensor.Equals(tensor1.AsReadOnlyTensorSpan(), 1);

            Assert.Equal(new bool[] { true, false, false}, result.ToArray());

            result = Tensor.Equals(tensor1.AsReadOnlyTensorSpan(), 2);

            Assert.Equal(new bool[] { false, true, false }, result.ToArray());

            result = Tensor.Equals(tensor1.AsReadOnlyTensorSpan(), 3);

            Assert.Equal(new bool[] { false, false, true }, result.ToArray());

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4, 5, 6 }, lengths: [2, 3]);
            tensor2 = Tensor.Create<int>(new int[] { 4, 5, 6 }, lengths: [3]);

            result = Tensor.Equals(tensor1.AsReadOnlyTensorSpan(), tensor2);

            Assert.Equal(new bool[] { false, false, false, true, true, true }, result.ToArray());
        }

        [Fact]
        public void TensorEqualsAllTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<int> tensor2 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);

            bool result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);

            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), 1);
            Assert.False(result);

            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), 2);
            Assert.False(result);

            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), 3);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 1, 1 }, lengths: [3]);
            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), 1);
            Assert.True(result);

            tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3, 4, 5, 6 }, lengths: [2, 3]);
            tensor2 = Tensor.Create<int>(new int[] { 4, 5, 6 }, lengths: [3]);

            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.False(result);

            tensor1 = Tensor.Create<int>(new int[] { 4, 5, 6, 4, 5, 6 }, lengths: [2, 3]);
            result = Tensor.EqualsAll(tensor1.AsReadOnlyTensorSpan(), tensor2);
            Assert.True(result);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public static void TensorComparisonRentedBufferEarlyExit(int rank)
        {
            nint[] lengths = new nint[rank];
            Array.Fill(lengths, (nint)1);
            lengths[^1] = 2;
            Tensor<int> tensor = Tensor.Create([1, 2], lengths);

            Assert.False(Tensor.EqualsAll(tensor.AsReadOnlyTensorSpan(), 1));
            Assert.True(Tensor.GreaterThanAll(tensor.AsReadOnlyTensorSpan(), 0));
        }

        [Fact]
        public void TensorFilteredUpdateTest()
        {
            Tensor<int> tensor1 = Tensor.Create<int>(new int[] { 1, 2, 3 }, lengths: [3]);
            Tensor<bool> filter = Tensor.Create<bool>(new bool[] { true, false, false }, lengths: [3]);
            Tensor<int> replace = Tensor.Create<int>(new int[] { -1, -1, -1 }, lengths: [3]);

            Tensor.FilteredUpdate(tensor1.AsTensorSpan(), filter, 2);
            Assert.Equal(new int[] { 2, 2, 3 }, tensor1.ToArray());

            Tensor.FilteredUpdate(tensor1.AsTensorSpan(), filter, replace);
            Assert.Equal(new int[] { -1, 2, 3 }, tensor1.ToArray());

            filter = Tensor.Create<bool>(new bool[] { true, true, true}, lengths: [3]);
            Tensor.FilteredUpdate(tensor1.AsTensorSpan(), filter, replace);
            Assert.Equal(new int[] { -1, -1, -1 }, tensor1.ToArray());
        }

        [Fact]
        public void TensorObjectFillTests()
        {
            ITensor tensor = Tensor.Create<int>(new int[4], new nint[] { 2, 2 });
            tensor.Fill(5);

            Assert.Equal(5, tensor[0, 0]);
            Assert.Equal(5, tensor[0, 1]);
            Assert.Equal(5, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);

            Assert.Throws<ArgumentException>(() => tensor.Fill("invalid"));
            Assert.Throws<ArgumentException>(() => tensor.Fill(null));

            tensor.Fill((object)5);
            Assert.Equal(5, tensor[0, 0]);
            Assert.Equal(5, tensor[0, 1]);
            Assert.Equal(5, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);
        }

        [Fact]
        public void TensorObjectIndexerTests()
        {
            ITensor tensor = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            Assert.Equal(1, tensor[new nint[] { 0, 0 }]);
            Assert.Equal(2, tensor[new nint[] { 0, 1 }]);
            Assert.Equal(3, tensor[new nint[] { 1, 0 }]);
            Assert.Equal(4, tensor[new nint[] { 1, 1 }]);

            tensor[new nint[] { 0, 0 }] = 10;
            tensor[new nint[] { 0, 1 }] = 20;
            tensor[new nint[] { 1, 0 }] = 30;
            tensor[new nint[] { 1, 1 }] = 40;

            Assert.Equal(10, tensor[new nint[] { 0, 0 }]);
            Assert.Equal(20, tensor[new nint[] { 0, 1 }]);
            Assert.Equal(30, tensor[new nint[] { 1, 0 }]);
            Assert.Equal(40, tensor[new nint[] { 1, 1 }]);

            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { 2, 0 }]);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { 0, 2 }]);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { -1, 0 }]);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { -1, -1 }]);

            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { 2, 0 }] = 10);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { 0, 2 }] = 20);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { -1, 0 }] = 20);
            Assert.Throws<IndexOutOfRangeException>(() => tensor[new nint[] { -1, -1 }] = 20);
        }

        [Fact]
        public void TensorGetPinnedHandleTests()
        {
            Tensor<int> tensor = Tensor.Create<int>(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            using MemoryHandle handle = tensor.GetPinnedHandle();
            unsafe
            {
                int* ptr = (int*)handle.Pointer;
                Assert.Equal(1, ptr[0]);
                Assert.Equal(2, ptr[1]);
                Assert.Equal(3, ptr[2]);
                Assert.Equal(4, ptr[3]);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public unsafe void TensorSlicePinningStartsAtSlice(bool useRanges)
        {
            Tensor<int> parent = Tensor.Create([91, 92, 11, 12, 21, 22], [3, 2]);
            Tensor<int> slice = useRanges
                ? parent.Slice(1..3, ..)
                : parent.Slice((ReadOnlySpan<nint>)[1, 0]);

            Assert.Equal(11, slice.GetPinnableReference());
            Assert.Equal(11, slice.AsTensorSpan().GetPinnableReference());

            using MemoryHandle handle = slice.GetPinnedHandle();
            Assert.Equal([11, 12, 21, 22], new ReadOnlySpan<int>(handle.Pointer, 4).ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TensorFlattenToLeavesExtraDestinationUntouched(bool strided)
        {
            Tensor<int> source = strided
                ? Tensor.Create([1, 2, 3, 4, 5], [2, 2], [3, 1])
                : Tensor.Create([1, 2, 4, 5], [2, 2]);
            int[] destination = [9, 9, 9, 9, 9, 9];

            Assert.True(source.TryFlattenTo(destination));
            Assert.Equal([1, 2, 4, 5, 9, 9], destination);

            Array.Fill(destination, 9);
            source.AsReadOnlyTensorSpan().FlattenTo(destination);
            Assert.Equal([1, 2, 4, 5, 9, 9], destination);
        }

        [Fact]
        public void IsDenseTests()
        {
            // Dense

            Assert.True(Tensor.CreateFromShape<int>([(nint)1]).IsDense);
            Assert.True(Tensor.CreateFromShape<int>([(nint)1], [0]).IsDense);

            Assert.True(Tensor.CreateFromShape<int>([(nint)2]).IsDense);
            Assert.True(Tensor.CreateFromShape<int>([(nint)2], [1]).IsDense);

            Assert.True(Tensor.CreateFromShape<int>([(nint)1, 2]).IsDense);
            Assert.True(Tensor.CreateFromShape<int>([(nint)1, 2], [0, 1]).IsDense);

            Assert.True(Tensor.CreateFromShape<int>([(nint)2, 2]).IsDense);
            Assert.True(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1]).IsDense);

            Assert.True(Tensor.CreateFromShape<int>([(nint)4, 3]).IsDense);
            Assert.True(Tensor.CreateFromShape<int>([(nint)4, 3], [3, 1]).IsDense);

            // Non-dense

            Assert.False(Tensor.CreateFromShape<int>([(nint)2], [0]).IsDense);
            Assert.False(Tensor.CreateFromShape<int>([(nint)2], [2]).IsDense);

            Assert.False(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1])).IsDense);
            Assert.False(Tensor.CreateFromShape<int>([(nint)2, 2], [1, 0]).IsDense);

            Assert.False(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)3, 4], [8, 1])).IsDense);
            Assert.False(Tensor.CreateFromShape<int>([(nint)3, 4], [1, 0]).IsDense);
        }

        [Fact]
        public void HasAnyDenseDimensionTests()
        {
            // Dense

            Assert.True(Tensor.CreateFromShape<int>([(nint)1]).HasAnyDenseDimensions);
            Assert.True(Tensor.CreateFromShape<int>([(nint)1], [0]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)2]).HasAnyDenseDimensions);
            Assert.True(Tensor.CreateFromShape<int>([(nint)2], [1]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)1, 2]).HasAnyDenseDimensions);
            Assert.True(Tensor.CreateFromShape<int>([(nint)1, 2], [0, 1]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)2, 2]).HasAnyDenseDimensions);
            Assert.True(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)4, 3]).HasAnyDenseDimensions);
            Assert.True(Tensor.CreateFromShape<int>([(nint)4, 3], [3, 1]).HasAnyDenseDimensions);

            // Non-dense w/ Dense Dimension

            Assert.True(Tensor.CreateFromShape<int>([(nint)1], [0]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)2, 1], [1, 0]).HasAnyDenseDimensions);

            Assert.True(Tensor.CreateFromShape<int>([(nint)3, 1], [1, 0]).HasAnyDenseDimensions);

            // Non-dense w/ Non-dense Dimension

            Assert.False(Tensor.CreateFromShape<int>([(nint)2], [0]).HasAnyDenseDimensions);

            Assert.False(Tensor.CreateFromShape<int>([(nint)2, 2], [1, 0]).HasAnyDenseDimensions);

            Assert.False(Tensor.CreateFromShape<int>([(nint)3, 4], [1, 0]).HasAnyDenseDimensions);

            Assert.False(Tensor.CreateFromShape<int>([(nint)2], [2]).HasAnyDenseDimensions);

            Assert.False(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1])).HasAnyDenseDimensions);

            Assert.False(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)3, 4], [8, 1])).HasAnyDenseDimensions);
        }

        [Fact]
        public void ToDenseTensorTests()
        {
            // Dense

            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)1]));
            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)1], [0]));

            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)2]));
            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)2], [1]));

            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)1, 2]));
            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)1, 2], [0, 1]));

            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)2, 2]));
            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1]));

            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)4, 3]));
            AssertReturnsSelf(Tensor.CreateFromShape<int>([(nint)4, 3], [3, 1]));

            // Non-dense

            AssertReturnsNewTensor(Tensor.CreateFromShape<int>([(nint)2], [0]));
            AssertReturnsNewTensor(Tensor.CreateFromShape<int>([(nint)2], [2]));

            AssertReturnsNewTensor(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)2, 2], [2, 1])));
            AssertReturnsNewTensor(Tensor.CreateFromShape<int>([(nint)2, 2], [1, 0]));

            AssertReturnsNewTensor(Tensor.Transpose(Tensor.CreateFromShape<int>([(nint)3, 4], [8, 1])));
            AssertReturnsNewTensor(Tensor.CreateFromShape<int>([(nint)3, 4], [1, 0]));

            static void AssertReturnsSelf<T>(Tensor<T> tensor)
            {
                Assert.Same(tensor, tensor.ToDenseTensor());
            }

            static void AssertReturnsNewTensor<T>(Tensor<T> tensor)
                where T : IEqualityOperators<T, T, bool>
            {
                Tensor<T> denseTensor = tensor.ToDenseTensor();
                Assert.NotSame(tensor, denseTensor);

                Assert.Equal(tensor.FlattenedLength, denseTensor.FlattenedLength);
                Assert.True(Tensor.EqualsAll<T>(tensor, denseTensor));
            }
        }

        [Fact]
        public static void GetSpanTest()
        {
            Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            Span<int> span = tensorSpan.GetSpan([0, 0], 16);
            Assert.Equal(16, span.Length);
            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], span);

            span = tensorSpan.GetSpan([1, 1], 3);
            Assert.Equal(3, span.Length);
            Assert.Equal([5, 6, 7], span);

            span = tensorSpan.GetSpan([3, 0], 4);
            Assert.Equal(4, span.Length);
            Assert.Equal([12, 13, 14, 15], span);

            span = tensorSpan.GetSpan([0, 3], 1);
            Assert.Equal(1, span.Length);
            Assert.Equal([3], span);

            span = tensorSpan.GetSpan([3, 3], 1);
            Assert.Equal(1, span.Length);
            Assert.Equal([15], span);
        }

        [Fact]
        public static void GetSpanThrowsForInvalidIndexesTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([4, 0], 17);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([0, 4], 17);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([4, 4], 17);
            });
        }

        [Fact]
        public static void GetSpanThrowsForInvalidLengthsTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([0, 0], -1);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([0, 0], 17);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([1, 1], 4);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([3, 0], 5);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([0, 3], 2);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.GetSpan([3, 3], 2);
            });
        }

        [Fact]
        public static void TryGetSpanTest()
        {
            Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            Assert.True(tensorSpan.TryGetSpan([0, 0], 16, out Span<int> span));
            Assert.Equal(16, span.Length);
            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], span);

            Assert.True(tensorSpan.TryGetSpan([1, 1], 3, out span));
            Assert.Equal(3, span.Length);
            Assert.Equal([5, 6, 7], span);

            Assert.True(tensorSpan.TryGetSpan([3, 0], 4, out span));
            Assert.Equal(4, span.Length);
            Assert.Equal([12, 13, 14, 15], span);

            Assert.True(tensorSpan.TryGetSpan([0, 3], 1, out span));
            Assert.Equal(1, span.Length);
            Assert.Equal([3], span);

            Assert.True(tensorSpan.TryGetSpan([3, 3], 1, out span));
            Assert.Equal(1, span.Length);
            Assert.Equal([15], span);
        }

        [Fact]
        public static void TryGetSpanThrowsForInvalidIndexesTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.TryGetSpan([4, 0], 17, out Span<int> _);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.TryGetSpan([0, 4], 17, out Span<int> _);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = tensorSpan.TryGetSpan([4, 4], 17, out Span<int> _);
            });
        }

        [Fact]
        public static void TryGetSpanFailsForInvalidLengthsTest()
        {
            Tensor<int> tensorSpan = Tensor.Create(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            Assert.False(tensorSpan.TryGetSpan([0, 0], -1, out Span<int> span));
            Assert.Equal(0, span.Length);

            Assert.False(tensorSpan.TryGetSpan([0, 0], 17, out span));
            Assert.Equal(0, span.Length);

            Assert.False(tensorSpan.TryGetSpan([1, 1], 4, out span));
            Assert.Equal(0, span.Length);

            Assert.False(tensorSpan.TryGetSpan([3, 0], 5, out span));
            Assert.Equal(0, span.Length);

            Assert.False(tensorSpan.TryGetSpan([0, 3], 2, out span));
            Assert.Equal(0, span.Length);

            Assert.False(tensorSpan.TryGetSpan([3, 3], 2, out span));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void ToStringTest()
        {
            Tensor<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = "System.Numerics.Tensors.Tensor<Int32>[5]";
            Assert.Equal(expected, tensor.ToString());

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = "System.Numerics.Tensors.Tensor<Int32>[2, 2]";
            Assert.Equal(expected, tensor.ToString());

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = "System.Numerics.Tensors.Tensor<Int32>[3, 3, 3]";
            Assert.Equal(expected, tensor.ToString());
        }

        [Fact]
        public static void ToStringAllDataTest()
        {
            Tensor<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.Tensor<Int32>[5] {
                  [1, 2, 3, 4, 5]
                }
                """;
            Assert.Equal(expected, tensor.ToString([5]));

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[2, 2] {
                  [1, 2],
                  [3, 4]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 2]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[3, 3, 3] {
                  [
                    [1, 2, 3],
                    [4, 5, 6],
                    [7, 8, 9]
                  ],
                  [
                    [10, 11, 12],
                    [13, 14, 15],
                    [16, 17, 18]
                  ],
                  [
                    [19, 20, 21],
                    [22, 23, 24],
                    [25, 26, 27]
                  ]
                }
                """;
            Assert.Equal(expected, tensor.ToString([3, 3, 3]));
        }

        [Fact]
        public static void ToStringPartialDataTest()
        {
            Tensor<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.Tensor<Int32>[5] {
                  [1, 2, 3, ..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([3]));
            
            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[2, 2] {
                  [1, ..],
                  [3, ..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 1]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[3, 3, 3] {
                  [
                    [1, 2, ..],
                    [4, 5, ..],
                    ..
                  ],
                  [
                    [10, 11, ..],
                    [13, 14, ..],
                    ..
                  ],
                  ..
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 2, 2]));
        }

        [Fact]
        public static void ToStringZeroDataTest()
        {
            Tensor<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.Tensor<Int32>[5] {
                  [..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([0]));
            
            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[2, 2] {
                  [..],
                  [..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 0]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.Tensor<Int32>[3, 3, 3] {
                  [
                    ..
                  ],
                  [
                    ..
                  ],
                  ..
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 0, 2]));
        }
    }

    [Collection(nameof(DisableParallelization))]
    public class TensorLargeMemoryTests
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 2)]
        [InlineData(false, 3)]
        [InlineData(true, 3)]
        [OuterLoop]
        public static void TensorLargeDenseQueries(bool singletonDimensions, int query) =>
            TensorTests.TensorLargeDenseQueries(singletonDimensions, query);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(-17)]
        [InlineData(17)]
        [OuterLoop]
        public static void TensorCopyToLargeDenseOverlap(int shift) =>
            TensorTests.TensorCopyToLargeDenseOverlap(shift);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorLargeDenseElementwiseOperations(bool singletonDimensions) =>
            TensorTests.TensorLargeDenseElementwiseOperations(singletonDimensions);

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop]
        public static void TensorLargeDenseConversionPreservesElementOffsets() =>
            TensorTests.TensorLargeDenseConversionPreservesElementOffsets();

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorLargeDenseReductionAndReversal(bool singletonDimensions) =>
            TensorTests.TensorLargeDenseReductionAndReversal(singletonDimensions);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorLargeDenseReverseIndependentRuns(bool sourcePadded) =>
            TensorTests.TensorLargeDenseReverseIndependentRuns(sourcePadded);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [OuterLoop]
        public static void TensorLargeDenseInPlaceReverseChunks(int layout) =>
            TensorTests.TensorLargeDenseInPlaceReverseChunks(layout);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorLargeDenseRandomFillPreservesDrawOrder(bool gaussian) =>
            TensorTests.TensorLargeDenseRandomFillPreservesDrawOrder(gaussian);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [OuterLoop]
        public static void TensorResizeToDenseDestinationLongerThanSpan(int sourceLayout) =>
            TensorTests.TensorResizeToDenseDestinationLongerThanSpan(sourceLayout);

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop]
        public static void TensorResizeToLargeDenseSourceSmallDestination() =>
            TensorTests.TensorResizeToLargeDenseSourceSmallDestination();

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(-17, -32)]
        [InlineData(-17, 0)]
        [InlineData(-17, 32)]
        [InlineData(0, 0)]
        [InlineData(17, -32)]
        [InlineData(17, 0)]
        [InlineData(17, 32)]
        [OuterLoop]
        public static void TensorResizeToLargeDenseOverlap(int shift, int lengthDifference) =>
            TensorTests.TensorResizeToLargeDenseOverlap(shift, lengthDifference);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorClearAndFillDenseLongerThanSpan(bool singletonDimensions) =>
            TensorTests.TensorClearAndFillDenseLongerThanSpan(singletonDimensions);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(-1)]
        [InlineData(0)]
        [OuterLoop]
        public static void TensorConcatenateDenseDestinationLongerThanSpan(int dimension) =>
            TensorTests.TensorConcatenateDenseDestinationLongerThanSpan(dimension);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public static void TensorLargeDenseJoinInnerBlocks(bool stack) =>
            TensorTests.TensorLargeDenseJoinInnerBlocks(stack);
    }

    /// <summary>
    /// Test complex number type that implements IRootFunctions for testing consistency between
    /// TensorPrimitives.StdDev and Tensor.StdDev
    /// </summary>
    public readonly struct TestComplex(Complex value) : IRootFunctions<TestComplex>, IEquatable<TestComplex>
    {
        private readonly Complex _value = value;
        
        public double Real => _value.Real;
        public double Imaginary => _value.Imaginary;

        public static TestComplex One => new(Complex.One);
        public static int Radix => 2;
        public static TestComplex Zero => new(Complex.Zero);
        public static TestComplex E => new(new(Math.E, 0));
        public static TestComplex Pi => new(new(Math.PI, 0));
        public static TestComplex Tau => new(new(Math.Tau, 0));

        public static TestComplex operator +(TestComplex left, TestComplex right) => new(left._value + right._value);
        public static TestComplex operator *(TestComplex left, TestComplex right) => new(left._value * right._value);
        public static TestComplex operator /(TestComplex left, TestComplex right) => new(left._value / right._value);
        public static TestComplex operator -(TestComplex left, TestComplex right) => new(left._value - right._value);
        public static TestComplex Sqrt(TestComplex x) => new(Complex.Sqrt(x._value));
        public static TestComplex Abs(TestComplex value) => new(new(Complex.Abs(value._value), 0));
        public static TestComplex AdditiveIdentity => new(Complex.Zero);
        public static TestComplex CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther> => new(Complex.CreateChecked(value));
        
        // Override Object methods
        public override bool Equals(object? obj) => obj is TestComplex other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();
        
        // IEquatable<TestComplex>
        public bool Equals(TestComplex other) => _value.Equals(other._value);
        
        // Operators
        public static bool operator ==(TestComplex left, TestComplex right) => left.Equals(right);
        public static bool operator !=(TestComplex left, TestComplex right) => !left.Equals(right);
        
        // Required interface implementations not needed for tests - throw NotImplementedException
        public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
        public static TestComplex Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TestComplex result) => throw new NotImplementedException();
        public static TestComplex Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TestComplex result) => throw new NotImplementedException();
        public static TestComplex operator --(TestComplex value) => throw new NotImplementedException();
        public static TestComplex operator ++(TestComplex value) => throw new NotImplementedException();
        public static TestComplex MultiplicativeIdentity => new(Complex.One);
        public static TestComplex operator -(TestComplex value) => new(-value._value);
        public static TestComplex operator +(TestComplex value) => new(value._value);
        public static bool IsCanonical(TestComplex value) => throw new NotImplementedException();
        public static bool IsComplexNumber(TestComplex value) => throw new NotImplementedException();
        public static bool IsEvenInteger(TestComplex value) => throw new NotImplementedException();
        public static bool IsFinite(TestComplex value) => throw new NotImplementedException();
        public static bool IsImaginaryNumber(TestComplex value) => throw new NotImplementedException();
        public static bool IsInfinity(TestComplex value) => throw new NotImplementedException();
        public static bool IsInteger(TestComplex value) => throw new NotImplementedException();
        public static bool IsNaN(TestComplex value) => throw new NotImplementedException();
        public static bool IsNegative(TestComplex value) => throw new NotImplementedException();
        public static bool IsNegativeInfinity(TestComplex value) => throw new NotImplementedException();
        public static bool IsNormal(TestComplex value) => throw new NotImplementedException();
        public static bool IsOddInteger(TestComplex value) => throw new NotImplementedException();
        public static bool IsPositive(TestComplex value) => throw new NotImplementedException();
        public static bool IsPositiveInfinity(TestComplex value) => throw new NotImplementedException();
        public static bool IsRealNumber(TestComplex value) => throw new NotImplementedException();
        public static bool IsSubnormal(TestComplex value) => throw new NotImplementedException();
        public static bool IsZero(TestComplex value) => throw new NotImplementedException();
        public static TestComplex MaxMagnitude(TestComplex x, TestComplex y) => throw new NotImplementedException();
        public static TestComplex MaxMagnitudeNumber(TestComplex x, TestComplex y) => throw new NotImplementedException();
        public static TestComplex MinMagnitude(TestComplex x, TestComplex y) => throw new NotImplementedException();
        public static TestComplex MinMagnitudeNumber(TestComplex x, TestComplex y) => throw new NotImplementedException();
        public static TestComplex Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        public static TestComplex Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        public static bool TryConvertFromSaturating<TOther>(TOther value, out TestComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryConvertFromTruncating<TOther>(TOther value, out TestComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryConvertFromChecked<TOther>(TOther value, out TestComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryConvertToChecked<TOther>(TestComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryConvertToSaturating<TOther>(TestComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryConvertToTruncating<TOther>(TestComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TestComplex result) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out TestComplex result) => throw new NotImplementedException();
        public static TestComplex Cbrt(TestComplex x) => throw new NotImplementedException();
        public static TestComplex Hypot(TestComplex x, TestComplex y) => throw new NotImplementedException();
        public static TestComplex RootN(TestComplex x, int n) => throw new NotImplementedException();
    }
}
