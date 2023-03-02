// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public ref partial struct SequenceReader<T> where T : unmanaged, IEquatable<T>
    {
        // keep all fields explicit to track (and pack) space

        // deconstruct position for packing purposes
        private object? _currentPositionObject;
        private int _currentPositionInteger, _currentSpanIndex;

        private readonly long _length;
        private long _consumedAtStartOfCurrentSpan;
        private readonly ReadOnlySequence<T> _sequence;
        private ReadOnlySpan<T> _currentSpan;

        /// <summary>
        /// Create a <see cref="SequenceReader{T}"/> over the given <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SequenceReader(ReadOnlySequence<T> sequence)
        {
            _sequence = sequence;

            SequencePosition position = sequence.Start;
            _currentPositionObject = position.GetObject();
            _currentPositionInteger = position.GetInteger();
            _currentSpan = sequence.FirstSpan;
            _currentSpanIndex = 0;
            _consumedAtStartOfCurrentSpan = 0;

            if (sequence.IsSingleSegment)
            {
                _length = _currentSpan.Length;
            }
            else
            {
                _length = -1; // computed on-demand
                if (_currentSpan.IsEmpty)
                {
                    // edge-case; multi-segment with zero-length as first
                    TryGetNextSpan();
                }
            }

            AssertValidPosition();
        }

        /// <summary>
        /// True when there is no more data in the <see cref="Sequence"/>.
        /// </summary>
        public readonly bool End => _currentSpanIndex == _currentSpan.Length; // because of eager fetch, only ever true at EOF

        /// <summary>
        /// The underlying <see cref="ReadOnlySequence{T}"/> for the reader.
        /// </summary>
        public readonly ReadOnlySequence<T> Sequence => _sequence;

        /// <summary>
        /// Gets the unread portion of the <see cref="Sequence"/>.
        /// </summary>
        /// <value>
        /// The unread portion of the <see cref="Sequence"/>.
        /// </value>
        public readonly ReadOnlySequence<T> UnreadSequence => _sequence.Slice(Position);

        /// <summary>
        /// The current position in the <see cref="Sequence"/>.
        /// </summary>
        public readonly SequencePosition Position => new(_currentPositionObject, _currentPositionInteger + _currentSpanIndex); // since index in same segment, this is valid

        /// <summary>
        /// The current segment in the <see cref="Sequence"/> as a span.
        /// </summary>
        public readonly ReadOnlySpan<T> CurrentSpan => _currentSpan;

        /// <summary>
        /// The index in the <see cref="CurrentSpan"/>.
        /// </summary>
        public readonly int CurrentSpanIndex => _currentSpanIndex;

        /// <summary>
        /// The unread portion of the <see cref="CurrentSpan"/>.
        /// </summary>
        public readonly ReadOnlySpan<T> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentSpan.Slice(_currentSpanIndex);
        }

        /// <summary>
        /// The total number of <typeparamref name="T"/>'s processed by the reader.
        /// </summary>
        public readonly long Consumed => _consumedAtStartOfCurrentSpan + _currentSpanIndex;

        /// <summary>
        /// Remaining <typeparamref name="T"/>'s in the reader's <see cref="Sequence"/>.
        /// </summary>
        public readonly long Remaining => Length - Consumed;

        /// <summary>
        /// Count of <typeparamref name="T"/> in the reader's <see cref="Sequence"/>.
        /// </summary>
        public readonly long Length
        {
            get
            {
                if (_length < 0)
                {
                    // Cast-away readonly to initialize lazy field
                    Unsafe.AsRef(in _length) = _sequence.Length;
                }
                return _length;
            }
        }

        /// <summary>
        /// Peeks at the next value without advancing the reader.
        /// </summary>
        /// <param name="value">The next value or default if at the end.</param>
        /// <returns>False if at the end of the reader.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryPeek(out T value)
        {
            AssertValidPosition();

            // hoisting into locals for the compare allows us to avoid a bounds check
            int i = _currentSpanIndex;
            ReadOnlySpan<T> currentSpan = _currentSpan;

            if ((uint)i < (uint)currentSpan.Length)
            {
                value = currentSpan[i];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Peeks at the next value at specific offset without advancing the reader.
        /// </summary>
        /// <param name="offset">The offset from current position.</param>
        /// <param name="value">The next value, or the default value if at the end of the reader.</param>
        /// <returns><c>true</c> if the reader is not at its end and the peek operation succeeded; <c>false</c> if at the end of the reader.</returns>
        public readonly bool TryPeek(long offset, out T value)
        {
            AssertValidPosition();
            if (offset < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();

            // If we've got data and offset is not out of bounds
            if (_currentSpanIndex == _currentSpan.Length || Remaining <= offset)
            {
                value = default;
                return false;
            }

            // Sum CurrentSpanIndex + offset could overflow as is but the value of offset should be very large
            // because we check Remaining <= offset above so to overflow we should have a ReadOnlySequence close to 8 exabytes
            Debug.Assert(_currentSpanIndex + offset >= 0);

            // If offset doesn't fall inside current segment move to next until we find correct one
            if ((_currentSpanIndex + offset) <= _currentSpan.Length - 1)
            {
                Debug.Assert(offset <= int.MaxValue);

                value = _currentSpan[_currentSpanIndex + (int)offset];
                return true;
            }
            else
            {
                long remainingOffset = offset - (_currentSpan.Length - _currentSpanIndex);

                object? segment = _currentPositionObject;

                ReadOnlySpan<T> currentSpan = default;
                while (TryGetNextBuffer(in _sequence, ref segment, ref currentSpan))
                {
                    if (remainingOffset >= currentSpan.Length)
                    {
                        // Subtract current non consumed data
                        remainingOffset -= currentSpan.Length;
                    }
                    else
                    {
                        break;
                    }
                }
                value = currentSpan[(int)remainingOffset];
                return true;
            }
        }

        /// <summary>
        /// Read the next value and advance the reader.
        /// </summary>
        /// <param name="value">The next value or default if at the end.</param>
        /// <returns>False if at the end of the reader.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out T value)
        {
            AssertValidPosition();

            // hoisting into locals for the compare allows us to avoid a bounds check
            int i = _currentSpanIndex;
            ReadOnlySpan<T> currentSpan = _currentSpan;

            if ((uint)i < (uint)currentSpan.Length)
            {
                value = currentSpan[i];

                if ((_currentSpanIndex = i + 1) == currentSpan.Length)
                {
                    TryGetNextSpan(); // move ahead eagerly
                }
                AssertValidPosition();
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Move the reader back the specified number of items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if trying to rewind a negative amount or more than <see cref="Consumed"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(long count)
        {
            if ((ulong)count > (ulong)Consumed)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return;
            }

            Consumed -= count;

            if (_currentSpanIndex >= count)
            {
                _currentSpanIndex -= (int)count;
                AssertValidPosition();
            }
            else
            {
                // Current segment doesn't have enough data, scan backward through segments
                RetreatToPreviousSpan(Consumed - count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RetreatToPreviousSpan(long consumed)
        {
            ResetReader();
            Advance(consumed);
        }

        private void ResetReader()
        {
            // preserve the length - it can be relatively expensive to calculate on demand
            long length = _length;

            // reset all state
            this = new(_sequence);

            // reinstate the length
            Unsafe.AsRef(_length) = length;
        }

        /// <summary>
        /// Get the next segment with available data, if any.
        /// </summary>
        private bool TryGetNextSpan()
        {
            int lastLength = _currentSpan.Length;
            object? segment = _currentPositionObject;
            if (!TryGetNextBuffer(in _sequence, ref segment, ref _currentSpan))
            {
                return false;
            }
            _consumedAtStartOfCurrentSpan += lastLength; // track consumed length
            _currentSpanIndex = 0;
            _currentPositionObject = segment;
            _currentPositionInteger = 0; // all non-first segments start at zero

            AssertValidPosition();
            return true;
        }

        [Conditional("DEBUG")]
        private readonly void AssertValidPosition()
        {
            Debug.Assert(_currentSpanIndex >= 0 && _currentSpanIndex <= _currentSpan.Length, "span index out of range");
            if (_currentSpanIndex == _currentSpan.Length) // should only be at-length for EOF
            {
                ReadOnlySpan<T> span = default;
                object? segment = _currentPositionObject;
                Debug.Assert(!TryGetNextBuffer(in _sequence, ref segment, ref span), "failed to eagerly read-ahead");
            }
        }

        private static bool TryGetNextBuffer(in ReadOnlySequence<T> sequence, ref object? @object, ref ReadOnlySpan<T> buffer)
        {
            SequencePosition end = sequence.End;
            object? endObject = end.GetObject();

            ReadOnlySequenceSegment<T>? segment = @object as ReadOnlySequenceSegment<T>;
            if (segment is not null)
            {
                while (!ReferenceEquals(segment, endObject) && (segment = segment!.Next) is not null)
                {
                    ReadOnlySpan<T> span = segment.Memory.Span;

                    if (ReferenceEquals(segment, endObject))
                    {
                        // the last segment ends early
                        span = span.Slice(0, end.GetInteger());
                    }

                    if (!span.IsEmpty) // skip empty segments
                    {
                        // note: only update object+buffer on success
                        @object = segment;
                        buffer = span;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
            AssertValidPosition();
            const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
            if ((count & TooBigOrNegative) == 0 && _currentSpan.Length - _currentSpanIndex > (int)count)
            {
                _currentSpanIndex += (int)count;
            }
            else
            {
                // Can't satisfy from the current span
                AdvanceToNextSpan(count);
            }
            AssertValidPosition();
        }

        /// <summary>
        /// Unchecked helper to avoid unnecessary checks where you know count is valid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AdvanceCurrentSpan(long count)
        {
            // worst case here is we end at the exact end of the current span
            Debug.Assert(count >= 0 && _currentSpanIndex + count <= _currentSpan.Length);

            _currentSpanIndex += (int)count;
            if (_currentSpanIndex == _currentSpan.Length)
            {
                TryGetNextSpan();
            }
            AssertValidPosition();
        }

        private void AdvanceToNextSpan(long count)
        {
            AssertValidPosition();
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            while (true)
            {
                int remaining = _currentSpan.Length - _currentSpanIndex;

                if (remaining > count)
                {
                    _currentSpanIndex += (int)count;
                    count = 0;
                    break;
                }

                count -= remaining;
                Debug.Assert(count >= 0);

                // always want to move to next span here, even
                // if we've consumed everything we want (to enforce
                // eager fetch)
                if (!TryGetNextSpan() || count == 0)
                {
                    break;
                }
            }

            if (count != 0)
            {
                // Not enough data left- adjust for where we actually ended and throw
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }
            AssertValidPosition();
        }

        /// <summary>
        /// Copies data from the current <see cref="Position"/> to the given <paramref name="destination"/> span if there
        /// is enough data to fill it.
        /// </summary>
        /// <remarks>
        /// This API is used to copy a fixed amount of data out of the sequence if possible. It does not advance
        /// the reader. To look ahead for a specific stream of data <see cref="IsNext(ReadOnlySpan{T}, bool)"/> can be used.
        /// </remarks>
        /// <param name="destination">Destination span to copy to.</param>
        /// <returns>True if there is enough data to completely fill the <paramref name="destination"/> span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<T> destination)
        {
            // This API doesn't advance to facilitate conditional advancement based on the data returned.
            // We don't provide an advance option to allow easier utilizing of stack allocated destination spans.
            // (Because we can make this method readonly we can guarantee that we won't capture the span.)

            ReadOnlySpan<T> firstSpan = UnreadSpan;
            if (firstSpan.Length >= destination.Length)
            {
                firstSpan.Slice(0, destination.Length).CopyTo(destination);
                return true;
            }

            // Not enough in the current span to satisfy the request, fall through to the slow path
            return TryCopyMultisegment(destination);
        }

        internal readonly bool TryCopyMultisegment(Span<T> destination)
        {
            // If we don't have enough to fill the requested buffer, return false
            if (Remaining < destination.Length)
                return false;

            ReadOnlySpan<T> currentSpan = UnreadSpan;
            Debug.Assert(currentSpan.Length < destination.Length);
            currentSpan.CopyTo(destination);
            int copied = currentSpan.Length;

            object? segment = _currentPositionObject;
            while (TryGetNextBuffer(in _sequence, ref segment, ref currentSpan))
            {
                int toCopy = Math.Min(currentSpan.Length, destination.Length - copied);
                currentSpan.Slice(0, toCopy).CopyTo(destination.Slice(copied));
                copied += toCopy;
                if (copied >= destination.Length)
                {
                    break;
                }
            }

            return true;
        }
    }
}
