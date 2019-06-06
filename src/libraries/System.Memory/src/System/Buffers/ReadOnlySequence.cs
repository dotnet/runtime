// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Represents a sequence that can read a sequential series of <typeparam name="T" />.
    /// </summary>
    [DebuggerTypeProxy(typeof(ReadOnlySequenceDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly partial struct ReadOnlySequence<T>
    {
        // The data is essentially two SequencePositions, however the Start and End SequencePositions are deconstructed to improve packing.
        private readonly object? _startObject;
        private readonly object? _endObject;
        private readonly int _startInteger;
        private readonly int _endInteger;

        /// <summary>
        /// Returns empty <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(Array.Empty<T>());

        /// <summary>
        /// Length of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public long Length => GetLength();

        /// <summary>
        /// Determines if the <see cref="ReadOnlySequence{T}"/> is empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Determines if the <see cref="ReadOnlySequence{T}"/> contains a single <see cref="ReadOnlyMemory{T}"/> segment.
        /// </summary>
        public bool IsSingleSegment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _startObject == _endObject;
        }

        /// <summary>
        /// Gets <see cref="ReadOnlyMemory{T}"/> from the first segment.
        /// </summary>
        public ReadOnlyMemory<T> First => GetFirstBuffer();

        /// <summary>
        /// Gets <see cref="ReadOnlySpan{T}"/> from the first segment.
        /// </summary>
        public ReadOnlySpan<T> FirstSpan => GetFirstSpan();

        /// <summary>
        /// A position to the start of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public SequencePosition Start
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new SequencePosition(_startObject, _startInteger);
        }

        /// <summary>
        /// A position to the end of the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public SequencePosition End
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new SequencePosition(_endObject, _endInteger);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySequence(object? startSegment, int startIndexAndFlags, object? endSegment, int endIndexAndFlags)
        {
            // Used by SliceImpl to create new ReadOnlySequence

            // startSegment and endSegment can be null for default ReadOnlySequence only
            Debug.Assert((startSegment != null && endSegment != null) ||
                (startSegment == null && endSegment == null && startIndexAndFlags == 0 && endIndexAndFlags == 0));

            _startObject = startSegment;
            _endObject = endSegment;
            _startInteger = startIndexAndFlags;
            _endInteger = endIndexAndFlags;
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from linked memory list represented by start and end segments
        /// and corresponding indexes in them.
        /// </summary>
        public ReadOnlySequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
        {
            if (startSegment == null ||
                endSegment == null ||
                (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) ||
                (uint)startSegment.Memory.Length < (uint)startIndex ||
                (uint)endSegment.Memory.Length < (uint)endIndex ||
                (startSegment == endSegment && endIndex < startIndex))
                ThrowHelper.ThrowArgumentValidationException(startSegment, startIndex, endSegment);

            _startObject = startSegment;
            _endObject = endSegment;
            _startInteger = ReadOnlySequence.SegmentToSequenceStart(startIndex);
            _endInteger = ReadOnlySequence.SegmentToSequenceEnd(endIndex);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="T:T[]"/>.
        /// </summary>
        public ReadOnlySequence(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            _startObject = array!; // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
            _endObject = array!; // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
            _startInteger = ReadOnlySequence.ArrayToSequenceStart(0);
            _endInteger = ReadOnlySequence.ArrayToSequenceEnd(array!.Length); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="T:T[]"/>, start and index.
        /// </summary>
        public ReadOnlySequence(T[] array, int start, int length)
        {
            if (array == null ||
                (uint)start > (uint)array.Length ||
                (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentValidationException(array, start);

            _startObject = array;
            _endObject = array;
            _startInteger = ReadOnlySequence.ArrayToSequenceStart(start);
            _endInteger = ReadOnlySequence.ArrayToSequenceEnd(start + length);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="ReadOnlyMemory{T}"/>.
        /// Consumer is expected to manage lifetime of memory until <see cref="ReadOnlySequence{T}"/> is not used anymore.
        /// </summary>
        public ReadOnlySequence(ReadOnlyMemory<T> memory)
        {
            if (MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<T>? manager, out int index, out int length))
            {
                _startObject = manager;
                _endObject = manager;
                _startInteger = ReadOnlySequence.MemoryManagerToSequenceStart(index);
                _endInteger = ReadOnlySequence.MemoryManagerToSequenceEnd(length);
            }
            else if (MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment))
            {
                T[]? array = segment.Array;
                int start = segment.Offset;
                _startObject = array;
                _endObject = array;
                _startInteger = ReadOnlySequence.ArrayToSequenceStart(start);
                _endInteger = ReadOnlySequence.ArrayToSequenceEnd(start + segment.Count);
            }
            else if (typeof(T) == typeof(char))
            {
                if (!MemoryMarshal.TryGetString((ReadOnlyMemory<char>)(object)memory, out string? text, out int start, out length))
                    ThrowHelper.ThrowInvalidOperationException();

                _startObject = text;
                _endObject = text;
                _startInteger = ReadOnlySequence.StringToSequenceStart(start);
                _endInteger = ReadOnlySequence.StringToSequenceEnd(start + length);
            }
            else
            {
                // Should never be reached
                ThrowHelper.ThrowInvalidOperationException();
                _startObject = null;
                _endObject = null;
                _startInteger = 0;
                _endInteger = 0;
            }
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A slice that consists of <paramref name="length" /> elements from the current instance starting at index <paramref name="start" />.</returns>
        public ReadOnlySequence<T> Slice(long start, long length)
        {
            if (start < 0 || length < 0)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

            SequencePosition begin;
            SequencePosition end;

            int startIndex = GetIndex(_startInteger);
            int endIndex = GetIndex(_endInteger);

            object? startObject = _startObject;
            object? endObject = _endObject;

            if (startObject != endObject)
            {
                Debug.Assert(startObject != null);
                var startSegment = (ReadOnlySequenceSegment<T>)startObject;

                int currentLength = startSegment.Memory.Length - startIndex;

                // Position in start segment
                if (currentLength > start)
                {
                    startIndex += (int)start;
                    begin = new SequencePosition(startObject, startIndex);

                    end = GetEndPosition(startSegment, startObject, startIndex, endObject!, endIndex, length);
                }
                else
                {
                    if (currentLength < 0)
                        ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

                    begin = SeekMultiSegment(startSegment.Next!, endObject!, endIndex, start - currentLength, ExceptionArgument.start);

                    int beginIndex = GetIndex(begin);
                    object beginObject = begin.GetObject()!;

                    if (beginObject != endObject)
                    {
                        Debug.Assert(beginObject != null);
                        end = GetEndPosition((ReadOnlySequenceSegment<T>)beginObject, beginObject, beginIndex, endObject!, endIndex, length);
                    }
                    else
                    {
                        if (endIndex - beginIndex < length)
                            ThrowHelper.ThrowStartOrEndArgumentValidationException(0);  // Passing value >= 0 means throw exception on length argument

                        end = new SequencePosition(beginObject, beginIndex + (int)length);
                    }
                }
            }
            else
            {
                if (endIndex - startIndex < start)
                    ThrowHelper.ThrowStartOrEndArgumentValidationException(-1); // Passing value < 0 means throw exception on start argument

                startIndex += (int)start;
                begin = new SequencePosition(startObject, startIndex);

                if (endIndex - startIndex < length)
                    ThrowHelper.ThrowStartOrEndArgumentValidationException(0);  // Passing value >= 0 means throw exception on length argument

                end = new SequencePosition(startObject, startIndex + (int)length);
            }

            return SliceImpl(begin, end);
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/> and ending at <paramref name="end"/> (exclusive).
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The ending (exclusive) <see cref="SequencePosition"/> of the slice.</param>
        /// <returns>A slice that consists of items from the <paramref name="start" /> index to, but not including, the <paramref name="end" /> sequence position in the current read-only sequence.</returns>
        public ReadOnlySequence<T> Slice(long start, SequencePosition end)
        {
            if (start < 0)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

            uint sliceEndIndex = (uint)GetIndex(end);
            object? sliceEndObject = end.GetObject();

            uint startIndex = (uint)GetIndex(_startInteger);
            object? startObject = _startObject;

            uint endIndex = (uint)GetIndex(_endInteger);
            object? endObject = _endObject;

            // Single-Segment Sequence
            if (startObject == endObject)
            {
                if (!InRange(sliceEndIndex, startIndex, endIndex))
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
                }

                if (sliceEndIndex - startIndex < start)
                    ThrowHelper.ThrowStartOrEndArgumentValidationException(-1); // Passing value < 0 means throw exception on start argument

                goto FoundInFirstSegment;
            }

            // Multi-Segment Sequence
            var startSegment = (ReadOnlySequenceSegment<T>)startObject!;
            ulong startRange = (ulong)(startSegment.RunningIndex + startIndex);
            ulong sliceRange = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject!).RunningIndex + sliceEndIndex);

            // This optimization works because we know sliceEndIndex, startIndex, and endIndex are all >= 0
            Debug.Assert(sliceEndIndex >= 0 && startIndex >= 0 && endIndex >= 0);
            if (!InRange(
                sliceRange,
                startRange,
                (ulong)(((ReadOnlySequenceSegment<T>)endObject!).RunningIndex + endIndex)))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
            }

            if (startRange + (ulong)start > sliceRange)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            }

            int currentLength = startSegment.Memory.Length - (int)startIndex;

            // Position not in startSegment
            if (currentLength <= start)
            {
                if (currentLength < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

                // End of segment. Move to start of next.
                SequencePosition begin = SeekMultiSegment(startSegment.Next!, sliceEndObject, (int)sliceEndIndex, start - currentLength, ExceptionArgument.start);
                return SliceImpl(begin, end);
            }

        FoundInFirstSegment:
            // startIndex + start <= int.MaxValue
            Debug.Assert(start <= int.MaxValue - startIndex);
            return SliceImpl(new SequencePosition(startObject, (int)startIndex + (int)start), end);
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items.
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A slice that consists of <paramref name="length" /> elements from the current instance starting at sequence position <paramref name="start" />.</returns>
        public ReadOnlySequence<T> Slice(SequencePosition start, long length)
        {
            // Check start before length
            uint sliceStartIndex = (uint)GetIndex(start);
            object? sliceStartObject = start.GetObject();

            uint startIndex = (uint)GetIndex(_startInteger);
            object? startObject = _startObject;

            uint endIndex = (uint)GetIndex(_endInteger);
            object? endObject = _endObject;

            // Single-Segment Sequence
            if (startObject == endObject)
            {
                if (!InRange(sliceStartIndex, startIndex, endIndex))
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
                }

                if (length < 0)
                    // Passing value >= 0 means throw exception on length argument
                    ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

                if (endIndex - sliceStartIndex < length)
                    ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

                goto FoundInFirstSegment;
            }

            // Multi-Segment Sequence
            var sliceStartSegment = (ReadOnlySequenceSegment<T>)sliceStartObject!;
            ulong sliceRange = (ulong)((sliceStartSegment.RunningIndex + sliceStartIndex));
            ulong startRange = (ulong)(((ReadOnlySequenceSegment<T>)startObject!).RunningIndex + startIndex);
            ulong endRange = (ulong)(((ReadOnlySequenceSegment<T>)endObject!).RunningIndex + endIndex);

            // This optimization works because we know sliceStartIndex, startIndex, and endIndex are all >= 0
            Debug.Assert(sliceStartIndex >= 0 && startIndex >= 0 && endIndex >= 0);
            if (!InRange(sliceRange, startRange, endRange))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
            }

            if (length < 0)
                // Passing value >= 0 means throw exception on length argument
                ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

            if (sliceRange + (ulong)length > endRange)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }

            int currentLength = sliceStartSegment.Memory.Length - (int)sliceStartIndex;

            // Position not in startSegment
            if (currentLength < length)
            {
                if (currentLength < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

                // End of segment. Move to start of next.
                SequencePosition end = SeekMultiSegment(sliceStartSegment.Next!, endObject, (int)endIndex, length - currentLength, ExceptionArgument.length);
                return SliceImpl(start, end);
            }

        FoundInFirstSegment:
            // sliceStartIndex + length <= int.MaxValue
            Debug.Assert(length <= int.MaxValue - sliceStartIndex);
            return SliceImpl(start, new SequencePosition(sliceStartObject, (int)sliceStartIndex + (int)length));
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A slice that consists of <paramref name="length" /> elements from the current instance starting at index <paramref name="start" />.</returns>
        public ReadOnlySequence<T> Slice(int start, int length) => Slice((long)start, length);

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/> and ending at <paramref name="end"/> (exclusive).
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The ending (exclusive) <see cref="SequencePosition"/> of the slice.</param>
        /// <returns>A slice that consists of items from the <paramref name="start" /> index to, but not including, the <paramref name="end" /> sequence position in the current read-only sequence.</returns>
        public ReadOnlySequence<T> Slice(int start, SequencePosition end) => Slice((long)start, end);

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items.
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A slice that consists of <paramref name="length" /> elements from the current instance starting at sequence position <paramref name="start" />.</returns>
        public ReadOnlySequence<T> Slice(SequencePosition start, int length) => Slice(start, (long)length);

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at <paramref name="end"/> (exclusive).
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="end">The ending (exclusive) <see cref="SequencePosition"/> of the slice.</param>
        /// <returns>A slice that consists of items from the <paramref name="start" /> sequence position to, but not including, the <paramref name="end" /> sequence position in the current read-only sequence.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySequence<T> Slice(SequencePosition start, SequencePosition end)
        {
            BoundsCheck((uint)GetIndex(start), start.GetObject(), (uint)GetIndex(end), end.GetObject());
            return SliceImpl(start, end);
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}" />, beginning at a specified sequence position and continuing to the end of the read-only sequence.
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <returns>A slice starting at sequence position <paramref name="start" /> and continuing to the end of the current read-only sequence.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySequence<T> Slice(SequencePosition start)
        {
            BoundsCheck(start);
            return SliceImpl(start);
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ReadOnlySequence{T}" /> , beginning at a specified index and continuing to the end of the read-only sequence.
        /// </summary>
        /// <param name="start">The start index at which to begin this slice.</param>
        /// <returns>A slice starting at index <paramref name="start" /> and continuing to the end of the current read-only sequence.</returns>
        public ReadOnlySequence<T> Slice(long start)
        {
            if (start < 0)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

            if (start == 0)
                return this;

            SequencePosition begin = Seek(start, ExceptionArgument.start);
            return SliceImpl(begin);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                ReadOnlySequence<T> localThis = this;
                ReadOnlySequence<char> charSequence = Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<char>>(ref localThis);

                if (SequenceMarshal.TryGetString(charSequence, out string? text, out int start, out int length))
                {
                    return text!.Substring(start, length); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                }

                if (Length < int.MaxValue)
                {
                    return string.Create((int)Length, charSequence, (span, sequence) => sequence.CopyTo(span)); 
                }
            }

            return string.Format("System.Buffers.ReadOnlySequence<{0}>[{1}]", typeof(T).Name, Length);
        }

        /// <summary>
        /// Returns an enumerator over the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Returns a new <see cref="SequencePosition"/> at an <paramref name="offset"/> from the start of the sequence.
        /// </summary>
        public SequencePosition GetPosition(long offset)
        {
            if (offset < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();

            return Seek(offset);
        }

        /// <summary>
        /// Returns a new <see cref="SequencePosition"/> at an <paramref name="offset"/> from the <paramref name="origin"/>
        /// </summary>
        public SequencePosition GetPosition(long offset, SequencePosition origin)
        {
            if (offset < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();

            return Seek(origin, offset);
        }

        /// <summary>
        /// Tries to retrieve next segment after <paramref name="position"/> and return its contents in <paramref name="memory"/>.
        /// Returns <code>false</code> if end of <see cref="ReadOnlySequence{T}"/> was reached otherwise <code>true</code>.
        /// Sets <paramref name="position"/> to the beginning of next segment if <paramref name="advance"/> is set to <code>true</code>.
        /// </summary>
        public bool TryGet(ref SequencePosition position, out ReadOnlyMemory<T> memory, bool advance = true)
        {
            bool result = TryGetBuffer(position, out memory, out SequencePosition next);
            if (advance)
            {
                position = next;
            }

            return result;
        }

        /// <summary>
        /// An enumerator over the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public struct Enumerator
        {
            private readonly ReadOnlySequence<T> _sequence;
            private SequencePosition _next;
            private ReadOnlyMemory<T> _currentMemory;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="sequence">The <see cref="ReadOnlySequence{T}"/> to enumerate.</param>
            public Enumerator(in ReadOnlySequence<T> sequence)
            {
                _currentMemory = default;
                _next = sequence.Start;
                _sequence = sequence;
            }

            /// <summary>
            /// The current <see cref="ReadOnlyMemory{T}"/>
            /// </summary>
            public ReadOnlyMemory<T> Current => _currentMemory;

            /// <summary>
            /// Moves to the next <see cref="ReadOnlyMemory{T}"/> in the <see cref="ReadOnlySequence{T}"/>
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (_next.GetObject() == null)
                {
                    return false;
                }

                return _sequence.TryGet(ref _next, out _currentMemory);
            }
        }

        private enum SequenceType
        {
            MultiSegment = 0x00,
            Array = 0x1,
            MemoryManager = 0x2,
            String = 0x3,
            Empty = 0x4
        }
    }

    internal static class ReadOnlySequence
    {
        public const int FlagBitMask = 1 << 31;
        public const int IndexBitMask = ~FlagBitMask;

        public const int SegmentStartMask = 0;
        public const int SegmentEndMask = 0;

        public const int ArrayStartMask = 0;
        public const int ArrayEndMask = FlagBitMask;

        public const int MemoryManagerStartMask = FlagBitMask;
        public const int MemoryManagerEndMask = 0;

        public const int StringStartMask = FlagBitMask;
        public const int StringEndMask = FlagBitMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SegmentToSequenceStart(int startIndex) => startIndex | SegmentStartMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SegmentToSequenceEnd(int endIndex) => endIndex | SegmentEndMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ArrayToSequenceStart(int startIndex) => startIndex | ArrayStartMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ArrayToSequenceEnd(int endIndex) => endIndex | ArrayEndMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemoryManagerToSequenceStart(int startIndex) => startIndex | MemoryManagerStartMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemoryManagerToSequenceEnd(int endIndex) => endIndex | MemoryManagerEndMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StringToSequenceStart(int startIndex) => startIndex | StringStartMask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StringToSequenceEnd(int endIndex) => endIndex | StringEndMask;
    }
}
