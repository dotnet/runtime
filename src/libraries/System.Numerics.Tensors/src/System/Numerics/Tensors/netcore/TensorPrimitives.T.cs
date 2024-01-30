// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

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
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Abs(<paramref name="x" />[i])</c>.
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

        /// <summary>Computes the element-wise angle in radians whose cosine is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Acos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Acos<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AcosOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic arc-cosine of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Acosh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Acosh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AcoshOperator<T>>(x, destination);

        /// <summary>Computes the element-wise angle in radians whose cosine is the specifed number and divides the result by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.AcosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void AcosPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AcosPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise angle in radians whose sine is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Asin(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Asin<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic arc-sine of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Asinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Asinh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinhOperator<T>>(x, destination);

        /// <summary>Computes the element-wise angle in radians whose sine is the specifed number and divides the result by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.AsinPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void AsinPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise angle in radians whose tangent is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AtanOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic arc-tangent of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atanh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AtanhOperator<T>>(x, destination);

        /// <summary>Computes the element-wise angle in radians whose tangent is the specifed number and divides the result by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.AtanPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void AtanPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AtanPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="y" /> must be same as length of <paramref name="x" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(ReadOnlySpan<T> y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(ReadOnlySpan<T> y, T x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a scalar.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />, <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(T y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeScalarSpanIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors and divides the result by Pi.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="y" /> must be same as length of <paramref name="x" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2Pi<T>(ReadOnlySpan<T> y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanIntoSpan<T, Atan2PiOperator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors and divides the result by Pi.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2Pi<T>(ReadOnlySpan<T> y, T x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarIntoSpan<T, Atan2PiOperator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors and divides the result by Pi.</summary>
        /// <param name="y">The first tensor, represented as a scalar.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />, <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2Pi<T>(T y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeScalarSpanIntoSpan<T, Atan2PiOperator<T>>(y, x, destination);

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

        /// <summary>Computes the element-wise bitwise AND of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] &amp; <paramref name="y" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void BitwiseAnd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, BitwiseAndOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise bitwise AND of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] &amp; <paramref name="y" /></c>.
        /// </para>
        /// </remarks>
        public static void BitwiseAnd<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, BitwiseAndOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise bitwise OR of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] | <paramref name="y" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void BitwiseOr<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, BitwiseOrOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise bitwise OR of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] | <paramref name="y" /></c>.
        /// </para>
        /// </remarks>
        public static void BitwiseOr<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, BitwiseOrOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise ceiling of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Ceiling(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Ceiling<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, CeilingOperator<T>>(x, destination);

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateChecked(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertChecked<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (!TryConvertUniversal(source, destination))
            {
                InvokeSpanIntoSpan<TFrom, TTo, ConvertCheckedFallbackOperator<TFrom, TTo>>(source, destination);
            }
        }

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateSaturating(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertSaturating<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (!TryConvertUniversal(source, destination))
            {
                InvokeSpanIntoSpan<TFrom, TTo, ConvertSaturatingFallbackOperator<TFrom, TTo>>(source, destination);
            }
        }

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateTruncating(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertTruncating<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (TryConvertUniversal(source, destination))
            {
                return;
            }

            if (((typeof(TFrom) == typeof(byte) || typeof(TFrom) == typeof(sbyte)) && (typeof(TTo) == typeof(byte) || typeof(TTo) == typeof(sbyte))) ||
                ((typeof(TFrom) == typeof(ushort) || typeof(TFrom) == typeof(short)) && (typeof(TTo) == typeof(ushort) || typeof(TTo) == typeof(short))) ||
                ((IsUInt32Like<TFrom>() || IsInt32Like<TFrom>()) && (IsUInt32Like<TTo>() || IsInt32Like<TTo>())) ||
                ((IsUInt64Like<TFrom>() || IsInt64Like<TFrom>()) && (IsUInt64Like<TTo>() || IsInt64Like<TTo>())))
            {
                source.CopyTo(Rename<TTo, TFrom>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(float) && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<float, uint, ConvertSingleToUInt32>(Rename<TFrom, float>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(float) && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<float, int, ConvertSingleToInt32>(Rename<TFrom, float>(source), Rename<TTo, int>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(double) && IsUInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<double, ulong, ConvertDoubleToUInt64>(Rename<TFrom, double>(source), Rename<TTo, ulong>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(double) && IsInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<double, long, ConvertDoubleToInt64>(Rename<TFrom, double>(source), Rename<TTo, long>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(byte))
            {
                InvokeSpanIntoSpan_2to1<ushort, byte, NarrowUInt16ToByteOperator>(Rename<TFrom, ushort>(source), Rename<TTo, byte>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(sbyte))
            {
                InvokeSpanIntoSpan_2to1<short, sbyte, NarrowInt16ToSByteOperator>(Rename<TFrom, short>(source), Rename<TTo, sbyte>(destination));
                return;
            }

            if (IsUInt32Like<TFrom>() && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan_2to1<uint, ushort, NarrowUInt32ToUInt16Operator>(Rename<TFrom, uint>(source), Rename<TTo, ushort>(destination));
                return;
            }

            if (IsInt32Like<TFrom>() && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan_2to1<int, short, NarrowInt32ToInt16Operator>(Rename<TFrom, int>(source), Rename<TTo, short>(destination));
                return;
            }

            if (IsUInt64Like<TFrom>() && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<ulong, uint, NarrowUInt64ToUInt32Operator>(Rename<TFrom, ulong>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (IsInt64Like<TFrom>() && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<long, int, NarrowInt64ToInt32Operator>(Rename<TFrom, long>(source), Rename<TTo, int>(destination));
                return;
            }

            InvokeSpanIntoSpan<TFrom, TTo, ConvertTruncatingFallbackOperator<TFrom, TTo>>(source, destination);
        }

        /// <summary>Performs conversions that are the same regardless of checked, truncating, or saturation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // at most one of the branches will be kept
        private static bool TryConvertUniversal<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (typeof(TFrom) == typeof(TTo))
            {
                if (source.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                ValidateInputOutputSpanNonOverlapping(source, Rename<TTo, TFrom>(destination));

                source.CopyTo(Rename<TTo, TFrom>(destination));
                return true;
            }

            if (IsInt32Like<TFrom>() && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan<int, float, ConvertInt32ToSingle>(Rename<TFrom, int>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (IsUInt32Like<TFrom>() && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan<uint, float, ConvertUInt32ToSingle>(Rename<TFrom, uint>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (IsInt64Like<TFrom>() && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan<long, double, ConvertInt64ToDouble>(Rename<TFrom, long>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (IsUInt64Like<TFrom>() && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan<ulong, double, ConvertUInt64ToDouble>(Rename<TFrom, ulong>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(Half))
            {
                InvokeSpanIntoSpan_2to1<float, ushort, NarrowSingleToHalfAsUInt16Operator>(Rename<TFrom, float>(source), Rename<TTo, ushort>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(Half) && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan_1to2<short, float, WidenHalfAsInt16ToSingleOperator>(Rename<TFrom, short>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan_1to2<float, double, WidenSingleToDoubleOperator>(Rename<TFrom, float>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan_2to1<double, float, NarrowDoubleToSingleOperator>(Rename<TFrom, double>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(byte) && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan_1to2<byte, ushort, WidenByteToUInt16Operator>(Rename<TFrom, byte>(source), Rename<TTo, ushort>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(sbyte) && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan_1to2<sbyte, short, WidenSByteToInt16Operator>(Rename<TFrom, sbyte>(source), Rename<TTo, short>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(ushort) && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<ushort, uint, WidenUInt16ToUInt32Operator>(Rename<TFrom, ushort>(source), Rename<TTo, uint>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(short) && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<short, int, WidenInt16ToInt32Operator>(Rename<TFrom, short>(source), Rename<TTo, int>(destination));
                return true;
            }

            if (IsUInt32Like<TTo>() && IsUInt64Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<uint, ulong, WidenUInt32ToUInt64Operator>(Rename<TFrom, uint>(source), Rename<TTo, ulong>(destination));
                return true;
            }

            if (IsInt32Like<TFrom>() && IsInt64Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<int, long, WidenInt32ToInt64Operator>(Rename<TFrom, int>(source), Rename<TTo, long>(destination));
                return true;
            }

            return false;
        }

        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="sign" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="sign"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> sign, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, CopySignOperator<T>>(x, sign, destination);

        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, T sign, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, CopySignOperator<T>>(x, sign, destination);

        /// <summary>Computes the element-wise cosine of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Cos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Cos<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, CosOperator<T>>(x, destination);

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void CosPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, CosPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic cosine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Cosh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is also NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
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
        /// This method effectively computes <c>TensorPrimitives.Dot(x, y) / (<typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(x)) * <typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(y)).</c>
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

        /// <summary>Computes the element-wise cube root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Cbrt(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Cbrt<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanIntoSpan<T, CbrtOperator<T>>(x, destination);

        /// <summary>Computes the element-wise conversion of each number of degrees in the specified tensor to radiansx.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.DegreesToRadians(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void DegreesToRadians<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, DegreesToRadiansOperator<T>>(x, destination);

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
        ///     T result = <typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(difference));
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

        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" /> / <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeScalarSpanIntoSpan<T, DivideOperator<T>>(x, y, destination);

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
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i])</c>.
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

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.ExpM1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void ExpM1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, ExpM1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp2(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp2<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, Exp2Operator<T>>(x, destination);

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp2M1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp2M1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, Exp2M1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp10(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp10<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, Exp10Operator<T>>(x, destination);

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp10M1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp10M1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, Exp10M1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Floor(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Floor<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, FloorOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hypotensue given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Hypot(<paramref name="x" />[i], <paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Hypot<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanSpanIntoSpan<T, HypotOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Ieee754Remainder(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Ieee754Remainder<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanIntoSpan<T, Ieee754RemainderOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Ieee754Remainder(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// </remarks>
        public static void Ieee754Remainder<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarIntoSpan<T, Ieee754RemainderOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Ieee754Remainder(<paramref name="x" />, <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Ieee754Remainder<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeScalarSpanIntoSpan<T, Ieee754RemainderOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.ILogB(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void ILogB<T>(ReadOnlySpan<T> x, Span<int> destination)
            where T : IFloatingPointIeee754<T>
        {
            if (typeof(T) == typeof(double))
            {
                // Special-case double as the only vectorizable floating-point type whose size != sizeof(int).
                InvokeSpanIntoSpan_2to1<double, int, ILogBDoubleOperator>(Rename<T, double>(x), destination);
            }
            else
            {
                InvokeSpanIntoSpan<T, int, ILogBOperator<T>>(x, destination);
            }
        }

        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to NaN
        /// is present, the index of the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMax<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxOperator<T>>(x);

        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the largest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to NaN
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMaxMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxMagnitudeOperator<T>>(x);

        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the minimum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value equal to NaN
        /// is present, the index of the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMin<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMinOperator<T>>(x);

        /// <summary>Searches for the index of the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the smallest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to NaN
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMinMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMinMagnitudeOperator<T>>(x);

        /// <summary>Computes the element-wise leading zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.LeadingZeroCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void LeadingZeroCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, LeadingZeroCountOperator<T>>(x, destination);

        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="amount">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and length of <paramref name="amount" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="amount"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />[i], <paramref name="amount" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanSpanIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="amount">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />[i], <paramref name="amount" />)</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanScalarIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="amount">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="amount" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="amount"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />, <paramref name="amount" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarSpanIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i])</c>.
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
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log2(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its base 2 logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log2<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log2Operator<T>>(x, destination);

        /// <summary>Computes the element-wise base 10 logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log10(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its base 10 logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log10<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log10Operator<T>>(x, destination);

        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of numbers in the specified tensor plus 1.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.LogP1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm plus 1 is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void LogP1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, LogP1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise base 2 logarithm of numbers in the specified tensor plus 1.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log2P1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its base 2 logarithm plus 1 is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log2P1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log2P1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise base 10 logarithm of numbers in the specified tensor plus 1.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log10P1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its base 10 logarithm plus 1 is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log10P1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log10P1Operator<T>>(x, destination);

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanSpanIntoSpan<T, LogBaseOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanScalarIntoSpan<T, LogBaseOperator<T>>(x, y, destination);

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
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
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

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />)</c>.
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
        public static void Max<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, MaxPropagateNaNOperator<T>>(x, y, destination);

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
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        /// <remarks>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MaxMagnitude<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanIntoSpan<T, MaxMagnitudePropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />)</c>.</remarks>
        /// <remarks>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MaxMagnitude<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanScalarIntoSpan<T, MaxMagnitudePropagateNaNOperator<T>>(x, y, destination);

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
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
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

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />)</c>.
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
        public static void Min<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, MinPropagateNaNOperator<T>>(x, y, destination);

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
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MinMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
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

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MinMagnitude(<paramref name="x" />[i], <paramref name="y" />)</c>.</remarks>
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
        public static void MinMagnitude<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanScalarIntoSpan<T, MinMagnitudePropagateNaNOperator<T>>(x, y, destination);

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
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanSpanIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

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
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanScalarIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

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
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarSpanIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

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
        /// This method effectively computes <c><typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(x))</c>.
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

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = ~<paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void OnesComplement<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanIntoSpan<T, OnesComplementOperator<T>>(x, destination);

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.PopCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void PopCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, PopCountOperator<T>>(x, destination);

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeSpanSpanIntoSpan<T, PowOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeSpanScalarIntoSpan<T, PowOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />, <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeScalarSpanIntoSpan<T, PowOperator<T>>(x, y, destination);

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

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.RadiansToDegrees(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void RadiansToDegrees<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, RadiansToDegreesOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void Reciprocal<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, ReciprocalOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalEstimate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalEstimateOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of the square root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalSqrt<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalSqrtOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of the square root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalSqrtEstimate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalSqrtEstimateOperator<T>>(x, destination);

        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RootN(<paramref name="x" />[i], <paramref name="n"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RootN<T>(ReadOnlySpan<T> x, int n, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanIntoSpan(x, new RootNOperator<T>(n), destination);

        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RotateLeft(<paramref name="x" />[i], <paramref name="rotateAmount"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RotateLeft<T>(ReadOnlySpan<T> x, int rotateAmount, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan(x, new RotateLeftOperator<T>(rotateAmount), destination);

        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RotateRight(<paramref name="x" />[i], <paramref name="rotateAmount"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RotateRight<T>(ReadOnlySpan<T> x, int rotateAmount, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan(x, new RotateRightOperator<T>(rotateAmount), destination);

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            Round(x, digits: 0, MidpointRounding.ToEven, destination);

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="mode"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, MidpointRounding mode, Span<T> destination)
            where T : IFloatingPoint<T> =>
            Round(x, digits: 0, mode, destination);

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="digits">The number of fractional digits to which the numbers in <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="digits"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, int digits, Span<T> destination) where T : IFloatingPoint<T> =>
            Round(x, digits, MidpointRounding.ToEven, destination);

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="digits">The number of fractional digits to which the numbers in <paramref name="x" /> should be rounded.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="mode"/> is invalid.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="digits"/>, <paramref name="mode"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, int digits, MidpointRounding mode, Span<T> destination)
            where T : IFloatingPoint<T>
        {
            if (digits == 0)
            {
                switch (mode)
                {
                    case MidpointRounding.ToZero:
                        Truncate(x, destination);
                        return;

                    case MidpointRounding.ToNegativeInfinity:
                        Floor(x, destination);
                        return;

                    case MidpointRounding.ToPositiveInfinity:
                        Ceiling(x, destination);
                        return;

                    case MidpointRounding.AwayFromZero:
                    case MidpointRounding.ToEven:
                        // TODO: Vectorize the remaining modes
                        break;
                }
            }

            if ((uint)mode > (uint)MidpointRounding.ToPositiveInfinity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, typeof(MidpointRounding)), nameof(mode));
            }

            InvokeSpanIntoSpan(x, new RoundOperator<T>(digits, mode), destination);
        }

        /// <summary>Computes the element-wise product of numbers in the specified tensor and their base-radix raised to the specified power.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="n">The value to which base-radix is raised before multipliying x, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.ILogB(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void ScaleB<T>(ReadOnlySpan<T> x, int n, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan(x, new ScaleBOperator<T>(n), destination);

        /// <summary>Computes the element-wise shifting left of numbers in the specified tensor by the specified shift amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="shiftAmount">The number of bits to shift, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] &lt;&lt; <paramref name="shiftAmount"/></c>.
        /// </para>
        /// </remarks>
        public static void ShiftLeft<T>(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            where T : IShiftOperators<T, int, T> =>
            InvokeSpanIntoSpan(x, new ShiftLeftOperator<T>(shiftAmount), destination);

        /// <summary>Computes the element-wise arithmetic (signed) shifting right of numbers in the specified tensor by the specified shift amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="shiftAmount">The number of bits to shift, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] &gt;&gt; <paramref name="shiftAmount"/></c>.
        /// </para>
        /// </remarks>
        public static void ShiftRightArithmetic<T>(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            where T : IShiftOperators<T, int, T> =>
            InvokeSpanIntoSpan(x, new ShiftRightArithmeticOperator<T>(shiftAmount), destination);

        /// <summary>Computes the element-wise logical (unsigned) shifting right of numbers in the specified tensor by the specified shift amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="shiftAmount">The number of bits to shift, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] &gt;&gt;&gt; <paramref name="shiftAmount"/></c>.
        /// </para>
        /// </remarks>
        public static void ShiftRightLogical<T>(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            where T : IShiftOperators<T, int, T> =>
            InvokeSpanIntoSpan(x, new ShiftRightLogicalOperator<T>(shiftAmount), destination);

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1f / (1f + <typeparamref name="T"/>.Exp(-<paramref name="x" />[i]))</c>.
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

        /// <summary>Computes the element-wise sine of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Sin(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sin<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, SinOperator<T>>(x, destination);

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.SinPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SinPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, SinPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Sinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, or <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// the corresponding destination location is set to that value.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi / 180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sinh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, SinhOperator<T>>(x, destination);

        /// <summary>Computes the element-wise sine and cosine of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="sinDestination">The destination tensor for the element-wise sine result, represented as a span.</param>
        /// <param name="cosDestination">The destination tensor for the element-wise cosine result, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="sinDestination"/> or <paramref name="cosDestination" /> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="sinDestination" />[i], <paramref name="cosDestination" />[i]) = <typeparamref name="T"/>.SinCos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SinCos<T>(ReadOnlySpan<T> x, Span<T> sinDestination, Span<T> cosDestination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan_TwoOutputs<T, SinCosOperator<T>>(x, sinDestination, cosDestination);

        /// <summary>Computes the element-wise sine and cosine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="sinPiDestination">The destination tensor for the element-wise sine result, represented as a span.</param>
        /// <param name="cosPiDestination">The destination tensor for the element-wise cosine result, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="sinPiDestination"/> or <paramref name="cosPiDestination" /> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="sinPiDestination" />[i], <paramref name="cosPiDestination" />[i]) = <typeparamref name="T"/>.SinCos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SinCosPi<T>(ReadOnlySpan<T> x, Span<T> sinPiDestination, Span<T> cosPiDestination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan_TwoOutputs<T, SinCosPiOperator<T>>(x, sinPiDestination, cosPiDestination);

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes a sum of <c><typeparamref name="T"/>.Exp(x[i])</c> for all elements in <paramref name="x"/>.
        /// It then effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i]) / sum</c>.
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

        /// <summary>Computes the element-wise square root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Sqrt(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Sqrt<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanIntoSpan<T, SqrtOperator<T>>(x, destination);

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

        /// <summary>Computes the element-wise difference between numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" /> - <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Subtract<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : ISubtractionOperators<T, T, T> =>
            InvokeScalarSpanIntoSpan<T, SubtractOperator<T>>(x, y, destination);

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

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Tan(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tan<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, TanOperator<T>>(x, destination);

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.TanPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void TanPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, TanPiOperator<T>>(x, destination);

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Tanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the corresponding destination location is set to -1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the corresponding destination location is set to 1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the corresponding destination location is set to NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi / 180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tanh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, TanhOperator<T>>(x, destination);

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.TrailingZeroCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void TrailingZeroCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, TrailingZeroCountOperator<T>>(x, destination);

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Truncate(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Truncate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, TruncateOperator<T>>(x, destination);

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] ^ <paramref name="y" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void Xor<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, XorOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] ^ <paramref name="y" /></c>.
        /// </para>
        /// </remarks>
        public static void Xor<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IBitwiseOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, XorOperator<T>>(x, y, destination);
    }
}
