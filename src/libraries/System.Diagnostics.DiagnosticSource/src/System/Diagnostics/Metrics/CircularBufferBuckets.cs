// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from OpenTelemetry .NET implementation:
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/805dd6b4abfa18ef2706d04c30d0ed28dbc2955e/src/OpenTelemetry/Metrics/CircularBufferBuckets.cs#L1
// Licensed under the Apache 2.0 License. See LICENSE: https://github.com/open-telemetry/opentelemetry-dotnet/blob/805dd6b4abfa18ef2706d04c30d0ed28dbc2955e/LICENSE.TXT
// Copyright The OpenTelemetry Authors

using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// A histogram buckets implementation based on circular buffer.
    /// </summary>
    internal sealed class CircularBufferBuckets
    {
        private long[]? _trait;
        private int _begin; // Auto initialized to 0
        private int _end = -1;

        public CircularBufferBuckets(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
            }

            Capacity = capacity;
        }

        /// <summary>
        /// Gets the capacity of the <see cref="CircularBufferBuckets"/>.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Gets the size of the <see cref="CircularBufferBuckets"/>.
        /// </summary>
        public int Size => _end - _begin + 1;

        /// <summary>
        /// Returns the value of <c>Bucket[index]</c>.
        /// </summary>
        /// <param name="index">The index of the bucket.</param>
        /// <remarks>
        /// The "index" value can be positive, zero or negative.
        /// This method does not validate if "index" falls into [begin, end],
        /// the caller is responsible for the validation.
        /// </remarks>
        public long this[int index]
        {
            get
            {
                Debug.Assert(_trait is not null, "trait was null");

                return _trait![ModuloIndex(index)];
            }
        }

        /// <summary>
        /// Attempts to increment the value of <c>Bucket[index]</c> by <c>value</c>.
        /// </summary>
        /// <param name="index">The index of the bucket.</param>
        /// <param name="value">The increment.</param>
        /// <returns>
        /// Returns <c>0</c> if the increment attempt succeeded;
        /// Returns a positive integer indicating the minimum scale reduction level
        /// if the increment attempt failed.
        /// </returns>
        /// <remarks>
        /// The "index" value can be positive, zero or negative.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TryIncrement(int index, long value = 1)
        {
            var capacity = Capacity;

            if (_trait == null)
            {
                _trait = new long[capacity];

                _begin = index;
                _end = index;
                _trait[ModuloIndex(index)] += value;

                return 0;
            }

            var begin = _begin;
            var end = _end;

            if (index > end)
            {
                end = index;
            }
            else if (index < begin)
            {
                begin = index;
            }
            else
            {
                _trait[ModuloIndex(index)] += value;
                return 0;
            }

            var diff = end - begin;

            if (diff >= capacity || diff < 0)
            {
                return CalculateScaleReduction(begin, end, capacity);
            }

            _begin = begin;
            _end = end;

            _trait[ModuloIndex(index)] += value;

            return 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int CalculateScaleReduction(int begin, int end, int capacity)
            {
                Debug.Assert(capacity >= 2, "The capacity must be at least 2.");

                var retVal = 0;
                var diff = end - begin;

                while (diff >= capacity || diff < 0)
                {
                    begin >>= 1;
                    end >>= 1;
                    diff = end - begin;
                    retVal++;
                }

                return retVal;
            }
        }

        public void ScaleDown(int level = 1)
        {
            Debug.Assert(level > 0, "The scale down level must be a positive integer.");

            if (_trait == null)
            {
                return;
            }

            // 0 <= offset < capacity <= 2147483647
            uint capacity = (uint)Capacity;
            var offset = (uint)ModuloIndex(_begin);

            var currentBegin = _begin;
            var currentEnd = _end;

            for (int i = 0; i < level; i++)
            {
                var newBegin = currentBegin >> 1;
                var newEnd = currentEnd >> 1;

                if (currentBegin != currentEnd)
                {
                    if (currentBegin % 2 == 0)
                    {
                        ScaleDownInternal(_trait, offset, currentBegin, currentEnd, capacity);
                    }
                    else
                    {
                        currentBegin++;

                        if (currentBegin != currentEnd)
                        {
                            ScaleDownInternal(_trait, offset + 1, currentBegin, currentEnd, capacity);
                        }
                    }
                }

                currentBegin = newBegin;
                currentEnd = newEnd;
            }

            _begin = currentBegin;
            _end = currentEnd;

            if (capacity > 1)
            {
                AdjustPosition(_trait, offset, (uint)ModuloIndex(currentBegin), (uint)(currentEnd - currentBegin + 1), capacity);
            }

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void ScaleDownInternal(long[] array, uint offset, int begin, int end, uint capacity)
            {
                for (var index = begin + 1; index < end; index++)
                {
                    Consolidate(array, (offset + (uint)(index - begin)) % capacity, (offset + (uint)((index >> 1) - (begin >> 1))) % capacity);
                }

                // Don't merge below call into above for loop.
                // Merging causes above loop to be infinite if end = int.MaxValue, because index <= int.MaxValue is always true.
                Consolidate(array, (offset + (uint)(end - begin)) % capacity, (offset + (uint)((end >> 1) - (begin >> 1))) % capacity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AdjustPosition(long[] array, uint src, uint dst, uint size, uint capacity)
            {
                var advancement = (dst + capacity - src) % capacity;

                if (advancement == 0)
                {
                    return;
                }

                if (size - 1 == advancement && advancement << 1 == capacity)
                {
                    Exchange(array, src++, dst++);
                    size -= 2;
                }
                else if (advancement < size)
                {
                    src = src + size - 1;
                    dst = dst + size - 1;

                    while (size-- != 0)
                    {
                        Move(array, src-- % capacity, dst-- % capacity);
                    }

                    return;
                }

                while (size-- != 0)
                {
                    Move(array, src++ % capacity, dst++ % capacity);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Consolidate(long[] array, uint src, uint dst)
            {
                array[dst] += array[src];
                array[src] = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Exchange(long[] array, uint src, uint dst)
            {
                var value = array[dst];
                array[dst] = array[src];
                array[src] = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Move(long[] array, uint src, uint dst)
            {
                array[dst] = array[src];
                array[src] = 0;
            }
        }

        public long[] ToArray()
        {
            var size = Size;

            if (_trait == null || size <= 0)
            {
                return Array.Empty<long>();
            }

            var result = new long[size];

            for (var i = 0; i < size; ++i)
            {
                result[i] = _trait[ModuloIndex(_begin + i)];
            }

            return result;
        }

        internal void Clear()
        {
            if (_trait is not null)
            {
#if NET
                Array.Clear(_trait);
#else
                Array.Clear(_trait, 0, _trait.Length);
#endif
            }

            _begin = 0;
            _end = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ModuloIndex(int value) => PositiveModulo32(value, Capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PositiveModulo32(int value, int divisor)
        {
            Debug.Assert(divisor > 0, $"{nameof(divisor)} must be a positive integer.");

            value %= divisor;

            if (value < 0)
            {
                value += divisor;
            }

            return value;
        }
    }
}
