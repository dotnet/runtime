// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System
{
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Returns the minimum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="T" /> implements <see cref="System.IComparable{T}" />, the <see cref="Min{T}(ReadOnlySpan{T})" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="T" /> implements <see cref="System.IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the minimum value. If the span contains at least one non-null value, the minimum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Min<T>(this ReadOnlySpan<T> span)
        {
            if (typeof(T) == typeof(byte)) return MinMaxInteger<T, byte, MinCalc<byte>>(span);
            if (typeof(T) == typeof(sbyte)) return MinMaxInteger<T, sbyte, MinCalc<sbyte>>(span);
            if (typeof(T) == typeof(ushort)) return MinMaxInteger<T, ushort, MinCalc<ushort>>(span);
            if (typeof(T) == typeof(short)) return MinMaxInteger<T, short, MinCalc<short>>(span);
            if (typeof(T) == typeof(char)) return MinMaxInteger<T, ushort, MinCalc<ushort>>(span);
            if (typeof(T) == typeof(uint)) return MinMaxInteger<T, uint, MinCalc<uint>>(span);
            if (typeof(T) == typeof(int)) return MinMaxInteger<T, int, MinCalc<int>>(span);
            if (typeof(T) == typeof(ulong)) return MinMaxInteger<T, ulong, MinCalc<ulong>>(span);
            if (typeof(T) == typeof(long)) return MinMaxInteger<T, long, MinCalc<long>>(span);
            if (typeof(T) == typeof(nuint)) return MinMaxInteger<T, nuint, MinCalc<nuint>>(span);
            if (typeof(T) == typeof(nint)) return MinMaxInteger<T, nint, MinCalc<nint>>(span);
            if (typeof(T) == typeof(Int128)) return MinMaxInteger<T, Int128, MinCalc<Int128>>(span);
            if (typeof(T) == typeof(UInt128)) return MinMaxInteger<T, UInt128, MinCalc<UInt128>>(span);

            return MinMax<T, MinDirection>(span, Comparer<T>.Default);
        }

        /// <summary>
        /// Returns the minimum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the minimum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values. If <see langword="null"/>, uses <see cref="Comparer{T}.Default"/>.</param>
        /// <returns>The minimum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the minimum value. If the span contains at least one non-null value, the minimum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Min<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            if (comparer is null || comparer == Comparer<T>.Default)
            {
                return Min(span);
            }

            return MinMax<T, MinDirection>(span, comparer);
        }

        /// <summary>
        /// Returns the maximum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="T" /> implements <see cref="System.IComparable{T}" />, the <see cref="Max{T}(ReadOnlySpan{T})" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="T" /> implements <see cref="System.IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the maximum value. If the span contains at least one non-null value, the maximum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Max<T>(this ReadOnlySpan<T> span)
        {
            if (typeof(T) == typeof(byte)) return MinMaxInteger<T, byte, MaxCalc<byte>>(span);
            if (typeof(T) == typeof(sbyte)) return MinMaxInteger<T, sbyte, MaxCalc<sbyte>>(span);
            if (typeof(T) == typeof(ushort)) return MinMaxInteger<T, ushort, MaxCalc<ushort>>(span);
            if (typeof(T) == typeof(short)) return MinMaxInteger<T, short, MaxCalc<short>>(span);
            if (typeof(T) == typeof(char)) return MinMaxInteger<T, ushort, MaxCalc<ushort>>(span);
            if (typeof(T) == typeof(uint)) return MinMaxInteger<T, uint, MaxCalc<uint>>(span);
            if (typeof(T) == typeof(int)) return MinMaxInteger<T, int, MaxCalc<int>>(span);
            if (typeof(T) == typeof(ulong)) return MinMaxInteger<T, ulong, MaxCalc<ulong>>(span);
            if (typeof(T) == typeof(long)) return MinMaxInteger<T, long, MaxCalc<long>>(span);
            if (typeof(T) == typeof(nuint)) return MinMaxInteger<T, nuint, MaxCalc<nuint>>(span);
            if (typeof(T) == typeof(nint)) return MinMaxInteger<T, nint, MaxCalc<nint>>(span);
            if (typeof(T) == typeof(Int128)) return MinMaxInteger<T, Int128, MaxCalc<Int128>>(span);
            if (typeof(T) == typeof(UInt128)) return MinMaxInteger<T, UInt128, MaxCalc<UInt128>>(span);

            return MinMax<T, MaxDirection>(span, Comparer<T>.Default);
        }

        /// <summary>
        /// Returns the maximum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values. If <see langword="null"/>, uses <see cref="Comparer{T}.Default"/>.</param>
        /// <returns>The maximum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the maximum value. If the span contains at least one non-null value, the maximum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Max<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            if (comparer is null || comparer == Comparer<T>.Default)
            {
                return Max(span);
            }

            return MinMax<T, MaxDirection>(span, comparer);
        }

        private interface IMinMaxDirection
        {
            static abstract bool CompareResult(int comparison);
        }

        private readonly struct MinDirection : IMinMaxDirection
        {
            public static bool CompareResult(int comparison) => comparison < 0;
        }

        private readonly struct MaxDirection : IMinMaxDirection
        {
            public static bool CompareResult(int comparison) => comparison > 0;
        }

        private static T? MinMax<T, TDirection>(this ReadOnlySpan<T> span, IComparer<T> comparer)
            where TDirection : struct, IMinMaxDirection
        {
            T? value = default;

            if (value is null)
            {
                int i;

                for (i = 0; i < span.Length; i++)
                {
                    value = span[i];

                    if (value is not null)
                    {
                        break;
                    }
                }

                for (; (uint)i < (uint)span.Length; i++)
                {
                    T next = span[i];
                    if (next is not null && TDirection.CompareResult(comparer.Compare(next, value)))
                    {
                        value = next;
                    }
                }
            }
            else
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NoElements);
                }

                value = span[0];

                if (comparer == Comparer<T>.Default)
                {
                    for (int i = 1; i < span.Length; i++)
                    {
                        T next = span[i];
                        if (TDirection.CompareResult(Comparer<T>.Default.Compare(next, value)))
                        {
                            value = next;
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < span.Length; i++)
                    {
                        T next = span[i];
                        if (TDirection.CompareResult(comparer.Compare(next, value)))
                        {
                            value = next;
                        }
                    }
                }
            }

            return value;
        }

        private interface IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            static abstract bool Compare(T left, T right);
            static abstract Vector128<T> Compare(Vector128<T> left, Vector128<T> right);
            static abstract Vector256<T> Compare(Vector256<T> left, Vector256<T> right);
            static abstract Vector512<T> Compare(Vector512<T> left, Vector512<T> right);
        }

        private readonly struct MinCalc<T> : IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static bool Compare(T left, T right) => left < right;
            public static Vector128<T> Compare(Vector128<T> left, Vector128<T> right) => Vector128.Min(left, right);
            public static Vector256<T> Compare(Vector256<T> left, Vector256<T> right) => Vector256.Min(left, right);
            public static Vector512<T> Compare(Vector512<T> left, Vector512<T> right) => Vector512.Min(left, right);
        }

        private readonly struct MaxCalc<T> : IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static bool Compare(T left, T right) => left > right;
            public static Vector128<T> Compare(Vector128<T> left, Vector128<T> right) => Vector128.Max(left, right);
            public static Vector256<T> Compare(Vector256<T> left, Vector256<T> right) => Vector256.Max(left, right);
            public static Vector512<T> Compare(Vector512<T> left, Vector512<T> right) => Vector512.Max(left, right);
        }

        private static TOuter MinMaxInteger<TOuter, TInner, TMinMax>(this ReadOnlySpan<TOuter> span)
            where TInner : struct, IBinaryInteger<TInner>
            where TMinMax : IMinMaxCalc<TInner> =>
            Unsafe.BitCast<TInner, TOuter>(MinMaxInteger<TInner, TMinMax>(Unsafe.BitCast<ReadOnlySpan<TOuter>, ReadOnlySpan<TInner>>(span)));

        private static T MinMaxInteger<T, TMinMax>(this ReadOnlySpan<T> span)
            where T : struct, IBinaryInteger<T>
            where TMinMax : IMinMaxCalc<T>
        {
            T value;

            if (span.IsEmpty)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NoElements);
            }

            if (!Vector128.IsHardwareAccelerated || !Vector128<T>.IsSupported || span.Length < Vector128<T>.Count)
            {
                value = span[0];
                for (int i = 1; i < span.Length; i++)
                {
                    if (TMinMax.Compare(span[i], value))
                    {
                        value = span[i];
                    }
                }
                return value;
            }

            // All vectorized paths reduce to 128-bit, so we can use that as our accumulator
            // regardless of the maximum supported vector size.
            Vector128<T> best128;

            if (!Vector256.IsHardwareAccelerated || span.Length < Vector256<T>.Count)
            {
                ReadOnlySpan<T> data = span;
                Vector128<T> best = Vector128.Create(data);
                data = data.Slice(Vector128<T>.Count);

                while (data.Length > Vector128<T>.Count)
                {
                    best = TMinMax.Compare(best, Vector128.Create(data));
                    data = data.Slice(Vector128<T>.Count);
                }
                best128 = TMinMax.Compare(best, Vector128.Create(span.Slice(span.Length - Vector128<T>.Count)));
            }
            else if (!Vector512.IsHardwareAccelerated || span.Length < Vector512<T>.Count)
            {
                ReadOnlySpan<T> data = span;
                Vector256<T> best = Vector256.Create(data);
                data = data.Slice(Vector256<T>.Count);

                while (data.Length > Vector256<T>.Count)
                {
                    best = TMinMax.Compare(best, Vector256.Create(data));
                    data = data.Slice(Vector256<T>.Count);
                }
                best = TMinMax.Compare(best, Vector256.Create(span.Slice(span.Length - Vector256<T>.Count)));

                // Reduce to 128-bit
                best128 = TMinMax.Compare(best.GetLower(), best.GetUpper());
            }
            else
            {
                ReadOnlySpan<T> data = span;
                Vector512<T> best = Vector512.Create(data);
                data = data.Slice(Vector512<T>.Count);

                while (data.Length > Vector512<T>.Count)
                {
                    best = TMinMax.Compare(best, Vector512.Create(data));
                    data = data.Slice(Vector512<T>.Count);
                }
                best = TMinMax.Compare(best, Vector512.Create(span.Slice(span.Length - Vector512<T>.Count)));

                // Reduce to 128-bit
                Vector256<T> best256 = TMinMax.Compare(best.GetLower(), best.GetUpper());
                best128 = TMinMax.Compare(best256.GetLower(), best256.GetUpper());
            }

            // Reduce to single value
            value = HorizontalMinMax<T, TMinMax>(best128);

            return value;
        }

        /// <summary>Reduces a <see cref="Vector128{T}"/> to a single element using <typeparamref name="TMinMax"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalMinMax<T, TMinMax>(Vector128<T> x)
            where T : struct, IBinaryInteger<T>
            where TMinMax : IMinMaxCalc<T>
        {
            // Perform log2(Vector128<T>.Count) reductions, each combining the vector with a shuffled
            // copy of itself so that lane 0 ends up holding the min/max of all original lanes.
            if (Vector128<T>.Count == 16)
            {
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsByte(),
                    Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsByte(),
                    Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsByte(),
                    Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsByte(),
                    Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
            }
            else if (Vector128<T>.Count == 8)
            {
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt16(),
                    Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt16(),
                    Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt16(),
                    Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>());
            }
            else if (Vector128<T>.Count == 4)
            {
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>());
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>());
            }
            else
            {
                Debug.Assert(Vector128<T>.Count == 2);
                x = TMinMax.Compare(x, Vector128.Shuffle(x.AsInt64(), Vector128.Create(1, 0)).As<long, T>());
            }
            return x.ToScalar();
        }
    }
}
