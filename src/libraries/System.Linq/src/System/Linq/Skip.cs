// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            if (count <= 0)
            {
                // Return source if not actually skipping, but only if it's a type from here, to avoid
                // issues if collections are used as keys or otherwise must not be aliased.
                if (source is Iterator<TSource>)
                {
                    return source;
                }

                count = 0;
            }
#if !OPTIMIZE_FOR_SIZE
            else if (source is Iterator<TSource> iterator)
            {
                return iterator.Skip(count) ?? Empty<TSource>();
            }
#endif

            return SkipIterator(source, count);
        }

        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return SkipWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource element = e.Current;
                    if (!predicate(element))
                    {
                        yield return element;
                        while (e.MoveNext())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return SkipWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                int index = -1;
                while (e.MoveNext())
                {
                    checked
                    {
                        index++;
                    }

                    TSource element = e.Current;
                    if (!predicate(element, index))
                    {
                        yield return element;
                        while (e.MoveNext())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return
                IsEmptyArray(source) ? [] :
                count <= 0 ? source.Skip(0) :
                TakeRangeFromEndIterator(source,
                    isStartIndexFromEnd: false, startIndex: 0,
                    isEndIndexFromEnd: true, endIndex: count);
        }
    }
}
