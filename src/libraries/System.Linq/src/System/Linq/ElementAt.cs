// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence.
        /// </exception>
        /// <returns>The element at the specified position in the <paramref name="source" /> sequence.</returns>
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

            if (TryGetElementAt(source, index.Value, out TSource? result))
            {
                return result;
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

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <returns>
        ///   <see langword="default" /> if <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence; otherwise, the element at the specified position in the <paramref name="source" /> sequence.
        /// </returns>
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

            TryGetElementAt(source, index.Value, out TSource? result);
            return result;
        }

        private static bool TryGetElementAt<TSource>(IEnumerable<TSource> source, int indexFromEnd, [NotNullWhen(true)] out TSource? result)
        {
            Debug.Assert(indexFromEnd >= 0);

            result = default;
            if (indexFromEnd == 0)
            {
                return false;
            }

            if (source is IPartition<TSource> partition)
            {
                int count = partition.GetCount(onlyIfCheap: true);
                if (count == 0)
                {
                    return false;
                }

                if (count > 0)
                {
                    if (indexFromEnd <= count)
                    {
                        result = partition.TryGetElementAt(count - indexFromEnd, out bool found);
                        return found;
                    }

                    return false;
                }
            }
            else if (source is IList<TSource> list)
            {
                int count = list.Count;
                if (indexFromEnd <= count)
                {
                    result = list[count - indexFromEnd]!;
                    return true;
                }

                return false;
            }

            using IEnumerator<TSource> e = source.GetEnumerator();
            if (!e.MoveNext())
            {
                return false;
            }

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
                result = queue.Dequeue()!;
                return true;
            }

            return false;
        }
    }
}
