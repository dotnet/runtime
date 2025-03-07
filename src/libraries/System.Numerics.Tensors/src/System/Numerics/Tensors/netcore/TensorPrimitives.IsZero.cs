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
        /// <summary>Computes for each value in the specified tensor whether it's zero.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsZero(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsZero<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsZeroOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are zero.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are zero; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsZeroAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            All<T, IsZeroOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is zero.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is zero; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsZeroAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            Any<T, IsZeroOperator<T>>(x);

        /// <summary>T.IsZero(x)</summary>
        private readonly struct IsZeroOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;
            public static bool Invoke(T x) => T.IsZero(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Equals(x, Vector128<T>.Zero);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Equals(x, Vector256<T>.Zero);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Equals(x, Vector512<T>.Zero);
        }
    }
}
