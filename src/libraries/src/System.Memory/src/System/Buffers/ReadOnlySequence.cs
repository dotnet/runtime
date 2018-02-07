// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// Represents a sequence that can read a sequential series of <typeparam name="T" />.
    /// </summary>
    public readonly partial struct ReadOnlySequence<T>
    {
        private const int IndexBitMask = 0x7FFFFFFF;

        private const int MemoryListStartMask = 0;
        private const int MemoryListEndMask = 0;

        private const int ArrayStartMask = 0;
        private const int ArrayEndMask = 1 << 31;

        private const int OwnedMemoryStartMask = 1 << 31;
        private const int OwnedMemoryEndMask = 0;

        private readonly SequencePosition _sequenceStart;
        private readonly SequencePosition _sequenceEnd;

        /// <summary>
        /// Returns empty <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(new T[0]);

        /// <summary>
        /// Length of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public long Length => GetLength(_sequenceStart, _sequenceEnd);

        /// <summary>
        /// Determines if the <see cref="ReadOnlySequence{T}"/> is empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Determines if the <see cref="ReadOnlySequence{T}"/> contains a single <see cref="ReadOnlyMemory{T}"/> segment.
        /// </summary>
        public bool IsSingleSegment => _sequenceStart.Segment == _sequenceEnd.Segment;

        /// <summary>
        /// Gets <see cref="ReadOnlyMemory{T}"/> from the first segment.
        /// </summary>
        public ReadOnlyMemory<T> First
        {
            get
            {
                TryGetBuffer(_sequenceStart, _sequenceEnd, out ReadOnlyMemory<T> first, out _);
                return first;
            }
        }

        /// <summary>
        /// A position to the start of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public SequencePosition Start => _sequenceStart;

        /// <summary>
        /// A position to the end of the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public SequencePosition End => _sequenceEnd;

        private ReadOnlySequence(object startSegment, int startIndex, object endSegment, int endIndex)
        {
            Debug.Assert(startSegment != null);
            Debug.Assert(endSegment != null);

            _sequenceStart = new SequencePosition(startSegment, startIndex);
            _sequenceEnd = new SequencePosition(endSegment, endIndex);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from linked memory list represented by start and end segments
        /// and corresponding indexes in them.
        /// </summary>
        public ReadOnlySequence(IMemoryList<T> startSegment, int startIndex, IMemoryList<T> endSegment, int endIndex)
        {
            if (startSegment == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.startSegment);
            if (endSegment == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endSegment);
            if (startIndex < 0 || startSegment.Memory.Length < startIndex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex);
            if (endIndex < 0 || endSegment.Memory.Length < endIndex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endIndex);
            if (startSegment == endSegment && endIndex < startIndex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endIndex);

            _sequenceStart = new SequencePosition(startSegment, startIndex | MemoryListStartMask);
            _sequenceEnd = new SequencePosition(endSegment, endIndex | MemoryListEndMask);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="T:T[]"/>.
        /// </summary>
        public ReadOnlySequence(T[] array) : this(array, 0, array.Length)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="T:T[]"/>, start and index.
        /// </summary>
        public ReadOnlySequence(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (start < 0 || start > array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            if (length < 0 || length > array.Length - start)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            _sequenceStart = new SequencePosition(array, start | ArrayStartMask);
            _sequenceEnd = new SequencePosition(array, start + length | ArrayEndMask);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="ReadOnlyMemory{T}"/>.
        /// Consumer is expected to manage lifetime of memory until <see cref="ReadOnlySequence{T}"/> is not used anymore.
        /// </summary>
        public ReadOnlySequence(ReadOnlyMemory<T> readOnlyMemory)
        {
            ReadOnlySequenceSegment segment = new ReadOnlySequenceSegment
            {
                Memory = MemoryMarshal.AsMemory(readOnlyMemory)
            };
            _sequenceStart = new SequencePosition(segment, 0 | MemoryListStartMask);
            _sequenceEnd = new SequencePosition(segment, readOnlyMemory.Length | MemoryListEndMask);
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="OwnedMemory{T}"/>.
        /// Consumer is expected to manage lifetime of memory until <see cref="ReadOnlySequence{T}"/> is not used anymore.
        /// </summary>
        public ReadOnlySequence(OwnedMemory<T> ownedMemory): this(ownedMemory, 0, ownedMemory.Length)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="OwnedMemory{T}"/>, start and length.
        /// Consumer is expected to manage lifetime of memory until <see cref="ReadOnlySequence{T}"/> is not used anymore.
        /// </summary>
        public ReadOnlySequence(OwnedMemory<T> ownedMemory, int start, int length)
        {
            if (ownedMemory == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.ownedMemory);
            if (start < 0 || start > ownedMemory.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            if (length < 0 || length > ownedMemory.Length - start)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            _sequenceStart = new SequencePosition(ownedMemory, start | OwnedMemoryStartMask);
            _sequenceEnd = new SequencePosition(ownedMemory, start + length | OwnedMemoryEndMask);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The length of the slice</param>
        public ReadOnlySequence<T> Slice(long start, long length)
        {
            SequencePosition begin = Seek(_sequenceStart, _sequenceEnd, start, false);
            SequencePosition end = Seek(begin, _sequenceEnd, length, false);
            return SliceImpl(begin, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at <paramref name="end"/> (inclusive).
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The end (inclusive) of the slice</param>
        public ReadOnlySequence<T> Slice(long start, SequencePosition end)
        {
            BoundsCheck(_sequenceEnd, end);

            SequencePosition begin = Seek(_sequenceStart, end, start);
            return SliceImpl(begin, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="length">The length of the slice</param>
        public ReadOnlySequence<T> Slice(SequencePosition start, long length)
        {
            BoundsCheck(_sequenceEnd, start);

            SequencePosition end = Seek(start, _sequenceEnd, length, false);
            return SliceImpl(start, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The length of the slice</param>
        public ReadOnlySequence<T> Slice(int start, int length)
        {
            SequencePosition begin = Seek(_sequenceStart, _sequenceEnd, start, false);
            SequencePosition end = Seek(begin, _sequenceEnd, length, false);
            return SliceImpl(begin, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at <paramref name="end"/> (inclusive).
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The end (inclusive) of the slice</param>
        public ReadOnlySequence<T> Slice(int start, SequencePosition end)
        {
            BoundsCheck(_sequenceEnd, end);

            SequencePosition begin = Seek(_sequenceStart, end, start);
            return SliceImpl(begin, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at '<paramref name="start"/>, with <paramref name="length"/> items
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="length">The length of the slice</param>
        public ReadOnlySequence<T> Slice(SequencePosition start, int length)
        {
            BoundsCheck(_sequenceEnd, start);

            SequencePosition end = Seek(start, _sequenceEnd, length, false);
            return SliceImpl(start, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at <paramref name="end"/> (inclusive).
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        /// <param name="end">The ending (inclusive) <see cref="SequencePosition"/> of the slice</param>
        public ReadOnlySequence<T> Slice(SequencePosition start, SequencePosition end)
        {
            BoundsCheck(_sequenceEnd, end);
            BoundsCheck(end, start);

            return SliceImpl(start, end);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at the existing <see cref="ReadOnlySequence{T}"/>'s end.
        /// </summary>
        /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
        public ReadOnlySequence<T> Slice(SequencePosition start)
        {
            BoundsCheck(_sequenceEnd, start);

            return SliceImpl(start, _sequenceEnd);
        }

        /// <summary>
        /// Forms a slice out of the given <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, ending at the existing <see cref="ReadOnlySequence{T}"/>'s end.
        /// </summary>
        /// <param name="start">The start index at which to begin this slice.</param>
        public ReadOnlySequence<T> Slice(long start)
        {
            if (start == 0) return this;

            SequencePosition begin = Seek(_sequenceStart, _sequenceEnd, start, false);
            return SliceImpl(begin, _sequenceEnd);
        }

        /// <inheritdoc />
        public override string ToString() => string.Format("System.Buffers.ReadOnlySequence<{0}>[{1}]", typeof(T).Name, Length);

        /// <summary>
        /// Returns an enumerator over the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Returns a new <see cref="SequencePosition"/> at an <paramref name="offset"/> from the <paramref name="origin"/>
        /// </summary>
        public SequencePosition GetPosition(SequencePosition origin, long offset)
        {
            if (offset < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.offset);

            return Seek(origin, _sequenceEnd, offset, false);
        }

        /// <summary>
        /// Tries to retrieve next segment after <paramref name="position"/> and return its contents in <paramref name="data"/>.
        /// Returns <code>false</code> if end of <see cref="ReadOnlySequence{T}"/> was reached otherwise <code>true</code>.
        /// Sets <paramref name="position"/> to the beginning of next segment if <paramref name="advance"/> is set to <code>true</code>.
        /// </summary>
        public bool TryGet(ref SequencePosition position, out ReadOnlyMemory<T> data, bool advance = true)
        {
            bool result = TryGetBuffer(position, End, out data, out SequencePosition next);
            if (advance)
            {
                position = next;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySequence<T> SliceImpl(SequencePosition begin, SequencePosition end)
        {
            // In this method we reset high order bits from indices
            // of positions that were passed in
            // and apply type bits specific for current ReadOnlySequence type

            return new ReadOnlySequence<T>(
                begin.Segment,
                begin.Index & IndexBitMask | (Start.Index & ~IndexBitMask),
                end.Segment,
                end.Index & IndexBitMask | (End.Index & ~IndexBitMask)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SequenceType GetSequenceType()
        {
            // We take high order bits of two indexes index and move them
            // to a first and second position to convert to BufferType
            // Masking with 2 is required to only keep the second bit of Start.Index
            return (SequenceType)((((uint)Start.Index >> 30) & 2) | (uint)End.Index >> 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int index) => index & IndexBitMask;

        private enum SequenceType
        {
            MemoryList = 0x00,
            Array = 0x1,
            OwnedMemory = 0x2
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
            public Enumerator(ReadOnlySequence<T> sequence)
            {
                _sequence = sequence;
                _currentMemory = default;
                _next = sequence.Start;
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
                if (_next.Segment == null)
                {
                    return false;
                }

                return _sequence.TryGet(ref _next, out _currentMemory);
            }
        }
    }
}
