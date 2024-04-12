// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        /// <summary>T.Hypot(x, y)</summary>
        private readonly struct HypotOperator<T> : IBinaryOperator<T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x, T y) => T.Hypot(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => Vector128.Sqrt((x * x) + (y * y));
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => Vector256.Sqrt((x * x) + (y * y));
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => Vector512.Sqrt((x * x) + (y * y));
        }
    }
}
