// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    public ref partial struct SequenceReader<T> where T : unmanaged, IEquatable<T>
    {
        private SequencePosition _currentPosition;
        private SequencePosition _nextPosition;
        private readonly long _length;
        private long _consumedAtStartOfCurrentSpan;

        /// <summary>
        /// Create a <see cref="SequenceReader{T}"/> over the given <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SequenceReader(ReadOnlySequence<T> sequence)
        {
            _currentSpanIndex = 0;
            _consumedAtStartOfCurrentSpan = 0;
            Sequence = sequence;
            _currentPosition = sequence.Start;
            _length = -1;
            sequence.GetFirstSpan(out ReadOnlySpan<T> first, out _nextPosition);
            SetCurrrentSpan(first);

            if (_currentSpanIndex == CurrentSpanLength && !sequence.IsSingleSegment)
            {
                GetNextSpan();
            }
        }

        /// <summary>
        /// True when there is no more data in the <see cref="Sequence"/>.
        /// </summary>
        public readonly bool End => _currentSpanIndex == CurrentSpanLength; // because of eager fetch, only ever true at EOF

        /// <summary>
        /// The underlying <see cref="ReadOnlySequence{T}"/> for the reader.
        /// </summary>
        public ReadOnlySequence<T> Sequence { get; }

        /// <summary>
        /// Gets the unread portion of the <see cref="Sequence"/>.
        /// </summary>
        /// <value>
        /// The unread portion of the <see cref="Sequence"/>.
        /// </value>
        public readonly ReadOnlySequence<T> UnreadSequence => Sequence.Slice(Position);

        /// <summary>
        /// The current position in the <see cref="Sequence"/>.
        /// </summary>
        public readonly SequencePosition Position
            => Sequence.GetPosition(_currentSpanIndex, _currentPosition);

        /// <summary>
        /// The current segment in the <see cref="Sequence"/> as a span.
        /// </summary>
        public readonly ReadOnlySpan<T> CurrentSpan
        {
            get
            {
#if NET7_0_OR_GREATER
                return MemoryMarshal.CreateReadOnlySpan<T>(ref CurrentSpanStart, CurrentSpanLength);
#else
                return _currentSpan;
#endif
            }
        }

        // only NET7+ can use 'ref T' field; use directly when possible;
        // on down-level TFMs, use JIT-friendly property
#if NET7_0_OR_GREATER
        private ref T CurrentSpanStart;
        private int CurrentSpanLength, _currentSpanIndex;
        private void SetCurrrentSpan(ReadOnlySpan<T> span)
        {
            _consumedAtStartOfCurrentSpan += CurrentSpanLength; // account for previous
            CurrentSpanStart = ref Unsafe.AsRef(in span.GetPinnableReference());
            CurrentSpanLength = span.Length;
            _currentSpanIndex = 0;
        }
        private void WipeCurrentSpan() => CurrentSpanLength = _currentSpanIndex = 0; // no need to wipe ref
#else
        private ReadOnlySpan<T> _currentSpan;
        private ref T CurrentSpanStart => ref Unsafe.AsRef(in current.GetPinnableReference());
        private void SetCurrentSpan(ReadOnlySpan<T> span)
        {
            _currentSpan = span;
            _currentSpanIndex = 0;
        }
        private void WipeCurrentSpan()
        {
            _currentSpan = default;
            _currentSpanIndex = 0;
        }
#endif

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
            get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref CurrentSpanStart, _currentSpanIndex), CurrentSpanLength - _currentSpanIndex);
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
                    Unsafe.AsRef(in _length) = Sequence.Length;
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
            if (_currentSpanIndex == CurrentSpanLength) // only true at EOF due to eager read
            {
                value = default;
                return false;
            }
            value = Unsafe.Add(ref CurrentSpanStart, _currentSpanIndex);
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
            if (_currentSpanIndex == CurrentSpanLength || Remaining <= offset)
            {
                value = default;
                return false;
            }

            // Sum CurrentSpanIndex + offset could overflow as is but the value of offset should be very large
            // because we check Remaining <= offset above so to overflow we should have a ReadOnlySequence close to 8 exabytes
            Debug.Assert(_currentSpanIndex + offset >= 0);

            // If offset doesn't fall inside current segment move to next until we find correct one
            if ((_currentSpanIndex + offset) <= CurrentSpanLength - 1)
            {
                Debug.Assert(offset <= int.MaxValue);

                value = Unsafe.Add(ref CurrentSpanStart, _currentSpanIndex + (int)offset);
                return true;
            }
            else
            {
                long remainingOffset = offset - (CurrentSpanLength - _currentSpanIndex);
                SequencePosition nextPosition = _nextPosition;
                ReadOnlyMemory<T> currentMemory;

                while (Sequence.TryGet(ref nextPosition, out currentMemory, advance: true))
                {
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
            switch (CurrentSpanLength - _currentSpanIndex)
            {
                case 0: // end of current span; since we move ahead eagerly: EOF
                    value = default;
                    return false;
                case 1: // one left in current span
                    value = Unsafe.Add(ref CurrentSpanStart, _currentSpanIndex);
                    GetNextSpan(); // move ahead eagerly
                    return true;
                default:
                    value = Unsafe.Add(ref CurrentSpanStart, _currentSpanIndex++);
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
            _consumedAtStartOfCurrentSpan = 0;
            _currentPosition = Sequence.Start;
            _nextPosition = _currentPosition;

            // make sure SetCurrrentSpan doesn't count the existing
            // span when advancing _runningIndex
            WipeCurrentSpan();

            if (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true))
            {
                if (memory.Length == 0)
                {
                    // No data in the first span, move to one with data
                    GetNextSpan();
                }
                else
                {
                    SetCurrrentSpan(memory.Span);
                }
            }
            else
            {
                // No data in any spans and at end of sequence
                SetCurrrentSpan(default);
            }
        }

        /// <summary>
        /// Get the next segment with available data, if any.
        /// </summary>
        private bool GetNextSpan()
        {
            if (!Sequence.IsSingleSegment)
            {
                SequencePosition previousNextPosition = _nextPosition;
                while (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true))
                {
                    _currentPosition = previousNextPosition;
                    if (memory.Length > 0)
                    {
                        SetCurrrentSpan(memory.Span);
                        return true;
                    }
                    previousNextPosition = _nextPosition;
                }
            }
            SetCurrrentSpan(default);
            return false;
        }

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
            const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
            if ((count & TooBigOrNegative) == 0 && CurrentSpanLength - _currentSpanIndex > (int)count)
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
            Debug.Assert(count >= 0 && _currentSpanIndex + count <= CurrentSpanLength);

            _currentSpanIndex += (int)count;
            if (_currentSpanIndex == CurrentSpanLength)
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
                int remaining = CurrentSpanLength - _currentSpanIndex;

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

            SequencePosition next = _nextPosition;
            while (Sequence.TryGet(ref next, out ReadOnlyMemory<T> nextSegment, true))
            {
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
