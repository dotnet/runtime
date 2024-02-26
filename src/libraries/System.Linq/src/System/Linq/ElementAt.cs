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

            TSource? element = TryGetElementAt(source, index, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return element!;
        }

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence.</exception>
        /// <returns>The element at the specified position in the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.</para>
        /// <para>This method throws an exception if <paramref name="index" /> is out of range. To instead return a default value when the specified index is out of range, use the <see cref="O:Enumerable.ElementAtOrDefault" /> method.</para>
        /// </remarks>
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

            if (source.TryGetNonEnumeratedCount(out int count))
            {
                return source.ElementAt(count - index.Value);
            }

            if (!TryGetElementFromEnd(source, index.Value, out TSource? element))
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

            return TryGetElementAt(source, index, out _);
        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <returns><see langword="default" /> if <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence; otherwise, the element at the specified position in the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.</para>
        /// <para>The default value for reference and nullable types is <see langword="null" />.</para>
        /// </remarks>
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

            if (source.TryGetNonEnumeratedCount(out int count))
            {
                return source.ElementAtOrDefault(count - index.Value);
            }

            TryGetElementFromEnd(source, index.Value, out TSource? element);
            return element;
        }

        private static TSource? TryGetElementAt<TSource>(this IEnumerable<TSource> source, int index, out bool found) =>
#if !OPTIMIZE_FOR_SIZE
            source is Iterator<TSource> iterator ? iterator.TryGetElementAt(index, out found) :
#endif
            TryGetElementAtNonIterator(source, index, out found);

        private static TSource? TryGetElementAtNonIterator<TSource>(IEnumerable<TSource> source, int index, out bool found)
        {
            Debug.Assert(source != null);

            if (source is IList<TSource> list)
            {
                if ((uint)index < (uint)list.Count)
                {
                    found = true;
                    return list[index];
                }
            }
            else
            {
                if (index >= 0)
                {
                    using IEnumerator<TSource> e = source.GetEnumerator();
                    while (e.MoveNext())
                    {
                        if (index == 0)
                        {
                            found = true;
                            return e.Current;
                        }

                        index--;
                    }
                }
            }

            found = false;
            return default;
        }

        private static bool TryGetElementFromEnd<TSource>(IEnumerable<TSource> source, int indexFromEnd, [MaybeNullWhen(false)] out TSource element)
        {
            Debug.Assert(source != null);

            if (indexFromEnd > 0)
            {
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
                        element = queue.Dequeue();
                        return true;
                    }
                }
            }

            element = default;
            return false;
        }
    }
}
