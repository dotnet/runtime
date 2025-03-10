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
        /// <summary>Computes for each value in the specified tensor whether it's a naN.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsNaN(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsNaN<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T>
        {
            if (!MayBeNaN<T>())
            {
                if (x.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                destination.Slice(0, x.Length).Clear();
                return;
            }

            InvokeSpanIntoSpan<T, IsNaNOperator<T>>(x, destination);
        }

        /// <summary>Computes whether all of the values in the specified tensor are naNs.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are naNs; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNaNAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeNaN<T>() && All<T, IsNaNOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is a naN.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is a naN; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNaNAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            MayBeNaN<T>() && Any<T, IsNaNOperator<T>>(x);

        /// <summary>Gets whether any value could be complex.</summary>
        private static bool MayBeNaN<T>() =>
            !IsPrimitiveBinaryInteger<T>() &&
            typeof(T) != typeof(decimal);

        /// <summary>T.IsNaN(x)</summary>
        private readonly struct IsNaNOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;
            public static bool Invoke(T x) => T.IsNaN(x);
            public static Vector128<T> Invoke(Vector128<T> x) => IsNaN<T>(x);
            public static Vector256<T> Invoke(Vector256<T> x) => IsNaN<T>(x);
            public static Vector512<T> Invoke(Vector512<T> x) => IsNaN<T>(x);
        }
    }
}
