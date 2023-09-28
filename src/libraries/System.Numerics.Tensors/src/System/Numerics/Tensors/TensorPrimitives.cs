// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of: <c>MathF.Abs(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = MathF.Abs(<paramref name="x" />[i])</c>.</remarks>
        public static void Abs(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<AbsoluteOperator>(x, destination);

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

        /// <summary>Computes the cosine similarity between two non-zero vectors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The cosine similarity between the two vectors.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' and '<paramref name="y" />' must not be empty.</exception>
        public static float CosineSimilarity(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            return CosineSimilarityCore(x, y);
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
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            return MathF.Sqrt(Aggregate<SubtractSquaredOperator, AddOperator>(x, y));
        }

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

            return Aggregate<MultiplyOperator, AddOperator>(x, y);
        }

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

        /// <summary>Computes the index of the maximum element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        public static unsafe int IndexOfMax(ReadOnlySpan<float> x)
        {
            int result = -1;

            if (!x.IsEmpty)
            {
                float max = float.NegativeInfinity;

                for (int i = 0; i < x.Length; i++)
                {
                    // This matches the IEEE 754:2019 `maximum` function.
                    // It propagates NaN inputs back to the caller and
                    // otherwise returns the greater of the inputs.
                    // It treats +0 as greater than -0 as per the specification.

                    float current = x[i];

                    if (current != max)
                    {
                        if (float.IsNaN(current))
                        {
                            return i;
                        }

                        if (max < current)
                        {
                            result = i;
                            max = current;
                        }
                    }
                    else if (IsNegative(max) && !IsNegative(current))
                    {
                        result = i;
                        max = current;
                    }
                }
            }

            return result;
        }

        /// <summary>Computes the index of the element in <paramref name="x"/> with the maximum magnitude.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element with the maximum magnitude, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>This method corresponds to the <c>iamax</c> method defined by <c>BLAS1</c>.</remarks>
        public static unsafe int IndexOfMaxMagnitude(ReadOnlySpan<float> x)
        {
            int result = -1;

            if (!x.IsEmpty)
            {
                float max = float.NegativeInfinity;
                float maxMag = float.NegativeInfinity;

                for (int i = 0; i < x.Length; i++)
                {
                    // This matches the IEEE 754:2019 `maximumMagnitude` function.
                    // It propagates NaN inputs back to the caller and
                    // otherwise returns the input with a greater magnitude.
                    // It treats +0 as greater than -0 as per the specification.

                    float current = x[i];
                    float currentMag = Math.Abs(current);

                    if (currentMag != maxMag)
                    {
                        if (float.IsNaN(currentMag))
                        {
                            return i;
                        }

                        if (maxMag < currentMag)
                        {
                            result = i;
                            max = current;
                            maxMag = currentMag;
                        }
                    }
                    else if (IsNegative(max) && !IsNegative(current))
                    {
                        result = i;
                        max = current;
                        maxMag = currentMag;
                    }
                }
            }

            return result;
        }

        /// <summary>Computes the index of the minimum element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the minimum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        public static unsafe int IndexOfMin(ReadOnlySpan<float> x)
        {
            int result = -1;

            if (!x.IsEmpty)
            {
                float min = float.PositiveInfinity;

                for (int i = 0; i < x.Length; i++)
                {
                    // This matches the IEEE 754:2019 `minimum` function.
                    // It propagates NaN inputs back to the caller and
                    // otherwise returns the lesser of the inputs.
                    // It treats +0 as greater than -0 as per the specification.

                    float current = x[i];

                    if (current != min)
                    {
                        if (float.IsNaN(current))
                        {
                            return i;
                        }

                        if (current < min)
                        {
                            result = i;
                            min = current;
                        }
                    }
                    else if (IsNegative(current) && !IsNegative(min))
                    {
                        result = i;
                        min = current;
                    }
                }
            }

            return result;
        }

        /// <summary>Computes the index of the element in <paramref name="x"/> with the minimum magnitude.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element with the minimum magnitude, or -1 if <paramref name="x"/> is empty.</returns>
        public static unsafe int IndexOfMinMagnitude(ReadOnlySpan<float> x)
        {
            int result = -1;

            if (!x.IsEmpty)
            {
                float min = float.PositiveInfinity;
                float minMag = float.PositiveInfinity;

                for (int i = 0; i < x.Length; i++)
                {
                    // This matches the IEEE 754:2019 `minimumMagnitude` function
                    // It propagates NaN inputs back to the caller and
                    // otherwise returns the input with a lesser magnitude.
                    // It treats +0 as greater than -0 as per the specification.

                    float current = x[i];
                    float currentMag = Math.Abs(current);

                    if (currentMag != minMag)
                    {
                        if (float.IsNaN(currentMag))
                        {
                            return i;
                        }

                        if (currentMag < minMag)
                        {
                            result = i;
                            min = current;
                            minMag = currentMag;
                        }
                    }
                    else if (IsNegative(current) && !IsNegative(min))
                    {
                        result = i;
                        min = current;
                        minMag = currentMag;
                    }
                }
            }

            return result;
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

        /// <summary>Computes the element-wise result of: <c>log2(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Log2(<paramref name="x" />[i])</c>.</remarks>
        public static void Log2(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = Log2(x[i]);
            }
        }

        /// <summary>Computes the maximum element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be greater than zero.</exception>
        public static float Max(ReadOnlySpan<float> x) =>
            MinMaxCore<MaxOperator>(x);

        /// <summary>Computes the element-wise result of: <c>MathF.Max(<paramref name="x" />, <paramref name="y" />)</c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = MathF.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        public static void Max(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MaxPropagateNaNOperator>(x, y, destination);

        /// <summary>Computes the maximum magnitude of any element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum magnitude of any element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be greater than zero.</exception>
        public static float MaxMagnitude(ReadOnlySpan<float> x) =>
            MinMaxCore<MaxMagnitudeOperator>(x);

        /// <summary>Computes the element-wise result of: <c>MathF.MaxMagnitude(<paramref name="x" />, <paramref name="y" />)</c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = MathF.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        public static void MaxMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MaxMagnitudePropagateNaNOperator>(x, y, destination);

        /// <summary>Computes the minimum element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The minimum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be greater than zero.</exception>
        public static float Min(ReadOnlySpan<float> x) =>
            MinMaxCore<MinOperator>(x);

        /// <summary>Computes the element-wise result of: <c>MathF.Min(<paramref name="x" />, <paramref name="y" />)</c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = MathF.Min(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        public static void Min(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MinPropagateNaNOperator>(x, y, destination);

        /// <summary>Computes the minimum magnitude of any element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The minimum magnitude of any element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be greater than zero.</exception>
        public static float MinMagnitude(ReadOnlySpan<float> x) =>
            MinMaxCore<MinMagnitudeOperator>(x);

        /// <summary>Computes the element-wise result of: <c>MathF.MinMagnitude(<paramref name="x" />, <paramref name="y" />)</c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = MathF.MinMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        public static void MinMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MinMagnitudePropagateNaNOperator>(x, y, destination);

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

        /// <summary>Computes the element-wise result of: <c>-<paramref name="x" /></c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = -<paramref name="x" />[i]</c>.</remarks>
        public static void Negate(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<NegateOperator>(x, destination);

        /// <summary>
        /// A mathematical operation that takes a vector and returns the L2 norm.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <returns>The L2 norm.</returns>
        public static float Norm(ReadOnlySpan<float> x) => // BLAS1: nrm2
            MathF.Sqrt(Aggregate<SquaredOperator, AddOperator>(x));

        /// <summary>Computes the product of all elements in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of multiplying all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be greater than zero.</exception>
        public static float Product(ReadOnlySpan<float> x)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<IdentityOperator, MultiplyOperator>(x);
        }

        /// <summary>Computes the product of the element-wise result of: <c><paramref name="x" /> - <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise subtraction of the elements in the second tensor from the first tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>This method effectively does <c><see cref="TensorPrimitives" />.Product(<see cref="TensorPrimitives" />.Subtract(<paramref name="x" />, <paramref name="y" />))</c>.</remarks>
        public static float ProductOfDifferences(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            return Aggregate<SubtractOperator, MultiplyOperator>(x, y);
        }

        /// <summary>Computes the product of the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise additions of the elements in each tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>This method effectively does <c><see cref="TensorPrimitives" />.Product(<see cref="TensorPrimitives" />.Add(<paramref name="x" />, <paramref name="y" />))</c>.</remarks>
        public static float ProductOfSums(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            return Aggregate<AddOperator, MultiplyOperator>(x, y);
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
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = 1f / (1 + MathF.Exp(-x[i]));
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

        /// <summary>
        /// A function that takes a collection of real numbers and returns a probability distribution.
        /// </summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException">'<paramref name="x" />' must not be empty.</exception>
        public static void SoftMax(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            float expSum = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                expSum += MathF.Exp(x[i]);
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Exp(x[i]) / expSum;
            }
        }

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

        /// <summary>Computes the sum of all elements in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding all elements in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        public static float Sum(ReadOnlySpan<float> x) =>
            Aggregate<IdentityOperator, AddOperator>(x);

        /// <summary>Computes the sum of the absolute values of every element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the absolute value of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        ///     <para>This method effectively does <c><see cref="TensorPrimitives" />.Sum(<see cref="TensorPrimitives" />.Abs(<paramref name="x" />))</c>.</para>
        ///     <para>This method corresponds to the <c>asum</c> method defined by <c>BLAS1</c>.</para>
        /// </remarks>
        public static float SumOfMagnitudes(ReadOnlySpan<float> x) =>
            Aggregate<AbsoluteOperator, AddOperator>(x);

        /// <summary>Computes the sum of the squares of every element in <paramref name="x"/>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding every element in <paramref name="x"/> multiplied by itself, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>This method effectively does <c><see cref="TensorPrimitives" />.Sum(<see cref="TensorPrimitives" />.Multiply(<paramref name="x" />, <paramref name="x" />))</c>.</remarks>
        public static float SumOfSquares(ReadOnlySpan<float> x) =>
            Aggregate<SquaredOperator, AddOperator>(x);

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

        /// <summary>Mask used to handle remaining elements after vectorized handling of the input.</summary>
        /// <remarks>
        /// Logically 16 rows of 16 uints. The Nth row should be used to handle N remaining elements at the
        /// end of the input, where elements in the vector prior to that will be zero'd.
        /// </remarks>
        private static ReadOnlySpan<uint> RemainderUInt32Mask_16x16 => new uint[]
        {
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0x00000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
            0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
        };
    }
}
