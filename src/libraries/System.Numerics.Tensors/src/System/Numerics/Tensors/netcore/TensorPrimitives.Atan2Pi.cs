// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>T.Atan2Pi(y, x)</summary>
        private readonly struct Atan2PiOperator<T> : IBinaryOperator<T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => Atan2Operator<T>.Vectorizable;
            public static T Invoke(T y, T x) => T.Atan2Pi(y, x);
            public static Vector128<T> Invoke(Vector128<T> y, Vector128<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> y, Vector256<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> y, Vector512<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector512.Create(T.Pi);
        }
    }
}
