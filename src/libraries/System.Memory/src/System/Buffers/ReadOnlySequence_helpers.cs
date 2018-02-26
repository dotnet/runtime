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
            if (start.GetObject() == null)
            {
                data = default;
                next = default;
                return false;
            }

            int startIndex = start.GetInteger();
            int endIndex = end.GetInteger();
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    var segment = (IMemoryList<T>)start.GetObject();
                    Memory<T> bufferSegmentMemory = segment.Memory;
                    int currentEndIndex = bufferSegmentMemory.Length;

                    if (segment == end.GetObject())
                    {
                        currentEndIndex = endIndex;
                        next = default;
                    }
                    else
                    {
                        IMemoryList<T> nextSegment = segment.Next;
                        if (nextSegment == null)
                        {
                            if (end.GetObject() != null)
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
                    var ownedMemory = (OwnedMemory<T>)start.GetObject();
                    if (ownedMemory != end.GetObject())
                    {
                        ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                    }

                    data = ownedMemory.Memory.Slice(startIndex, endIndex - startIndex);
                    next = default;
                    return true;

                case SequenceType.Array:
                    var array = (T[])start.GetObject();

                    if (array != end.GetObject())
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
            int startIndex = start.GetInteger();
            int endIndex = end.GetInteger();
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    if (start.GetObject() == end.GetObject() && endIndex - startIndex >= count)
                    {
                        return new SequencePosition(start.GetObject(), startIndex + count);
                    }
                    return SeekMultiSegment((IMemoryList<byte>)start.GetObject(), startIndex, (IMemoryList<byte>)end.GetObject(), endIndex, count, checkEndReachable);

                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    if (start.GetObject() != end.GetObject())
                    {
                        ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
                    }

                    if (endIndex - startIndex >= count)
                    {
                        return new SequencePosition(start.GetObject(), startIndex + count);
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
            int startIndex = start.GetInteger();
            int endIndex = end.GetInteger();
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    if (start.GetObject() == end.GetObject() && endIndex - startIndex >= count)
                    {
                        // end.Index >= count + Index and end.Index is int
                        return new SequencePosition(start.GetObject(), startIndex + (int)count);
                    }
                    return SeekMultiSegment((IMemoryList<byte>)start.GetObject(), startIndex, (IMemoryList<byte>)end.GetObject(), endIndex, count, checkEndReachable);

                case SequenceType.OwnedMemory:
                case SequenceType.Array:
                    if (endIndex - startIndex >= count)
                    {
                        // end.Index >= count + Index and end.Index is int
                        return new SequencePosition(start.GetObject(), startIndex + (int)count);
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
            int startIndex = start.GetInteger();
            int endIndex = end.GetInteger();
            SequenceType type = GetSequenceType();

            startIndex = GetIndex(startIndex);
            endIndex = GetIndex(endIndex);

            switch (type)
            {
                case SequenceType.MemoryList:
                    return GetLength((IMemoryList<T>)start.GetObject(), startIndex, (IMemoryList<T>)end.GetObject(), endIndex);

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
            int startIndex = start.GetInteger();
            int endIndex = position.GetInteger();
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
                    IMemoryList<T> segment = (IMemoryList<T>)position.GetObject();
                    IMemoryList<T> memoryList = (IMemoryList<T>)start.GetObject();

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

        internal bool TryGetMemoryList(out IMemoryList<T> startSegment, out int startIndex, out IMemoryList<T> endSegment, out int endIndex)
        {
            if (Start.GetObject() == null  || GetSequenceType() != SequenceType.MemoryList)
            {
                startSegment = null;
                endSegment = null;
                startIndex = 0;
                endIndex = 0;
                return false;
            }

            startIndex = GetIndex(Start.GetInteger());
            endIndex = GetIndex(End.GetInteger());
            startSegment = (IMemoryList<T>)Start.GetObject();
            endSegment = (IMemoryList<T>)End.GetObject();
            return true;
        }

        internal bool TryGetArray(out ArraySegment<T> array)
        {
            if (Start.GetObject() == null  || GetSequenceType() != SequenceType.Array)
            {
                array = default;
                return false;
            }

            int startIndex = GetIndex(Start.GetInteger());
            array = new ArraySegment<T>((T[])Start.GetObject(), startIndex, GetIndex(End.GetInteger()) - startIndex);
            return true;
        }

        internal bool TryGetOwnedMemory(out OwnedMemory<T> ownedMemory, out int start, out int length)
        {
            if (Start.GetObject() == null  || GetSequenceType() != SequenceType.OwnedMemory)
            {
                ownedMemory = default;
                start = 0;
                length = 0;
                return false;
            }

            ownedMemory = (OwnedMemory<T>)Start.GetObject();
            start = GetIndex(Start.GetInteger());
            length = GetIndex(End.GetInteger()) - start;
            return true;
        }

        internal bool TryGetReadOnlyMemory(out ReadOnlyMemory<T> memory)
        {
            // Currently ReadOnlyMemory is stored inside single segment of IMemoryList
            if (TryGetMemoryList(out IMemoryList<T> startSegment, out int startIndex, out IMemoryList<T> endSegment, out int endIndex) &&
                startSegment == endSegment)
            {
                memory = startSegment.Memory.Slice(startIndex, endIndex - startIndex);
                return true;
            }

            memory = default;
            return false;
        }

        private class ReadOnlySequenceSegment : IMemoryList<T>
        {
            public Memory<T> Memory { get; set; }
            public IMemoryList<T> Next { get; set; }
            public long RunningIndex { get; set; }
        }
    }
}
