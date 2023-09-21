// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Numerics.Tensors.Tests
{
    public static partial class TensorPrimitivesTests
    {
        private const double Tolerance = 0.0001;

        public static IEnumerable<object[]> TensorLengths =>
            from length in Enumerable.Range(1, 128)
            select new object[] { length };

        private static readonly Random s_random = new Random(20230828);

        private static BoundedMemory<float> CreateTensor(int size) => BoundedMemory.Allocate<float>(size);

        private static BoundedMemory<float> CreateAndFillTensor(int size)
        {
            BoundedMemory<float> tensor = CreateTensor(size);
            FillTensor(tensor.Span);
            return tensor;
        }

        private static void FillTensor(Span<float> tensor)
        {
            for (int i = 0; i < tensor.Length; i++)
            {
                tensor[i] = NextSingle();
            }
        }

        private static float NextSingle()
        {
            // For testing purposes, get a mix of negative and positive values.
            return (float)((s_random.NextDouble() * 2) - 1);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] + y[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] + y, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] - y[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTensorAndScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] - y, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SubtractTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] * y[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] * y, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] / y[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTensorAndScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(x[i] / y, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void DivideTensorAndScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void NegateTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Negate(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(-x[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void NegateTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Negate(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] + y) * multiplier[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForMismatchedLengths_x_z(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend, destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTwoTensorsAndAddWithScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalarAndAddWithTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal((x[i] * y) + addend[i], destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyTensorAndScalarAndAddWithTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ExpTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Exp(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Exp(x[i]), destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ExpTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Exp(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void LogTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Log(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Log(x[i]), destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void LogTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CoshTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Cosh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Cosh(x[i]), destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CoshTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Cosh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SinhTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Sinh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Sinh(x[i]), destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SinhTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sinh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void TanhTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Tanh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                Assert.Equal(MathF.Tanh(x[i]), destination[i], Tolerance);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void TanhTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Tanh(x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CosineSimilarity_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(x, y));
        }

        [Fact]
        public static void CosineSimilarity_ThrowsForEmpty_x_y()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(ReadOnlySpan<float>.Empty, ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(ReadOnlySpan<float>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(CreateTensor(1), ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [InlineData(new float[] { 3, 2, 0, 5 }, new float[] { 1, 0, 0, 0 }, 0.48666f)]
        [InlineData(new float[] { 1, 1, 1, 1, 1, 0 }, new float[] { 1, 1, 1, 1, 0, 1 }, 0.80f)]
        public static void CosineSimilarity_KnownValues(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.CosineSimilarity(x, y), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CosineSimilarity(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float dot = 0f, squareX = 0f, squareY = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dot += x[i] * y[i];
                squareX += x[i] * x[i];
                squareY += y[i] * y[i];
            }

            Assert.Equal(dot / (Math.Sqrt(squareX) * Math.Sqrt(squareY)), TensorPrimitives.CosineSimilarity(x, y), Tolerance);
        }

        [Fact]
        public static void Distance_ThrowsForEmpty_x_y()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(ReadOnlySpan<float>.Empty, ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(ReadOnlySpan<float>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(CreateTensor(1), ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Distance_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(x, y));
        }

        [Theory]
        [InlineData(new float[] { 3, 2 }, new float[] { 4, 1 }, 1.4142f)]
        [InlineData(new float[] { 0, 4 }, new float[] { 6, 2 }, 6.3245f)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 5.1961f)]
        [InlineData(new float[] { 5, 1, 6, 10 }, new float[] { 7, 2, 8, 4 }, 6.7082f)]
        public static void Distance_KnownValues(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.Distance(x, y), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Distance(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float distance = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                distance += (x[i] - y[i]) * (x[i] - y[i]);
            }

            Assert.Equal(Math.Sqrt(distance), TensorPrimitives.Distance(x, y), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Dot_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Dot(x, y));
        }

        [Theory]
        [InlineData(new float[] { 1, 3, -5 }, new float[] { 4, -2, -1 }, 3)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 32)]
        [InlineData(new float[] { 1, 2, 3, 10, 8 }, new float[] { 4, 5, 6, -2, 7 }, 68)]
        [InlineData(new float[] { }, new float[] { }, 0)]
        public static void Dot_KnownValues(float[] x, float[] y, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.Dot(x, y), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Dot(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float dot = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dot += x[i] * y[i];
            }

            Assert.Equal(dot, TensorPrimitives.Dot(x, y), Tolerance);
        }

        [Theory]
        [InlineData(new float[] { 1, 2, 3 }, 3.7416575f)]
        [InlineData(new float[] { 3, 4 }, 5)]
        [InlineData(new float[] { 3 }, 3)]
        [InlineData(new float[] { 3, 4, 1, 2 }, 5.477226)]
        [InlineData(new float[] { }, 0f)]
        public static void L2Normalize_KnownValues(float[] x, float expectedResult)
        {
            Assert.Equal(expectedResult, TensorPrimitives.L2Normalize(x), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void L2Normalize(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            float sumOfSquares = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                sumOfSquares += x[i] * x[i];
            }

            Assert.Equal(Math.Sqrt(sumOfSquares), TensorPrimitives.L2Normalize(x), Tolerance);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SoftMax_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.SoftMax(x, destination));
        }

        [Theory]
        [InlineData(new float[] { 3, 1, .2f }, new float[] { 0.8360188f, 0.11314284f, 0.05083836f })]
        [InlineData(new float[] { 3, 4, 1 }, new float[] { 0.2594f, 0.705384f, 0.0351f })]
        [InlineData(new float[] { 5, 3 }, new float[] { 0.8807f, 0.1192f })]
        [InlineData(new float[] { 4, 2, 1, 9 }, new float[] { 0.0066f, 9.04658e-4f, 3.32805e-4f, 0.9920f})]
        public static void SoftMax(float[] x, float[] expectedResult)
        {
            using BoundedMemory<float> dest = CreateTensor(x.Length);
            TensorPrimitives.SoftMax(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], Tolerance);
            }
        }

        [Fact]
        public static void SoftMax_DestinationLongerThanSource()
        {
            float[] x = [3, 1, .2f];
            float[] expectedResult = [0.8360188f, 0.11314284f, 0.05083836f];
            using BoundedMemory<float> dest = CreateTensor(x.Length + 1);
            TensorPrimitives.SoftMax(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], Tolerance);
            }
        }

        [Fact]
        public static void SoftMax_ThrowsForEmptyInput()
        {
            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.SoftMax(ReadOnlySpan<float>.Empty, CreateTensor(1)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sigmoid(x, destination));
        }

        [Theory]
        [InlineData(new float[] { -5, -4.5f, -4 }, new float[] { 0.0066f, 0.0109f, 0.0179f })]
        [InlineData(new float[] { 4.5f, 5 }, new float[] { 0.9890f, 0.9933f })]
        [InlineData(new float[] { 0, -3, 3, .5f }, new float[] { 0.5f, 0.0474f, 0.9525f, 0.6224f })]
        public static void Sigmoid(float[] x, float[] expectedResult)
        {
            using BoundedMemory<float> dest = CreateTensor(x.Length);
            TensorPrimitives.Sigmoid(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], Tolerance);
            }
        }

        [Fact]
        public static void Sigmoid_DestinationLongerThanSource()
        {
            float[] x = [-5, -4.5f, -4];
            float[] expectedResult = [0.0066f, 0.0109f, 0.0179f];
            using BoundedMemory<float> dest = CreateTensor(x.Length + 1);

            TensorPrimitives.Sigmoid(x, dest);

            float originalLast = dest[dest.Length - 1];
            for (int i = 0; i < x.Length; i++)
            {
                Assert.Equal(expectedResult[i], dest[i], Tolerance);
            }
            Assert.Equal(originalLast, dest[dest.Length - 1]);
        }

        [Fact]
        public static void Sigmoid_ThrowsForEmptyInput()
        {
            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.Sigmoid(ReadOnlySpan<float>.Empty, CreateTensor(1)));
        }

        [Fact]
        public static void IndexOfMax_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, TensorPrimitives.IndexOfMax(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMax(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = Enumerable.Max(MemoryMarshal.ToEnumerable<float>(x.Memory)) + 1;
                Assert.Equal(expected, TensorPrimitives.IndexOfMax(x));
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMax_FirstNaNReturned(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = float.NaN;
                x[tensorLength - 1] = float.NaN;
                Assert.Equal(expected, TensorPrimitives.IndexOfMax(x));
            }
        }

        [Fact]
        public static void IndexOfMax_Negative0LesserThanPositive0()
        {
            Assert.Equal(1, TensorPrimitives.IndexOfMax([-0f, +0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMax([-0f, -0f, -0f, -0f]));
            Assert.Equal(4, TensorPrimitives.IndexOfMax([-0f, -0f, -0f, -0f, +0f, +0f, +0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMax([+0f, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMax([-1, -0f]));
            Assert.Equal(2, TensorPrimitives.IndexOfMax([-1, -0f, 1]));
        }

        [Fact]
        public static void IndexOfMin_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, TensorPrimitives.IndexOfMin(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMin(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = Enumerable.Min(MemoryMarshal.ToEnumerable<float>(x.Memory)) - 1;
                Assert.Equal(expected, TensorPrimitives.IndexOfMin(x));
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMin_FirstNaNReturned(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = float.NaN;
                x[tensorLength - 1] = float.NaN;
                Assert.Equal(expected, TensorPrimitives.IndexOfMin(x));
            }
        }

        [Fact]
        public static void IndexOfMin_Negative0LesserThanPositive0()
        {
            Assert.Equal(0, TensorPrimitives.IndexOfMin([-0f, +0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMin([+0f, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMin([+0f, -0f, -0f, -0f, -0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMin([-1, -0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMin([-1, -0f, 1]));
        }

        [Fact]
        public static void IndexOfMaxMagnitude_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, TensorPrimitives.IndexOfMaxMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMaxMagnitude(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = Enumerable.Max(MemoryMarshal.ToEnumerable<float>(x.Memory), Math.Abs) + 1;
                Assert.Equal(expected, TensorPrimitives.IndexOfMaxMagnitude(x));
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMaxMagnitude_FirstNaNReturned(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = float.NaN;
                x[tensorLength - 1] = float.NaN;
                Assert.Equal(expected, TensorPrimitives.IndexOfMaxMagnitude(x));
            }
        }

        [Fact]
        public static void IndexOfMaxMagnitude_Negative0LesserThanPositive0()
        {
            Assert.Equal(0, TensorPrimitives.IndexOfMaxMagnitude([-0f, -0f, -0f, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMaxMagnitude([-0f, +0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMaxMagnitude([-0f, +0f, +0f, +0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMaxMagnitude([+0f, -0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMaxMagnitude([-1, -0f]));
            Assert.Equal(2, TensorPrimitives.IndexOfMaxMagnitude([-1, -0f, 1]));
        }

        [Fact]
        public static void IndexOfMinMagnitude_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, TensorPrimitives.IndexOfMinMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMinMagnitude(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateTensor(tensorLength);
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] = i % 2 == 0 ? 42 : -42;
                }

                x[expected] = -41;

                Assert.Equal(expected, TensorPrimitives.IndexOfMinMagnitude(x));
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void IndexOfMinMagnitude_FirstNaNReturned(int tensorLength)
        {
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
                x[expected] = float.NaN;
                x[tensorLength - 1] = float.NaN;
                Assert.Equal(expected, TensorPrimitives.IndexOfMinMagnitude(x));
            }
        }

        [Fact]
        public static void IndexOfMinMagnitude_Negative0LesserThanPositive0()
        {
            Assert.Equal(0, TensorPrimitives.IndexOfMinMagnitude([-0f, -0f, -0f, -0f]));
            Assert.Equal(0, TensorPrimitives.IndexOfMinMagnitude([-0f, +0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMinMagnitude([+0f, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMinMagnitude([+0f, -0f, -0f, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMinMagnitude([-1, -0f]));
            Assert.Equal(1, TensorPrimitives.IndexOfMinMagnitude([-1, -0f, 1]));
        }

        [Fact]
        public static void Max_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Max(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Max(MemoryMarshal.ToEnumerable<float>(x.Memory)), TensorPrimitives.Max(x));

            float max = float.NegativeInfinity;
            foreach (float f in x.Span)
            {
                max = Math.Max(max, f);
            }
            Assert.Equal(max, TensorPrimitives.Max(x));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.Max(x));
            }
        }

        [Fact]
        public static void Max_Negative0LesserThanPositive0()
        {
            Assert.Equal(+0f, TensorPrimitives.Max([-0f, +0f]));
            Assert.Equal(+0f, TensorPrimitives.Max([+0f, -0f]));
            Assert.Equal(-0f, TensorPrimitives.Max([-1, -0f]));
            Assert.Equal(1, TensorPrimitives.Max([-1, -0f, 1]));
        }

        [Fact]
        public static void MaxMagnitude_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MaxMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Max(MemoryMarshal.ToEnumerable<float>(x.Memory), MathF.Abs), TensorPrimitives.MaxMagnitude(x));

            float max = 0;
            foreach (float f in x.Span)
            {
                max = Math.Max(max, MathF.Abs(f));
            }
            Assert.Equal(max, TensorPrimitives.MaxMagnitude(x));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.MaxMagnitude(x));
            }
        }

        [Fact]
        public static void MaxMagnitude_Negative0LesserThanPositive0()
        {
            Assert.Equal(+0f, TensorPrimitives.MaxMagnitude([-0f, +0f]));
            Assert.Equal(+0f, TensorPrimitives.MaxMagnitude([+0f, -0f]));
            Assert.Equal(1, TensorPrimitives.MaxMagnitude([-1, -0f]));
            Assert.Equal(1, TensorPrimitives.MaxMagnitude([-1, -0f, 1]));
            Assert.Equal(0f, TensorPrimitives.MaxMagnitude([-0f, -0f, -0f, -0f, -0f, 0f]));
            Assert.Equal(1, TensorPrimitives.MaxMagnitude([-0f, -0f, -0f, -0f, -1, -0f, 0f, 1]));
        }

        [Fact]
        public static void Min_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Min(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Min(MemoryMarshal.ToEnumerable<float>(x.Memory)), TensorPrimitives.Min(x));

            float min = float.PositiveInfinity;
            foreach (float f in x.Span)
            {
                min = Math.Min(min, f);
            }
            Assert.Equal(min, TensorPrimitives.Min(x));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.Min(x));
            }
        }

        [Fact]
        public static void Min_Negative0LesserThanPositive0()
        {
            Assert.Equal(-0f, TensorPrimitives.Min([-0f, +0f]));
            Assert.Equal(-0f, TensorPrimitives.Min([+0f, -0f]));
            Assert.Equal(-1, TensorPrimitives.Min([-1, -0f]));
            Assert.Equal(-1, TensorPrimitives.Min([-1, -0f, 1]));
        }

        [Fact]
        public static void MinMagnitude_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MinMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Min(MemoryMarshal.ToEnumerable<float>(x.Memory), MathF.Abs), TensorPrimitives.MinMagnitude(x));

            float min = float.PositiveInfinity;
            foreach (float f in x.Span)
            {
                min = Math.Min(min, MathF.Abs(f));
            }
            Assert.Equal(min, TensorPrimitives.MinMagnitude(x));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.MinMagnitude(x));
            }
        }

        [Fact]
        public static void MinMagnitude_Negative0LesserThanPositive0()
        {
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-0f, +0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([+0f, -0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-1, -0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-1, -0f, 1]));
        }

        [Fact]
        public static void Product_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Product(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Product(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            float f = x[0];
            for (int i = 1; i < x.Length; i++)
            {
                f *= x[i];
            }

            Assert.Equal(f, TensorPrimitives.Product(x), Tolerance);
        }

        [Fact]
        public static void Product_KnownValues()
        {
            Assert.Equal(1, TensorPrimitives.Product([1]));
            Assert.Equal(-2, TensorPrimitives.Product([1, -2]));
            Assert.Equal(-6, TensorPrimitives.Product([1, -2, 3]));
            Assert.Equal(24, TensorPrimitives.Product([1, -2, 3, -4]));
            Assert.Equal(120, TensorPrimitives.Product([1, -2, 3, -4, 5]));
            Assert.Equal(-720, TensorPrimitives.Product([1, -2, 3, -4, 5, -6]));
            Assert.Equal(0, TensorPrimitives.Product([1, -2, 3, -4, 5, -6, 0]));
            Assert.Equal(0, TensorPrimitives.Product([0, 1, -2, 3, -4, 5, -6]));
            Assert.Equal(0, TensorPrimitives.Product([1, -2, 3, 0, -4, 5, -6]));
            Assert.Equal(float.NaN, TensorPrimitives.Product([1, -2, 3, float.NaN, -4, 5, -6]));
        }

        [Fact]
        public static void ProductOfDifferences_ThrowsForEmptyAndMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfDifferences(ReadOnlySpan<float>.Empty, ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfDifferences(ReadOnlySpan<float>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfDifferences(CreateTensor(1), ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfDifferences(CreateTensor(44), CreateTensor(43)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfDifferences(CreateTensor(43), CreateTensor(44)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ProductOfDifferences(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float f = x[0] - y[0];
            for (int i = 1; i < x.Length; i++)
            {
                f *= x[i] - y[i];
            }
            Assert.Equal(f, TensorPrimitives.ProductOfDifferences(x, y), Tolerance);
        }

        [Fact]
        public static void ProductOfDifferences_KnownValues()
        {
            Assert.Equal(0, TensorPrimitives.ProductOfDifferences([0], [0]));
            Assert.Equal(0, TensorPrimitives.ProductOfDifferences([1], [1]));
            Assert.Equal(1, TensorPrimitives.ProductOfDifferences([1], [0]));
            Assert.Equal(-1, TensorPrimitives.ProductOfDifferences([0], [1]));
            Assert.Equal(-1, TensorPrimitives.ProductOfDifferences([1, 2, 3, 4, 5], [2, 3, 4, 5, 6]));
            Assert.Equal(120, TensorPrimitives.ProductOfDifferences([1, 2, 3, 4, 5], [0, 0, 0, 0, 0]));
            Assert.Equal(-120, TensorPrimitives.ProductOfDifferences([0, 0, 0, 0, 0], [1, 2, 3, 4, 5]));
            Assert.Equal(float.NaN, TensorPrimitives.ProductOfDifferences([1, 2, float.NaN, 4, 5], [0, 0, 0, 0, 0]));
        }

        [Fact]
        public static void ProductOfSums_ThrowsForEmptyAndMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfSums(ReadOnlySpan<float>.Empty, ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfSums(ReadOnlySpan<float>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfSums(CreateTensor(1), ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfSums(CreateTensor(44), CreateTensor(43)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.ProductOfSums(CreateTensor(43), CreateTensor(44)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ProductOfSums(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float f = x[0] + y[0];
            for (int i = 1; i < x.Length; i++)
            {
                f *= x[i] + y[i];
            }
            Assert.Equal(f, TensorPrimitives.ProductOfSums(x, y), Tolerance);
        }

        [Fact]
        public static void ProductOfSums_KnownValues()
        {
            Assert.Equal(0, TensorPrimitives.ProductOfSums([0], [0]));
            Assert.Equal(1, TensorPrimitives.ProductOfSums([0], [1]));
            Assert.Equal(1, TensorPrimitives.ProductOfSums([1], [0]));
            Assert.Equal(2, TensorPrimitives.ProductOfSums([1], [1]));
            Assert.Equal(10395, TensorPrimitives.ProductOfSums([1, 2, 3, 4, 5], [2, 3, 4, 5, 6]));
            Assert.Equal(120, TensorPrimitives.ProductOfSums([1, 2, 3, 4, 5], [0, 0, 0, 0, 0]));
            Assert.Equal(120, TensorPrimitives.ProductOfSums([0, 0, 0, 0, 0], [1, 2, 3, 4, 5]));
            Assert.Equal(float.NaN, TensorPrimitives.ProductOfSums([1, 2, float.NaN, 4, 5], [0, 0, 0, 0, 0]));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sum(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Sum(MemoryMarshal.ToEnumerable<float>(x.Memory)), TensorPrimitives.Sum(x), Tolerance);

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += f;
            }
            Assert.Equal(sum, TensorPrimitives.Sum(x), Tolerance);
        }

        [Fact]
        public static void Sum_KnownValues()
        {
            Assert.Equal(0, TensorPrimitives.Sum([0]));
            Assert.Equal(1, TensorPrimitives.Sum([0, 1]));
            Assert.Equal(6, TensorPrimitives.Sum([1, 2, 3]));
            Assert.Equal(0, TensorPrimitives.Sum([-3, 0, 3]));
            Assert.Equal(float.NaN, TensorPrimitives.Sum([-3, float.NaN, 3]));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SumOfSquares(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Sum(MemoryMarshal.ToEnumerable<float>(x.Memory), v => v * v), TensorPrimitives.SumOfSquares(x), Tolerance);

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += f * f;
            }
            Assert.Equal(sum, TensorPrimitives.SumOfSquares(x), Tolerance);
        }

        [Fact]
        public static void SumOfSquares_KnownValues()
        {
            Assert.Equal(0, TensorPrimitives.SumOfSquares([0]));
            Assert.Equal(1, TensorPrimitives.SumOfSquares([0, 1]));
            Assert.Equal(14, TensorPrimitives.SumOfSquares([1, 2, 3]));
            Assert.Equal(18, TensorPrimitives.SumOfSquares([-3, 0, 3]));
            Assert.Equal(float.NaN, TensorPrimitives.SumOfSquares([-3, float.NaN, 3]));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SumOfMagnitudes(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Sum(MemoryMarshal.ToEnumerable<float>(x.Memory), MathF.Abs), TensorPrimitives.SumOfMagnitudes(x), Tolerance);

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += MathF.Abs(f);
            }
            Assert.Equal(sum, TensorPrimitives.SumOfMagnitudes(x), Tolerance);
        }

        [Fact]
        public static void SumOfMagnitudes_KnownValues()
        {
            Assert.Equal(0, TensorPrimitives.SumOfMagnitudes([0]));
            Assert.Equal(1, TensorPrimitives.SumOfMagnitudes([0, 1]));
            Assert.Equal(6, TensorPrimitives.SumOfMagnitudes([1, 2, 3]));
            Assert.Equal(6, TensorPrimitives.SumOfMagnitudes([-3, 0, 3]));
            Assert.Equal(float.NaN, TensorPrimitives.SumOfMagnitudes([-3, float.NaN, 3]));
        }
    }
}
