// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static int Min(this IEnumerable<int> source) => MinMaxInteger<int, MinCalc<int>>(source);

        public static long Min(this IEnumerable<long> source) => MinMaxInteger<long, MinCalc<long>>(source);

        private readonly struct MinCalc<T> : IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static bool Compare(T left, T right) => left < right;
            public static Vector128<T> Compare(Vector128<T> left, Vector128<T> right) => Vector128.Min(left, right);
            public static Vector256<T> Compare(Vector256<T> left, Vector256<T> right) => Vector256.Min(left, right);
            public static Vector512<T> Compare(Vector512<T> left, Vector512<T> right) => Vector512.Min(left, right);
        }

        public static int? Min(this IEnumerable<int?> source) => MinInteger(source);

        public static long? Min(this IEnumerable<long?> source) => MinInteger(source);

        private static T? MinInteger<T>(this IEnumerable<T?> source) where T : struct, IBinaryInteger<T>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            T? value = null;
            using (IEnumerator<T?> e = source.GetEnumerator())
            {
                // Start off knowing that we have a non-null value (or exit here, knowing we don't)
                // so we don't have to keep testing for nullity.
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                // Keep hold of the wrapped value, and do comparisons on that, rather than
                // using the lifted operation each time.
                T valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    T? cur = e.Current;
                    T x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static float Min(this IEnumerable<float> source) => MinFloat(source);

        public static float? Min(this IEnumerable<float?> source) => MinFloat(source);

        public static double Min(this IEnumerable<double> source) => MinFloat(source);

        public static double? Min(this IEnumerable<double?> source) => MinFloat(source);

        private static T MinFloat<T>(this IEnumerable<T> source) where T : struct, IFloatingPointIeee754<T>
        {
            T value;

            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source.TryGetSpan(out ReadOnlySpan<T> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = span[0];
                for (int i = 1; (uint)i < (uint)span.Length; i++)
                {
                    T current = span[i];
                    if (current < value)
                    {
                        value = current;
                    }
                    else if (T.IsNaN(current))
                    {
                        return current;
                    }
                }

                return value;
            }

            using (IEnumerator<T> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                if (T.IsNaN(value))
                {
                    return value;
                }

                while (e.MoveNext())
                {
                    T x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity.
                    // Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (T.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        private static T? MinFloat<T>(this IEnumerable<T?> source) where T : struct, IFloatingPointIeee754<T>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            T? value = null;
            using (IEnumerator<T?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                T valueVal = value.GetValueOrDefault();
                if (T.IsNaN(valueVal))
                {
                    return value;
                }

                while (e.MoveNext())
                {
                    T? cur = e.Current;
                    if (cur.HasValue)
                    {
                        T x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (T.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static decimal Min(this IEnumerable<decimal> source)
        {
            decimal value;

            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source.TryGetSpan(out ReadOnlySpan<decimal> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = span[0];
                for (int i = 1; (uint)i < (uint)span.Length; i++)
                {
                    if (span[i] < value)
                    {
                        value = span[i];
                    }
                }

                return value;
            }

            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    decimal x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static decimal? Min(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal? value = null;
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = e.Current;
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TSource? Min<TSource>(this IEnumerable<TSource> source) => Min(source, comparer: null);

        /// <summary>Returns the minimum value in a generic sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No object in <paramref name="source" /> implements the <see cref="System.IComparable" /> or <see cref="System.IComparable{T}" /> interface.</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="TSource" /> implements <see cref="System.IComparable{T}" />, the <see cref="Min{T}(IEnumerable{T})" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="TSource" /> implements <see cref="System.IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="TSource" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Min()` clause translates to an invocation of <see cref="O:Enumerable.Min" />.</para>
        /// </remarks>
        public static TSource? Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            comparer ??= Comparer<TSource>.Default;

            // TODO https://github.com/dotnet/csharplang/discussions/6308: Update this to use generic constraint bridging if/when available.
            if (typeof(TSource) == typeof(byte) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<byte, MinCalc<byte>>((IEnumerable<byte>)source);
            if (typeof(TSource) == typeof(sbyte) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<sbyte, MinCalc<sbyte>>((IEnumerable<sbyte>)source);
            if (typeof(TSource) == typeof(ushort) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<ushort, MinCalc<ushort>>((IEnumerable<ushort>)source);
            if (typeof(TSource) == typeof(short) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<short, MinCalc<short>>((IEnumerable<short>)source);
            if (typeof(TSource) == typeof(char) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<char, MinCalc<char>>((IEnumerable<char>)source);
            if (typeof(TSource) == typeof(uint) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<uint, MinCalc<uint>>((IEnumerable<uint>)source);
            if (typeof(TSource) == typeof(int) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<int, MinCalc<int>>((IEnumerable<int>)source);
            if (typeof(TSource) == typeof(ulong) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<ulong, MinCalc<ulong>>((IEnumerable<ulong>)source);
            if (typeof(TSource) == typeof(long) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<long, MinCalc<long>>((IEnumerable<long>)source);
            if (typeof(TSource) == typeof(nuint) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<nuint, MinCalc<nuint>>((IEnumerable<nuint>)source);
            if (typeof(TSource) == typeof(nint) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<nint, MinCalc<nint>>((IEnumerable<nint>)source);
            if (typeof(TSource) == typeof(Int128) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<Int128, MinCalc<Int128>>((IEnumerable<Int128>)source);
            if (typeof(TSource) == typeof(UInt128) && comparer == Comparer<TSource>.Default) return (TSource)(object)MinMaxInteger<UInt128, MinCalc<UInt128>>((IEnumerable<UInt128>)source);

            TSource? value = default;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (value == null)
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = e.Current;
                    }
                    while (value == null);

                    while (e.MoveNext())
                    {
                        TSource next = e.Current;
                        if (next != null && comparer.Compare(next, value) < 0)
                        {
                            value = next;
                        }
                    }
                }
                else
                {
                    if (!e.MoveNext())
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    value = e.Current;
                    if (comparer == Comparer<TSource>.Default)
                    {
                        while (e.MoveNext())
                        {
                            TSource next = e.Current;
                            if (Comparer<TSource>.Default.Compare(next, value) < 0)
                            {
                                value = next;
                            }
                        }
                    }
                    else
                    {
                        while (e.MoveNext())
                        {
                            TSource next = e.Current;
                            if (comparer.Compare(next, value) < 0)
                            {
                                value = next;
                            }
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="System.IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MinBy(source, keySelector, comparer: null);

        /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            comparer ??= Comparer<TKey>.Default;

            using IEnumerator<TSource> e = source.GetEnumerator();

            if (!e.MoveNext())
            {
                if (default(TSource) is null)
                {
                    return default;
                }
                else
                {
                    ThrowHelper.ThrowNoElementsException();
                }
            }

            TSource value = e.Current;
            TKey key = keySelector(value);

            if (default(TKey) is null)
            {
                if (key == null)
                {
                    TSource firstValue = value;

                    do
                    {
                        if (!e.MoveNext())
                        {
                            // All keys are null, surface the first element.
                            return firstValue;
                        }

                        value = e.Current;
                        key = keySelector(value);
                    }
                    while (key == null);
                }

                while (e.MoveNext())
                {
                    TSource nextValue = e.Current;
                    TKey nextKey = keySelector(nextValue);
                    if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
            else
            {
                if (comparer == Comparer<TKey>.Default)
                {
                    while (e.MoveNext())
                    {
                        TSource nextValue = e.Current;
                        TKey nextKey = keySelector(nextValue);
                        if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
                        {
                            key = nextKey;
                            value = nextValue;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        TSource nextValue = e.Current;
                        TKey nextKey = keySelector(nextValue);
                        if (comparer.Compare(nextKey, key) < 0)
                        {
                            key = nextKey;
                            value = nextValue;
                        }
                    }
                }
            }

            return value;
        }

        public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => MinInteger(source, selector);

        public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => MinInteger(source, selector);

        public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => MinInteger(source, selector);

        public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => MinInteger(source, selector);

        private static TResult MinInteger<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct, IBinaryInteger<TResult>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    TResult x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        private static TResult? MinInteger<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector) where TResult : struct, IBinaryInteger<TResult>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                // Start off knowing that we've a non-null value (or exit here, knowing we don't)
                // so we don't have to keep testing for nullity.
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                // Keep hold of the wrapped value, and do comparisons on that, rather than
                // using the lifted operation each time.
                TResult valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    TResult? cur = selector(e.Current);
                    TResult x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) => MinFloat(source, selector);

        public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) => MinFloat(source, selector);

        public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) => MinFloat(source, selector);

        public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) => MinFloat(source, selector);

        private static TResult MinFloat<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct, IFloatingPointIeee754<TResult>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                if (TResult.IsNaN(value))
                {
                    return value;
                }

                while (e.MoveNext())
                {
                    TResult x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity.
                    // Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (TResult.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        private static TResult? MinFloat<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector) where TResult : struct, IFloatingPointIeee754<TResult>
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                TResult valueVal = value.GetValueOrDefault();
                if (TResult.IsNaN(valueVal))
                {
                    return value;
                }

                while (e.MoveNext())
                {
                    TResult? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        TResult x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (TResult.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    decimal x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = selector(e.Current);
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TResult? Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult? value = default;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (value == null)
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = selector(e.Current);
                    }
                    while (value == null);

                    Comparer<TResult> comparer = Comparer<TResult>.Default;
                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (x != null && comparer.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
                else
                {
                    if (!e.MoveNext())
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    value = selector(e.Current);
                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (Comparer<TResult>.Default.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
            }

            return value;
        }
    }
}
