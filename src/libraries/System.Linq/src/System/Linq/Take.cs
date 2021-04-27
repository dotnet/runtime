// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 ?
                Empty<TSource>() :
                TakeIterator<TSource>(source, count);
        }

        /// <summary>Returns a specified range of contiguous elements from a sequence.</summary>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the specified <paramref name="range" /> of elements from the <paramref name="source" /> sequence.</returns>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Range range)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            Index start = range.Start;
            Index end = range.End;
            bool isStartIndexFromEnd = start.IsFromEnd;
            bool isEndIndexFromEnd = end.IsFromEnd;
            int startIndex = start.Value;
            int endIndex = end.Value;
            Debug.Assert(startIndex >= 0);
            Debug.Assert(endIndex >= 0);

            if (isStartIndexFromEnd)
            {
                if (startIndex == 0 || (isEndIndexFromEnd && endIndex >= startIndex))
                {
                    return Empty<TSource>();
                }
            }
            else if (!isEndIndexFromEnd)
            {
                return startIndex >= endIndex
                    ? Empty<TSource>()
                    : TakeRangeIterator(source, startIndex, endIndex);
            }

            return TakeRangeFromEndIterator(source, isStartIndexFromEnd, startIndex, isEndIndexFromEnd, endIndex);
        }

        private static IEnumerable<TSource> TakeRangeFromEndIterator<TSource>(IEnumerable<TSource> source, bool isStartIndexFromEnd, int startIndex, bool isEndIndexFromEnd, int endIndex)
        {
            Debug.Assert(source != null);
            Debug.Assert(isStartIndexFromEnd || isEndIndexFromEnd);
            Debug.Assert(isStartIndexFromEnd
                ? startIndex > 0 && (!isEndIndexFromEnd || startIndex > endIndex)
                : startIndex >= 0 && (isEndIndexFromEnd || startIndex < endIndex));

            // Attempt to extract the count of the source enumerator,
            // in order to convert fromEnd indices to regular indices.
            // Enumerable counts can change over time, so it is very
            // important that this check happens at enumeration time;
            // do not move it outside of the iterator method.
            if (source.TryGetNonEnumeratedCount(out int count))
            {
                startIndex = CalculateStartIndex(isStartIndexFromEnd, startIndex, count);
                endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count);

                if (startIndex < endIndex)
                {
                    foreach (TSource element in TakeRangeIterator(source, startIndex, endIndex))
                    {
                        yield return element;
                    }
                }

                yield break;
            }

            Queue<TSource> queue;

            if (isStartIndexFromEnd)
            {
                // TakeLast compat: enumerator should be disposed before yielding the first element.
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        yield break;
                    }

                    queue = new Queue<TSource>();
                    queue.Enqueue(e.Current);
                    count = 1;

                    while (e.MoveNext())
                    {
                        if (count < startIndex)
                        {
                            queue.Enqueue(e.Current);
                            ++count;
                        }
                        else
                        {
                            do
                            {
                                queue.Dequeue();
                                queue.Enqueue(e.Current);
                                checked { ++count; }
                            } while (e.MoveNext());
                            break;
                        }
                    }

                    Debug.Assert(queue.Count == Math.Min(count, startIndex));
                }

                startIndex = CalculateStartIndex(isStartIndexFromEnd: true, startIndex, count);
                endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count);
                Debug.Assert(endIndex - startIndex <= queue.Count);

                for (int rangeIndex = startIndex; rangeIndex < endIndex; rangeIndex++)
                {
                    yield return queue.Dequeue();
                }
            }
            else
            {
                Debug.Assert(!isStartIndexFromEnd && isEndIndexFromEnd);

                // SkipLast compat: the enumerator should be disposed at the end of the enumeration.
                using IEnumerator<TSource> e = source.GetEnumerator();

                count = 0;
                while (count < startIndex && e.MoveNext())
                {
                    ++count;
                }

                if (count == startIndex)
                {
                    queue = new Queue<TSource>();
                    while (e.MoveNext())
                    {
                        if (queue.Count == endIndex)
                        {
                            do
                            {
                                queue.Enqueue(e.Current);
                                yield return queue.Dequeue();
                            } while (e.MoveNext());

                            break;
                        }
                        else
                        {
                            queue.Enqueue(e.Current);
                        }
                    }
                }
            }

            static int CalculateStartIndex(bool isStartIndexFromEnd, int startIndex, int count) =>
                Math.Max(0, isStartIndexFromEnd ? count - startIndex : startIndex);

            static int CalculateEndIndex(bool isEndIndexFromEnd, int endIndex, int count) =>
                Math.Min(count, isEndIndexFromEnd ? count - endIndex : endIndex);
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource element in source)
            {
                if (!predicate(element))
                {
                    break;
                }

                yield return element;
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                if (!predicate(element, index))
                {
                    break;
                }

                yield return element;
            }
        }

        public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 ?
                Empty<TSource>() :
                TakeRangeFromEndIterator(source,
                    isStartIndexFromEnd: true, startIndex: count,
                    isEndIndexFromEnd: true, endIndex: 0);
        }
    }
}
