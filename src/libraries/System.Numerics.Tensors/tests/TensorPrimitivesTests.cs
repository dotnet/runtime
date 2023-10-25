// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Numerics.Tensors.Tests
{
    public static partial class TensorPrimitivesTests
    {
        #region Test Utilities
        public static IEnumerable<object[]> TensorLengthsIncluding0 =>
            TensorLengths.Concat(new object[][] { [0] });

        public static IEnumerable<object[]> TensorLengths =>
            from length in Enumerable.Range(1, 256)
            select new object[] { length };

        public static IEnumerable<object[]> VectorLengthAndIteratedRange(float min, float max, float increment)
        {
            foreach (int length in new[] { 4, 8, 16 })
            {
                for (float f = min; f <= max; f += increment)
                {
                    yield return new object[] { length, f };
                }
            }
        }

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

        private static float NextSingle() =>
            // For testing purposes, get a mix of negative and positive values.
            (float)((s_random.NextDouble() * 2) - 1);

        private static void AssertEqualTolerance(double expected, double actual, double tolerance = 0.00001f)
        {
            double diff = Math.Abs(expected - actual);
            if (diff > tolerance &&
                diff > Math.Max(Math.Abs(expected), Math.Abs(actual)) * tolerance)
            {
                throw EqualException.ForMismatchedValues(expected, actual);
            }
        }

        private static unsafe float MathFMaxMagnitude(float x, float y)
        {
            float ax = MathF.Abs(x), ay = MathF.Abs(y);
            return (ax > ay) || float.IsNaN(ax) || (ax == ay && *(int*)&x >= 0) ? x : y;
        }

        private static unsafe float MathFMinMagnitude(float x, float y)
        {
            float ax = MathF.Abs(x), ay = MathF.Abs(y);
            return (ax < ay) || float.IsNaN(ax) || (ax == ay && *(int*)&x < 0) ? x : y;
        }

        private static unsafe float UInt32ToSingle(uint i) => *(float*)&i;

        private static unsafe float SingleToUInt32(float f) => *(uint*)&f;

        /// <summary>Gets a variety of special values (e.g. NaN).</summary>
        private static IEnumerable<float> GetSpecialValues()
        {
            // NaN
            yield return UInt32ToSingle(0xFFC0_0000); // -qNaN / float.NaN
            yield return UInt32ToSingle(0xFFFF_FFFF); // -qNaN / all-bits-set
            yield return UInt32ToSingle(0x7FC0_0000); // +qNaN
            yield return UInt32ToSingle(0xFFA0_0000); // -sNaN
            yield return UInt32ToSingle(0x7FA0_0000); // +sNaN

            // +Infinity, -Infinity
            yield return float.PositiveInfinity;
            yield return float.NegativeInfinity;

            // +Zero, -Zero
            yield return +0.0f;
            yield return -0.0f;

            // Subnormals
            yield return +float.Epsilon;
            yield return -float.Epsilon;
            yield return UInt32ToSingle(0x007F_FFFF);
            yield return UInt32ToSingle(0x807F_FFFF);

            // Normals
            yield return UInt32ToSingle(0x0080_0000);
            yield return UInt32ToSingle(0x8080_0000);
            yield return UInt32ToSingle(0x7F7F_FFFF); // MaxValue
            yield return UInt32ToSingle(0xFF7F_FFFF); // MinValue
        }

        /// <summary>
        /// Runs the specified action for each special value. Before the action is invoked,
        /// the value is stored into a random position in <paramref name="x"/>, and the original
        /// value is subsequently restored.
        /// </summary>
        private static void RunForEachSpecialValue(Action action, BoundedMemory<float> x)
        {
            foreach (float value in GetSpecialValues())
            {
                int pos = s_random.Next(x.Length);
                float orig = x[pos];
                x[pos] = value;

                action();

                x[pos] = orig;
            }
        }

        /// <summary>
        /// Loads a variety of special values (e.g. NaN) into random positions in <paramref name="x"/>
        /// and related values into the corresponding positions in <paramref name="y"/>.
        /// </summary>
        private static void SetSpecialValues(Span<float> x, Span<float> y)
        {
            int pos;

            // NaNs
            pos = s_random.Next(x.Length);
            x[pos] = float.NaN;
            y[pos] = UInt32ToSingle(0x7FC0_0000);

            // +Infinity, -Infinity
            pos = s_random.Next(x.Length);
            x[pos] = float.PositiveInfinity;
            y[pos] = float.NegativeInfinity;

            // +Zero, -Zero
            pos = s_random.Next(x.Length);
            x[pos] = +0.0f;
            y[pos] = -0.0f;

            // +Epsilon, -Epsilon
            pos = s_random.Next(x.Length);
            x[pos] = +float.Epsilon;
            y[pos] = -float.Epsilon;

            // Same magnitude, opposite sign
            pos = s_random.Next(x.Length);
            x[pos] = +5.0f;
            y[pos] = -5.0f;
        }
        #endregion

        #region Abs
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Abs(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Abs(x, destination);

            for (int i = 0; i < x.Length; i++)
            {
                AssertEqualTolerance(MathF.Abs(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Abs_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Abs(x, x);

            for (int i = 0; i < x.Length; i++)
            {
                AssertEqualTolerance(MathF.Abs(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Abs_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Abs(x, destination));
        }

        [Fact]
        public static void Abs_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Abs(array.AsSpan(1, 5), array.AsSpan(0, 5)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Abs(array.AsSpan(1, 5), array.AsSpan(2, 5)));
        }
        #endregion

        #region Add
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Add_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] + y[i], destination[i]);
            }

            float[] xOrig = x.Span.ToArray();

            // Validate that the destination can be the same as an input.
            TensorPrimitives.Add(x, x, x);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] + xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Add_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Add(x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] + xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Add_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Add(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Add_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void Add_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(6, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Add_TensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Add(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] + y, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Add_TensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.Add(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] + y, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Add_TensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(x, y, destination));
        }

        [Fact]
        public static void Add_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), 42, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Add(array.AsSpan(1, 2), 42, array.AsSpan(2, 2)));
        }
        #endregion

        #region AddMultiply
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_ThreeTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] + y[i]) * multiplier[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_ThreeTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.AddMultiply(x, x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] + xOrig[i]) * xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_ThreeTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> z = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, z, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, z, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(z, x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_ThreeTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddMultiply_ThreeTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(5, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_TensorTensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] + y[i]) * multiplier, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_TensorTensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float multiplier = NextSingle();

            TensorPrimitives.AddMultiply(x, x, multiplier, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] + xOrig[i]) * multiplier, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_TensorTensorScalar_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(y, x, multiplier, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_TensorTensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float multiplier = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddMultiply_TensorTensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(5, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_TensorScalarTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.AddMultiply(x, y, multiplier, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] + y) * multiplier[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void AddMultiply_TensorScalarTensor_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.AddMultiply(x, y, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] + y) * xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_TensorScalarTensor_ThrowsForMismatchedLengths_x_z(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> z = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(x, y, z, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.AddMultiply(z, y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void AddMultiply_TensorScalarTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> multiplier = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(x, y, multiplier, destination));
        }

        [Fact]
        public static void AddMultiply_TensorScalarTensor_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.AddMultiply(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Cosh
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Cosh(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Cosh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Cosh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Cosh_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Cosh(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Cosh(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Cosh_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Cosh(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Cosh(x[i]), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(VectorLengthAndIteratedRange), new object[] { -100f, 100f, 3f })]
        public static void Cosh_ValueRange(int vectorLength, float element)
        {
            float[] x = new float[vectorLength];
            float[] dest = new float[vectorLength];

            x.AsSpan().Fill(element);
            TensorPrimitives.Cosh(x, dest);

            float expected = MathF.Cosh(element);
            foreach (float actual in dest)
            {
                AssertEqualTolerance(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Cosh_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Cosh(x, destination));
        }

        [Fact]
        public static void Cosh_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Cosh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Cosh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region CosineSimilarity
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void CosineSimilarity_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(x, y));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.CosineSimilarity(y, x));
        }

        [Fact]
        public static void CosineSimilarity_ThrowsForEmpty()
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
            AssertEqualTolerance(expectedResult, TensorPrimitives.CosineSimilarity(x, y));
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

            AssertEqualTolerance(dot / (MathF.Sqrt(squareX) * MathF.Sqrt(squareY)), TensorPrimitives.CosineSimilarity(x, y));
        }
        #endregion

        #region Distance
        [Fact]
        public static void Distance_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(ReadOnlySpan<float>.Empty, ReadOnlySpan<float>.Empty));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(ReadOnlySpan<float>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(CreateTensor(1), ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Distance_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(x, y));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Distance(y, x));
        }

        [Theory]
        [InlineData(new float[] { 3, 2 }, new float[] { 4, 1 }, 1.4142f)]
        [InlineData(new float[] { 0, 4 }, new float[] { 6, 2 }, 6.3245f)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 5.19615f)]
        [InlineData(new float[] { 5, 1, 6, 10 }, new float[] { 7, 2, 8, 4 }, 6.7082f)]
        public static void Distance_KnownValues(float[] x, float[] y, float expectedResult)
        {
            AssertEqualTolerance(expectedResult, TensorPrimitives.Distance(x, y));
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

            AssertEqualTolerance(MathF.Sqrt(distance), TensorPrimitives.Distance(x, y));
        }
        #endregion

        #region Divide
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Divide_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] / y[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Divide_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Divide(x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] / xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Divide_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Divide(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Divide_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void Divide_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Divide_TensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Divide(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] / y, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Divide_TensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.Divide(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] / y, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Divide_TensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(x, y, destination));
        }

        [Fact]
        public static void Divide_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Dot
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Dot_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Dot(x, y));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Dot(y, x));
        }

        [Theory]
        [InlineData(new float[] { 1, 3, -5 }, new float[] { 4, -2, -1 }, 3)]
        [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 32)]
        [InlineData(new float[] { 1, 2, 3, 10, 8 }, new float[] { 4, 5, 6, -2, 7 }, 68)]
        [InlineData(new float[] { }, new float[] { }, 0)]
        public static void Dot_KnownValues(float[] x, float[] y, float expectedResult)
        {
            AssertEqualTolerance(expectedResult, TensorPrimitives.Dot(x, y));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Dot(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);

            float dot = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dot += x[i] * y[i];
            }

            AssertEqualTolerance(dot, TensorPrimitives.Dot(x, y));
        }
        #endregion

        #region Exp
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Exp(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Exp(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Exp(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Exp_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Exp(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Exp(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Exp_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Exp(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Exp(x[i]), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Exp_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Exp(x, destination));
        }

        [Fact]
        public static void Exp_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Exp(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Exp(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region IndexOfMax
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
        #endregion

        #region IndexOfMaxMagnitude
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
        #endregion

        #region IndexOfMin
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
        #endregion

        #region IndexOfMinMagnitude
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
        #endregion

        #region Log
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Log(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Log(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Log(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Log_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Log(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Log(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Log_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Log(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Log(x[i]), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Log_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log(x, destination));
        }

        [Fact]
        public static void Log_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Log2
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Log2(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Log2(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Log(x[i], 2), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Log2_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Log2(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Log(xOrig[i], 2), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Log2_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Log2(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Log(x[i], 2), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Log2_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log2(x, destination));
        }

        [Fact]
        public static void Log2_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log2(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Log2(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Max
        [Fact]
        public static void Max_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Max(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_Tensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Max(MemoryMarshal.ToEnumerable<float>(x.Memory)), TensorPrimitives.Max(x));

            float max = float.NegativeInfinity;
            foreach (float f in x.Span)
            {
                max = Math.Max(max, f);
            }

            Assert.Equal(max, TensorPrimitives.Max(x));
            Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMax(x)]), SingleToUInt32(TensorPrimitives.Max(x)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_Tensor_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                float max = float.NegativeInfinity;
                foreach (float f in x.Span)
                {
                    max = Math.Max(max, f);
                }

                Assert.Equal(max, TensorPrimitives.Max(x));
                Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMax(x)]), SingleToUInt32(TensorPrimitives.Max(x)));
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_Tensor_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                FillTensor(x);
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.Max(x));
            }
        }

        [Fact]
        public static void Max_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(+0f, TensorPrimitives.Max([-0f, +0f]));
            Assert.Equal(+0f, TensorPrimitives.Max([+0f, -0f]));
            Assert.Equal(-0f, TensorPrimitives.Max([-1, -0f]));
            Assert.Equal(1, TensorPrimitives.Max([-1, -0f, 1]));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Max_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Max(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Max(x[i], y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Max_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

            TensorPrimitives.Max(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Max(xOrig[i], y[i]), x[i]);
            }

            xOrig.AsSpan().CopyTo(x.Span);
            yOrig.AsSpan().CopyTo(y.Span);

            TensorPrimitives.Max(x, y, y);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Max(x[i], yOrig[i]), y[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_TwoTensors_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            SetSpecialValues(x, y);

            TensorPrimitives.Max(x, y, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Max(x[i], y[i]), destination[i]);
            }

            TensorPrimitives.Max(y, x, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Max(y[i], x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Max(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Max(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Max_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Max(x, y, destination));
        }

        [Fact]
        public static void Max_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region MaxMagnitude
        [Fact]
        public static void MaxMagnitude_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MaxMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_Tensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            float maxMagnitude = x[0];
            foreach (float f in x.Span)
            {
                maxMagnitude = MathFMaxMagnitude(maxMagnitude, f);
            }

            Assert.Equal(maxMagnitude, TensorPrimitives.MaxMagnitude(x));
            Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMaxMagnitude(x)]), SingleToUInt32(TensorPrimitives.MaxMagnitude(x)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_Tensor_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                float maxMagnitude = x[0];
                foreach (float f in x.Span)
                {
                    maxMagnitude = MathFMaxMagnitude(maxMagnitude, f);
                }

                Assert.Equal(maxMagnitude, TensorPrimitives.MaxMagnitude(x));
                Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMaxMagnitude(x)]), SingleToUInt32(TensorPrimitives.MaxMagnitude(x)));
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_Tensor_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                FillTensor(x);
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.MaxMagnitude(x));
            }
        }

        [Fact]
        public static void MaxMagnitude_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(+0f, TensorPrimitives.MaxMagnitude([-0f, +0f]));
            Assert.Equal(+0f, TensorPrimitives.MaxMagnitude([+0f, -0f]));
            Assert.Equal(-1, TensorPrimitives.MaxMagnitude([-1, -0f]));
            Assert.Equal(1, TensorPrimitives.MaxMagnitude([-1, -0f, 1]));
            Assert.Equal(0f, TensorPrimitives.MaxMagnitude([-0f, -0f, -0f, -0f, -0f, 0f]));
            Assert.Equal(1, TensorPrimitives.MaxMagnitude([-0f, -0f, -0f, -0f, -1, -0f, 0f, 1]));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MaxMagnitude_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MaxMagnitude(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMaxMagnitude(x[i], y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MaxMagnitude_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

            TensorPrimitives.MaxMagnitude(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMaxMagnitude(xOrig[i], y[i]), x[i]);
            }

            xOrig.AsSpan().CopyTo(x.Span);
            yOrig.AsSpan().CopyTo(y.Span);

            TensorPrimitives.MaxMagnitude(x, y, y);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMaxMagnitude(x[i], yOrig[i]), y[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_TwoTensors_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            SetSpecialValues(x, y);

            TensorPrimitives.MaxMagnitude(x, y, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMaxMagnitude(x[i], y[i]), destination[i]);
            }

            TensorPrimitives.MaxMagnitude(y, x, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMaxMagnitude(y[i], x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MaxMagnitude(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MaxMagnitude(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MaxMagnitude_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MaxMagnitude(x, y, destination));
        }

        [Fact]
        public static void MaxMagnitude_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Min
        [Fact]
        public static void Min_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Min(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_Tensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            Assert.Equal(Enumerable.Min(MemoryMarshal.ToEnumerable<float>(x.Memory)), TensorPrimitives.Min(x));

            float min = float.PositiveInfinity;
            foreach (float f in x.Span)
            {
                min = Math.Min(min, f);
            }

            Assert.Equal(min, TensorPrimitives.Min(x));
            Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMin(x)]), SingleToUInt32(TensorPrimitives.Min(x)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_Tensor_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                float min = float.PositiveInfinity;
                foreach (float f in x.Span)
                {
                    min = Math.Min(min, f);
                }

                Assert.Equal(min, TensorPrimitives.Min(x));
                Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMin(x)]), SingleToUInt32(TensorPrimitives.Min(x)));
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_Tensor_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                FillTensor(x);
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.Min(x));
            }
        }

        [Fact]
        public static void Min_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(-0f, TensorPrimitives.Min([-0f, +0f]));
            Assert.Equal(-0f, TensorPrimitives.Min([+0f, -0f]));
            Assert.Equal(-1, TensorPrimitives.Min([-1, -0f]));
            Assert.Equal(-1, TensorPrimitives.Min([-1, -0f, 1]));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Min_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Min(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Min(x[i], y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Min_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

            TensorPrimitives.Min(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Min(xOrig[i], y[i]), x[i]);
            }

            xOrig.AsSpan().CopyTo(x.Span);
            yOrig.AsSpan().CopyTo(y.Span);

            TensorPrimitives.Min(x, y, y);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Min(x[i], yOrig[i]), y[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_TwoTensors_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            SetSpecialValues(x, y);

            TensorPrimitives.Min(x, y, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Min(x[i], y[i]), destination[i]);
            }

            TensorPrimitives.Min(y, x, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Min(y[i], x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Min(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Min(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Min_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Min(x, y, destination));
        }

        [Fact]
        public static void Min_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region MinMagnitude
        [Fact]
        public static void MinMagnitude_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MinMagnitude(ReadOnlySpan<float>.Empty));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_Tensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            float minMagnitude = x[0];
            foreach (float f in x.Span)
            {
                minMagnitude = MathFMinMagnitude(minMagnitude, f);
            }

            Assert.Equal(minMagnitude, TensorPrimitives.MinMagnitude(x));
            Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMinMagnitude(x)]), SingleToUInt32(TensorPrimitives.MinMagnitude(x)));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_Tensor_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                float minMagnitude = x[0];
                foreach (float f in x.Span)
                {
                    minMagnitude = MathFMinMagnitude(minMagnitude, f);
                }

                Assert.Equal(minMagnitude, TensorPrimitives.MinMagnitude(x));
                Assert.Equal(SingleToUInt32(x[TensorPrimitives.IndexOfMinMagnitude(x)]), SingleToUInt32(TensorPrimitives.MinMagnitude(x)));
            }, x);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_Tensor_NanReturned(int tensorLength)
        {
            using BoundedMemory<float> x = CreateTensor(tensorLength);
            foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
            {
                FillTensor(x);
                x[expected] = float.NaN;
                Assert.Equal(float.NaN, TensorPrimitives.MinMagnitude(x));
            }
        }

        [Fact]
        public static void MinMagnitude_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-0f, +0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([+0f, -0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-1, -0f]));
            Assert.Equal(0, TensorPrimitives.MinMagnitude([-1, -0f, 1]));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MinMagnitude_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MinMagnitude(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMinMagnitude(x[i], y[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MinMagnitude_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

            TensorPrimitives.MinMagnitude(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMinMagnitude(xOrig[i], y[i]), x[i]);
            }

            xOrig.AsSpan().CopyTo(x.Span);
            yOrig.AsSpan().CopyTo(y.Span);

            TensorPrimitives.MinMagnitude(x, y, y);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMinMagnitude(x[i], yOrig[i]), y[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_TwoTensors_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            SetSpecialValues(x, y);

            TensorPrimitives.MinMagnitude(x, y, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMinMagnitude(x[i], y[i]), destination[i]);
            }

            TensorPrimitives.MinMagnitude(y, x, destination);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathFMinMagnitude(y[i], x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MinMagnitude(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MinMagnitude(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MinMagnitude_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MinMagnitude(x, y, destination));
        }

        [Fact]
        public static void MinMagnitude_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Multiply
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Multiply_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] * y[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Multiply_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Multiply(x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] * xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Multiply_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Multiply(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Multiply_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void Multiply_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Multiply_TensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Multiply(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] * y, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Multiply_TensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.Multiply(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] * y, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Multiply_TensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(x, y, destination));
        }

        [Fact]
        public static void Multiply_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), 42, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Multiply(array.AsSpan(1, 2), 42, array.AsSpan(2, 2)));
        }
        #endregion

        #region MultiplyAdd
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_ThreeTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] * y[i]) + addend[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_ThreeTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.MultiplyAdd(x, x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] * xOrig[i]) + xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyAdd_ThreeTensors_ThrowsForMismatchedLengths_x_y(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> z = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, y, z, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(x, z, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.MultiplyAdd(z, x, y, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyAdd_ThreeTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyAdd_ThreeTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(5, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_TensorTensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] * y[i]) + addend, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_TensorTensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float addend = NextSingle();

            TensorPrimitives.MultiplyAdd(x, x, addend, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] * xOrig[i]) + addend, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyAdd_TensorTensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            float addend = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyAdd_TensorTensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), 42, array.AsSpan(5, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_TensorScalarTensor(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.MultiplyAdd(x, y, addend, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((x[i] * y) + addend[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void MultiplyAdd_TensorScalarTensor_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.MultiplyAdd(x, y, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance((xOrig[i] * y) + xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void MultiplyAdd_TensorScalarTensor_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> addend = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(x, y, addend, destination));
        }

        [Fact]
        public static void MultiplyAdd_TensorScalarTensor_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.MultiplyAdd(array.AsSpan(1, 2), 42, array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Negate
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Negate(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Negate(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(-x[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Negate_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Negate(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(-xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Negate_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Negate(x, destination));
        }

        [Fact]
        public static void Negate_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Negate(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Negate(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Norm
        [Theory]
        [InlineData(new float[] { 1, 2, 3 }, 3.7416575f)]
        [InlineData(new float[] { 3, 4 }, 5)]
        [InlineData(new float[] { 3 }, 3)]
        [InlineData(new float[] { 3, 4, 1, 2 }, 5.477226)]
        [InlineData(new float[] { }, 0f)]
        public static void Norm_KnownValues(float[] x, float expectedResult)
        {
            AssertEqualTolerance(expectedResult, TensorPrimitives.Norm(x));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Norm(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            float sumOfSquares = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                sumOfSquares += x[i] * x[i];
            }

            AssertEqualTolerance(MathF.Sqrt(sumOfSquares), TensorPrimitives.Norm(x));
        }
        #endregion

        #region Product
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

            AssertEqualTolerance(f, TensorPrimitives.Product(x));
        }

        [Theory]
        [InlineData(1, new float[] { 1 })]
        [InlineData(-2, new float[] { 1, -2 })]
        [InlineData(-6, new float[] { 1, -2, 3 })]
        [InlineData(24, new float[] { 1, -2, 3, -4 })]
        [InlineData(120, new float[] { 1, -2, 3, -4, 5 })]
        [InlineData(-720, new float[] { 1, -2, 3, -4, 5, -6 })]
        [InlineData(0, new float[] { 1, -2, 3, -4, 5, -6, 0 })]
        [InlineData(0, new float[] { 0, 1, -2, 3, -4, 5, -6 })]
        [InlineData(0, new float[] { 1, -2, 3, 0, -4, 5, -6 })]
        [InlineData(float.NaN, new float[] { 1, -2, 3, float.NaN, -4, 5, -6 })]
        public static void Product_KnownValues(float expected, float[] input)
        {
            Assert.Equal(expected, TensorPrimitives.Product(input));
        }
        #endregion

        #region ProductOfDifferences
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
            AssertEqualTolerance(f, TensorPrimitives.ProductOfDifferences(x, y));
        }

        [Theory]
        [InlineData(0, new float[] {0 }, new float[] {0})]
        [InlineData(0, new float[] {1 }, new float[] {1})]
        [InlineData(1, new float[] {1 }, new float[] {0})]
        [InlineData(-1, new float[] {0 }, new float[] {1})]
        [InlineData(-1, new float[] {1, 2, 3, 4, 5 }, new float[] {2, 3, 4, 5, 6})]
        [InlineData(120, new float[] {1, 2, 3, 4, 5 }, new float[] {0, 0, 0, 0, 0})]
        [InlineData(-120, new float[] {0, 0, 0, 0, 0 }, new float[] {1, 2, 3, 4, 5})]
        [InlineData(float.NaN, new float[] {1, 2, float.NaN, 4, 5 }, new float[] {0, 0, 0, 0, 0})]
        public static void ProductOfDifferences_KnownValues(float expected, float[] x, float[] y)
        {
            Assert.Equal(expected, TensorPrimitives.ProductOfDifferences(x, y));

        }
        #endregion

        #region ProductOfSums
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
            AssertEqualTolerance(f, TensorPrimitives.ProductOfSums(x, y));
        }

        [Theory]
        [InlineData(0, new float[] {0 }, new float[] { 0 })]
        [InlineData(1, new float[] {0 }, new float[] { 1 })]
        [InlineData(1, new float[] {1 }, new float[] { 0 })]
        [InlineData(2, new float[] {1 }, new float[] { 1 })]
        [InlineData(10395, new float[] {1, 2, 3, 4, 5 }, new float[] { 2, 3, 4, 5, 6 })]
        [InlineData(120, new float[] {1, 2, 3, 4, 5 }, new float[] { 0, 0, 0, 0, 0 })]
        [InlineData(120, new float[] {0, 0, 0, 0, 0 }, new float[] { 1, 2, 3, 4, 5 })]
        [InlineData(float.NaN, new float[] {1, 2, float.NaN, 4, 5 }, new float[] { 0, 0, 0, 0, 0 })]
        public static void ProductOfSums_KnownValues(float expected, float[] x, float[] y)
        {
            Assert.Equal(expected, TensorPrimitives.ProductOfSums(x, y));
        }
        #endregion

        #region Sigmoid
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Sigmoid(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(1f / (1f + MathF.Exp(-x[i])), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Sigmoid(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(1f / (1f + MathF.Exp(-xOrig[i])), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Sigmoid(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(1f / (1f + MathF.Exp(-x[i])), destination[i]);
                }
            }, x);
        }

        [Theory]
        [InlineData(new float[] { -5, -4.5f, -4 }, new float[] { 0.0066f, 0.0109f, 0.0179f })]
        [InlineData(new float[] { 4.5f, 5 }, new float[] { 0.9890f, 0.9933f })]
        [InlineData(new float[] { 0, -3, 3, .5f }, new float[] { 0.5f, 0.0474f, 0.9525f, 0.6224f })]
        public static void Sigmoid_KnownValues(float[] x, float[] expectedResult)
        {
            using BoundedMemory<float> dest = CreateTensor(x.Length);
            TensorPrimitives.Sigmoid(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                AssertEqualTolerance(expectedResult[i], dest[i], 0.0001f);
            }
        }

        [Theory]
        [InlineData(new float[] { -5, -4.5f, -4 }, new float[] { 0.0066f, 0.0109f, 0.0179f })]
        public static void Sigmoid_DestinationLongerThanSource(float[] x, float[] expectedResult)
        {
            using BoundedMemory<float> dest = CreateTensor(x.Length + 1);

            TensorPrimitives.Sigmoid(x, dest);

            float originalLast = dest[dest.Length - 1];
            for (int i = 0; i < x.Length; i++)
            {
                AssertEqualTolerance(expectedResult[i], dest[i], 0.0001f);
            }
            Assert.Equal(originalLast, dest[dest.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sigmoid_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sigmoid(x, destination));
        }

        [Fact]
        public static void Sigmoid_ThrowsForEmptyInput()
        {
            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.Sigmoid(ReadOnlySpan<float>.Empty, CreateTensor(1)));
        }

        [Fact]
        public static void Sigmoid_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sigmoid(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sigmoid(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Sinh
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Sinh(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Sinh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Sinh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Sinh_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Sinh(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Sinh(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sinh_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Sinh(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Sinh(x[i]), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(VectorLengthAndIteratedRange), new object[] { -100f, 100f, 3f })]
        public static void Sinh_ValueRange(int vectorLengths, float element)
        {
            float[] x = new float[vectorLengths];
            float[] dest = new float[vectorLengths];

            x.AsSpan().Fill(element);
            TensorPrimitives.Sinh(x, dest);

            float expected = MathF.Sinh(element);
            foreach (float actual in dest)
            {
                AssertEqualTolerance(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sinh_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sinh(x, destination));
        }

        [Fact]
        public static void Sinh_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sinh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Sinh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region SoftMax
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SoftMax(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.SoftMax(x, destination);

            float expSum = MemoryMarshal.ToEnumerable<float>(x.Memory).Sum(MathF.Exp);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Exp(x[i]) / expSum, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SoftMax_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.SoftMax(x, x);

            float expSum = xOrig.Sum(MathF.Exp);
            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Exp(xOrig[i]) / expSum, x[i]);
            }
        }

        [Theory]
        [InlineData(new float[] { 3, 1, .2f }, new float[] { 0.8360188f, 0.11314284f, 0.05083836f })]
        [InlineData(new float[] { 3, 4, 1 }, new float[] { 0.2594f, 0.705384f, 0.0351f })]
        [InlineData(new float[] { 5, 3 }, new float[] { 0.8807f, 0.1192f })]
        [InlineData(new float[] { 4, 2, 1, 9 }, new float[] { 0.0066f, 9.04658e-4f, 3.32805e-4f, 0.9920f })]
        public static void SoftMax_KnownValues(float[] x, float[] expectedResult)
        {
            using BoundedMemory<float> dest = CreateTensor(x.Length);
            TensorPrimitives.SoftMax(x, dest);

            for (int i = 0; i < x.Length; i++)
            {
                AssertEqualTolerance(expectedResult[i], dest[i], 0.0001f);
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
                AssertEqualTolerance(expectedResult[i], dest[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SoftMax_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.SoftMax(x, destination));
        }

        [Fact]
        public static void SoftMax_ThrowsForEmptyInput()
        {
            AssertExtensions.Throws<ArgumentException>(() => TensorPrimitives.SoftMax(ReadOnlySpan<float>.Empty, CreateTensor(1)));
        }

        [Fact]
        public static void SoftMax_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.SoftMax(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.SoftMax(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Subtract
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Subtract_TwoTensors(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] - y[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Subtract_TwoTensors_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Subtract(x, x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] - xOrig[i], x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Subtract_TwoTensors_ThrowsForMismatchedLengths(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength - 1);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(x, y, destination));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.Subtract(y, x, destination));
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Subtract_TwoTensors_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> y = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void Subtract_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Subtract_TensorScalar(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Subtract(x, y, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(x[i] - y, destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Subtract_TensorScalar_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();
            float y = NextSingle();

            TensorPrimitives.Subtract(x, y, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(xOrig[i] - y, x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Subtract_TensorScalar_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float y = NextSingle();
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(x, y, destination));
        }

        [Fact]
        public static void Subtract_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), 42, array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Subtract(array.AsSpan(1, 2), 42, array.AsSpan(2, 2)));
        }
        #endregion

        #region Sum
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Sum(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            AssertEqualTolerance(MemoryMarshal.ToEnumerable<float>(x.Memory).Sum(), TensorPrimitives.Sum(x));

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += f;
            }
            AssertEqualTolerance(sum, TensorPrimitives.Sum(x));
        }

        [Theory]
        [InlineData(0, new float[] { 0 })]
        [InlineData(1, new float[] { 0, 1 })]
        [InlineData(6, new float[] { 1, 2, 3 })]
        [InlineData(0, new float[] { -3, 0, 3 })]
        [InlineData(float.NaN, new float[] { -3, float.NaN, 3 })]
        public static void Sum_KnownValues(float expected, float[] x)
        {
            Assert.Equal(expected, TensorPrimitives.Sum(x));
        }
        #endregion

        #region SumOfMagnitudes
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SumOfMagnitudes(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            AssertEqualTolerance(Enumerable.Sum(MemoryMarshal.ToEnumerable<float>(x.Memory), MathF.Abs), TensorPrimitives.SumOfMagnitudes(x));

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += MathF.Abs(f);
            }
            AssertEqualTolerance(sum, TensorPrimitives.SumOfMagnitudes(x));
        }

        [Theory]
        [InlineData(0, new float[] { 0 })]
        [InlineData(1, new float[] { 0, 1 })]
        [InlineData(6, new float[] { 1, 2, 3 })]
        [InlineData(6, new float[] { -3, 0, 3 })]
        [InlineData(float.NaN, new float[] { -3, float.NaN, 3 })]
        public static void SumOfMagnitudes_KnownValues(float expected, float[] x)
        {
            Assert.Equal(expected, TensorPrimitives.SumOfMagnitudes(x));
        }
        #endregion

        #region SumOfSquares
        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void SumOfSquares(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);

            AssertEqualTolerance(Enumerable.Sum(MemoryMarshal.ToEnumerable<float>(x.Memory), v => v * v), TensorPrimitives.SumOfSquares(x));

            float sum = 0;
            foreach (float f in x.Span)
            {
                sum += f * f;
            }
            AssertEqualTolerance(sum, TensorPrimitives.SumOfSquares(x));
        }

        [Theory]
        [InlineData(0, new float[] { 0 })]
        [InlineData(1, new float[] { 0, 1 })]
        [InlineData(14, new float[] { 1, 2, 3 })]
        [InlineData(18, new float[] { -3, 0, 3 })]
        [InlineData(float.NaN, new float[] { -3, float.NaN, 3 })]
        public static void SumOfSquares_KnownValues(float expected, float[] x)
        {
            Assert.Equal(expected, TensorPrimitives.SumOfSquares(x));
        }
        #endregion

        #region Tanh
        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Tanh(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            TensorPrimitives.Tanh(x, destination);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Tanh(x[i]), destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengthsIncluding0))]
        public static void Tanh_InPlace(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            float[] xOrig = x.Span.ToArray();

            TensorPrimitives.Tanh(x, x);

            for (int i = 0; i < tensorLength; i++)
            {
                AssertEqualTolerance(MathF.Tanh(xOrig[i]), x[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Tanh_SpecialValues(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength);

            RunForEachSpecialValue(() =>
            {
                TensorPrimitives.Tanh(x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MathF.Tanh(x[i]), destination[i]);
                }
            }, x);
        }

        [Theory]
        [MemberData(nameof(VectorLengthAndIteratedRange), new object[] { -11f, 11f, 0.2f })]
        public static void Tanh_ValueRange(int vectorLengths, float element)
        {
            float[] x = new float[vectorLengths];
            float[] dest = new float[vectorLengths];

            x.AsSpan().Fill(element);
            TensorPrimitives.Tanh(x, dest);

            float expected = MathF.Tanh(element);
            foreach (float actual in dest)
            {
                AssertEqualTolerance(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void Tanh_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> x = CreateAndFillTensor(tensorLength);
            using BoundedMemory<float> destination = CreateTensor(tensorLength - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Tanh(x, destination));
        }

        [Fact]
        public static void Tanh_ThrowsForOverlapppingInputsWithOutputs()
        {
            float[] array = new float[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Tanh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.Tanh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion
    }
}
