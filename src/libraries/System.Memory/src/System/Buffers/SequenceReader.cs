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
        private object? _currentPositionObject, _nextPositionObject;
        private int _currentPositionInteger, _nextPositionInteger, _currentSpanIndex;

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

            var position = sequence.Start;
            _currentPositionObject = position.GetObject();
            _currentPositionInteger = position.GetInteger();
            sequence.GetFirstSpan(out _currentSpan, next: out position);
            _nextPositionObject = position.GetObject();
            _nextPositionInteger = position.GetInteger();
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
                    GetNextSpan();
                }
            }
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
            if (_currentSpanIndex == _currentSpan.Length) // only true at EOF due to eager read
            {
                value = default;
                return false;
            }
            value = _currentSpan[_currentSpanIndex];
            return true;
        }

        /// <summary>
        /// Peeks at the next value at specific offset without advancing the reader.
        /// </summary>
        /// <param name="offset">The offset from current position.</param>
        /// <param name="value">The next value, or the default value if at the end of the reader.</param>
        /// <returns><c>true</c> if the reader is not at its end and the peek operation succeeded; <c>false</c> if at the end of the reader.</returns>
        public readonly bool TryPeek(long offset, out T value)
        {
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
                SequencePosition position = new(_nextPositionObject, _nextPositionInteger);

                ReadOnlyMemory<T> currentMemory;
                while (_sequence.TryGetBuffer(position, out currentMemory, out var next))
                {
                    position = next;
                    // Skip empty segment
                    if (currentMemory.Length > 0)
                    {
                        if (remainingOffset >= currentMemory.Length)
                        {
                            // Subtract current non consumed data
                            remainingOffset -= currentMemory.Length;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                value = currentMemory.Span[(int)remainingOffset];
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
            switch (_currentSpan.Length - _currentSpanIndex)
            {
                case 0: // end of current span; since we move ahead eagerly: EOF
                    value = default;
                    return false;
                case 1: // one left in current span
                    value = _currentSpan[_currentSpanIndex];
                    GetNextSpan(); // move ahead eagerly
                    return true;
                default:
                    value = _currentSpan[_currentSpanIndex++];
                    return true;

            }
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
            var length = _length;

            // reset all state
            this = new(_sequence);

            // reinstate the length
            Unsafe.AsRef(_length) = length;
        }

        /// <summary>
        /// Get the next segment with available data, if any.
        /// </summary>
        private bool GetNextSpan()
        {
            _consumedAtStartOfCurrentSpan += _currentSpan.Length; // track consumed length
            SequencePosition position;
            if (!_sequence.IsSingleSegment)
            {
                position = new(_nextPositionObject, _nextPositionInteger);

                while (_sequence.TryGetBuffer(position, out var memory, out var next))
                {
                    if (memory.Length > 0)
                    {
                        _currentSpan = memory.Span;
                        _currentSpanIndex = 0;
                        _currentPositionObject = position.GetObject();
                        _currentPositionInteger = position.GetInteger();
                        _nextPositionObject = next.GetObject();
                        _nextPositionInteger = next.GetInteger();
                        return true;
                    }
                    position = next;
                }
            }

            // at EOF
            position = _sequence.End;
            _currentPositionObject = position.GetObject();
            _currentPositionInteger = position.GetInteger();
            _nextPositionObject = default;
            _nextPositionInteger = default;
            _currentSpan = default;
            _currentSpanIndex = 0;
            Unsafe.AsRef(in _length) = _consumedAtStartOfCurrentSpan; // since we know it, avoid later cost if Length accessed
            return false;
        }

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
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
                GetNextSpan();
        }

        private void AdvanceToNextSpan(long count)
        {
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
                if (!GetNextSpan() || count == 0)
                {
                    break;
                }
            }

            if (count != 0)
            {
                // Not enough data left- adjust for where we actually ended and throw
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }
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

            ReadOnlySpan<T> firstSpan = UnreadSpan;
            Debug.Assert(firstSpan.Length < destination.Length);
            firstSpan.CopyTo(destination);
            int copied = firstSpan.Length;

            SequencePosition position = new(_nextPositionObject, _nextPositionInteger);
            while (_sequence.TryGetBuffer(position, out var nextSegment, out var next))
            {
                position = next;
                if (nextSegment.Length > 0)
                {
                    ReadOnlySpan<T> nextSpan = nextSegment.Span;
                    int toCopy = Math.Min(nextSpan.Length, destination.Length - copied);
                    nextSpan.Slice(0, toCopy).CopyTo(destination.Slice(copied));
                    copied += toCopy;
                    if (copied >= destination.Length)
                    {
                        break;
                    }
                }
            }

            return true;
        }
    }
}
