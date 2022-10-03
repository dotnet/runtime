// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static int Max(this IEnumerable<int> source) => MaxInteger(source);

        public static int? Max(this IEnumerable<int?> source) => MaxInteger(source);

        public static long Max(this IEnumerable<long> source) => MaxInteger(source);

        public static long? Max(this IEnumerable<long?> source) => MaxInteger(source);

        private static T MaxInteger<T>(this IEnumerable<T> source) where T : struct, IBinaryInteger<T>
        {
            T value;

            if (source.TryGetSpan(out ReadOnlySpan<T> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                // Vectorize the search if possible.
                int index;
                if (Vector.IsHardwareAccelerated && span.Length >= Vector<T>.Count * 2)
                {
                    // The span is at least two vectors long. Create a vector from the first N elements,
                    // and then repeatedly compare that against the next vector from the span.  At the end,
                    // the resulting vector will contain the maximum values found, and we then need only
                    // to find the max of those.
                    var maxes = new Vector<T>(span);
                    index = Vector<T>.Count;
                    do
                    {
                        maxes = Vector.Max(maxes, new Vector<T>(span.Slice(index)));
                        index += Vector<T>.Count;
                    }
                    while (index + Vector<T>.Count <= span.Length);

                    value = maxes[0];
                    for (int i = 1; i < Vector<T>.Count; i++)
                    {
                        if (maxes[i] > value)
                        {
                            value = maxes[i];
                        }
                    }
                }
                else
                {
                    value = span[0];
                    index = 1;
                }

                // Iterate through the remaining elements, comparing against the max.
                for (int i = index; (uint)i < (uint)span.Length; i++)
                {
                    if (span[i] > value)
                    {
                        value = span[i];
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
                while (e.MoveNext())
                {
                    T x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        private static T? MaxInteger<T>(this IEnumerable<T?> source) where T : struct, IBinaryInteger<T>
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
                if (valueVal >= T.Zero)
                {
                    // To avoid having to check cur.HasValue every iteration of the loop,
                    // we special-case the circumstance where the first value we found
                    // is >= 0.  We can then compare its value against the value stored in
                    // all subsequent nullables, regardless of whether they're null or not,
                    // because if they are null, the value will be 0 and the comparison will
                    // still be accurate.
                    while (e.MoveNext())
                    {
                        T? cur = e.Current;
                        T x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        T? cur = e.Current;
                        T x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        public static double Max(this IEnumerable<double> source) => MaxFloat(source);

        public static double? Max(this IEnumerable<double?> source) => MaxFloat(source);

        public static float Max(this IEnumerable<float> source) => MaxFloat(source);

        public static float? Max(this IEnumerable<float?> source) => MaxFloat(source);

        private static T MaxFloat<T>(this IEnumerable<T> source) where T : struct, IFloatingPointIeee754<T>
        {
            T value;

            if (source.TryGetSpan(out ReadOnlySpan<T> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                int i;
                for (i = 0; i < span.Length && T.IsNaN(span[i]); i++) ;

                if (i == span.Length)
                {
                    return span[^1];
                }

                for (value = span[i]; (uint)i < (uint)span.Length; i++)
                {
                    if (span[i] > value)
                    {
                        value = span[i];
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

                // As described in a comment on Min(this IEnumerable<T>) NaN is ordered
                // less than all other values. We need to do explicit checks to ensure this, but
                // once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                value = e.Current;
                while (T.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }

                while (e.MoveNext())
                {
                    T x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        private static T? MaxFloat<T>(this IEnumerable<T?> source) where T : struct, IFloatingPointIeee754<T>
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
                while (T.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    T? cur = e.Current;
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    T? cur = e.Current;
                    T x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static decimal Max(this IEnumerable<decimal> source)
        {
            decimal value;

            if (source.TryGetSpan(out ReadOnlySpan<decimal> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = span[0];
                for (int i = 1; (uint)i < (uint)span.Length; i++)
                {
                    if (span[i] > value)
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
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }


        public static decimal? Max(this IEnumerable<decimal?> source)
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
                    if (cur.HasValue && x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TSource? Max<TSource>(this IEnumerable<TSource> source) => Max(source, comparer: null);

        /// <summary>Returns the maximum value in a generic sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No object in <paramref name="source" /> implements the <see cref="System.IComparable" /> or <see cref="System.IComparable{T}" /> interface.</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="TSource" /> implements <see cref="System.IComparable{T}" />, the <see cref="Max{T}(IEnumerable{T})" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="TSource" /> implements <see cref="System.IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="TSource" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        public static TSource? Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            comparer ??= Comparer<TSource>.Default;

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
                        if (next != null && comparer.Compare(next, value) > 0)
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
                            if (Comparer<TSource>.Default.Compare(next, value) > 0)
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
                            if (comparer.Compare(next, value) > 0)
                            {
                                value = next;
                            }
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>The value with the maximum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="System.IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MaxBy(source, keySelector, null);

        /// <summary>Returns the maximum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <returns>The value with the maximum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
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
                    if (nextKey != null && comparer.Compare(nextKey, key) > 0)
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
                        if (Comparer<TKey>.Default.Compare(nextKey, key) > 0)
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
                        if (comparer.Compare(nextKey, key) > 0)
                        {
                            key = nextKey;
                            value = nextValue;
                        }
                    }
                }
            }

            return value;
        }

        public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => MaxInteger(source, selector);

        public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => MaxInteger(source, selector);

        public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => MaxInteger(source, selector);

        public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => MaxInteger(source, selector);

        private static TResult MaxInteger<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct, IBinaryInteger<TResult>
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
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        private static TResult? MaxInteger<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector) where TResult : struct, IBinaryInteger<TResult>
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
                if (valueVal >= TResult.Zero)
                {
                    // We can fast-path this case where we know HasValue will
                    // never affect the outcome, without constantly checking
                    // if we're in such a state. Similar fast-paths could
                    // be done for other cases, but as all-positive
                    // or mostly-positive integer values are quite common in real-world
                    // uses, it's only been done in this direction for int? and long?.
                    while (e.MoveNext())
                    {
                        TResult? cur = selector(e.Current);
                        TResult x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        TResult? cur = selector(e.Current);
                        TResult x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) => MaxFloat(source, selector);

        public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) => MaxFloat(source, selector);

        public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) => MaxFloat(source, selector);

        public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) => MaxFloat(source, selector);

        private static TResult MaxFloat<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct, IFloatingPointIeee754<TResult>
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
                while (TResult.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }

                while (e.MoveNext())
                {
                    TResult x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        private static TResult? MaxFloat<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector) where TResult : struct, IFloatingPointIeee754<TResult>
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
                while (TResult.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    TResult? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    TResult? cur = selector(e.Current);
                    TResult x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
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
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
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
                    if (cur.HasValue && x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TResult? Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
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
                        if (x != null && comparer.Compare(x, value) > 0)
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
                        if (Comparer<TResult>.Default.Compare(x, value) > 0)
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
