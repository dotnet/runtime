// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (count <= 0 || IsEmptyArray(source))
            {
                return [];
            }

            return SpeedOptimizedTakeIterator(source, count);
        }

        /// <summary>Returns a specified range of contiguous elements from a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the specified <paramref name="range" /> of elements from the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para><see cref="O:Enumerable.Take" /> enumerates <paramref name="source" /> and yields elements whose indices belong to the specified <paramref name="range"/>.</para>
        /// </remarks>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Range range)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
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
                    return [];
                }
            }
            else if (!isEndIndexFromEnd)
            {
                if (startIndex >= endIndex)
                {
                    return [];
                }

                return SpeedOptimizedTakeRangeIterator(source, startIndex, endIndex);
            }

            return TakeRangeFromEndIterator(source, isStartIndexFromEnd, startIndex, isEndIndexFromEnd, endIndex);
        }

        private static IEnumerable<TSource> TakeRangeFromEndIterator<TSource>(IEnumerable<TSource> source, bool isStartIndexFromEnd, int startIndex, bool isEndIndexFromEnd, int endIndex)
        {
            Debug.Assert(source is not null);
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
                    IEnumerable<TSource> rangeIterator = SpeedOptimizedTakeRangeIterator(source, startIndex, endIndex);
                    foreach (TSource element in rangeIterator)
                    {
                        yield return element;
                    }
                }

                yield break;
            }

            if (isStartIndexFromEnd)
            {
                // Buffer the last `startIndex` elements using a pooled array as a ring buffer, avoiding the
                // allocation and per-element enqueue/dequeue overhead of Queue<TSource>. The pooled array is
                // returned to the pool when enumeration completes or is abandoned.
                Debug.Assert(startIndex > 0);

                // The pooled window retains at most `startIndex` elements, so the ring buffer wraps at this
                // capacity. Capture it now because `startIndex` is repurposed below as the recomputed start index.
                int capacity = startIndex;

                TSource[]? buffer = null;
                int bufferCount = 0; // number of valid elements currently retained
                int head = 0;        // logical index of the oldest retained element (only advances once the ring is full)

                // Own the rented buffer for the whole operation with a single finally. Buffering, the source
                // enumerator's disposal, and yielding can each throw; this returns the buffer exactly once on every
                // path (including abandoned enumeration) and never double-returns, because the buffer variable always
                // identifies the single array currently rented from the pool.
                try
                {
                    // TakeLast compat: enumerator should be disposed before yielding the first element.
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (!e.MoveNext())
                        {
                            yield break;
                        }

                        buffer = ArrayPool<TSource>.Shared.Rent(Math.Min(startIndex, 4));
                        buffer[0] = e.Current;
                        bufferCount = 1;
                        head = 0;
                        count = 1;

                        while (e.MoveNext())
                        {
                            if (count < startIndex)
                            {
                                // The retained window isn't full yet; append, growing the pooled buffer if needed.
                                if (bufferCount == buffer.Length)
                                {
                                    int newSize = (int)Math.Min((uint)capacity, 2 * (uint)buffer.Length);
                                    TSource[] newBuffer = ArrayPool<TSource>.Shared.Rent(newSize);
                                    Array.Copy(buffer, newBuffer, bufferCount);
                                    ReturnToPool(buffer);
                                    buffer = newBuffer;
                                }

                                buffer[bufferCount++] = e.Current;
                                ++count;
                            }
                            else
                            {
                                // The window is full; overwrite the oldest element (ring buffer of capacity `startIndex`).
                                do
                                {
                                    buffer[head] = e.Current;
                                    head = head + 1 == capacity ? 0 : head + 1;
                                    checked { ++count; }
                                } while (e.MoveNext());
                                break;
                            }
                        }
                    }

                    Debug.Assert(bufferCount == Math.Min(count, startIndex));

                    startIndex = CalculateStartIndex(isStartIndexFromEnd: true, startIndex, count);
                    endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count);
                    Debug.Assert(endIndex - startIndex <= bufferCount);

                    // The retained window holds `bufferCount` elements in order starting at `head`. Its first
                    // element corresponds to the recomputed original index `startIndex`, so the elements to yield
                    // are simply the first (endIndex - startIndex) of the window.
                    int index = head;
                    for (int rangeIndex = startIndex; rangeIndex < endIndex; rangeIndex++)
                    {
                        yield return buffer[index];
                        index = index + 1 == capacity ? 0 : index + 1;
                    }
                }
                finally
                {
                    if (buffer is not null)
                    {
                        ReturnToPool(buffer);
                    }
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
                    Queue<TSource> queue = new Queue<TSource>();
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

            static void ReturnToPool(TSource[] buffer) =>
                ArrayPool<TSource>.Shared.Return(buffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TSource>());
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (IsEmptyArray(source))
            {
                return [];
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
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (IsEmptyArray(source))
            {
                return [];
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
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 || IsEmptyArray(source) ?
                [] :
                TakeRangeFromEndIterator(source,
                    isStartIndexFromEnd: true, startIndex: count,
                    isEndIndexFromEnd: true, endIndex: 0);
        }
    }
}
