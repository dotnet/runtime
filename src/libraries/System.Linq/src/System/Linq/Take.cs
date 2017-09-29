// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw Error.ArgumentNull(nameof(source));
            }

            if (count <= 0)
            {
                return EmptyPartition<TSource>.Instance;
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.Take(count);
            }

            if (source is IList<TSource> sourceList)
            {
                return new ListPartition<TSource>(sourceList, 0, count - 1);
            }

            return new EnumerablePartition<TSource>(source, 0, count - 1);
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
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
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
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
                throw Error.ArgumentNull(nameof(source));
            }

            if (count <= 0)
            {
                return EmptyPartition<TSource>.Instance;
            }

            return TakeLastIterator(source, count);
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
