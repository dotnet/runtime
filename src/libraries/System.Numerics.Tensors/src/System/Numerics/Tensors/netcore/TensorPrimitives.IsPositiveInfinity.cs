// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes for each value in the specified tensor whether it's positive infinity.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsPositiveInfinity(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsPositiveInfinity<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsPositiveInfinityOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are positive infinity.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are positive infinity; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPositiveInfinityAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBePositiveInfinity<T>() && All<T, IsPositiveInfinityOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is positive infinity.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is positive infinity; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsPositiveInfinityAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBePositiveInfinity<T>() && Any<T, IsPositiveInfinityOperator<T>>(x);

        /// <summary>Gets whether any value could be complex.</summary>
        private static bool MayBePositiveInfinity<T>() =>
            !IsPrimitiveBinaryInteger<T>() &&
            typeof(T) != typeof(decimal);

        /// <summary>T.IsPositiveInfinity(x)</summary>
        private readonly struct IsPositiveInfinityOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static bool Invoke(T x) => T.IsPositiveInfinity(x);

#if NET10_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.IsPositiveInfinity(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.IsPositiveInfinity(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.IsPositiveInfinity(x);
#else
            public static Vector128<T> Invoke(Vector128<T> x) =>
                typeof(T) == typeof(float) ? Vector128.Equals(x, Vector128.Create(float.PositiveInfinity).As<float, T>()) :
                typeof(T) == typeof(double) ? Vector128.Equals(x, Vector128.Create(double.PositiveInfinity).As<double, T>()) :
                Vector128<T>.Zero;

            public static Vector256<T> Invoke(Vector256<T> x) =>
                typeof(T) == typeof(float) ? Vector256.Equals(x, Vector256.Create(float.PositiveInfinity).As<float, T>()) :
                typeof(T) == typeof(double) ? Vector256.Equals(x, Vector256.Create(double.PositiveInfinity).As<double, T>()) :
                Vector256<T>.Zero;

            public static Vector512<T> Invoke(Vector512<T> x) =>
                typeof(T) == typeof(float) ? Vector512.Equals(x, Vector512.Create(float.PositiveInfinity).As<float, T>()) :
                typeof(T) == typeof(double) ? Vector512.Equals(x, Vector512.Create(double.PositiveInfinity).As<double, T>()) :
                Vector512<T>.Zero;
#endif
        }
    }
}
