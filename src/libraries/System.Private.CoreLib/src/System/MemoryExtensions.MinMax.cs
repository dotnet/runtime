// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? Min<T>(this ReadOnlySpan<T> span) =>
            Min(span, comparer: null);

        /// <summary>
        /// Returns the minimum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the minimum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values.</param>
        /// <returns>The minimum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the minimum value. If the span contains at least one non-null value, the minimum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Min<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            comparer ??= Comparer<T>.Default;

            if (typeof(T) == typeof(byte) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<byte, MinCalc<byte>>(Cast<T, byte>(span));
            if (typeof(T) == typeof(sbyte) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<sbyte, MinCalc<sbyte>>(Cast<T, sbyte>(span));
            if (typeof(T) == typeof(ushort) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<ushort, MinCalc<ushort>>(Cast<T, ushort>(span));
            if (typeof(T) == typeof(short) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<short, MinCalc<short>>(Cast<T, short>(span));
            if (typeof(T) == typeof(char) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<char, MinCalc<char>>(Cast<T, char>(span));
            if (typeof(T) == typeof(uint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<uint, MinCalc<uint>>(Cast<T, uint>(span));
            if (typeof(T) == typeof(int) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<int, MinCalc<int>>(Cast<T, int>(span));
            if (typeof(T) == typeof(ulong) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<ulong, MinCalc<ulong>>(Cast<T, ulong>(span));
            if (typeof(T) == typeof(long) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<long, MinCalc<long>>(Cast<T, long>(span));
            if (typeof(T) == typeof(nuint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<nuint, MinCalc<nuint>>(Cast<T, nuint>(span));
            if (typeof(T) == typeof(nint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<nint, MinCalc<nint>>(Cast<T, nint>(span));
            if (typeof(T) == typeof(Int128) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<Int128, MinCalc<Int128>>(Cast<T, Int128>(span));
            if (typeof(T) == typeof(UInt128) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<UInt128, MinCalc<UInt128>>(Cast<T, UInt128>(span));

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? Max<T>(this ReadOnlySpan<T> span) =>
            Max(span, comparer: null);

        /// <summary>
        /// Returns the maximum value in the span.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values.</param>
        /// <returns>The maximum value in the span.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="span"/> is empty and <typeparamref name="T"/> is a non-nullable value type.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="T" /> is a reference type and the span sequence is empty, this method returns <see langword="null" />.</para>
        /// <para>Null values are ignored when determining the maximum value. If the span contains at least one non-null value, the maximum of those values is returned. If the span does not contain any non-null values, <see langword="null" /> is returned.</para>
        /// </remarks>
        public static T? Max<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            comparer ??= Comparer<T>.Default;

            if (typeof(T) == typeof(byte) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<byte, MaxCalc<byte>>(Cast<T, byte>(span));
            if (typeof(T) == typeof(sbyte) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<sbyte, MaxCalc<sbyte>>(Cast<T, sbyte>(span));
            if (typeof(T) == typeof(ushort) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<ushort, MaxCalc<ushort>>(Cast<T, ushort>(span));
            if (typeof(T) == typeof(short) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<short, MaxCalc<short>>(Cast<T, short>(span));
            if (typeof(T) == typeof(char) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<char, MaxCalc<char>>(Cast<T, char>(span));
            if (typeof(T) == typeof(uint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<uint, MaxCalc<uint>>(Cast<T, uint>(span));
            if (typeof(T) == typeof(int) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<int, MaxCalc<int>>(Cast<T, int>(span));
            if (typeof(T) == typeof(ulong) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<ulong, MaxCalc<ulong>>(Cast<T, ulong>(span));
            if (typeof(T) == typeof(long) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<long, MaxCalc<long>>(Cast<T, long>(span));
            if (typeof(T) == typeof(nuint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<nuint, MaxCalc<nuint>>(Cast<T, nuint>(span));
            if (typeof(T) == typeof(nint) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<nint, MaxCalc<nint>>(Cast<T, nint>(span));
            if (typeof(T) == typeof(Int128) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<Int128, MaxCalc<Int128>>(Cast<T, Int128>(span));
            if (typeof(T) == typeof(UInt128) && comparer == Comparer<T>.Default) return (T)(object)MinMaxInteger<UInt128, MaxCalc<UInt128>>(Cast<T, UInt128>(span));

            return MinMax<T, MaxDirection>(span, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span)
            where TTo : struct
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)),
                span.Length);

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
            int i = 0;

            if (value is null)
            {
                do
                {
                    if (i >= span.Length)
                    {
                        return value;
                    }

                    value = span[i++];
                }
                while (value is null);

                while (i < span.Length)
                {
                    T next = span[i++];
                    if (next is not null && TDirection.CompareResult(comparer.Compare(next, value)))
                    {
                        value = next;
                    }
                }
            }
            else
            {
                if (i >= span.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NoElements);
                }

                value = span[i++];
                if (comparer == Comparer<T>.Default)
                {
                    while (i < span.Length)
                    {
                        T next = span[i++];
                        if (TDirection.CompareResult(Comparer<T>.Default.Compare(next, value)))
                        {
                            value = next;
                        }
                    }
                }
                else
                {
                    while (i < span.Length)
                    {
                        T next = span[i++];
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
            public static abstract bool Compare(T left, T right);
            public static abstract Vector128<T> Compare(Vector128<T> left, Vector128<T> right);
            public static abstract Vector256<T> Compare(Vector256<T> left, Vector256<T> right);
            public static abstract Vector512<T> Compare(Vector512<T> left, Vector512<T> right);
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
