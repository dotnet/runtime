// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public readonly partial struct ReadOnlySequence<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetBuffer(SequencePosition start, SequencePosition end, out ReadOnlyMemory<T> data, out SequencePosition next)
        {
            if (start.Segment == null)
            {
                data = default;
                next = default;
                return false;
            }

            int startIndex = start.Index;
            int endIndex = end.Index;
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    var segment = (IMemoryList<T>)start.Segment;
                    Memory<T> bufferSegmentMemory = segment.Memory;
                    int currentEndIndex = bufferSegmentMemory.Length;

                    if (segment == end.Segment)
                    {
                        currentEndIndex = endIndex;
                        next = default;
                    }
                    else
                    {
                        IMemoryList<T> nextSegment = segment.Next;
                        if (nextSegment == null)
                        {
                            if (end.Segment != null)
                            {
                                ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                            }

                            next = default;
                        }
                        else
                        {
                            next = new SequencePosition(nextSegment, 0);
                        }
                    }

                    data = bufferSegmentMemory.Slice(startIndex, currentEndIndex - startIndex);
                    return true;

                case SequenceType.OwnedMemory:
                    var ownedMemory = (OwnedMemory<T>)start.Segment;
                    if (ownedMemory != end.Segment)
                    {
                        ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                    }

                    data = ownedMemory.Memory.Slice(startIndex, endIndex - startIndex);
                    next = default;
                    return true;

                case SequenceType.Array:
                    var array = (T[])start.Segment;

                    if (array != end.Segment)
                    {
                        ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                    }

                    data = new Memory<T>(array, startIndex, endIndex - startIndex);
                    next = default;
                    return true;

                default:
                    ThrowHelper.ThrowInvalidOperationException_UnexpectedSegmentType();
                    next = default;
                    data = default;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(SequencePosition start, SequencePosition end, int count, bool checkEndReachable = true)
        {
            int startIndex = start.Index;
            int endIndex = end.Index;
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    if (start.Segment == end.Segment && endIndex - startIndex >= count)
                    {
                        return new SequencePosition(start.Segment, startIndex + count);
                    }
                    return SeekMultiSegment((IMemoryList<byte>)start.Segment, startIndex, (IMemoryList<byte>)end.Segment, endIndex, count, checkEndReachable);

                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    if (start.Segment != end.Segment)
                    {
                        ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                    }

                    if (endIndex - startIndex >= count)
                    {
                        return new SequencePosition(start.Segment, startIndex + count);
                    }

                    ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();
                    return default;

                default:
                    ThrowHelper.ThrowInvalidOperationException_UnexpectedSegmentType();
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(SequencePosition start, SequencePosition end, long count, bool checkEndReachable = true)
        {
            int startIndex = start.Index;
            int endIndex = end.Index;
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    if (start.Segment == end.Segment && endIndex - startIndex >= count)
                    {
                        // end.Index >= count + Index and end.Index is int
                        return new SequencePosition(start.Segment, startIndex + (int)count);
                    }
                    return SeekMultiSegment((IMemoryList<byte>)start.Segment, startIndex, (IMemoryList<byte>)end.Segment, endIndex, count, checkEndReachable);

                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    if (endIndex - startIndex >= count)
                    {
                        // end.Index >= count + Index and end.Index is int
                        return new SequencePosition(start.Segment, startIndex + (int)count);
                    }

                    ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();
                    return default;

                default:
                    ThrowHelper.ThrowInvalidOperationException_UnexpectedSegmentType();
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SequencePosition SeekMultiSegment(IMemoryList<byte> start, int startIndex, IMemoryList<byte> end, int endPosition, long count, bool checkEndReachable)
        {
            SequencePosition result = default;
            bool foundResult = false;
            IMemoryList<byte> current = start;
            int currentIndex = startIndex;

            while (current != null)
            {
                // We need to loop up until the end to make sure start and end are connected
                // if end is not trusted
                if (!foundResult)
                {
                    var isEnd = current == end;
                    int currentEnd = isEnd ? endPosition : current.Memory.Length;
                    int currentLength = currentEnd - currentIndex;

                    // We would prefer to put position in the beginning of next segment
                    // then past the end of previous one, but only if we are not leaving current buffer
                    if (currentLength > count ||
                       (currentLength == count && isEnd))
                    {
                        result = new SequencePosition(current, currentIndex + (int)count);
                        foundResult = true;
                        if (!checkEndReachable)
                        {
                            break;
                        }
                    }

                    count -= currentLength;
                }

                if (current.Next == null && current != end)
                {
                    ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                }

                current = current.Next;
                currentIndex = 0;
            }

            if (!foundResult)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetLength(SequencePosition start, SequencePosition end)
        {
            int startIndex = start.Index;
            int endIndex = end.Index;
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    return GetLength((IMemoryList<T>)start.Segment, startIndex, (IMemoryList<T>)end.Segment, endIndex);

                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    return endIndex - startIndex;

                default:
                    ThrowHelper.ThrowInvalidOperationException_UnexpectedSegmentType();
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetLength(IMemoryList<T> start, int startIndex, IMemoryList<T> endSegment, int endIndex)
        {
            if (start == endSegment)
            {
                return endIndex - startIndex;
            }

            return (endSegment.RunningIndex - start.Next.RunningIndex) // Length of data in between first and last segment
                   + (start.Memory.Length - startIndex) // Length of data in first segment
                   + endIndex; // Length of data in last segment
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoundsCheck(SequencePosition start, SequencePosition position)
        {
            int startIndex = start.Index;
            int endIndex = position.Index;
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    if (endIndex > startIndex)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
                    }
                    return;

                case SequenceType.MemoryList:
                    IMemoryList<T> segment = (IMemoryList<T>)position.Segment;
                    IMemoryList<T> memoryList = (IMemoryList<T>)start.Segment;

                    if (segment.RunningIndex - startIndex > memoryList.RunningIndex - endIndex)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
                    }
                    return;

                default:
                    ThrowHelper.ThrowInvalidOperationException_UnexpectedSegmentType();
                    return;
            }
        }

        private class ReadOnlySequenceSegment : IMemoryList<T>
        {
            public Memory<T> Memory { get; set; }
            public IMemoryList<T> Next { get; set; }
            public long RunningIndex { get; set; }
        }
    }
}
