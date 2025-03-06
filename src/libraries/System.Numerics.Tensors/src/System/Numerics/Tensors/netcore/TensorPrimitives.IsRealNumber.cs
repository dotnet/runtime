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
        /// <summary>Computes for each value in the specified tensor whether it's a real number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsRealNumber(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsRealNumber<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T>
        {
            if (AlwaysReal<T>())
            {
                if (x.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                destination.Slice(0, x.Length).Fill(true);
                return;
            }

            InvokeSpanIntoSpan<T, IsRealNumberOperator<T>>(x, destination);
        }

        /// <summary>Computes whether all of the values in the specified tensor are real numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are real numbers; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsRealNumberAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysReal<T>() || All<T, IsRealNumberOperator<T>>(x));

        /// <summary>Computes whether any of the values in the specified tensor is a real number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is a real number; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsRealNumberAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysReal<T>() || Any<T, IsRealNumberOperator<T>>(x));

        /// <summary>Gets whether all values of the specified type are always real.</summary>
        private static bool AlwaysReal<T>() =>
            IsPrimitiveBinaryInteger<T>() ||
            typeof(T) == typeof(decimal);

        /// <summary>T.IsRealNumber(x)</summary>
        private readonly struct IsRealNumberOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;
            public static bool Invoke(T x) => T.IsRealNumber(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ~IsNaN(x);
            public static Vector256<T> Invoke(Vector256<T> x) => ~IsNaN(x);
            public static Vector512<T> Invoke(Vector512<T> x) => ~IsNaN(x);
        }
    }
}
