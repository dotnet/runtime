// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
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

            if (!TryGetElementAt(source, index, isIndexFromEnd: false, out TSource? element))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return element;
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

            if (!TryGetElementAt(source, index.Value, index.IsFromEnd, out TSource? element))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return element;
        }

        public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            TryGetElementAt(source, index, isIndexFromEnd: false, out TSource? element);
            return element;
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

            TryGetElementAt(source, index.Value, index.IsFromEnd, out TSource? element);
            return element;
        }

        private static bool TryGetElementAt<TSource>(IEnumerable<TSource> source, int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TSource element)
        {
            Debug.Assert(source != null);

            element = default;
            if (index < 0 || (isIndexFromEnd && index == 0))
            {
                return false;
            }

            if (source is IList<TSource> list)
            {
                int listCount = list.Count;
                if (listCount > 0)
                {
                    if (isIndexFromEnd)
                    {
                        index = listCount - index;
                        if (index < 0)
                        {
                            return false;
                        }
                    }

                    if (index < listCount)
                    {
                        element = list[index];
                        return true;
                    }
                }

                return false;
            }

            int count = -1;
            if (source is IIListProvider<TSource> listProvider)
            {
                if (source is IPartition<TSource> partition)
                {
                    if (isIndexFromEnd)
                    {
                        count = partition.GetCount(onlyIfCheap: true);
                        if (count > 0)
                        {
                            element = partition.TryGetElementAt(count - index, out bool found);
                            return found;
                        }
                    }
                    else
                    {
                        element = partition.TryGetElementAt(index, out bool found);
                        return found;
                    }
                }
                else
                {
                    count = listProvider.GetCount(onlyIfCheap: true);
                }
            }
            else if (source is ICollection<TSource> collectionoft)
            {
                count = collectionoft.Count;
            }
            else if (source is ICollection collection)
            {
                count = collection.Count;
            }

            if (count == 0)
            {
                return false;
            }

            if (count > 0)
            {
                if (isIndexFromEnd)
                {
                    index = count - index;
                    if (index < 0)
                    {
                        return false;
                    }

                    isIndexFromEnd = false;
                }

                if (index >= count)
                {
                    return false;
                }
            }

            using IEnumerator<TSource> e = source.GetEnumerator();
            if (isIndexFromEnd)
            {
                if (!e.MoveNext())
                {
                    return false;
                }

                Queue<TSource> queue = new();
                queue.Enqueue(e.Current);
                while (e.MoveNext())
                {
                    if (queue.Count == index)
                    {
                        queue.Dequeue();
                    }

                    queue.Enqueue(e.Current);
                }

                if (queue.Count == index)
                {
                    element = queue.Dequeue();
                    return true;
                }
            }
            else
            {
                while (e.MoveNext())
                {
                    if (index == 0)
                    {
                        element = e.Current;
                        return true;
                    }

                    index--;
                }
            }

            return false;
        }
    }
}
