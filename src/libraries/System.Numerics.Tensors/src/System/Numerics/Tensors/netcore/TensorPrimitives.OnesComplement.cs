// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>~x</summary>
        private readonly struct OnesComplementOperator<T> : IUnaryOperator<T, T> where T : IBitwiseOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => ~x;
            public static Vector128<T> Invoke(Vector128<T> x) => ~x;
            public static Vector256<T> Invoke(Vector256<T> x) => ~x;
            public static Vector512<T> Invoke(Vector512<T> x) => ~x;
        }
    }
}
