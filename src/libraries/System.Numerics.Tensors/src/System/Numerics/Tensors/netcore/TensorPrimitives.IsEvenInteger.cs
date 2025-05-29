// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes for each value in the specified tensor whether it's an even integral number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsEvenInteger(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsEvenInteger<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsEvenIntegerOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are even integral numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are even integral numbers; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsEvenIntegerAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            All<T, IsEvenIntegerOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is an even integral number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is an even integral number; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsEvenIntegerAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            Any<T, IsEvenIntegerOperator<T>>(x);

        /// <summary>T.IsEvenInteger(x)</summary>
        private readonly struct IsEvenIntegerOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Invoke(T x) => T.IsEvenInteger(x);

#if NET10_0_OR_GREATER
            public static bool Vectorizable => true;
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.IsEvenInteger(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.IsEvenInteger(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.IsEvenInteger(x);
#else
            public static bool Vectorizable => typeof(T) != typeof(float) && typeof(T) != typeof(double);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Equals(x & Vector128<T>.One, Vector128<T>.Zero);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Equals(x & Vector256<T>.One, Vector256<T>.Zero);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Equals(x & Vector512<T>.One, Vector512<T>.Zero);
#endif

        }
    }
}
