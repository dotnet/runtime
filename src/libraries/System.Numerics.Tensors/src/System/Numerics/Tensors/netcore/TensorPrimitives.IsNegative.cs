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
        /// <summary>Computes for each value in the specified tensor whether it's negative.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsNegative(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsNegative<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T>
        {
            if (!MayBeNegative<T>())
            {
                if (x.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                destination.Slice(0, x.Length).Clear();
                return;
            }

            InvokeSpanIntoSpan<T, IsNegativeOperator<T>>(x, destination);
        }

        /// <summary>Computes whether all of the values in the specified tensor are negative.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are negative; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNegativeAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeNegative<T>() && All<T, IsNegativeOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is negative.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is negative; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNegativeAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeNegative<T>() && Any<T, IsNegativeOperator<T>>(x);

        /// <summary>Gets whether any value could be complex.</summary>
        private static bool MayBeNegative<T>() =>
            typeof(T) != typeof(byte) &&
            typeof(T) != typeof(ushort) && typeof(T) != typeof(char) &&
            typeof(T) != typeof(uint) &&
            typeof(T) != typeof(ulong) &&
            typeof(T) != typeof(nuint) &&
            typeof(T) != typeof(UInt128);

        /// <summary>T.IsNegative(x)</summary>
        private readonly struct IsNegativeOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;
            public static bool Invoke(T x) => T.IsNegative(x);
            public static Vector128<T> Invoke(Vector128<T> x) => IsNegative<T>(x);
            public static Vector256<T> Invoke(Vector256<T> x) => IsNegative<T>(x);
            public static Vector512<T> Invoke(Vector512<T> x) => IsNegative<T>(x);
        }
    }
}
