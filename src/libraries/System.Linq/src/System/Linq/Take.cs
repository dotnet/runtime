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
                Init(ref source, ref startIndex, ref endIndex, isEndIndexFromEnd);

                [MethodImpl(MethodImplOptions.NoInlining)]
                static void Init(ref IEnumerable<TSource> source, ref int startIndex, ref int endIndex, bool isEndIndexFromEnd)
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
                    using var buffer = new FromEndRangeBuffer<TSource>(startIndex, scratchBuffer);
                    do
                    {
                        buffer.Enqueue(e.Current);
                    } while (e.MoveNext());

                    startIndex = CalculateStartIndex(true, startIndex, buffer.TotalCount);
                    endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, buffer.TotalCount);
                    var count = endIndex - startIndex;
                    Debug.Assert(count >= 0 && count <= buffer.TotalCount);
                    source = buffer.Build(count, out startIndex);
                    endIndex = startIndex + count;
                }

                for (; startIndex < endIndex; startIndex++)
                {
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
        /// Provides a circular buffer for efficiently handling Range operations with from-end indices.
        /// </summary>
        /// <remarks>
        /// This struct is designed to support LINQ's Take(Range) operations when indices are specified from the end (e.g., ^5..^2).
        /// It uses a circular buffer pattern to maintain only the necessary elements, avoiding the need to buffer the entire sequence.
        /// The buffer starts with a stack-allocated scratch buffer and can grow to a heap-allocated array if needed.
        /// </remarks>
        /// <typeparam name="T">Specifies the element type of the sequence being buffered.</typeparam>
        internal ref struct FromEndRangeBuffer<T>(int maxCapacity, Span<T> scratchBuffer)
        {
            /// <summary>The current buffer, which may point to either the initial scratch buffer or a heap-allocated array.</summary>
            /// <remarks>This acts as a circular buffer once the capacity is reached.</remarks>
            private Span<T> _buffer = scratchBuffer;

            /// This field has two allocation strategies:
            /// 1. When growing but not yet at maxCapacity: Rented from ArrayPool for better performance
            /// 2. When reaching exactly maxCapacity: Allocated directly with GC.AllocateUninitializedArray
            /// The allocation strategy effects:
            /// 1. building the final array
            /// 2. returning the array to the pool
            private T[]? _array = null;

            /// <summary>The index where the next element will be inserted in the circular buffer.</summary>
            /// <remarks>When this reaches maxCapacity, it wraps back to 0 to implement circular behavior.</remarks>
            private int _tail = 0;

            /// <summary>The total number of elements that have been enqueued.</summary>
            /// <remarks>This can exceed maxCapacity, unlike the actual buffer size which is capped.</remarks>
            private int _totalCount = 0;

            /// <summary>Gets the total number of elements that have been enqueued.</summary>
            /// <remarks>This is used to calculate the actual range indices when converting from-end indices.</remarks>
            public int TotalCount => _totalCount;

            /// <summary>Adds an element to the circular buffer.</summary>
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
                    checked {  _totalCount++; }
                }
                else
                {
                    SlowEnqueue(item);
                }
            }

            /// <summary>Constructs the final array containing the requested range of elements.</summary>
            /// <remarks>
            /// This method extracts the appropriate elements from the circular buffer based on the
            /// calculated range. It handles both the case where all elements fit in the buffer
            /// and the case where only the last maxCapacity elements are retained.
            /// </remarks>
            /// <param name="count">The number of elements to include in the result.</param>
            /// <param name="head">Outputs the starting index in the circular buffer for the range.</param>
            /// <returns>An array containing the requested elements from the buffer.</returns>
            public T[] Build(int count, out int head)
            {
                // Calculate the head position in the circular buffer
                // This represents where the oldest relevant element is located
                head = _tail - Math.Min(_totalCount, maxCapacity);
                if (head < 0) head += maxCapacity;

                int actualCount = Math.Min(count, _totalCount);
                if (actualCount <= 0)
                    return Array.Empty<T>();

                // If we have a heap array that matches the max capacity, return it directly
                if (_array != null && _array.Length == maxCapacity) return _array;

                // For simple cases where the buffer hasn't wrapped or we need all elements
                if (actualCount == maxCapacity || _totalCount < maxCapacity)
                {
                    return _buffer[..actualCount].ToArray();
                }

                // For complex cases, we need to copy from a circular buffer
                var result = GC.AllocateUninitializedArray<T>(actualCount);
                var resultSpan = result.AsSpan();

                // Calculate how much we can copy from the head to the end of the buffer
                int firstPart = head + actualCount <= maxCapacity ? actualCount : Math.Min(maxCapacity, _buffer.Length) - head;

                // Copy the first part (from head to end of buffer or all needed elements)
                _buffer.Slice(head, firstPart).CopyTo(resultSpan);

                // If we wrapped around, copy the remaining elements from the beginning of the buffer
                if (firstPart != actualCount)
                {
                    _buffer[..(actualCount - firstPart)].CopyTo(resultSpan[firstPart..]);
                }

                head = 0;
                return result;
            }

            /// <summary>Handles buffer expansion when the initial scratch buffer is not enough.</summary>
            /// <remarks>
            /// This method is called when the buffer needs to grow beyond its current capacity.
            /// It follows a doubling strategy similar to SegmentedArrayBuilder, but caps at maxCapacity.
            /// The method is marked NoInlining to keep the hot path (Enqueue) small and inlinable.
            /// </remarks>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowEnqueue(in T item)
            {
                // Calculate new capacity with doubling strategy
                int newCapacity = _buffer.Length * 2;
                if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;

                T[] newArray;
                // If we've reached max capacity, allocate exactly what we need
                // Otherwise, rent from the pool for better performance
                if (newCapacity >= maxCapacity)
                {
                    newCapacity = maxCapacity;
                    newArray = GC.AllocateUninitializedArray<T>(newCapacity);
                }
                else
                {
                    newArray = ArrayPool<T>.Shared.Rent(newCapacity);
                }

                // Copy existing data to the new array
                _buffer.CopyTo(newArray);

                // Return the old array to the pool if it was rented
                T[]? toReturn = _array;
                if (toReturn != null)
                {
                    // Clear references to avoid rooting objects unnecessarily
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        _buffer.Clear();
                    }

                    ArrayPool<T>.Shared.Return(toReturn);
                }

                // Update to use the new array
                _array = newArray;
                _buffer = newArray.AsSpan();
                _buffer[_tail] = item;
                _totalCount = ++_tail;

                // Implement circular buffer wrapping
                if (_tail == maxCapacity) _tail = 0;
            }

            /// <summary>Releases any rented arrays back to the array pool.</summary>
            /// <remarks>
            /// This ensures proper cleanup of pooled resources. Arrays that exactly match maxCapacity
            /// are not returned to the pool as they were allocated specifically for this use.
            /// Reference types are cleared before returning to avoid artificial rooting.
            /// </remarks>
            public void Dispose()
            {
                T[]? toReturn = _array;

                if (toReturn != null)
                {
                    _array = null;

                    // Don't return arrays that were allocated at exactly maxCapacity
                    // as these were not rented from the pool
                    if (toReturn.Length == maxCapacity) return;

                    // Clear references to avoid keeping objects alive in the pool
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
