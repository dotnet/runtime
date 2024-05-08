// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise absolute value of each single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = MathF.Abs(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The absolute value of a <see cref="float"/> is its numeric value without its sign. For example, the absolute value of both 1.2e-03 and -1.2e03 is 1.2e03.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="float.NegativeInfinity"/> or <see cref="float.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="float.PositiveInfinity"/>.
        /// If a value is equal to <see cref="float.NaN"/>, the result stored into the corresponding destination location is the original NaN value with the sign bit removed.
        /// </para>
        /// </remarks>
        public static void Abs(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<AbsoluteOperator_Single>(x, destination);

        /// <summary>Computes the element-wise addition of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Add(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<AddOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise addition of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Add(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<AddOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and the length of <paramref name="multiplier" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="multiplier"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> multiplier, Span<float> destination) =>
            InvokeSpanSpanSpanIntoSpan<AddMultiplyOperator_Single>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" /></c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float multiplier, Span<float> destination) =>
            InvokeSpanSpanScalarIntoSpan<AddMultiplyOperator_Single>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="multiplier" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="multiplier"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />) * <paramref name="multiplier" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> multiplier, Span<float> destination) =>
            InvokeSpanScalarSpanIntoSpan<AddMultiplyOperator_Single>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise hyperbolic cosine of each single-precision floating-point radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Cosh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="float.NegativeInfinity"/> or <see cref="float.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="float.PositiveInfinity"/>.
        /// If a value is equal to <see cref="float.NaN"/>, the result stored into the corresponding destination location is also NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <see cref="MathF.PI"/>/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Cosh(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<CoshOperator_Single>(x, destination);

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The cosine similarity of the two tensors.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>TensorPrimitives.Dot(x, y) / (MathF.Sqrt(TensorPrimitives.SumOfSquares(x)) * MathF.Sqrt(TensorPrimitives.SumOfSquares(y)).</c>
        /// </para>
        /// <para>
        /// If any element in either input tensor is equal to <see cref="float.NegativeInfinity"/>, <see cref="float.PositiveInfinity"/>, or <see cref="float.NaN"/>,
        /// NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float CosineSimilarity(ReadOnlySpan<float> x, ReadOnlySpan<float> y) =>
            CosineSimilarityCore(x, y);

        /// <summary>Computes the distance between two points, specified as non-empty, equal-length tensors of single-precision floating-point numbers, in Euclidean space.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The Euclidean distance.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;float&gt; difference = ...;
        ///     TensorPrimitives.Subtract(x, y, difference);
        ///     float result = MathF.Sqrt(TensorPrimitives.SumOfSquares(difference));
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// If any element in either input tensor is equal to <see cref="float.NaN"/>, NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return MathF.Sqrt(Aggregate<SubtractSquaredOperator_Single, AddOperator_Single>(x, y));
        }

        /// <summary>Computes the element-wise division of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<DivideOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise division of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<DivideOperator_Single>(x, y, destination);

        /// <summary>Computes the dot product of two tensors containing single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The dot product.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;float&gt; products = ...;
        ///     TensorPrimitives.Multiply(x, y, products);
        ///     float result = TensorPrimitives.Sum(products);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate products. It corresponds to the <c>dot</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input elements is equal to <see cref="float.NaN"/>, the resulting value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Dot(ReadOnlySpan<float> x, ReadOnlySpan<float> y) =>
            Aggregate<MultiplyOperator_Single, AddOperator_Single>(x, y);

        /// <summary>Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Exp(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals <see cref="float.NaN"/> or <see cref="float.PositiveInfinity"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value equals <see cref="float.NegativeInfinity"/>, the result stored into the corresponding destination location is set to 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<ExpOperator_Single>(x, destination);

        /// <summary>Searches for the index of the largest single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the index of the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMax(ReadOnlySpan<float> x) =>
            IndexOfMinMaxCore<float, IndexOfMaxOperator_Single>(x);

        /// <summary>Searches for the index of the single-precision floating-point number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the largest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMaxMagnitude(ReadOnlySpan<float> x) =>
            IndexOfMinMaxCore<float, IndexOfMaxMagnitudeOperator_Single>(x);

        /// <summary>Searches for the index of the smallest single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the minimum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the index of the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMin(ReadOnlySpan<float> x) =>
            IndexOfMinMaxCore<float, IndexOfMinOperator_Single>(x);

        /// <summary>Searches for the index of the single-precision floating-point number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the smallest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMinMagnitude(ReadOnlySpan<float> x) =>
            IndexOfMinMaxCore<float, IndexOfMinMagnitudeOperator_Single>(x);

        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of single-precision floating-point numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Log(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="float.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="float.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="float.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<LogOperator_Single>(x, destination);

        /// <summary>Computes the element-wise base 2 logarithm of single-precision floating-point numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Log2(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="float.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="float.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="float.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log2(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<Log2Operator_Single>(x, destination);

        /// <summary>Searches for the largest single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Max(ReadOnlySpan<float> x) =>
            MinMaxCore<MaxOperator_Single>(x);

        /// <summary>Computes the element-wise maximum of the single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = MathF.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="float.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MaxPropagateNaNOperator_Single>(x, y, destination);

        /// <summary>Searches for the single-precision floating-point number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The element in <paramref name="x"/> with the largest magnitude (absolute value).</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float MaxMagnitude(ReadOnlySpan<float> x) =>
            MinMaxCore<MaxMagnitudeOperator_Single>(x);

        /// <summary>Computes the element-wise single-precision floating-point number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = MathF.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        /// <remarks>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MaxMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MaxMagnitudePropagateNaNOperator_Single>(x, y, destination);

        /// <summary>Searches for the smallest single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The minimum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value is equal to <see cref="float.NaN"/>
        /// is present, the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Min(ReadOnlySpan<float> x) =>
            MinMaxCore<MinOperator_Single>(x);

        /// <summary>Computes the element-wise minimum of the single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = MathF.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="float.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Min(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MinPropagateNaNOperator_Single>(x, y, destination);

        /// <summary>Searches for the single-precision floating-point number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The element in <paramref name="x"/> with the smallest magnitude (absolute value).</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to <see cref="float.NaN"/>
        /// is present, the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float MinMagnitude(ReadOnlySpan<float> x) =>
            MinMaxCore<MinMagnitudeOperator_Single>(x);

        /// <summary>Computes the element-wise single-precision floating-point number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = MathF.MinMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If either value is equal to <see cref="float.NaN"/>,
        /// that value is stored as the result. If the two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MinMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MinMagnitudePropagateNaNOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise product of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] * <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Multiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<MultiplyOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise product of single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] * <paramref name="y" /></c>.
        /// It corresponds to the <c>scal</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Multiply(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<MultiplyOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and length of <paramref name="addend" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="addend"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> addend, Span<float> destination) =>
            InvokeSpanSpanSpanIntoSpan<MultiplyAddOperator_Single>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" /></c>.
        /// It corresponds to the <c>axpy</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float addend, Span<float> destination) =>
            InvokeSpanSpanScalarIntoSpan<MultiplyAddOperator_Single>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="addend" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="addend"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />) + <paramref name="addend" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> addend, Span<float> destination) =>
            InvokeSpanScalarSpanIntoSpan<MultiplyAddOperator_Single>(x, y, addend, destination);

        /// <summary>Computes the element-wise negation of each single-precision floating-point number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = -<paramref name="x" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Negate(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<NegateOperator_Single>(x, destination);

        /// <summary>Computes the Euclidean norm of the specified tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <returns>The norm.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>MathF.Sqrt(TensorPrimitives.SumOfSquares(x))</c>.
        /// This is often referred to as the Euclidean norm or L2 norm.
        /// It corresponds to the <c>nrm2</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input values is equal to <see cref="float.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Norm(ReadOnlySpan<float> x) =>
            MathF.Sqrt(SumOfSquares(x));

        /// <summary>Computes the product of all elements in the specified non-empty tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of multiplying all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// If any of the input values is equal to <see cref="float.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Product(ReadOnlySpan<float> x)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<IdentityOperator_Single, MultiplyOperator_Single>(x);
        }

        /// <summary>Computes the product of the element-wise differences of the single-precision floating-point numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise subtraction of the elements in the second tensor from the first tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;float&gt; differences = ...;
        ///     TensorPrimitives.Subtract(x, y, differences);
        ///     float result = TensorPrimitives.Product(differences);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float ProductOfDifferences(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<SubtractOperator_Single, MultiplyOperator_Single>(x, y);
        }

        /// <summary>Computes the product of the element-wise sums of the single-precision floating-point numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise additions of the elements in each tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;float&gt; sums = ...;
        ///     TensorPrimitives.Add(x, y, sums);
        ///     float result = TensorPrimitives.Product(sums);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate sums.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float ProductOfSums(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<AddOperator_Single, MultiplyOperator_Single>(x, y);
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1f / (1f + <see cref="MathF" />.Exp(-<paramref name="x" />[i]))</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sigmoid(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            InvokeSpanIntoSpan<SigmoidOperator_Single>(x, destination);
        }

        /// <summary>Computes the element-wise hyperbolic sine of each single-precision floating-point radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Sinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="float.NegativeInfinity"/>, <see cref="float.PositiveInfinity"/>, or <see cref="float.NaN"/>,
        /// the corresponding destination location is set to that value.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <see cref="MathF.PI"/>/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sinh(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<SinhOperator_Single>(x, destination);

        /// <summary>Computes the softmax function over the specified non-empty tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes a sum of <c>MathF.Exp(x[i])</c> for all elements in <paramref name="x"/>.
        /// It then effectively computes <c><paramref name="destination" />[i] = MathF.Exp(<paramref name="x" />[i]) / sum</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
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

            ValidateInputOutputSpanNonOverlapping(x, destination);

            float expSum = Aggregate<ExpOperator_Single, AddOperator_Single>(x);

            InvokeSpanScalarIntoSpan<ExpOperator_Single, DivideOperator_Single>(x, expSum, destination);
        }

        /// <summary>Computes the element-wise difference between single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] - <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Subtract(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) =>
            InvokeSpanSpanIntoSpan<SubtractOperator_Single>(x, y, destination);

        /// <summary>Computes the element-wise difference between single-precision floating-point numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] - <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="float.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Subtract(ReadOnlySpan<float> x, float y, Span<float> destination) =>
            InvokeSpanScalarIntoSpan<SubtractOperator_Single>(x, y, destination);

        /// <summary>Computes the sum of all elements in the specified tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding all elements in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// If any of the values in the input is equal to <see cref="float.NaN"/>, the result is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float Sum(ReadOnlySpan<float> x) =>
            Aggregate<IdentityOperator_Single, AddOperator_Single>(x);

        /// <summary>Computes the sum of the absolute values of every element in the specified tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the absolute value of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;float&gt; absoluteValues = ...;
        ///     TensorPrimitives.Abs(x, absoluteValues);
        ///     float result = TensorPrimitives.Sum(absoluteValues);
        /// </c>
        /// but without requiring intermediate storage for the absolute values. It corresponds to the <c>asum</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float SumOfMagnitudes(ReadOnlySpan<float> x) =>
            Aggregate<AbsoluteOperator_Single, AddOperator_Single>(x);

        /// <summary>Computes the sum of the square of every element in the specified tensor of single-precision floating-point numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the square of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;float&gt; squaredValues = ...;
        ///     TensorPrimitives.Multiply(x, x, squaredValues);
        ///     float result = TensorPrimitives.Sum(squaredValues);
        /// </c>
        /// but without requiring intermediate storage for the squared values.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static float SumOfSquares(ReadOnlySpan<float> x) =>
            Aggregate<SquaredOperator_Single, AddOperator_Single>(x);

        /// <summary>Computes the element-wise hyperbolic tangent of each single-precision floating-point radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Tanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="float.NegativeInfinity"/>, the corresponding destination location is set to -1.
        /// If a value is equal to <see cref="float.PositiveInfinity"/>, the corresponding destination location is set to 1.
        /// If a value is equal to <see cref="float.NaN"/>, the corresponding destination location is set to NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <see cref="MathF.PI"/>/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tanh(ReadOnlySpan<float> x, Span<float> destination) =>
            InvokeSpanIntoSpan<TanhOperator_Single>(x, destination);
    }
}
