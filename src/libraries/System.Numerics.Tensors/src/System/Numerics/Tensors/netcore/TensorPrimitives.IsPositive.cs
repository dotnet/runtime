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
        /// <summary>Computes for each value in the specified tensor whether it's positive.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsPositive(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsPositive<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsPositiveOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are positive.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are positive; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPositiveAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            All<T, IsPositiveOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is positive.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is positive; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPositiveAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            Any<T, IsPositiveOperator<T>>(x);

        /// <summary>T.IsPositive(x)</summary>
        private readonly struct IsPositiveOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;
            public static bool Invoke(T x) => T.IsPositive(x);
            public static Vector128<T> Invoke(Vector128<T> x) => IsPositive<T>(x);
            public static Vector256<T> Invoke(Vector256<T> x) => IsPositive<T>(x);
            public static Vector512<T> Invoke(Vector512<T> x) => IsPositive<T>(x);
        }
    }
}
