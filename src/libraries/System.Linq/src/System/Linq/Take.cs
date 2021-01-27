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
            int startIndexValue = start.Value;
            int endIndexValue = end.Value;
            Debug.Assert(startIndexValue >= 0);
            Debug.Assert(endIndexValue >= 0);

            if (source is IPartition<TSource> partition)
            {
                if (!isStartIndexFromEnd && !isEndIndexFromEnd)
                {
                    return partition.Skip(startIndexValue).Take(endIndexValue - startIndexValue);
                }

                int count = partition.GetCount(onlyIfCheap: true);
                if (count == 0)
                {
                    return Empty<TSource>();
                }

                if (count > 0)
                {
                    if (isStartIndexFromEnd)
                    {
                        startIndexValue = count - startIndexValue;
                    }

                    if (isEndIndexFromEnd)
                    {
                        endIndexValue = count - endIndexValue;
                    }

                    return partition.Skip(startIndexValue).Take(endIndexValue - startIndexValue);
                }
            }
            else if (source is IList<TSource> list)
            {
                int count = list.Count;
                if (count == 0)
                {
                    return Empty<TSource>();
                }

                Debug.Assert(count > 0);

                if (isStartIndexFromEnd)
                {
                    startIndexValue = count - startIndexValue;
                }

                if (isEndIndexFromEnd)
                {
                    endIndexValue = count - endIndexValue;
                }

                return startIndexValue < endIndexValue && startIndexValue < count
                    ? new ListPartition<TSource>(list, minIndexInclusive: startIndexValue, maxIndexInclusive: endIndexValue - 1)
                    : Empty<TSource>();
            }

            return TakeIterator(source, isStartIndexFromEnd, startIndexValue, isEndIndexFromEnd, endIndexValue);
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

        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, bool isStartIndexFromEnd, int startIndexValue, bool isEndIndexFromEnd, int endIndexValue)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndexValue >= 0);
            Debug.Assert(endIndexValue >= 0);

            using IEnumerator<TSource> e = source.GetEnumerator();
            int currentIndex = -1;
            if (isStartIndexFromEnd)
            {
                if (!e.MoveNext())
                {
                    yield break;
                }

                currentIndex++;
                Queue<TSource> queue = new();
                queue.Enqueue(e.Current);

                while (e.MoveNext())
                {
                    currentIndex++;
                    if (queue.Count == startIndexValue)
                    {
                        queue.Dequeue();
                    }

                    queue.Enqueue(e.Current);
                }

                if (queue.Count < startIndexValue)
                {
                    yield break;
                }

                int count = currentIndex + 1;
                startIndexValue = count - startIndexValue;
                if (isEndIndexFromEnd)
                {
                    endIndexValue = count - endIndexValue;
                }

                for (int index = startIndexValue; index < endIndexValue; index++)
                {
                    yield return queue.Dequeue();
                }
            }
            else
            {
                if (!e.MoveNext())
                {
                    yield break;
                }

                currentIndex++;

                while (currentIndex < startIndexValue && e.MoveNext())
                {
                    currentIndex++;
                }

                if (currentIndex != startIndexValue)
                {
                    yield break;
                }

                if (isEndIndexFromEnd)
                {
                    if (endIndexValue > 0)
                    {
                        Queue<TSource> queue = new();
                        do
                        {
                            if (queue.Count == endIndexValue)
                            {
                                yield return queue.Dequeue();
                            }

                            queue.Enqueue(e.Current);
                            currentIndex++;
                        } while (e.MoveNext());
                    }
                    else
                    {
                        do
                        {
                            yield return e.Current;
                            currentIndex++;
                        } while (e.MoveNext());
                    }
                }
                else
                {
                    if (startIndexValue >= endIndexValue)
                    {
                        yield break;
                    }

                    yield return e.Current;
                    while (++currentIndex < endIndexValue && e.MoveNext())
                    {
                        yield return e.Current;
                    }
                }
            }
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
                TakeLastIterator(source, count);
        }

        private static IEnumerable<TSource> TakeLastIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            Debug.Assert(source != null);
            Debug.Assert(count > 0);

            Queue<TSource> queue;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    yield break;
                }

                queue = new Queue<TSource>();
                queue.Enqueue(e.Current);

                while (e.MoveNext())
                {
                    if (queue.Count < count)
                    {
                        queue.Enqueue(e.Current);
                    }
                    else
                    {
                        do
                        {
                            queue.Dequeue();
                            queue.Enqueue(e.Current);
                        }
                        while (e.MoveNext());
                        break;
                    }
                }
            }

            Debug.Assert(queue.Count <= count);
            do
            {
                yield return queue.Dequeue();
            }
            while (queue.Count > 0);
        }
    }
}
