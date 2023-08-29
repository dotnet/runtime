// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Numerics.Tensors.Tests
{
    public static class TensorPrimitivesTests
    {
        private const int TensorSize = 512;

        private const int MismatchedTensorSize = 2;

        private static readonly Random s_random = new Random(20230828);

        private static float[] CreateTensor(int size)
        {
            return new float[size];
        }

        private static float[] CreateAndFillTensor(int size)
        {
            var tensor = CreateTensor(size);
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

        [Fact]
        public static void AddTwoTensors()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]), destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensors_ThrowsForMismatchedLengths()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void AddTwoTensors_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void AddTensorAndScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y), destination[i]);
            }
        }

        [Fact]
        public static void AddTensorAndScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void SubtractTwoTensors()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] - y[i]), destination[i]);
            }
        }

        [Fact]
        public static void SubtractTwoTensors_ThrowsForMismatchedLengths()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void SubtractTwoTensors_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void SubtractTensorAndScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] - y), destination[i]);
            }
        }

        [Fact]
        public static void SubtractTensorAndScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void MultiplyTwoTensors()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]), destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensors_ThrowsForMismatchedLengths()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void MultiplyTwoTensors_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void MultiplyTensorAndScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y), destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTensorAndScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void DivideTwoTensors()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] / y[i]), destination[i]);
            }
        }

        [Fact]
        public static void DivideTwoTensors_ThrowsForMismatchedLengths()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void DivideTwoTensors_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void DivideTensorAndScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] / y), destination[i]);
            }
        }

        [Fact]
        public static void DivideTensorAndScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void NegateTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Negate(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(-x[i], destination[i]);
            }
        }

        [Fact]
        public static void NegateTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Negate(x, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var multiplier = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier[i], destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_y()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var multiplier = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var multiplier = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var multiplier = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var multiplier = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier, destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var multiplier = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTensorAndScalarAndMultiplyWithTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var multiplier = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y) * multiplier[i], destination[i]);
            }
        }

        [Fact]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var multiplier = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var addend = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend[i], destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_y()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(MismatchedTensorSize);
            var addend = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var addend = CreateAndFillTensor(MismatchedTensorSize);
            var destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var addend = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithScalar()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var addend = NextSingle();
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend, destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithScalar_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = CreateAndFillTensor(TensorSize);
            var addend = NextSingle();
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTensorAndScalarAndAddWithTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var addend = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y) + addend[i], destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTensorAndScalarAndAddWithTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var y = NextSingle();
            var addend = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void ExpTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Exp(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Exp(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void ExpTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Exp(x, destination));
        }

        [Fact]
        public static void LogTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Log(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Log(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void LogTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Log(x, destination));
        }

        [Fact]
        public static void CoshTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Cosh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Cosh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void CoshTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Cosh(x, destination));
        }

        [Fact]
        public static void SinhTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Sinh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Sinh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void SinhTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Sinh(x, destination));
        }

        [Fact]
        public static void TanhTensor()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(TensorSize);

            TensorPrimitives.Tanh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Tanh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void TanhTensor_ThrowsForTooShortDestination()
        {
            var x = CreateAndFillTensor(TensorSize);
            var destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Tanh(x, destination));
        }
    }
}
