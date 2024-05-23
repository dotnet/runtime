// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            TSource? single = source.TryGetSingle(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return single!;
        }
        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? single = source.TryGetSingle(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return single!;
        }

        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source)
            => source.TryGetSingle(out _);

        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return the single element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The single element of the input sequence, or <paramref name="defaultValue" /> if the sequence contains no elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            var single = source.TryGetSingle(out bool found);
            return found ? single! : defaultValue;
        }

        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            => source.TryGetSingle(predicate, out _);

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The single element of the input sequence that satisfies the condition, or <paramref name="defaultValue" /> if no such element is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            var single = source.TryGetSingle(predicate, out bool found);
            return found ? single! : defaultValue;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IList<TSource> list)
            {
                switch (list.Count)
                {
                    case 0:
                        found = false;
                        return default;
                    case 1:
                        found = true;
                        return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    TSource result = e.Current;
                    if (!e.MoveNext())
                    {
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            ThrowHelper.ThrowMoreThanOneElementException();
            return default;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource result = e.Current;
                    if (predicate(result))
                    {
                        while (e.MoveNext())
                        {
                            if (predicate(e.Current))
                            {
                                ThrowHelper.ThrowMoreThanOneMatchException();
                            }
                        }
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            return default;
        }
    }
}
