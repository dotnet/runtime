// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes for each value in the specified tensor whether it's subnormal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsSubnormal(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsSubnormal<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsSubnormalOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are subnormal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are subnormal; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsSubnormalAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeSubnormal<T>() && All<T, IsSubnormalOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is subnormal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is subnormal; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsSubnormalAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeSubnormal<T>() && Any<T, IsSubnormalOperator<T>>(x);

        /// <summary>Gets whether any value could be complex.</summary>
        private static bool MayBeSubnormal<T>() =>
            !IsPrimitiveBinaryInteger<T>() &&
            typeof(T) != typeof(decimal);

        /// <summary>T.IsSubnormal(x)</summary>
        private readonly struct IsSubnormalOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static bool Invoke(T x) => T.IsSubnormal(x);

            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.IsSubnormal(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.IsSubnormal(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.IsSubnormal(x);
        }
    }
}
