// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Numerics.Tensors.Tests
{
    public static class TensorPrimitivesTests
    {
        public static IEnumerable<object[]> TensorLengths =>
            from length in new[] { 1, 2, 3, 4, 5, 7, 8, 9, 11, 12, 13, 15, 16, 17, 31, 32, 33, 100 }
            select new object[] { length };

        private static readonly Random s_random = new Random(20230828);

        private static float[] CreateTensor(int size)
        {
            return new float[size];
        }

        private static float[] CreateAndFillTensor(int size)
        {
            float[] tensor = CreateTensor(size);
            FillTensor(tensor);
            return tensor;
        }

        private static void FillTensor(float[] tensor)
        {
            for (int i = 0; i < tensor.Length; i++)
            {
                tensor[i] = NextSingle();
            }
        }

        private static float NextSingle()
        {
#if NETCOREAPP
            return s_random.NextSingle();
#else
            return (float)s_random.NextDouble();
#endif
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] - y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTensorAndScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] - y), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] / y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTensorAndScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] / y), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void NegateTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Negate(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(-x[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void NegateTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Negate(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] multiplier = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] multiplier = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] multiplier = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] multiplier = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float multiplier = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] multiplier = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y) * multiplier[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForMismatchedLengths_x_z(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] multiplier = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] multiplier = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] addend = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);
            float[] addend = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] addend = CreateAndFillTensor(tensorLength - 1);
            float[] destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float[] addend = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithScalar(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalarAndAddWithTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] addend = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y) + addend[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalarAndAddWithTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            float[] addend = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ExpTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Exp(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Exp(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ExpTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Exp(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void LogTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Log(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Log(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void LogTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CoshTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Cosh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Cosh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CoshTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Cosh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SinhTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Sinh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Sinh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SinhTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sinh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void TanhTensor(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength);

            TensorPrimitives.Tanh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Tanh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void TanhTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Tanh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CosineSimilarity_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(x, y));
        }

        [Fact]
        public static void CosineSimilarity_ThrowsForEmpty_x_y()
        {
            float[] x = [];
            float[] y = [];

            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(x, y));
        }

        [Theory]
        [InlineData(new float[] { 3, 2, 0, 5 }, new float[] { 1, 0, 0, 0 }, 0.49f)]
        [InlineData(new float[] { 1, 1, 1, 1, 1, 0 }, new float[] { 1, 1, 1, 1, 0, 1 }, 0.80f)]
        public static void CosineSimilarity(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.CosineSimilarity(x, y), .01f);
        }

        [Fact]
        public static void Distance_ThrowsForEmpty_x_y()
        {
            float[] x = [];
            float[] y = [];

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(x, y));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Distance_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(x, y));
        }

        [Theory]
        [InlineData(new float[] { 3, 2 }, new float[] { 4, 1 }, 1.4142f)]
        [InlineData(new float[] { 0, 4 }, new float[] { 6, 2 }, 6.3245f)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 5.1961f)]
        [InlineData(new float[] { 5, 1, 6, 10 }, new float[] { 7, 2, 8, 4 }, 6.7082f)]
        public static void Distance(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.Distance(x, y), .001f);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Dot_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Dot(x, y));
        }

        [Theory]
        [InlineData(new float[] { 1, 3, -5 }, new float[] { 4, -2, -1 }, 3)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 32)]
        [InlineData(new float[] { 1, 2, 3, 10, 8 }, new float[] { 4, 5, 6, -2, 7 }, 68)]
        [InlineData(new float[] { }, new float[] { }, 0)]
        public static void Dot(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.Dot(x, y), .001f);
        }

        [Theory]
        [InlineData(new float[] { 1, 2, 3 }, 3.7416)]
        [InlineData(new float[] { 3, 4 }, 5)]
        [InlineData(new float[] { 3 }, 3)]
        [InlineData(new float[] { 3, 4, 1, 2 }, 5.477)]
        [InlineData(new float[] { }, 0f)]
        public static void L2Normalize(float[] x, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.L2Normalize(x), .001f);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SoftMax_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.SoftMax(x, destination));
        }

        [Theory]
        [InlineData(new float[] { 3, 1, .2f }, new float[] { 0.8360188f, 0.11314284f, 0.05083836f })]
        [InlineData(new float[] { 3, 4, 1 }, new float[] { 0.2594f, 0.7052f, 0.0351f })]
        [InlineData(new float[] { 5, 3 }, new float[] { 0.8807f, 0.1192f })]
        [InlineData(new float[] { 4, 2, 1, 9 }, new float[] { 0.0066f, 9.04658e-4f, 3.32805e-4f, 0.9920f})]
        public static void SoftMax(float[] x, float[] expectedResult)
        {
            var dest = new float[x.Length];
            TensorPrimitives.SoftMax(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], .001f);
            }
        }

        [Fact]
        public static void SoftMax_DestinationLongerThanSource()
        {
            var x = new float[] { 3, 1, .2f };
            var expectedResult = new float[] { 0.8360188f, 0.11314284f, 0.05083836f };
            var dest = new float[x.Length + 1];
            TensorPrimitives.SoftMax(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], .001f);
            }
        }

        [Fact]
        public static void SoftMax_ThrowsForEmpty_x_y()
        {
            var x = new float[] { };
            var dest = new float[x.Length];

            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.SoftMax(x, dest));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid_ThrowsForTooShortDestination(int tensorLength)
        {
            float[] x = CreateAndFillTensor(tensorLength);
            float[] destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sigmoid(x, destination));
        }

        [Theory]
        [InlineData(new float[] { -5, -4.5f, -4 }, new float[] { 0.0066f, 0.0109f, 0.0179f })]
        [InlineData(new float[] { 4.5f, 5 }, new float[] { 0.9890f, 0.9933f })]
        [InlineData(new float[] { 0, -3, 3, .5f }, new float[] { 0.5f, 0.0474f, 0.9525f, 0.6224f })]
        public static void Sigmoid(float[] x, float[] expectedResult)
        {
            var dest = new float[x.Length];
            TensorPrimitives.Sigmoid(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], .001f);
            }
        }

        [Fact]
        public static void Sigmoid_DestinationLongerThanSource()
        {
            var x = new float[] { -5, -4.5f, -4 };
            var expectedResult = new float[] { 0.0066f, 0.0109f, 0.0179f };
            var dest = new float[x.Length + 1];
            TensorPrimitives.Sigmoid(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], .001f);
            }
        }

        [Fact]
        public static void Sigmoid_ThrowsForEmpty_x_y()
        {
            var x = new float[] { };
            var dest = new float[x.Length];

            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.Sigmoid(x, dest));
        }
    }
}
