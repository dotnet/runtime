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

            return IsSizeOptimized ? SizeOptimizedTakeIterator(source, count) : SpeedOptimizedTakeIterator(source, count);
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

                return IsSizeOptimized ? SizeOptimizedTakeRangeIterator(source, startIndex, endIndex) : SpeedOptimizedTakeRangeIterator(source, startIndex, endIndex);
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
                    IEnumerable<TSource> rangeIterator = IsSizeOptimized
                        ? SizeOptimizedTakeRangeIterator(source, startIndex, endIndex)
                        : SpeedOptimizedTakeRangeIterator(source, startIndex, endIndex);
                    foreach (TSource element in rangeIterator)
                    {
                        yield return element;
                    }
                }

                yield break;
            }

            Queue<TSource> queue;

            if (isStartIndexFromEnd)
            {
                PrepareArrayForFromEndRange(ref source, ref startIndex, ref endIndex, isEndIndexFromEnd);

                [MethodImpl(MethodImplOptions.NoInlining)]
                static void PrepareArrayForFromEndRange(ref IEnumerable<TSource> source, ref int startIndex, ref int endIndex, bool isEndIndexFromEnd)
                {

                    // TakeLast compat: enumerator should be disposed before yielding the first element.
                    using var e = source.GetEnumerator();
                    if (!e.MoveNext())
                    {
                        startIndex = 0;
                        endIndex = 0;
                        return;
                    }

                    var scratchBuffer = default(InlineArray16<TSource>);
                    using var buffer = new FromEndRangeCircularBuffer<TSource>(startIndex, scratchBuffer);
                    do
                    {
                        buffer.Enqueue(e.Current);
                    } while (e.MoveNext());

                    startIndex = CalculateStartIndex(true, startIndex, buffer.TotalCount);
                    endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, buffer.TotalCount);
                    int requestCount = endIndex - startIndex;
                    if (requestCount < 0) return;
                    source = buffer.Build(requestCount, out startIndex);
                    endIndex = startIndex + requestCount;
                }

                for (; startIndex < endIndex; startIndex++)
                {
                    // Here `source` is guaranteed to be TSource[] as per PrepareArrayForFromEndRange's Build() implementation.
                    // This allows us to avoid creating unnecessary field.
                    var array = Unsafe.As<TSource[]>(source);
                    yield return array[(uint)startIndex % (uint)array.Length];
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


        /// <summary>
        /// A circular buffer for efficiently handling Take(Range) operations with from-end indices.
        /// </summary>
        /// <remarks>
        /// Used by LINQ's Take(Range) when start/end indices are specified from the end (e.g., ^5..^2).
        /// This avoids buffering the entire sequence by retaining only the last <paramref name="maxCapacity"/> elements.
        /// Strategy:
        /// - Start with a stack-allocated scratch buffer (fast path, no heap allocation).
        /// - Grow to a heap array if needed (ArrayPool-rented until max capacity is reached).
        /// - Once full, overwrite oldest elements in circular fashion.
        /// </remarks>
        internal ref struct FromEndRangeCircularBuffer<T>(int maxCapacity, Span<T> scratchBuffer)
        {
            /// <summary>
            /// The underlying buffer, pointing either to the scratch buffer or a heap-allocated array.
            /// Acts as a circular buffer once full.
            /// </summary>
            private Span<T> _buffer = scratchBuffer;

            /// <summary>
            /// Heap array backing store, if allocated.
            /// - Rented from ArrayPool&lt;T&gt; if below maxCapacity.
            /// </summary>
            private T[]? _array = null;

            /// <summary>The index where the next element will be written.</summary>
            private int _tail = 0;

            /// <summary>Total number of elements enqueued over the lifetime of this buffer.</summary>
            private int _totalCount = 0;

            public int TotalCount => _totalCount;

            /// <summary>
            /// Adds an element to the buffer.
            /// Overwrites the oldest element once maxCapacity is reached (circular behavior).
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enqueue(T item)
            {
                var tail = _tail;
                var span = _buffer;

                if ((uint)tail < (uint)span.Length)
                {
                    span[tail] = item;
                    var newTail = tail + 1;
                    _tail = newTail == maxCapacity ? 0 : newTail;
                    checked { _totalCount++; }
                }
                else
                {
                    SlowEnqueue(item);
                }
            }

            /// <summary>
            /// Constructs the final array for the requested range.
            /// </summary>
            /// <param name="requestCount">Number of elements to include in the result.</param>
            /// <param name="head">Outputs the starting index in the buffer for the range.</param>
            /// <remarks>
            /// The returned array is safe to cast to TSource[] in calling code.
            /// If the array length equals maxCapacity, it may be the internal buffer itself.
            /// This is fine inside LINQ as the array is not mutated by consumers.
            /// </remarks>
            public T[] Build(int requestCount, out int head)
            {
                // Determine the oldest relevant element's index in the circular buffer
                head = _tail - Math.Min(_totalCount, maxCapacity);
                if (head < 0) head += maxCapacity;

                int actualCount = Math.Min(requestCount, _totalCount);
                if (actualCount <= 0)
                    return Array.Empty<T>();

                // Fast path: if we already have a heap array at maxCapacity, return it directly
                if (_array != null && _array.Length == maxCapacity)
                    return _array;

                var result = GC.AllocateUninitializedArray<T>(actualCount);
                var resultSpan = result.AsSpan();

                // Simple case: buffer hasn't wrapped or we're taking all elements
                if (actualCount == maxCapacity || _totalCount < maxCapacity)
                {
                    _buffer[..actualCount].CopyTo(resultSpan);
                }
                else
                {
                    // Complex case: must copy from wrapped circular buffer
                    int firstPart = head + actualCount <= maxCapacity
                        ? actualCount
                        : Math.Min(maxCapacity, _buffer.Length) - head;

                    _buffer.Slice(head, firstPart).CopyTo(resultSpan);

                    // Wrap-around case: copy remaining elements from the start of the buffer
                    if (firstPart != actualCount)
                    {
                        _buffer[..(actualCount - firstPart)].CopyTo(resultSpan[firstPart..]);
                    }

                    head = 0; // Consumers can now read from start
                }

                return result;
            }

            /// <summary>
            /// Expands the buffer when scratchBuffer is exhausted.
            /// Uses ArrayPool&lt;T&gt; until reaching maxCapacity, then allocates permanently.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowEnqueue(in T item)
            {
                int newCapacity = _buffer.Length * 2;
                if ((uint)newCapacity > Array.MaxLength) newCapacity = Math.Max(maxCapacity, Array.MaxLength);

                T[] newArray;
                if (newCapacity >= maxCapacity)
                {
                    newCapacity = maxCapacity;
                    newArray = GC.AllocateUninitializedArray<T>(newCapacity);
                }
                else
                {
                    newArray = ArrayPool<T>.Shared.Rent(newCapacity);
                }

                _buffer.CopyTo(newArray);

                if (_array != null && _array.Length != maxCapacity)
                {
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        _buffer.Clear();
                    }
                    ArrayPool<T>.Shared.Return(_array);
                }

                _array = newArray;
                _buffer = newArray.AsSpan();
                _buffer[_tail] = item;
                _totalCount++;
                _tail++;

                if (_tail == maxCapacity) _tail = 0;
            }

            /// <summary>
            /// Releases any rented arrays back to the pool.
            /// Arrays allocated at exactly maxCapacity are not returned (not pool-owned).
            /// </summary>
            public void Dispose()
            {
                if (_array != null)
                {
                    var toReturn = _array;
                    _array = null;

                    if (toReturn.Length == maxCapacity)
                        return; // Permanent allocation, skip return

                    ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }
            }
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
