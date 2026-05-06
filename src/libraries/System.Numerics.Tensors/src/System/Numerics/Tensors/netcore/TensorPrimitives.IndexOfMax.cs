// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to NaN
        /// is present, the index of the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMax<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxOperator<T>>(x);

        /// <summary>Returns the index of MathF.Max(x, y)</summary>
        internal readonly struct IndexOfMaxOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Compare(T x, T y)
            {
                if (x == y)
                {
                    return T.IsPositive(x) && T.IsNegative(y);
                }
                else
                {
                    return x > y;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Compare(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector128<T> equalResult = IsPositive(x) & IsNegative(y);
                    return Vector128.GreaterThan(x, y) | (Vector128.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector128.GreaterThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Compare(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector256<T> equalResult = IsPositive(x) & IsNegative(y);
                    return Vector256.GreaterThan(x, y) | (Vector256.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector256.GreaterThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Compare(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector512<T> equalResult = IsPositive(x) & IsNegative(y);
                    return Vector512.GreaterThan(x, y) | (Vector512.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector512.GreaterThan(x, y);
                }
            }
        }

        private static int IndexOfFirstMatch<T>(Vector128<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector256<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector512<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<T> ElementWiseSelect<T>(Vector128<T> mask, Vector128<T> left, Vector128<T> right)
        {
            if (Sse41.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Sse41.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Sse41.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 1) return Sse41.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (sizeof(T) == 2) return Sse41.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (sizeof(T) == 4) return Sse41.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Sse41.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector128.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<T> ElementWiseSelect<T>(Vector256<T> mask, Vector256<T> left, Vector256<T> right)
        {
            if (Avx2.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Avx2.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx2.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 1) return Avx2.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (sizeof(T) == 2) return Avx2.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (sizeof(T) == 4) return Avx2.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Avx2.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector256.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector512<T> ElementWiseSelect<T>(Vector512<T> mask, Vector512<T> left, Vector512<T> right)
        {
            if (Avx512F.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Avx512F.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx512F.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 4) return Avx512F.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Avx512F.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector512.ConditionalSelect(mask, left, right);
        }
    }
}
