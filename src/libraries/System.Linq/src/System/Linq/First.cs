// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            TSource? first = source.TryGetFirst(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return first!;
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? first = source.TryGetFirst(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return first!;
        }

        public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source) =>
            source.TryGetFirst(out _);

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}" /> to return the first element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            TSource? first = source.TryGetFirst(out bool found);
            return found ? first! : defaultValue;
        }

        public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) =>
            source.TryGetFirst(predicate, out _);

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            TSource? first = source.TryGetFirst(predicate, out bool found);
            return found ? first! : defaultValue;
        }


        private static TSource? TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return
#if !OPTIMIZE_FOR_SIZE
                source is Iterator<TSource> iterator ? iterator.TryGetFirst(out found) :
#endif
                TryGetFirstNonIterator(source, out found);
        }

        private static TSource? TryGetFirstNonIterator<TSource>(IEnumerable<TSource> source, out bool found)
        {
            if (source is IList<TSource> list)
            {
                if (list.Count > 0)
                {
                    found = true;
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        found = true;
                        return e.Current;
                    }
                }
            }

            found = false;
            return default;
        }

        private static TSource? TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    found = true;
                    return element;
                }
            }

            found = false;
            return default;
        }
    }
}
