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

        [Fact]
        public static void AddTwoTensors()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]), destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensors_ThrowsForMismatchedLengths()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void AddTwoTensors_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void AddTensorAndScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y), destination[i]);
            }
        }

        [Fact]
        public static void AddTensorAndScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void SubtractTwoTensors()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] - y[i]), destination[i]);
            }
        }

        [Fact]
        public static void SubtractTwoTensors_ThrowsForMismatchedLengths()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void SubtractTwoTensors_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void SubtractTensorAndScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] - y), destination[i]);
            }
        }

        [Fact]
        public static void SubtractTensorAndScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void MultiplyTwoTensors()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]), destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensors_ThrowsForMismatchedLengths()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void MultiplyTwoTensors_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void MultiplyTensorAndScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y), destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTensorAndScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void DivideTwoTensors()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] / y[i]), destination[i]);
            }
        }

        [Fact]
        public static void DivideTwoTensors_ThrowsForMismatchedLengths()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void DivideTwoTensors_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void DivideTensorAndScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] / y), destination[i]);
            }
        }

        [Fact]
        public static void DivideTensorAndScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void NegateTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Negate(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(-x[i], destination[i]);
            }
        }

        [Fact]
        public static void NegateTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Negate(x, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] multiplier = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier[i], destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_y()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] multiplier = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] multiplier = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithThirdTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] multiplier = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float multiplier = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y[i]) * multiplier, destination[i]);
            }
        }

        [Fact]
        public static void AddTwoTensorsAndMultiplyWithScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float multiplier = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddTensorAndScalarAndMultiplyWithTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] multiplier = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] + y) * multiplier[i], destination[i]);
            }
        }

        [Fact]
        public static void AddTensorAndScalarAndMultiplyWithTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] multiplier = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] addend = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend[i], destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_y()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(MismatchedTensorSize);
            float[] addend = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForMismatchedLengths_x_multiplier()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] addend = CreateAndFillTensor(MismatchedTensorSize);
            float[] destination = CreateTensor(TensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithThirdTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float[] addend = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithScalar()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float addend = NextSingle();
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y[i]) + addend, destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTwoTensorsAndAddWithScalar_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] y = CreateAndFillTensor(TensorSize);
            float addend = NextSingle();
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyTensorAndScalarAndAddWithTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] addend = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal((x[i] * y) + addend[i], destination[i]);
            }
        }

        [Fact]
        public static void MultiplyTensorAndScalarAndAddWithTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float y = NextSingle();
            float[] addend = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void ExpTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Exp(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Exp(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void ExpTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Exp(x, destination));
        }

        [Fact]
        public static void LogTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Log(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Log(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void LogTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Log(x, destination));
        }

        [Fact]
        public static void CoshTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Cosh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Cosh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void CoshTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Cosh(x, destination));
        }

        [Fact]
        public static void SinhTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Sinh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Sinh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void SinhTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Sinh(x, destination));
        }

        [Fact]
        public static void TanhTensor()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(TensorSize);

            TensorPrimitives.Tanh(x, destination);

            for (int i = 0; i < TensorSize; i++)
            {
                Assert.Equal(MathF.Tanh(x[i]), destination[i]);
            }
        }

        [Fact]
        public static void TanhTensor_ThrowsForTooShortDestination()
        {
            float[] x = CreateAndFillTensor(TensorSize);
            float[] destination = CreateTensor(MismatchedTensorSize);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Tanh(x, destination));
        }
    }
}
