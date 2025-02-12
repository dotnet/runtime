// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns a specified number of contiguous elements from the start of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the specified number
        /// of elements from the start of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Take<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() || count <= 0 ? Empty<TSource>() :
                Impl(source, count, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                int count,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return element;

                    if (--count == 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>Returns a specified range of contiguous elements from a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the start or the end.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that contains the specified <paramref name="range" /> of elements from the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>Take enumerates <paramref name="source" /> and yields elements whose indices belong to the specified <paramref name="range"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Take<TSource>(
            this IAsyncEnumerable<TSource> source,
            Range range)
        {
            ThrowHelper.ThrowIfNull(source);

            if (source.IsKnownEmpty())
            {
                return Empty<TSource>();
            }

            Index start = range.Start, end = range.End;
            bool isStartIndexFromEnd = start.IsFromEnd, isEndIndexFromEnd = end.IsFromEnd;
            int startIndex = start.Value, endIndex = end.Value;
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
                return
                    startIndex >= endIndex ? Empty<TSource>() :
                    Impl(source, startIndex, endIndex, default);
            }

            return TakeRangeFromEndIterator(source, isStartIndexFromEnd, startIndex, isEndIndexFromEnd, endIndex, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source, int startIndex, int endIndex,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                Debug.Assert(source is not null);
                Debug.Assert(startIndex >= 0 && startIndex < endIndex);

                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    int index = 0;
                    while (index < startIndex && await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ++index;
                    }

                    if (index < startIndex)
                    {
                        yield break;
                    }

                    while (index < endIndex && await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return e.Current;
                        ++index;
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        private static async IAsyncEnumerable<TSource> TakeRangeFromEndIterator<TSource>(
            IAsyncEnumerable<TSource> source,
            bool isStartIndexFromEnd,
            int startIndex,
            bool isEndIndexFromEnd,
            int endIndex,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Debug.Assert(source is not null);
            Debug.Assert(isStartIndexFromEnd || isEndIndexFromEnd);
            Debug.Assert(isStartIndexFromEnd
                ? startIndex > 0 && (!isEndIndexFromEnd || startIndex > endIndex)
                : startIndex >= 0 && (isEndIndexFromEnd || startIndex < endIndex));

            Queue<TSource> queue;
            int count;

            if (isStartIndexFromEnd)
            {
                // TakeLast compat: enumerator should be disposed before yielding the first element.
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    queue = new Queue<TSource>();
                    queue.Enqueue(e.Current);
                    count = 1;

                    while (await e.MoveNextAsync().ConfigureAwait(false))
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
                            }
                            while (await e.MoveNextAsync().ConfigureAwait(false));

                            break;
                        }
                    }

                    Debug.Assert(queue.Count == Math.Min(count, startIndex));
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }

                startIndex = CalculateStartIndexFromEnd(startIndex, count);
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
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    count = 0;
                    while (count < startIndex && await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ++count;
                    }

                    if (count == startIndex)
                    {
                        queue = new Queue<TSource>();
                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            if (queue.Count == endIndex)
                            {
                                do
                                {
                                    queue.Enqueue(e.Current);
                                    yield return queue.Dequeue();
                                }
                                while (await e.MoveNextAsync().ConfigureAwait(false));

                                break;
                            }
                            else
                            {
                                queue.Enqueue(e.Current);
                            }
                        }
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }

            static int CalculateStartIndexFromEnd(int startIndex, int count) =>
                Math.Max(0, count - startIndex);

            static int CalculateEndIndex(bool isEndIndexFromEnd, int endIndex, int count) =>
                Math.Min(count, isEndIndexFromEnd ? count - endIndex : endIndex);
        }
    }
}
