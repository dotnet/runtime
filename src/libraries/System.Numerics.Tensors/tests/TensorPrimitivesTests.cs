// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

// Shared helpers and Facts/Theories used by both generic methods on .NET Core and non-generic methods on .NET Framework

namespace System.Numerics.Tensors.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97295", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsNotMonoInterpreter))]
    public abstract class TensorPrimitivesTests<T> where T : unmanaged, IEquatable<T>
    {
        #region Abstract Methods Under Test
        protected abstract void Abs(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract void Add(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract void Add(ReadOnlySpan<T> x, T y, Span<T> destination);
        protected abstract void AddMultiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination);
        protected abstract void AddMultiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination);
        protected abstract void AddMultiply(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination);
        protected abstract void Cosh(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract T CosineSimilarity(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
        protected abstract T Distance(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
        protected abstract void Divide(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract void Divide(ReadOnlySpan<T> x, T y, Span<T> destination);
        protected abstract T Dot(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
        protected abstract void Exp(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract int IndexOfMax(ReadOnlySpan<T> x);
        protected abstract int IndexOfMaxMagnitude(ReadOnlySpan<T> x);
        protected abstract int IndexOfMin(ReadOnlySpan<T> x);
        protected abstract int IndexOfMinMagnitude(ReadOnlySpan<T> x);
        protected abstract void Log(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract void Log2(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract T Max(ReadOnlySpan<T> x);
        protected abstract void Max(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract T Max(T x, T y);
        protected abstract T MaxMagnitude(ReadOnlySpan<T> x);
        protected abstract void MaxMagnitude(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract T MaxMagnitude(T x, T y);
        protected abstract T Min(ReadOnlySpan<T> x);
        protected abstract void Min(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract T Min(T x, T y);
        protected abstract T MinMagnitude(ReadOnlySpan<T> x);
        protected abstract void MinMagnitude(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract T MinMagnitude(T x, T y);
        protected abstract void Multiply(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract void Multiply(ReadOnlySpan<T> x, T y, Span<T> destination);
        protected abstract void MultiplyAdd(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination);
        protected abstract void MultiplyAdd(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination);
        protected abstract void MultiplyAdd(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination);
        protected abstract void Negate(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract T Norm(ReadOnlySpan<T> x);
        protected abstract T Product(ReadOnlySpan<T> x);
        protected abstract T ProductOfSums(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
        protected abstract T ProductOfDifferences(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
        protected abstract void Sigmoid(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract void Sinh(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract void SoftMax(ReadOnlySpan<T> x, Span<T> destination);
        protected abstract void Subtract(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        protected abstract void Subtract(ReadOnlySpan<T> x, T y, Span<T> destination);
        protected abstract T Sum(ReadOnlySpan<T> x);
        protected abstract T SumOfMagnitudes(ReadOnlySpan<T> x);
        protected abstract T SumOfSquares(ReadOnlySpan<T> x);
        protected abstract void Tanh(ReadOnlySpan<T> x, Span<T> destination);
        #endregion

        #region Abstract Validation
        protected abstract T Abs(T x);
        protected abstract T Add(T x, T y);
        protected abstract T AddMultiply(T x, T y, T z);
        protected abstract T Cosh(T x);
        protected abstract T Divide(T x, T y);
        protected abstract T Exp(T x);
        protected abstract T Log(T x);
        protected abstract T Log2(T x);
        protected abstract T Multiply(T x, T y);
        protected abstract T Sinh(T x);
        protected abstract T Sqrt(T x);
        protected abstract T Subtract(T x, T y);
        protected abstract T Tanh(T x);

        protected abstract T NaN { get; }
        protected abstract T NegativeZero { get; }
        protected abstract T Zero { get; }
        protected abstract T One { get; }
        protected abstract T NegativeOne  { get; }
        protected abstract T MinValue { get; }
        #endregion

        #region Test Utilities

        public delegate void SpanDestinationDelegate(ReadOnlySpan<T> x, Span<T> destination);
        public delegate void SpanSpanDestinationDelegate(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);
        public delegate void SpanScalarDestinationDelegate<T1, T2, T3>(ReadOnlySpan<T1> x, T2 y, Span<T3> destination);
        public delegate void ScalarSpanDestinationDelegate(T x, ReadOnlySpan<T> y, Span<T> destination);
        public delegate void SpanSpanSpanDestinationDelegate(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination);
        public delegate void SpanSpanScalarDestinationDelegate(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination);
        public delegate void SpanScalarSpanDestinationDelegate(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination);
        public delegate void SpanDestinationDestinationDelegate(ReadOnlySpan<T> x, Span<T> destination1, Span<T> destination2);

        protected virtual bool IsFloatingPoint => typeof(T) == typeof(float) || typeof(T) == typeof(double);

        protected abstract T ConvertFromSingle(float f);

        protected abstract IEnumerable<T> GetSpecialValues();

        /// <summary>
        /// Loads a variety of special values (e.g. NaN) into random positions in <paramref name="x"/>
        /// and related values into the corresponding positions in <paramref name="y"/>.
        /// </summary>
        protected abstract void SetSpecialValues(Span<T> x, Span<T> y);

        protected abstract T NextRandom();

        protected abstract void AssertEqualTolerance(T expected, T actual, T? tolerance = null);

        protected abstract IEnumerable<(int Length, T Element)> VectorLengthAndIteratedRange(T min, T max, T increment);

        protected Random Random { get; } = new Random(42);

        protected BoundedMemory<T> CreateTensor(int size) => BoundedMemory.Allocate<T>(size);

        public BoundedMemory<T> CreateAndFillTensor(int size)
        {
            BoundedMemory<T> tensor = CreateTensor(size);
            FillTensor(tensor);
            return tensor;
        }

        protected void FillTensor(Span<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = NextRandom();
            }
        }

        protected void FillTensor(Span<T> span, T avoid)
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = NextRandom(avoid);
            }
        }

        protected T NextRandom(T avoid)
        {
            while (true)
            {
                T value = NextRandom();
                if (!value.Equals(avoid))
                {
                    return value;
                }
            }
        }

        /// <summary>
        /// Runs the specified action for each special value. Before the action is invoked,
        /// the value is stored into a random position in <paramref name="x"/>, and the original
        /// value is subsequently restored.
        /// </summary>
        protected void RunForEachSpecialValue(Action action, BoundedMemory<T> x)
        {
            foreach (T value in GetSpecialValues())
            {
                int pos = Random.Next(x.Length);
                T orig = x[pos];
                x[pos] = value;

                action();

                x[pos] = orig;
            }
        }
        #endregion

        #region Abs
        [Fact]
        public void Abs_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                FillTensor(x, MinValue);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Abs(x, destination);

                for (int i = 0; i < x.Length; i++)
                {
                    AssertEqualTolerance(Abs(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Abs_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                FillTensor(x, MinValue);
                T[] xOrig = x.Span.ToArray();

                Abs(x, x);

                for (int i = 0; i < x.Length; i++)
                {
                    AssertEqualTolerance(Abs(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Abs_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Abs(x, destination));
            });
        }

        [Fact]
        public void Abs_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Abs(array.AsSpan(1, 5), array.AsSpan(0, 5)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Abs(array.AsSpan(1, 5), array.AsSpan(2, 5)));
        }
        #endregion

        #region Add
        [Fact]
        public void Add_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Add(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Add_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Add(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Add_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Add(x, y, destination));
                Assert.Throws<ArgumentException>(() => Add(y, x, destination));
            });
        }

        [Fact]
        public void Add_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Add(x, y, destination));
            });
        }

        [Fact]
        public void Add_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(4, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), array.AsSpan(5, 2), array.AsSpan(6, 2)));
        }

        [Fact]
        public void Add_TensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Add(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(x[i], y), destination[i]);
                }
            });
        }

        [Fact]
        public void Add_TensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                Add(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(xOrig[i], y), x[i]);
                }
            });
        }

        [Fact]
        public void Add_TensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Add(x, y, destination));
            });
        }

        [Fact]
        public void Add_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Add(array.AsSpan(1, 2), default(T), array.AsSpan(2, 2)));
        }
        #endregion

        #region AddMultiply
        [Fact]
        public void AddMultiply_ThreeTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> multiplier = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                AddMultiply(x, y, multiplier, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(x[i], y[i], multiplier[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_ThreeTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                AddMultiply(x, x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(xOrig[i], xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_ThreeTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => AddMultiply(x, y, z, destination));
                Assert.Throws<ArgumentException>(() => AddMultiply(x, z, y, destination));
                Assert.Throws<ArgumentException>(() => AddMultiply(z, x, y, destination));
            });
        }

        [Fact]
        public void AddMultiply_ThreeTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> multiplier = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(x, y, multiplier, destination));
            });
        }

        [Fact]
        public void AddMultiply_ThreeTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(5, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }

        [Fact]
        public void AddMultiply_TensorTensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T multiplier = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                AddMultiply(x, y, multiplier, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(x[i], y[i], multiplier), destination[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_TensorTensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T multiplier = NextRandom();

                AddMultiply(x, x, multiplier, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(xOrig[i], xOrig[i], multiplier), x[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_TensorTensorScalar_ThrowsForMismatchedLengths_x_y()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                T multiplier = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => AddMultiply(x, y, multiplier, destination));
                Assert.Throws<ArgumentException>(() => AddMultiply(y, x, multiplier, destination));
            });
        }

        [Fact]
        public void AddMultiply_TensorTensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T multiplier = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(x, y, multiplier, destination));
            });
        }

        [Fact]
        public void AddMultiply_TensorTensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(5, 2)));
        }

        [Fact]
        public void AddMultiply_TensorScalarTensor()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> multiplier = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                AddMultiply(x, y, multiplier, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(x[i], y, multiplier[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_TensorScalarTensor_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                AddMultiply(x, y, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(AddMultiply(xOrig[i], y, xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void AddMultiply_TensorScalarTensor_ThrowsForMismatchedLengths_x_z()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => AddMultiply(x, y, z, destination));
                Assert.Throws<ArgumentException>(() => AddMultiply(z, y, x, destination));
            });
        }

        [Fact]
        public void AddMultiply_TensorScalarTensor_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> multiplier = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(x, y, multiplier, destination));
            });
        }

        [Fact]
        public void AddMultiply_TensorScalarTensor_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => AddMultiply(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Cosh
        [Fact]
        public void Cosh_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Cosh(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Cosh(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Cosh_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Cosh(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Cosh(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Cosh_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Cosh(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Cosh(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Cosh_ValueRange()
        {
            if (!IsFloatingPoint) return;

            Assert.All(VectorLengthAndIteratedRange(ConvertFromSingle(-100f), ConvertFromSingle(100f), ConvertFromSingle(3f)), arg =>
            {
                T[] x = new T[arg.Length];
                T[] dest = new T[arg.Length];

                x.AsSpan().Fill(arg.Element);
                Cosh(x, dest);

                T expected = Cosh(arg.Element);
                foreach (T actual in dest)
                {
                    AssertEqualTolerance(expected, actual);
                }
            });
        }

        [Fact]
        public void Cosh_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Cosh(x, destination));
            });
        }

        [Fact]
        public void Cosh_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Cosh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Cosh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region CosineSimilarity
        [Fact]
        public void CosineSimilarity_ThrowsForMismatchedLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);

                Assert.Throws<ArgumentException>(() => CosineSimilarity(x, y));
                Assert.Throws<ArgumentException>(() => CosineSimilarity(y, x));
            });
        }

        [Fact]
        public void CosineSimilarity_ThrowsForEmpty()
        {
            if (!IsFloatingPoint) return;

            Assert.Throws<ArgumentException>(() => CosineSimilarity(ReadOnlySpan<T>.Empty, ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => CosineSimilarity(ReadOnlySpan<T>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => CosineSimilarity(CreateTensor(1), ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void CosineSimilarity_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                T dot = default, squareX = default, squareY = default;
                for (int i = 0; i < x.Length; i++)
                {
                    dot = Add(dot, Multiply(x[i], y[i]));
                    squareX = Add(squareX, Multiply(x[i], x[i]));
                    squareY = Add(squareY, Multiply(y[i], y[i]));
                }

                AssertEqualTolerance(Divide(dot, Multiply(Sqrt(squareX), Sqrt(squareY))), CosineSimilarity(x, y));
            });
        }
        #endregion

        #region Distance
        [Fact]
        public void Distance_ThrowsForEmpty()
        {
            if (!IsFloatingPoint) return;

            Assert.Throws<ArgumentException>(() => Distance(ReadOnlySpan<T>.Empty, ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => Distance(ReadOnlySpan<T>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => Distance(CreateTensor(1), ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void Distance_ThrowsForMismatchedLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);

                Assert.Throws<ArgumentException>(() => Distance(x, y));
                Assert.Throws<ArgumentException>(() => Distance(y, x));
            });
        }

        [Fact]
        public void Distance_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                T distance = default;
                for (int i = 0; i < x.Length; i++)
                {
                    distance = Add(distance, Multiply(Subtract(x[i], y[i]), Subtract(x[i], y[i])));
                }

                AssertEqualTolerance(Sqrt(distance), Distance(x, y));
            });
        }
        #endregion

        #region Divide
        [Fact]
        public void Divide_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateTensor(tensorLength);
                FillTensor(y, Zero);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Divide(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Divide_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                FillTensor(x, Zero);
                T[] xOrig = x.Span.ToArray();

                Divide(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Divide_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Divide(x, y, destination));
                Assert.Throws<ArgumentException>(() => Divide(y, x, destination));
            });
        }

        [Fact]
        public void Divide_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Divide(x, y, destination));
            });
        }

        [Fact]
        public void Divide_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Fact]
        public void Divide_TensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom(default);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Divide(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(x[i], y), destination[i]);
                }
            });
        }

        [Fact]
        public void Divide_TensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom(default);

                Divide(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(xOrig[i], y), x[i]);
                }
            });
        }

        [Fact]
        public void Divide_TensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Divide(x, y, destination));
            });
        }

        [Fact]
        public void Divide_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Divide(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Dot
        [Fact]
        public void Dot_ThrowsForMismatchedLengths_x_y()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);

                Assert.Throws<ArgumentException>(() => Dot(x, y));
                Assert.Throws<ArgumentException>(() => Dot(y, x));
            });
        }

        [Fact]
        public void Dot_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                T dot = default;
                for (int i = 0; i < x.Length; i++)
                {
                    dot = Add(dot, Multiply(x[i], y[i]));
                }

                AssertEqualTolerance(dot, Dot(x, y));
            });
        }
        #endregion

        #region Exp
        [Fact]
        public void Exp_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Exp(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Exp(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Exp_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Exp(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Exp(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Exp_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Exp(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Exp(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Exp_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Exp(x, destination));
            });
        }

        [Fact]
        public void Exp_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Exp(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Exp(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region IndexOfMax
        [Fact]
        public void IndexOfMax_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, IndexOfMax(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void IndexOfMax_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = Enumerable.Max(MemoryMarshal.ToEnumerable<T>(x.Memory));
                    int actual = IndexOfMax(x.Span);
                    Assert.True(actual == expected || (actual < expected && x[actual].Equals(x[expected])), $"{tensorLength} {actual} {expected}     {string.Join(",", MemoryMarshal.ToEnumerable<T>(x.Memory))}");
                }
            });
        }

        [Fact]
        public void IndexOfMax_FirstNaNReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = ConvertFromSingle(float.NaN);
                    x[tensorLength - 1] = ConvertFromSingle(float.NaN);
                    Assert.Equal(expected, IndexOfMax(x.Span));
                }
            });
        }

        [Fact]
        public void IndexOfMax_Negative0LesserThanPositive0()
        {
            if (!IsFloatingPoint) return;

            Assert.Equal(1, IndexOfMax([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(0, IndexOfMax([ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f)]));
            Assert.Equal(4, IndexOfMax([ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(+0f), ConvertFromSingle(+0f), ConvertFromSingle(+0f)]));
            Assert.Equal(0, IndexOfMax([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMax([ConvertFromSingle(-1),  ConvertFromSingle(-0f)]));
            Assert.Equal(2, IndexOfMax([ConvertFromSingle(-1),  ConvertFromSingle(-0f), ConvertFromSingle(1f)]));
        }
        #endregion

        #region IndexOfMaxMagnitude
        [Fact]
        public void IndexOfMaxMagnitude_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, IndexOfMaxMagnitude(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void IndexOfMaxMagnitude_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateTensor(tensorLength);
                    FillTensor(x, MinValue);

                    T max = x[0];
                    for (int i = 0; i < x.Length; i++)
                    {
                        int compared = Comparer<T>.Default.Compare(Abs(x[i]), Abs(max));
                        if (compared > 0 || (compared == 0 && EqualityComparer<T>.Default.Equals(x[i], max)))
                        {
                            max = x[i];
                        }
                    }
                    x[expected] = max;

                    int actual = IndexOfMaxMagnitude(x.Span);

                    if (actual != expected)
                    {
                        Assert.True(actual < expected || Comparer<T>.Default.Compare(x[actual], x[expected]) > 0, $"{tensorLength} {actual} {expected}     {string.Join(",", MemoryMarshal.ToEnumerable<T>(x.Memory))}");
                        if (IsFloatingPoint)
                        {
                            AssertEqualTolerance(Abs(x[expected]), Abs(x[actual]));
                        }
                        else
                        {
                            Assert.Equal(Abs(x[expected]), Abs(x[actual]));
                        }
                    }
                }
            });
        }

        [Fact]
        public void IndexOfMaxMagnitude_FirstNaNReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = ConvertFromSingle(float.NaN);
                    x[tensorLength - 1] = ConvertFromSingle(float.NaN);
                    Assert.Equal(expected, IndexOfMaxMagnitude(x));
                }
            });
        }

        [Fact]
        public void IndexOfMaxMagnitude_Negative0LesserThanPositive0()
        {
            if (!IsFloatingPoint) return;

            Assert.Equal(0, IndexOfMaxMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMaxMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(1, IndexOfMaxMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(+0f), ConvertFromSingle(+0f), ConvertFromSingle(+0f)]));
            Assert.Equal(0, IndexOfMaxMagnitude([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(0, IndexOfMaxMagnitude([ConvertFromSingle(-1),  ConvertFromSingle(-0f)]));
            Assert.Equal(2, IndexOfMaxMagnitude([ConvertFromSingle(-1),  ConvertFromSingle(-0f), ConvertFromSingle(1f)]));
        }
        #endregion

        #region IndexOfMin
        [Fact]
        public void IndexOfMin_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, IndexOfMin(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void IndexOfMin_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = Enumerable.Min(MemoryMarshal.ToEnumerable<T>(x.Memory));
                    int actual = IndexOfMin(x.Span);
                    Assert.True(actual == expected || (actual < expected && x[actual].Equals(x[expected])), $"{tensorLength} {actual} {expected}     {string.Join(",", MemoryMarshal.ToEnumerable<T>(x.Memory))}");
                }
            });
        }

        [Fact]
        public void IndexOfMin_FirstNaNReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = ConvertFromSingle(float.NaN);
                    x[tensorLength - 1] = ConvertFromSingle(float.NaN);
                    Assert.Equal(expected, IndexOfMin(x));
                }
            });
        }

        [Fact]
        public void IndexOfMin_Negative0LesserThanPositive0()
        {
            if (!IsFloatingPoint) return;

            Assert.Equal(0, IndexOfMin([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(1, IndexOfMin([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMin([ConvertFromSingle(+0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f)]));
            Assert.Equal(0, IndexOfMin([ConvertFromSingle(-1),  ConvertFromSingle(-0f)]));
            Assert.Equal(0, IndexOfMin([ConvertFromSingle(-1),  ConvertFromSingle(-0f), ConvertFromSingle(1f)]));
        }
        #endregion

        #region IndexOfMinMagnitude
        [Fact]
        public void IndexOfMinMagnitude_ReturnsNegative1OnEmpty()
        {
            Assert.Equal(-1, IndexOfMinMagnitude(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void IndexOfMinMagnitude_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateTensor(tensorLength);
                    FillTensor(x, MinValue);

                    T min = x[0];
                    for (int i = 0; i < x.Length; i++)
                    {
                        int compared = Comparer<T>.Default.Compare(Abs(x[i]), Abs(min));
                        if (compared < 0 || (compared == 0 && Comparer<T>.Default.Compare(x[i], min) < 0))
                        {
                            min = x[i];
                        }
                    }

                    x[expected] = min;
                    int actual = IndexOfMinMagnitude(x.Span);

                    if (actual != expected)
                    {
                        Assert.True(actual < expected || Comparer<T>.Default.Compare(x[actual], x[expected]) < 0, $"{tensorLength} {actual} {expected}     {string.Join(",", MemoryMarshal.ToEnumerable<T>(x.Memory))}");
                        if (IsFloatingPoint)
                        {
                            AssertEqualTolerance(Abs(x[expected]), Abs(x[actual]));
                        }
                        else
                        {
                            Assert.Equal(Abs(x[expected]), Abs(x[actual]));
                        }
                    }
                }
            });
        }

        [Fact]
        public void IndexOfMinMagnitude_FirstNaNReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                    x[expected] = ConvertFromSingle(float.NaN);
                    x[tensorLength - 1] = ConvertFromSingle(float.NaN);
                    Assert.Equal(expected, IndexOfMinMagnitude(x));
                }
            });
        }

        [Fact]
        public void IndexOfMinMagnitude_Negative0LesserThanPositive0()
        {
            if (!IsFloatingPoint) return;

            Assert.Equal(0, IndexOfMinMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f)]));
            Assert.Equal(0, IndexOfMinMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(1, IndexOfMinMagnitude([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMinMagnitude([ConvertFromSingle(+0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMinMagnitude([ConvertFromSingle(-1),  ConvertFromSingle(-0f)]));
            Assert.Equal(1, IndexOfMinMagnitude([ConvertFromSingle(-1),  ConvertFromSingle(-0f), ConvertFromSingle(1f)]));
        }
        #endregion

        #region Log
        [Fact]
        public void Log_AllValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Log(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Log(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Log_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Log(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Log(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Log_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Log(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Log(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Log_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Log(x, destination));
            });
        }

        [Fact]
        public void Log_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Log(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Log(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Log2
        [Fact]
        public void Log2_AllValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Log2(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Log2(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Log2_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Log2(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Log2(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Log2_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Log2(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Log2(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Log2_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Log2(x, destination));
            });
        }

        [Fact]
        public void Log2_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Log2(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Log2(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Max
        [Fact]
        public void Max_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => Max(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void Max_Tensor()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                Assert.Equal(Enumerable.Max(MemoryMarshal.ToEnumerable<T>(x.Memory)), Max(x));

                T max = x.Span[0];
                foreach (T f in x.Span)
                {
                    max = Max(max, f);
                }

                Assert.Equal(max, Max(x));

                // TODO: Put a variant of this back once we have IndexOf routines
                // Assert.Equal(SingleToUInt32(x[IndexOfMax(x)]), SingleToUInt32(Max(x)));
            });
        }

        [Fact]
        public void Max_Tensor_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    T max = x.Span[0];
                    foreach (T f in x.Span)
                    {
                        max = Max(max, f);
                    }

                    Assert.Equal(max, Max(x));

                    // TODO: Put a variant of this back once we have IndexOf routines
                    // Assert.Equal(SingleToUInt32(x[IndexOfMax(x)]), SingleToUInt32(Max(x)));
                }, x);
            });
        }

        [Fact]
        public void Max_Tensor_NanReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    FillTensor(x);
                    x[expected] = NaN;
                    Assert.Equal(NaN, Max(x));
                }
            });
        }

        [Fact]
        public void Max_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(ConvertFromSingle(+0f), Max([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(ConvertFromSingle(+0f), Max([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(-0f), Max([ConvertFromSingle(-1), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(1), Max([ConvertFromSingle(-1), ConvertFromSingle(-0f), ConvertFromSingle(1)]));
        }

        [Fact]
        public void Max_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Max(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Max(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Max_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

                Max(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Max(xOrig[i], y[i]), x[i]);
                }

                xOrig.AsSpan().CopyTo(x.Span);
                yOrig.AsSpan().CopyTo(y.Span);

                Max(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Max(x[i], yOrig[i]), y[i]);
                }
            });
        }

        [Fact]
        public void Max_TwoTensors_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                SetSpecialValues(x, y);

                Max(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Max(x[i], y[i]), destination[i]);
                }

                Max(y, x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Max(y[i], x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Max_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Max(x, y, destination));
                Assert.Throws<ArgumentException>(() => Max(y, x, destination));
            });
        }

        [Fact]
        public void Max_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Max(x, y, destination));
            });
        }

        [Fact]
        public void Max_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Max(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region MaxMagnitude
        [Fact]
        public void MaxMagnitude_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => MaxMagnitude(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void MaxMagnitude_Tensor()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T maxMagnitude = x[0];
                foreach (T f in x.Span)
                {
                    maxMagnitude = MaxMagnitude(maxMagnitude, f);
                }

                Assert.Equal(maxMagnitude, MaxMagnitude(x));

                // TODO: Put a variant of this back once we have IndexOf routines
                // Assert.Equal(SingleToUInt32(x[IndexOfMaxMagnitude(x)]), SingleToUInt32(MaxMagnitude(x)));
            });
        }

        [Fact]
        public void MaxMagnitude_Tensor_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    T maxMagnitude = x[0];
                    foreach (T f in x.Span)
                    {
                        maxMagnitude = MaxMagnitude(maxMagnitude, f);
                    }

                    Assert.Equal(maxMagnitude, MaxMagnitude(x));

                    // TODO: Put a variant of this back once we have IndexOf routines
                    // Assert.Equal(SingleToUInt32(x[IndexOfMaxMagnitude(x)]), SingleToUInt32(MaxMagnitude(x)));
                }, x);
            });
        }

        [Fact]
        public void MaxMagnitude_Tensor_NanReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    FillTensor(x);
                    x[expected] = ConvertFromSingle(float.NaN);
                    Assert.Equal(ConvertFromSingle(float.NaN), MaxMagnitude(x));
                }
            });
        }

        [Fact]
        public void MaxMagnitude_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(ConvertFromSingle(0), MaxMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(ConvertFromSingle(0), MaxMagnitude([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(-1), MaxMagnitude([ConvertFromSingle(-1), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(1), MaxMagnitude([ConvertFromSingle(-1), ConvertFromSingle(-0f), ConvertFromSingle(1)]));
            Assert.Equal(ConvertFromSingle(0), MaxMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(0f)]));
            Assert.Equal(ConvertFromSingle(1), MaxMagnitude( [ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-0f), ConvertFromSingle(-1), ConvertFromSingle(-0f), ConvertFromSingle(0f), ConvertFromSingle(1)]));
        }

        [Fact]
        public void MaxMagnitude_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                MaxMagnitude(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MaxMagnitude(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MaxMagnitude_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

                MaxMagnitude(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MaxMagnitude(xOrig[i], y[i]), x[i]);
                }

                xOrig.AsSpan().CopyTo(x.Span);
                yOrig.AsSpan().CopyTo(y.Span);

                MaxMagnitude(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MaxMagnitude(x[i], yOrig[i]), y[i]);
                }
            });
        }

        [Fact]
        public void MaxMagnitude_TwoTensors_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                SetSpecialValues(x, y);

                MaxMagnitude(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MaxMagnitude(x[i], y[i]), destination[i]);
                }

                MaxMagnitude(y, x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MaxMagnitude(y[i], x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MaxMagnitude_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => MaxMagnitude(x, y, destination));
                Assert.Throws<ArgumentException>(() => MaxMagnitude(y, x, destination));
            });
        }

        [Fact]
        public void MaxMagnitude_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => MaxMagnitude(x, y, destination));
            });
        }

        [Fact]
        public void MaxMagnitude_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MaxMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Min
        [Fact]
        public void Min_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => Min(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void Min_Tensor()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                Assert.Equal(Enumerable.Min(MemoryMarshal.ToEnumerable<T>(x.Memory)), Min(x));

                T min = ConvertFromSingle(float.PositiveInfinity);
                foreach (T f in x.Span)
                {
                    min = Min(min, f);
                }

                Assert.Equal(min, Min(x));

                // TODO: Put a variant of this back once we have IndexOf routines
                // Assert.Equal(SingleToUInt32(x[IndexOfMin(x)]), SingleToUInt32(Min(x)));
            });
        }

        [Fact]
        public void Min_Tensor_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    T min = ConvertFromSingle(float.PositiveInfinity);
                    foreach (T f in x.Span)
                    {
                        min = Min(min, f);
                    }

                    Assert.Equal(min, Min(x));

                    // TODO: Put a variant of this back once we have IndexOf routines
                    // Assert.Equal(SingleToUInt32(x[IndexOfMin(x)]), SingleToUInt32(Min(x)));
                }, x);
            });
        }

        [Fact]
        public void Min_Tensor_NanReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    FillTensor(x);
                    x[expected] = ConvertFromSingle(float.NaN);
                    Assert.Equal(ConvertFromSingle(float.NaN), Min(x));
                }
            });
        }

        [Fact]
        public void Min_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(ConvertFromSingle(-0f), Min([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(ConvertFromSingle(-0f), Min([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(-1), Min([ConvertFromSingle(-1), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(-1), Min([ConvertFromSingle(-1), ConvertFromSingle(-0f), ConvertFromSingle(1)]));
        }

        [Fact]
        public void Min_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Min(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Min(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Min_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

                Min(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Min(xOrig[i], y[i]), x[i]);
                }

                xOrig.AsSpan().CopyTo(x.Span);
                yOrig.AsSpan().CopyTo(y.Span);

                Min(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Min(x[i], yOrig[i]), y[i]);
                }
            });
        }

        [Fact]
        public void Min_TwoTensors_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                SetSpecialValues(x, y);

                Min(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Min(x[i], y[i]), destination[i]);
                }

                Min(y, x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Min(y[i], x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Min_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Min(x, y, destination));
                Assert.Throws<ArgumentException>(() => Min(y, x, destination));
            });
        }

        [Fact]
        public void Min_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Min(x, y, destination));
            });
        }

        [Fact]
        public void Min_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Min(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region MinMagnitude
        [Fact]
        public void MinMagnitude_Tensor_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => MinMagnitude(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void MinMagnitude_Tensor()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T minMagnitude = x[0];
                foreach (T f in x.Span)
                {
                    minMagnitude = MinMagnitude(minMagnitude, f);
                }

                Assert.Equal(minMagnitude, MinMagnitude(x));

                // TODO: Put a variant of this back once we have IndexOf routines
                // Assert.Equal(SingleToUInt32(x[IndexOfMinMagnitude(x)]), SingleToUInt32(MinMagnitude(x)));
            });
        }

        [Fact]
        public void MinMagnitude_Tensor_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    T minMagnitude = x[0];
                    foreach (T f in x.Span)
                    {
                        minMagnitude = MinMagnitude(minMagnitude, f);
                    }

                    Assert.Equal(minMagnitude, MinMagnitude(x));

                    // TODO: Put a variant of this back once we have IndexOf routines
                    // Assert.Equal(SingleToUInt32(x[IndexOfMinMagnitude(x)]), SingleToUInt32(MinMagnitude(x)));
                }, x);
            });
        }

        [Fact]
        public void MinMagnitude_Tensor_NanReturned()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                foreach (int expected in new[] { 0, tensorLength / 2, tensorLength - 1 })
                {
                    FillTensor(x);
                    x[expected] = ConvertFromSingle(float.NaN);
                    Assert.Equal(ConvertFromSingle(float.NaN), MinMagnitude(x));
                }
            });
        }

        [Fact]
        public void MinMagnitude_Tensor_Negative0LesserThanPositive0()
        {
            Assert.Equal(ConvertFromSingle(0), MinMagnitude([ConvertFromSingle(-0f), ConvertFromSingle(+0f)]));
            Assert.Equal(ConvertFromSingle(0), MinMagnitude([ConvertFromSingle(+0f), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(0), MinMagnitude([ConvertFromSingle(-1), ConvertFromSingle(-0f)]));
            Assert.Equal(ConvertFromSingle(0), MinMagnitude([ConvertFromSingle(-1), ConvertFromSingle(-0f), ConvertFromSingle(1)]));
        }

        [Fact]
        public void MinMagnitude_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                MinMagnitude(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MinMagnitude(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MinMagnitude_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray(), yOrig = y.Span.ToArray();

                MinMagnitude(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MinMagnitude(xOrig[i], y[i]), x[i]);
                }

                xOrig.AsSpan().CopyTo(x.Span);
                yOrig.AsSpan().CopyTo(y.Span);

                MinMagnitude(x, y, y);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MinMagnitude(x[i], yOrig[i]), y[i]);
                }
            });
        }

        [Fact]
        public void MinMagnitude_TwoTensors_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                SetSpecialValues(x, y);

                MinMagnitude(x, y, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MinMagnitude(x[i], y[i]), destination[i]);
                }

                MinMagnitude(y, x, destination);
                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(MinMagnitude(y[i], x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MinMagnitude_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => MinMagnitude(x, y, destination));
                Assert.Throws<ArgumentException>(() => MinMagnitude(y, x, destination));
            });
        }

        [Fact]
        public void MinMagnitude_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => MinMagnitude(x, y, destination));
            });
        }

        [Fact]
        public void MinMagnitude_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MinMagnitude(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Multiply
        [Fact]
        public void Multiply_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Multiply(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Multiply_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Multiply(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(xOrig[i], xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Multiply_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Multiply(x, y, destination));
                Assert.Throws<ArgumentException>(() => Multiply(y, x, destination));
            });
        }

        [Fact]
        public void Multiply_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(x, y, destination));
            });
        }

        [Fact]
        public void Multiply_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Fact]
        public void Multiply_TensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Multiply(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(x[i], y), destination[i]);
                }
            });
        }

        [Fact]
        public void Multiply_TensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                Multiply(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(xOrig[i], y), x[i]);
                }
            });
        }

        [Fact]
        public void Multiply_TensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(x, y, destination));
            });
        }

        [Fact]
        public void Multiply_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Multiply(array.AsSpan(1, 2), default(T), array.AsSpan(2, 2)));
        }
        #endregion

        #region MultiplyAdd
        [Fact]
        public void MultiplyAdd_ThreeTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> addend = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                MultiplyAdd(x, y, addend, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(x[i], y[i]), addend[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_ThreeTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                MultiplyAdd(x, x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(xOrig[i], xOrig[i]), xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_ThreeTensors_ThrowsForMismatchedLengths_x_y()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> z = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => MultiplyAdd(x, y, z, destination));
                Assert.Throws<ArgumentException>(() => MultiplyAdd(x, z, y, destination));
                Assert.Throws<ArgumentException>(() => MultiplyAdd(z, x, y, destination));
            });
        }

        [Fact]
        public void MultiplyAdd_ThreeTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> addend = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(x, y, addend, destination));
            });
        }

        [Fact]
        public void MultiplyAdd_ThreeTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(5, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(6, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(7, 2), array.AsSpan(8, 2)));
        }

        [Fact]
        public void MultiplyAdd_TensorTensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T addend = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                MultiplyAdd(x, y, addend, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(x[i], y[i]), addend), destination[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_TensorTensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T addend = NextRandom();

                MultiplyAdd(x, x, addend, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(xOrig[i], xOrig[i]), addend), x[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_TensorTensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                T addend = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(x, y, addend, destination));
            });
        }

        [Fact]
        public void MultiplyAdd_TensorTensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), array.AsSpan(4, 2), default(T), array.AsSpan(5, 2)));
        }

        [Fact]
        public void MultiplyAdd_TensorScalarTensor()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> addend = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                MultiplyAdd(x, y, addend, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(x[i], y), addend[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_TensorScalarTensor_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                MultiplyAdd(x, y, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Add(Multiply(xOrig[i], y), xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void MultiplyAdd_TensorScalarTensor_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> addend = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(x, y, addend, destination));
            });
        }

        [Fact]
        public void MultiplyAdd_TensorScalarTensor_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => MultiplyAdd(array.AsSpan(1, 2), default(T), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }
        #endregion

        #region Negate
        [Fact]
        public void Negate_AllLengths()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Negate(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(x[i], NegativeOne), destination[i]);
                }
            });
        }

        [Fact]
        public void Negate_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Negate(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Multiply(xOrig[i], NegativeOne), x[i]);
                }
            });
        }

        [Fact]
        public void Negate_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Negate(x, destination));
            });
        }

        [Fact]
        public void Negate_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Negate(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Negate(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Norm
        [Fact]
        public void Norm_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T sumOfSquares = Zero;
                for (int i = 0; i < x.Length; i++)
                {
                    sumOfSquares = Add(sumOfSquares, Multiply(x[i], x[i]));
                }

                AssertEqualTolerance(Sqrt(sumOfSquares), Norm(x));
            });
        }
        #endregion

        #region Product
        [Fact]
        public void Product_ThrowsForEmpty()
        {
            Assert.Throws<ArgumentException>(() => Product(ReadOnlySpan<T>.Empty));
        }

        [Fact]
        public void Product_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T f = x[0];
                for (int i = 1; i < x.Length; i++)
                {
                    f = Multiply(f, x[i]);
                }

                AssertEqualTolerance(f, Product(x));
            });
        }
        #endregion

        #region ProductOfDifferences
        [Fact]
        public void ProductOfDifferences_ThrowsForEmptyAndMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => ProductOfDifferences(ReadOnlySpan<T>.Empty, ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => ProductOfDifferences(ReadOnlySpan<T>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => ProductOfDifferences(CreateTensor(1), ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => ProductOfDifferences(CreateTensor(44), CreateTensor(43)));
            Assert.Throws<ArgumentException>(() => ProductOfDifferences(CreateTensor(43), CreateTensor(44)));
        }

        [Fact]
        public void ProductOfDifferences_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                T f = Subtract(x[0], y[0]);
                for (int i = 1; i < x.Length; i++)
                {
                    f = Multiply(f, Subtract(x[i], y[i]));
                }
                AssertEqualTolerance(f, ProductOfDifferences(x, y));
            });
        }
        #endregion

        #region ProductOfSums
        [Fact]
        public void ProductOfSums_ThrowsForEmptyAndMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() => ProductOfSums(ReadOnlySpan<T>.Empty, ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => ProductOfSums(ReadOnlySpan<T>.Empty, CreateTensor(1)));
            Assert.Throws<ArgumentException>(() => ProductOfSums(CreateTensor(1), ReadOnlySpan<T>.Empty));
            Assert.Throws<ArgumentException>(() => ProductOfSums(CreateTensor(44), CreateTensor(43)));
            Assert.Throws<ArgumentException>(() => ProductOfSums(CreateTensor(43), CreateTensor(44)));
        }

        [Fact]
        public void ProductOfSums_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);

                T f = Add(x[0], y[0]);
                for (int i = 1; i < x.Length; i++)
                {
                    f = Multiply(f, Add(x[i], y[i]));
                }
                AssertEqualTolerance(f, ProductOfSums(x, y));
            });
        }
        #endregion

        #region Sigmoid
        [Fact]
        public void Sigmoid_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Sigmoid(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(One, Add(One, Exp(Multiply(x[i], NegativeOne)))), destination[i]);
                }
            });
        }

        [Fact]
        public void Sigmoid_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Sigmoid(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(One, Add(One, Exp(Multiply(xOrig[i], NegativeOne)))), x[i]);
                }
            });
        }

        [Fact]
        public void Sigmoid_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Sigmoid(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Divide(One, Add(One, Exp(Multiply(x[i], NegativeOne)))), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Sigmoid_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Sigmoid(x, destination));
            });
        }

        [Fact]
        public void Sigmoid_ThrowsForEmptyInput()
        {
            if (!IsFloatingPoint) return;

            AssertExtensions.Throws<ArgumentException>(() => Sigmoid(ReadOnlySpan<T>.Empty, CreateTensor(1)));
        }

        [Fact]
        public void Sigmoid_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Sigmoid(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Sigmoid(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Sinh
        [Fact]
        public void Sinh_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Sinh(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Sinh(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Sinh_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Sinh(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Sinh(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Sinh_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Sinh(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Sinh(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Sinh_ValueRange()
        {
            if (!IsFloatingPoint) return;

            Assert.All(VectorLengthAndIteratedRange(ConvertFromSingle(-100f), ConvertFromSingle(100f), ConvertFromSingle(3f)), args =>
            {
                T[] x = new T[args.Length];
                T[] dest = new T[args.Length];

                x.AsSpan().Fill(args.Element);
                Sinh(x, dest);

                T expected = Sinh(args.Element);
                foreach (T actual in dest)
                {
                    AssertEqualTolerance(expected, actual);
                }
            });
        }

        [Fact]
        public void Sinh_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Sinh(x, destination));
            });
        }

        [Fact]
        public void Sinh_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Sinh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Sinh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region SoftMax
        [Fact]
        public void SoftMax_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                SoftMax(x, destination);

                T expSum = Zero;
                foreach (T value in x.Memory.Span)
                {
                    expSum = Add(expSum, Exp(value));
                }

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(Exp(x[i]), expSum), destination[i]);
                }
            });
        }

        [Fact]
        public void SoftMax_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                SoftMax(x, x);

                T expSum = Zero;
                foreach (T value in xOrig)
                {
                    expSum = Add(expSum, Exp(value));
                }

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Divide(Exp(xOrig[i]), expSum), x[i]);
                }
            });
        }

        [Fact]
        public void SoftMax_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => SoftMax(x, destination));
            });
        }

        [Fact]
        public void SoftMax_ThrowsForEmptyInput()
        {
            if (!IsFloatingPoint) return;

            AssertExtensions.Throws<ArgumentException>(() => SoftMax(ReadOnlySpan<T>.Empty, CreateTensor(1)));
        }

        [Fact]
        public void SoftMax_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => SoftMax(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => SoftMax(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion

        #region Subtract
        [Fact]
        public void Subtract_TwoTensors()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Subtract(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Subtract(x[i], y[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Subtract_TwoTensors_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Subtract(x, x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Zero, x[i]);
                }
            });
        }

        [Fact]
        public void Subtract_TwoTensors_ThrowsForMismatchedLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength - 1);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Assert.Throws<ArgumentException>(() => Subtract(x, y, destination));
                Assert.Throws<ArgumentException>(() => Subtract(y, x, destination));
            });
        }

        [Fact]
        public void Subtract_TwoTensors_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> y = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(x, y, destination));
            });
        }

        [Fact]
        public void Subtract_TwoTensors_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(2, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(3, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), array.AsSpan(4, 2), array.AsSpan(5, 2)));
        }

        [Fact]
        public void Subtract_TensorScalar()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Subtract(x, y, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Subtract(x[i], y), destination[i]);
                }
            });
        }

        [Fact]
        public void Subtract_TensorScalar_InPlace()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();
                T y = NextRandom();

                Subtract(x, y, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Subtract(xOrig[i], y), x[i]);
                }
            });
        }

        [Fact]
        public void Subtract_TensorScalar_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T y = NextRandom();
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(x, y, destination));
            });
        }

        [Fact]
        public void Subtract_TensorScalar_ThrowsForOverlapppingInputsWithOutputs()
        {
            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), default(T), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Subtract(array.AsSpan(1, 2), default(T), array.AsSpan(2, 2)));
        }
        #endregion

        #region Sum
        [Fact]
        public void Sum_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T sum = Zero;
                foreach (T value in x.Memory.Span)
                {
                    sum = Add(sum, value);
                }
                AssertEqualTolerance(sum, Sum(x));
            });
        }
        #endregion

        #region SumOfMagnitudes
        [Fact]
        public void SumOfMagnitudes_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateTensor(tensorLength);
                FillTensor(x, MinValue);

                T sum = Zero;
                foreach (T value in x.Memory.Span)
                {
                    sum = Add(sum, Abs(value));
                }
                AssertEqualTolerance(sum, SumOfMagnitudes(x));
            });
        }
        #endregion

        #region SumOfSquares
        [Fact]
        public void SumOfSquares_AllLengths()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);

                T sum = Zero;
                foreach (T value in x.Memory.Span)
                {
                    sum = Add(sum, Multiply(value, value));
                }
                AssertEqualTolerance(sum, SumOfSquares(x));
            });
        }
        #endregion

        #region Tanh
        [Fact]
        public void Tanh_AllLengths()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                Tanh(x, destination);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Tanh(x[i]), destination[i]);
                }
            });
        }

        [Fact]
        public void Tanh_InPlace()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                T[] xOrig = x.Span.ToArray();

                Tanh(x, x);

                for (int i = 0; i < tensorLength; i++)
                {
                    AssertEqualTolerance(Tanh(xOrig[i]), x[i]);
                }
            });
        }

        [Fact]
        public void Tanh_SpecialValues()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength);

                RunForEachSpecialValue(() =>
                {
                    Tanh(x, destination);
                    for (int i = 0; i < tensorLength; i++)
                    {
                        AssertEqualTolerance(Tanh(x[i]), destination[i]);
                    }
                }, x);
            });
        }

        [Fact]
        public void Tanh_ValueRange()
        {
            if (!IsFloatingPoint) return;

            Assert.All(VectorLengthAndIteratedRange(ConvertFromSingle(-11f), ConvertFromSingle(11f), ConvertFromSingle(0.2f)), args =>
            {
                T[] x = new T[args.Length];
                T[] dest = new T[args.Length];

                x.AsSpan().Fill(args.Element);
                Tanh(x, dest);

                T expected = Tanh(args.Element);
                foreach (T actual in dest)
                {
                    AssertEqualTolerance(expected, actual);
                }
            });
        }

        [Fact]
        public void Tanh_ThrowsForTooShortDestination()
        {
            if (!IsFloatingPoint) return;

            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<T> x = CreateAndFillTensor(tensorLength);
                using BoundedMemory<T> destination = CreateTensor(tensorLength - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => Tanh(x, destination));
            });
        }

        [Fact]
        public void Tanh_ThrowsForOverlapppingInputsWithOutputs()
        {
            if (!IsFloatingPoint) return;

            T[] array = new T[10];
            AssertExtensions.Throws<ArgumentException>("destination", () => Tanh(array.AsSpan(1, 2), array.AsSpan(0, 2)));
            AssertExtensions.Throws<ArgumentException>("destination", () => Tanh(array.AsSpan(1, 2), array.AsSpan(2, 2)));
        }
        #endregion
    }
}
