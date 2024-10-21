// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>x ^ y</summary>
        private readonly struct XorOperator<T> : IBinaryOperator<T> where T : IBitwiseOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x, T y) => x ^ y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x ^ y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x ^ y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x ^ y;
        }
    }
}
