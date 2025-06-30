// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes for each value in the specified tensor whether it's a power of two.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsPow2(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsPow2<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : IBinaryNumber<T> =>
            InvokeSpanIntoSpan<T, IsPow2Operator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are powers of two.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are powers of two; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPow2All<T>(ReadOnlySpan<T> x)
            where T : IBinaryNumber<T> =>
            !x.IsEmpty &&
            All<T, IsPow2Operator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor are powers of two.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> are powers of two; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPow2Any<T>(ReadOnlySpan<T> x)
            where T : IBinaryNumber<T> =>
            !x.IsEmpty &&
            Any<T, IsPow2Operator<T>>(x);

        /// <summary>T.IsPow2(x)</summary>
        private readonly struct IsPow2Operator<T> : IBooleanUnaryOperator<T>
            where T : IBinaryNumber<T>
        {
            public static bool Vectorizable =>
                // TODO: Vectorize for float and double
                typeof(T) != typeof(float) && typeof(T) != typeof(double);

            public static bool Invoke(T x) => T.IsPow2(x);

            public static Vector128<T> Invoke(Vector128<T> x) =>
                Vector128.Equals(x & (x - Vector128<T>.One), Vector128<T>.Zero) &
                Vector128.GreaterThan(x, Vector128<T>.Zero);

            public static Vector256<T> Invoke(Vector256<T> x) =>
                Vector256.Equals(x & (x - Vector256<T>.One), Vector256<T>.Zero) &
                Vector256.GreaterThan(x, Vector256<T>.Zero);

            public static Vector512<T> Invoke(Vector512<T> x) =>
                Vector512.Equals(x & (x - Vector512<T>.One), Vector512<T>.Zero) &
                Vector512.GreaterThan(x, Vector512<T>.Zero);
        }
    }
}
