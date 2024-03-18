// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>T &lt;&lt; amount</summary>
        private readonly struct ShiftLeftOperator<T>(int amount) : IStatefulUnaryOperator<T> where T : IShiftOperators<T, int, T>
        {
            private readonly int _amount = amount;

            public static bool Vectorizable => true;

            public T Invoke(T x) => x << _amount;
            public Vector128<T> Invoke(Vector128<T> x) => x << _amount;
            public Vector256<T> Invoke(Vector256<T> x) => x << _amount;
            public Vector512<T> Invoke(Vector512<T> x) => x << _amount;
        }

        /// <summary>T &gt;&gt; amount</summary>
        private readonly struct ShiftRightArithmeticOperator<T>(int amount) : IStatefulUnaryOperator<T> where T : IShiftOperators<T, int, T>
        {
            private readonly int _amount = amount;

            public static bool Vectorizable => true;

            public T Invoke(T x) => x >> _amount;
            public Vector128<T> Invoke(Vector128<T> x) => x >> _amount;
            public Vector256<T> Invoke(Vector256<T> x) => x >> _amount;
            public Vector512<T> Invoke(Vector512<T> x) => x >> _amount;
        }

        /// <summary>T &gt;&gt;&gt; amount</summary>
        private readonly struct ShiftRightLogicalOperator<T>(int amount) : IStatefulUnaryOperator<T> where T : IShiftOperators<T, int, T>
        {
            private readonly int _amount = amount;

            public static bool Vectorizable => true;

            public T Invoke(T x) => x >>> _amount;
            public Vector128<T> Invoke(Vector128<T> x) => x >>> _amount;
            public Vector256<T> Invoke(Vector256<T> x) => x >>> _amount;
            public Vector512<T> Invoke(Vector512<T> x) => x >>> _amount;
        }
    }
}
