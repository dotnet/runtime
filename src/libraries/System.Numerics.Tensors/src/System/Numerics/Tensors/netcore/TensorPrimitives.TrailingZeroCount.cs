// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>T.TrailingZeroCount(x)</summary>
        private readonly struct TrailingZeroCountOperator<T> : IUnaryOperator<T, T> where T : IBinaryInteger<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.TrailingZeroCount(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }
    }
}
