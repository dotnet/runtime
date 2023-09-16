// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" />[i]</c>.</remarks>
        public static unsafe void Add(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<AddOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" /></c>.</remarks>
        public static void Add(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<AddOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> - <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] - <paramref name="y" />[i]</c>.</remarks>
        public static void Subtract(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<SubtractOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> - <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] - <paramref name="y" /></c>.</remarks>
        public static void Subtract(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<SubtractOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> * <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] * <paramref name="y" /></c>.</remarks>
        public static void Multiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MultiplyOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> * <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        ///     <para>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] * <paramref name="y" /></c>.</para>
        ///     <para>This method corresponds to the <c>scal</c> method defined by <c>BLAS1</c>.</para>
        /// </remarks>
        public static void Multiply(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<MultiplyOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> / <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" /></c>.</remarks>
        public static void Divide(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<DivideOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> / <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" /></c>.</remarks>
        public static void Divide(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<DivideOperator>(x, y, destination);

        /// <summary>Computes the element-wise result of: <c>-<paramref name="x" /></c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = -<paramref name="x" />[i]</c>.</remarks>
        public static void Negate(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<NegateOperator>(x, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="multiplier" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" />[i]</c>.</remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> multiplier, Span<float> destination) =>
            InvokeSpanSpanSpanIntoSpan<AddMultiplyOperator>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" /></c>.</remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float multiplier, Span<float> destination) =>
            InvokeSpanSpanScalarIntoSpan<AddMultiplyOperator>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="multiplier" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />) * <paramref name="multiplier" />[i]</c>.</remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> multiplier, Span<float> destination) =>
            InvokeSpanScalarSpanIntoSpan<AddMultiplyOperator>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> * <paramref name="y" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="addend" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" />[i]</c>.</remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> addend, Span<float> destination) =>
            InvokeSpanSpanSpanIntoSpan<MultiplyAddOperator>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> * <paramref name="y" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        ///     <para>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" /></c>.</para>
        ///     <para>This method corresponds to the <c>axpy</c> method defined by <c>BLAS1</c>.</para>
        /// </remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float addend, Span<float> destination) =>
            InvokeSpanSpanScalarIntoSpan<MultiplyAddOperator>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of: <c>(<paramref name="x" /> * <paramref name="y" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="addend" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />) + <paramref name="addend" />[i]</c>.</remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> addend, Span<float> destination) =>
            InvokeSpanScalarSpanIntoSpan<MultiplyAddOperator>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of: <c>pow(e, <paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Exp(<paramref name="x" />[i])</c>.</remarks>
        public static void Exp(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Exp(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>ln(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Log(<paramref name="x" />[i])</c>.</remarks>
        public static void Log(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Log(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>cosh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Cosh(<paramref name="x" />[i])</c>.</remarks>
        public static void Cosh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Cosh(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>sinh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Sinh(<paramref name="x" />[i])</c>.</remarks>
        public static void Sinh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Sinh(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>tanh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Tanh(<paramref name="x" />[i])</c>.</remarks>
        public static void Tanh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Tanh(x[i]);
            }
        }

        /// <summary>Computes the cosine similarity between two non-zero vectors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The cosine similarity between the two vectors.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' and '<paramref name="y" />' must not be empty.</exception>
        public static float CosineSimilarity(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }
            if (x.Length == 0 || y.Length == 0)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            float dotprod = 0f;
            float magx = 0f;
            float magy = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                dotprod += x[i] * y[i];
                magx += x[i] * x[i];
                magy += y[i] * y[i];
            }

            return dotprod / (MathF.Sqrt(magx) * MathF.Sqrt(magy));
        }

        /// <summary>
        /// Compute the distance between two points in Euclidean space.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The Euclidean distance.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' and '<paramref name="y" />' must not be empty.</exception>
        public static float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }
            if (x.Length == 0 || y.Length == 0)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            float distance = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                float dist = x[i] - y[i];
                distance += dist * dist;
            }

            return MathF.Sqrt(distance);
        }

        /// <summary>
        /// A mathematical operation that takes two vectors and returns a scalar.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The dot product.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        public static float Dot(ReadOnlySpan<float> x, ReadOnlySpan<float> y) // BLAS1: dot
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            float dotprod = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                dotprod += x[i] * y[i];
            }

            return dotprod;
        }

        /// <summary>
        /// A mathematical operation that takes a vector and returns the L2 norm.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <returns>The L2 norm.</returns>
        public static float L2Normalize(ReadOnlySpan<float> x) // BLAS1: nrm2
        {
            float magx = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                magx += x[i] * x[i];
            }

            return MathF.Sqrt(magx);
        }

        /// <summary>
        /// A function that takes a collection of real numbers and returns a probability distribution.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' must not be empty.</exception>
        public static void SoftMax(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
            if (x.Length == 0)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            float expSum = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                expSum += MathF.Pow((float)Math.E, x[i]);
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Exp(x[i]) / expSum;
            }
        }

        /// <summary>
        /// A function that takes a real number and returns a value between 0 and 1.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' must not be empty.</exception>
        public static void Sigmoid(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
            if (x.Length == 0)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = 1f / (1 + MathF.Exp(-x[i]));
            }
        }
    }
}
