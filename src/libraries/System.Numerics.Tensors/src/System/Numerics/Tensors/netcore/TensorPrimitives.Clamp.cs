// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a span.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a span.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="min" /> and the length of <paramref name="max" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="min"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="max"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />[i], <paramref name="min" />[i], <paramref name="max" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> min, ReadOnlySpan<T> max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanSpanIntoSpan<T, ClampOperatorXMinMax<T>>(x, min, max, destination);

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a span.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a span.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="min" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="min"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />[i], <paramref name="min" />[i], <paramref name="max" />)</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> min, T max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanScalarIntoSpan<T, ClampOperatorXMinMax<T>>(x, min, max, destination);

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a span.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a scalar.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="max" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="max"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />[i], <paramref name="min" />, <paramref name="max" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(ReadOnlySpan<T> x, T min, ReadOnlySpan<T> max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarSpanIntoSpan<T, ClampOperatorXMinMax<T>>(x, min, max, destination);

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a scalar.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a span.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="min" /> must be same as length of <paramref name="max" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="min"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="max"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />, <paramref name="min" />[i], <paramref name="max" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(T x, ReadOnlySpan<T> min, ReadOnlySpan<T> max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanScalarIntoSpan<T, ClampOperatorMinMaxX<T>>(min, max, x, destination);

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a span.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a scalar.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />[i], <paramref name="min" />, <paramref name="max" />)</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(ReadOnlySpan<T> x, T min, T max, Span<T> destination)
            where T : INumber<T>
        {
            if (min > max)
            {
                ThrowHelper.ThrowArgument_MinGreaterThanMax();
            }

            InvokeSpanScalarScalarIntoSpan<T, ClampOperatorXMinMax<T>>(x, min, max, destination);
        }

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a scalar.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a span.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="min"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />, <paramref name="min" />[i], <paramref name="max" />)</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(T x, ReadOnlySpan<T> min, T max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarScalarIntoSpan<T, ClampOperatorMinMaxX<T>>(min, max, x, destination);

        /// <summary>
        /// Computes the element-wise result of clamping <paramref name="x"/> to within the inclusive range specified
        /// by <paramref name="min"/> and <paramref name="max"/> for the specified tensors.
        /// </summary>
        /// <param name="x">The tensor of values to clamp, represented as a scalar.</param>
        /// <param name="min">The tensor of inclusive lower bounds, represented as a scalar.</param>
        /// <param name="max">The tensor of inclusive upper bounds, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">An element-wise <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="max"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Clamp(<paramref name="x" />, <paramref name="min" />, <paramref name="max" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Clamp<T>(T x, T min, ReadOnlySpan<T> max, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarScalarIntoSpan<T, ClampOperatorMaxXMin<T>>(max, x, min, destination);

        /// <summary>T.Clamp(x, min, max)</summary>
        internal readonly struct ClampOperatorXMinMax<T> : ITernaryOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T min, T max) => T.Clamp(x, min, max);

#if NET9_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> min, Vector128<T> max) => Vector128.Clamp(x, min, max);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> min, Vector256<T> max) => Vector256.Clamp(x, min, max);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> min, Vector512<T> max) => Vector512.Clamp(x, min, max);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> min, Vector128<T> max)
            {
                if (Vector128.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> min, Vector256<T> max)
            {
                if (Vector256.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> min, Vector512<T> max)
            {
                if (Vector512.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }
#endif
        }

        /// <summary>T.Clamp(min, max, x)</summary>
        internal readonly struct ClampOperatorMinMaxX<T> : ITernaryOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T min, T max, T x) => T.Clamp(x, min, max);

#if NET9_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> min, Vector128<T> max, Vector128<T> x) => Vector128.Clamp(x, min, max);
            public static Vector256<T> Invoke(Vector256<T> min, Vector256<T> max, Vector256<T> x) => Vector256.Clamp(x, min, max);
            public static Vector512<T> Invoke(Vector512<T> min, Vector512<T> max, Vector512<T> x) => Vector512.Clamp(x, min, max);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> min, Vector128<T> max, Vector128<T> x)
            {
                if (Vector128.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> min, Vector256<T> max, Vector256<T> x)
            {
                if (Vector256.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> min, Vector512<T> max, Vector512<T> x)
            {
                if (Vector512.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }
#endif
        }

        /// <summary>T.Clamp(max, x, min)</summary>
        internal readonly struct ClampOperatorMaxXMin<T> : ITernaryOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T max, T x, T min) => T.Clamp(x, min, max);


#if NET9_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> max, Vector128<T> x, Vector128<T> min) => Vector128.Clamp(x, min, max);
            public static Vector256<T> Invoke(Vector256<T> max, Vector256<T> x, Vector256<T> min) => Vector256.Clamp(x, min, max);
            public static Vector512<T> Invoke(Vector512<T> max, Vector512<T> x, Vector512<T> min) => Vector512.Clamp(x, min, max);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> max, Vector128<T> x, Vector128<T> min)
            {
                if (Vector128.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> max, Vector256<T> x, Vector256<T> min)
            {
                if (Vector256.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> max, Vector512<T> x, Vector512<T> min)
            {
                if (Vector512.GreaterThanAny(min, max))
                {
                    ThrowHelper.ThrowArgument_MinGreaterThanMax();
                }

                return MinOperator<T>.Invoke(MaxOperator<T>.Invoke(x, min), max);
            }
#endif
        }
    }
}
