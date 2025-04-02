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
        /// <summary>Computes for each value in the specified tensor whether it's normal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.IsNormal(<paramref name="x" />[i])</c>.
        /// </remarks>
        public static void IsNormal<T>(ReadOnlySpan<T> x, Span<bool> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, IsNormalOperator<T>>(x, destination);

        /// <summary>Computes whether all of the values in the specified tensor are normal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if all of the values in <paramref name="x"/> are normal; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNormalAll<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            All<T, IsNormalOperator<T>>(x);

        /// <summary>Computes whether any of the values in the specified tensor is normal.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>
        /// <see langword="true"/> if any of the values in <paramref name="x"/> is normal; otherwise, <see langword="false"/>.
        /// If <paramref name="x"/> is empty, <see langword="false"/> is returned.
        /// </returns>
        public static bool IsNormalAny<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            !x.IsEmpty &&
            Any<T, IsNormalOperator<T>>(x);

        /// <summary>T.IsNormal(x)</summary>
        private readonly struct IsNormalOperator<T> : IBooleanUnaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static bool Invoke(T x) => T.IsNormal(x);

#if NET10_0_OR_GREATER
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.IsNormal(x);

            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.IsNormal(x);

            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.IsNormal(x);
#else
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<uint> smallestNormalBits = Vector128.Create(0x0080_0000u);
                    Vector128<uint> positiveInfinityBits = Vector128.Create(0x7F80_0000u);
                    Vector128<uint> bits = Vector128.Abs(x).AsUInt32();
                    return Vector128.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector128<ulong> smallestNormalBits = Vector128.Create(0x0010_0000_0000_0000ul);
                    Vector128<ulong> positiveInfinityBits = Vector128.Create(0x7FF0_0000_0000_0000ul);
                    Vector128<ulong> bits = Vector128.Abs(x).AsUInt64();
                    return Vector128.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<ulong, T>();
                }

                return ~Vector128.Equals(x, Vector128<T>.Zero);
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<uint> smallestNormalBits = Vector256.Create(0x0080_0000u);
                    Vector256<uint> positiveInfinityBits = Vector256.Create(0x7F80_0000u);
                    Vector256<uint> bits = Vector256.Abs(x).AsUInt32();
                    return Vector256.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<ulong> smallestNormalBits = Vector256.Create(0x0010_0000_0000_0000ul);
                    Vector256<ulong> positiveInfinityBits = Vector256.Create(0x7FF0_0000_0000_0000ul);
                    Vector256<ulong> bits = Vector256.Abs(x).AsUInt64();
                    return Vector256.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<ulong, T>();
                }

                return ~Vector256.Equals(x, Vector256<T>.Zero);
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<uint> smallestNormalBits = Vector512.Create(0x0080_0000u);
                    Vector512<uint> positiveInfinityBits = Vector512.Create(0x7F80_0000u);
                    Vector512<uint> bits = Vector512.Abs(x).AsUInt32();
                    return Vector512.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<uint, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector512<ulong> smallestNormalBits = Vector512.Create(0x0010_0000_0000_0000ul);
                    Vector512<ulong> positiveInfinityBits = Vector512.Create(0x7FF0_0000_0000_0000ul);
                    Vector512<ulong> bits = Vector512.Abs(x).AsUInt64();
                    return Vector512.LessThan(bits - smallestNormalBits, positiveInfinityBits - smallestNormalBits).As<ulong, T>();
                }

                return ~Vector512.Equals(x, Vector512<T>.Zero);
            }
#endif
        }
    }
}
