// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO:
// - Provide generic overloads for the IndexOfMin/Max{Magnitude} methods
namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise absolute value of each number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="OverflowException"><typeparamref name="T"/> is a signed integer type and <paramref name="x"/> contained a value equal to <typeparamref name="T"/>'s minimum value.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = MathF.Abs(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The absolute value of a <typeparamref name="T"/> is its numeric value without its sign. For example, the absolute value of both 1.2e-03 and -1.2e03 is 1.2e03.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is the original NaN value with the sign bit removed.
        /// </para>
        /// </remarks>
        public static void Abs<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, AbsoluteOperator<T>>(x, destination);

        /// <summary>Computes the element-wise addition of numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Add<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> =>
            InvokeSpanSpanIntoSpan<T, AddOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise addition of numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Add<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> =>
            InvokeSpanScalarIntoSpan<T, AddOperator<T>>(x, y, destination);

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
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanSpanIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

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
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanScalarIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

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
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanScalarSpanIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise hyperbolic cosine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Cosh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is also NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <see cref="MathF.PI"/>/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Cosh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, CoshOperator<T>>(x, destination);

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of numbers.</summary>
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
        /// If any element in either input tensor is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, or <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T CosineSimilarity<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IRootFunctions<T> =>
            CosineSimilarityCore(x, y);

        /// <summary>Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The Euclidean distance.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;T&gt; difference = ...;
        ///     TensorPrimitives.Subtract(x, y, difference);
        ///     T result = MathF.Sqrt(TensorPrimitives.SumOfSquares(difference));
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// If any element in either input tensor is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Distance<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return T.Sqrt(Aggregate<T, SubtractSquaredOperator<T>, AddOperator<T>>(x, y));
        }

        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, DivideOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, DivideOperator<T>>(x, y, destination);

        /// <summary>Computes the dot product of two tensors containing numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The dot product.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;T&gt; products = ...;
        ///     TensorPrimitives.Multiply(x, y, products);
        ///     T result = TensorPrimitives.Sum(products);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate products. It corresponds to the <c>dot</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input elements is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Dot<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T> =>
            Aggregate<T, MultiplyOperator<T>, AddOperator<T>>(x, y);

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Exp(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NaN"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the result stored into the corresponding destination location is set to 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, ExpOperator<T>>(x, destination);

        // TODO: Make IndexOfXx methods generic
        //
        ///// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        ///// <param name="x">The tensor, represented as a span.</param>
        ///// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        ///// <remarks>
        ///// <para>
        ///// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        ///// is present, the index of the first is returned. Positive 0 is considered greater than negative 0.
        ///// </para>
        ///// <para>
        ///// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        ///// operating systems or architectures.
        ///// </para>
        ///// </remarks>
        //public static int IndexOfMax<T>(ReadOnlySpan<T> x)
        //    where T : INumber<T> =>
        //    IndexOfMinMaxCore<T, IndexOfMaxOperator<T>>(x);
        //
        ///// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        ///// <param name="x">The tensor, represented as a span.</param>
        ///// <returns>The index of the element in <paramref name="x"/> with the largest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        ///// <remarks>
        ///// <para>
        ///// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        ///// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        ///// the positive value is considered to have the larger magnitude.
        ///// </para>
        ///// <para>
        ///// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        ///// operating systems or architectures.
        ///// </para>
        ///// </remarks>
        //public static int IndexOfMaxMagnitude<T>(ReadOnlySpan<T> x)
        //    where T : INumberBase<T> =>
        //    IndexOfMinMaxCore<T, IndexOfMaxMagnitudeOperator<T>>(x);
        //
        ///// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        ///// <param name="x">The tensor, represented as a span.</param>
        ///// <returns>The index of the minimum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        ///// <remarks>
        ///// <para>
        ///// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        ///// is present, the index of the first is returned. Negative 0 is considered smaller than positive 0.
        ///// </para>
        ///// <para>
        ///// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        ///// operating systems or architectures.
        ///// </para>
        ///// </remarks>
        //public static int IndexOfMin<T>(ReadOnlySpan<T> x)
        //    where T : INumber<T> =>
        //    IndexOfMinMaxCore<T, IndexOfMinOperator<T>>(x);
        //
        ///// <summary>Searches for the index of the number with the smallest magnitude in the specified tensor.</summary>
        ///// <param name="x">The tensor, represented as a span.</param>
        ///// <returns>The index of the element in <paramref name="x"/> with the smallest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        ///// <remarks>
        ///// <para>
        ///// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        ///// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        ///// the negative value is considered to have the smaller magnitude.
        ///// </para>
        ///// <para>
        ///// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        ///// operating systems or architectures.
        ///// </para>
        ///// </remarks>
        //public static int IndexOfMinMagnitude<T>(ReadOnlySpan<T> x)
        //    where T : INumberBase<T> =>
        //    IndexOfMinMaxCore<T, IndexOfMinMagnitudeOperator<T>>(x);

        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Log(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, LogOperator<T>>(x, destination);

        /// <summary>Computes the element-wise base 2 logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Log2(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log2<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log2Operator<T>>(x, destination);

        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Max<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            MinMaxCore<T, MaxOperator<T>>(x);

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
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
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, MaxPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The element in <paramref name="x"/> with the largest magnitude (absolute value).</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T MaxMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            MinMaxCore<T, MaxMagnitudeOperator<T>>(x);

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
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
        public static void MaxMagnitude<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanIntoSpan<T, MaxMagnitudePropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The minimum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Min<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            MinMaxCore<T, MinOperator<T>>(x);

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
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
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Min<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, MinPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The element in <paramref name="x"/> with the smallest magnitude (absolute value).</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T MinMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            MinMaxCore<T, MinMagnitudeOperator<T>>(x);

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
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
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. If the two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MinMagnitude<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanIntoSpan<T, MinMagnitudePropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise product of numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Multiply<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T> =>
            InvokeSpanSpanIntoSpan<T, MultiplyOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise product of numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Multiply<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T> =>
            InvokeSpanScalarIntoSpan<T, MultiplyOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanSpanIntoSpan<T, MultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T addend, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanScalarIntoSpan<T, MultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void MultiplyAdd<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanScalarSpanIntoSpan<T, MultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = -<paramref name="x" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Negate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IUnaryNegationOperators<T, T> =>
            InvokeSpanIntoSpan<T, NegateOperator<T>>(x, destination);

        /// <summary>Computes the Euclidean norm of the specified tensor of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <returns>The norm.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>MathF.Sqrt(TensorPrimitives.SumOfSquares(x))</c>.
        /// This is often referred to as the Euclidean norm or L2 norm.
        /// It corresponds to the <c>nrm2</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Norm<T>(ReadOnlySpan<T> x)
            where T : IRootFunctions<T> =>
            T.Sqrt(SumOfSquares(x));

        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of multiplying all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Product<T>(ReadOnlySpan<T> x)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, IdentityOperator<T>, MultiplyOperator<T>>(x);
        }

        /// <summary>Computes the product of the element-wise differences of the numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise subtraction of the elements in the second tensor from the first tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; differences = ...;
        ///     TensorPrimitives.Subtract(x, y, differences);
        ///     T result = TensorPrimitives.Product(differences);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T ProductOfDifferences<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, SubtractOperator<T>, MultiplyOperator<T>>(x, y);
        }

        /// <summary>Computes the product of the element-wise sums of the numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise additions of the elements in each tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; sums = ...;
        ///     TensorPrimitives.Add(x, y, sums);
        ///     T result = TensorPrimitives.Product(sums);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate sums.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T ProductOfSums<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, AddOperator<T>, MultiplyOperator<T>>(x, y);
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
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
        public static void Sigmoid<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            InvokeSpanIntoSpan<T, SigmoidOperator<T>>(x, destination);
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Sinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, or <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
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
        public static void Sinh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, SinhOperator<T>>(x, destination);

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
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
        public static void SoftMax<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
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

            T expSum = Aggregate<T, ExpOperator<T>, AddOperator<T>>(x);

            InvokeSpanScalarIntoSpan<T, ExpOperator<T>, DivideOperator<T>>(x, expSum, destination);
        }

        /// <summary>Computes the element-wise difference between numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Subtract<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : ISubtractionOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, SubtractOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise difference between numbers in the specified tensors.</summary>
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
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Subtract<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : ISubtractionOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, SubtractOperator<T>>(x, y, destination);

        /// <summary>Computes the sum of all elements in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding all elements in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// If any of the values in the input is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Sum<T>(ReadOnlySpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> =>
            Aggregate<T, IdentityOperator<T>, AddOperator<T>>(x);

        /// <summary>Computes the sum of the absolute values of every element in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the absolute value of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <exception cref="OverflowException"><typeparamref name="T"/> is a signed integer type and <paramref name="x"/> contained a value equal to <typeparamref name="T"/>'s minimum value.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; absoluteValues = ...;
        ///     TensorPrimitives.Abs(x, absoluteValues);
        ///     T result = TensorPrimitives.Sum(absoluteValues);
        /// </c>
        /// but without requiring intermediate storage for the absolute values. It corresponds to the <c>asum</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T SumOfMagnitudes<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            Aggregate<T, AbsoluteOperator<T>, AddOperator<T>>(x);

        /// <summary>Computes the sum of the square of every element in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the square of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; squaredValues = ...;
        ///     TensorPrimitives.Multiply(x, x, squaredValues);
        ///     T result = TensorPrimitives.Sum(squaredValues);
        /// </c>
        /// but without requiring intermediate storage for the squared values.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T SumOfSquares<T>(ReadOnlySpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T> =>
            Aggregate<T, SquaredOperator<T>, AddOperator<T>>(x);

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <see cref="MathF" />.Tanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the corresponding destination location is set to -1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the corresponding destination location is set to 1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the corresponding destination location is set to NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <see cref="MathF.PI"/>/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tanh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, TanhOperator<T>>(x, destination);
    }
}
