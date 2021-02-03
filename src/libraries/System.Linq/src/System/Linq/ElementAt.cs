// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                TSource? element = partition.TryGetElementAt(index, out bool found);
                if (found)
                {
                    return element!;
                }
            }
            else
            {
                if (source is IList<TSource> list)
                {
                    return list[index];
                }

                if (index >= 0)
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0)
                            {
                                return e.Current;
                            }

                            index--;
                        }
                    }
                }
            }

            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            return default;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, Index index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (!index.IsFromEnd)
            {
                return source.ElementAt(index.Value);
            }

            int indexFromEnd = index.Value;
            if (indexFromEnd > 0)
            {
                if (source is IPartition<TSource> partition)
                {
                    int count = partition.GetCount(onlyIfCheap: true);
                    if (count > 0)
                    {
                        TSource? element = partition.TryGetElementAt(count - indexFromEnd, out bool found);
                        if (found)
                        {
                            return element!;
                        }
                    }
                    else if (count == 0)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                    }
                }
                else if (source is IList<TSource> list)
                {
                    return list[index];
                }

                using IEnumerator<TSource> e = source.GetEnumerator();
                if (e.MoveNext())
                {
                    Queue<TSource> queue = new();
                    queue.Enqueue(e.Current);
                    while (e.MoveNext())
                    {
                        if (queue.Count == indexFromEnd)
                        {
                            queue.Dequeue();
                        }

                        queue.Enqueue(e.Current);
                    }

                    if (queue.Count == indexFromEnd)
                    {
                        return queue.Dequeue();
                    }
                }
            }

            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            return default;
        }

        public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetElementAt(index, out bool _);
            }

            if (index >= 0)
            {
                if (source is IList<TSource> list)
                {
                    if (index < list.Count)
                    {
                        return list[index];
                    }
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0)
                            {
                                return e.Current;
                            }

                            index--;
                        }
                    }
                }
            }

            return default;
        }

        public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, Index index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (!index.IsFromEnd)
            {
                return source.ElementAtOrDefault(index.Value);
            }

            int indexFromEnd = index.Value;
            if (indexFromEnd > 0)
            {
                if (source is IPartition<TSource> partition)
                {
                    int count = partition.GetCount(onlyIfCheap: true);
                    if (count > 0)
                    {
                        TSource? element = partition.TryGetElementAt(count - indexFromEnd, out bool found);
                        if (found)
                        {
                            return element!;
                        }
                    }
                    else if (count == 0)
                    {
                        return default;
                    }
                }
                else if (source is IList<TSource> list)
                {
                    int count = list.Count;
                    return indexFromEnd <= count ? list[count - indexFromEnd] : default;
                }

                using IEnumerator<TSource> e = source.GetEnumerator();
                if (e.MoveNext())
                {
                    Queue<TSource> queue = new();
                    queue.Enqueue(e.Current);
                    while (e.MoveNext())
                    {
                        if (queue.Count == indexFromEnd)
                        {
                            queue.Dequeue();
                        }

                        queue.Enqueue(e.Current);
                    }

                    if (queue.Count == indexFromEnd)
                    {
                        return queue.Dequeue();
                    }
                }
            }

            return default;
        }
    }
}
