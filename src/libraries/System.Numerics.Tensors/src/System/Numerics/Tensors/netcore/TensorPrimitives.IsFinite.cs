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
        /// <summary>Computes for each value in the specified tensor whether it's finite.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsFinite(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsFinite<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T>
        {
            if (AlwaysFinite<T>())
            {
                if (x.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                destination.Slice(0, x.Length).Fill(true);
                return;
            }

            InvokeSpanIntoSpan<T, IsFiniteOperator<T>>(x, destination);
        }

        /// <summary>Computes whether all of the values in the specified tensor are finite.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are finite; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsFiniteAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysFinite<T>() || All<T, IsFiniteOperator<T>>(x));

        /// <summary>Computes whether any of the values in the specified tensor is finite.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is finite; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsFiniteAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            (AlwaysFinite<T>() || Any<T, IsFiniteOperator<T>>(x));

        /// <summary>Gets whether all values of the specified type are always finite.</summary>
        private static bool AlwaysFinite<T>() =>
            IsPrimitiveBinaryInteger<T>() ||
            typeof(T) == typeof(decimal);

        /// <summary>T.IsFinite(x)</summary>
        private readonly struct IsFiniteOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static bool Invoke(T x) => T.IsFinite(x);

#if NET10_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.IsFinite(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.IsFinite(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.IsFinite(x);
#else
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<uint> positiveInfinityBits = Vector128.Create(0x7F80_0000u);
                    return (~Vector128.Equals(~x.AsUInt32() & positiveInfinityBits, Vector128<uint>.Zero)).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector128<ulong> positiveInfinityBits = Vector128.Create<ulong>(0x7FF0_0000_0000_0000u);
                    return (~Vector128.Equals(~x.AsUInt64() & positiveInfinityBits, Vector128<ulong>.Zero)).As<ulong, T>();
                }

                return Vector128<T>.AllBitsSet;
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<uint> positiveInfinityBits = Vector256.Create(0x7F80_0000u);
                    return (~Vector256.Equals(~x.AsUInt32() & positiveInfinityBits, Vector256<uint>.Zero)).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<ulong> positiveInfinityBits = Vector256.Create<ulong>(0x7FF0_0000_0000_0000u);
                    return (~Vector256.Equals(~x.AsUInt64() & positiveInfinityBits, Vector256<ulong>.Zero)).As<ulong, T>();
                }

                return Vector256<T>.AllBitsSet;
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<uint> positiveInfinityBits = Vector512.Create(0x7F80_0000u);
                    return (~Vector512.Equals(~x.AsUInt32() & positiveInfinityBits, Vector512<uint>.Zero)).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector512<ulong> positiveInfinityBits = Vector512.Create<ulong>(0x7FF0_0000_0000_0000u);
                    return (~Vector512.Equals(~x.AsUInt64() & positiveInfinityBits, Vector512<ulong>.Zero)).As<ulong, T>();
                }

                return Vector512<T>.AllBitsSet;
            }
#endif
        }
    }
}
