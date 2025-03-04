// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> or <paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] % <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void Remainder<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IModulusOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, RemainderOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] % <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void Remainder<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IModulusOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, RemainderOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" /> % <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void Remainder<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IModulusOperators<T, T, T> =>
            InvokeScalarSpanIntoSpan<T, RemainderOperator<T>>(x, y, destination);

        /// <summary>x % y</summary>
        internal readonly struct RemainderOperator<T> : IBinaryOperator<T>
            where T : IModulusOperators<T, T, T>
        {
            public static T Invoke(T x, T y) => x % y;

            public static bool Vectorizable => true;

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) =>
                typeof(T) == typeof(float) ? x - (TruncateOperator<float>.Invoke((x / y).AsSingle()).As<float, T>() * y) :
                typeof(T) == typeof(double) ? x - (TruncateOperator<double>.Invoke((x / y).AsDouble()).As<double, T>() * y) :
                x - ((x / y) * y);

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) =>
                typeof(T) == typeof(float) ? x - (TruncateOperator<float>.Invoke((x / y).AsSingle()).As<float, T>() * y) :
                typeof(T) == typeof(double) ? x - (TruncateOperator<double>.Invoke((x / y).AsDouble()).As<double, T>() * y) :
                x - ((x / y) * y);

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) =>
                typeof(T) == typeof(float) ? x - (TruncateOperator<float>.Invoke((x / y).AsSingle()).As<float, T>() * y) :
                typeof(T) == typeof(double) ? x - (TruncateOperator<double>.Invoke((x / y).AsDouble()).As<double, T>() * y) :
                x - ((x / y) * y);

        }
    }
}
