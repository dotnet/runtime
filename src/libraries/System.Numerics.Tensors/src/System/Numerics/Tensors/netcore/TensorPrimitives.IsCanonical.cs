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
        /// <summary>Computes for each value in the specified tensor whether it's in its canonical representation.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsCanonical(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsCanonical<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T>
        {
            if (AlwaysCanonical<T>())
            {
                if (x.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                destination.Slice(0, x.Length).Fill(true);
                return;
            }

            InvokeSpanIntoSpan<T, IsCanonicalOperator<T>>(x, destination);
        }

        /// <summary>Computes whether all of the values in the specified tensor are in their canonical representations.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are in their canonical representations; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsCanonicalAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysCanonical<T>() || All<T, IsCanonicalOperator<T>>(x));

        /// <summary>Computes whether any of the values in the specified tensor is in its canonical representation.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is in its canonical representation; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsCanonicalAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysCanonical<T>() || Any<T, IsCanonicalOperator<T>>(x));

        /// <summary>Gets whether all values of the specified type are always in canonical form.</summary>
        private static bool AlwaysCanonical<T>() =>
            IsPrimitiveFloatingPoint<T>() ||
            IsPrimitiveBinaryInteger<T>();

        /// <summary>T.IsCanonical(x)</summary>
        private readonly struct IsCanonicalOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true; // all vectorizable Ts are always canonical
            public static bool Invoke(T x) => T.IsCanonical(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128<T>.AllBitsSet;
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256<T>.AllBitsSet;
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512<T>.AllBitsSet;
        }
    }
}
