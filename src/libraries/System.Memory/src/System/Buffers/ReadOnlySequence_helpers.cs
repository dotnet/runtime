// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    public readonly partial struct ReadOnlySequence<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetBuffer(in SequencePosition start, in SequencePosition end, out ReadOnlyMemory<T> data, out SequencePosition next)
        {
            next = default;

            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            int length = endIndex - startIndex;
            object startSegment = start.GetObject();
            object endSegment = end.GetObject();

            if (type != SequenceType.IMemoryList && startSegment != endSegment)
                ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();

            if (type == SequenceType.IMemoryList)
            {
                IMemoryList<T> startMemoryList = (IMemoryList<T>)startSegment;
                if (startSegment != endSegment)
                {
                    next = GetBufferCrossSegment(startIndex, end, ref startMemoryList, ref length);
                }

                data = startMemoryList.Memory.Slice(startIndex, length);
            }
            else if (type == SequenceType.Array)
            {
                data = new ReadOnlyMemory<T>((T[])startSegment, startIndex, length);
            }
            else if (type == SequenceType.OwnedMemory)
            {
                data = ((OwnedMemory<T>)startSegment).Memory.Slice(startIndex, length);
            }
            else if (typeof(T) == typeof(char) && type == SequenceType.String)
            {
                data = (ReadOnlyMemory<T>)(object)((string)startSegment).AsMemory(startIndex, length);
            }
            else
            {
                data = default;
            }

            return type != SequenceType.Empty;
        }

        private static SequencePosition GetBufferCrossSegment(int startIndex, in SequencePosition end, ref IMemoryList<T> startMemoryList, ref int length)
        {
            IMemoryList<T> nextSegment = startMemoryList.Next;
            int currentLength = startMemoryList.Memory.Length - startIndex;

            while (currentLength == 0 && nextSegment != null)
            {
                // Skip empty Segments
                startMemoryList = nextSegment;
                nextSegment = nextSegment.Next;
                currentLength = startMemoryList.Memory.Length;
            }

            length = currentLength;
            SequencePosition next;
            if (nextSegment == null)
            {
                if (end.GetObject() != null)
                    ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();

                next = default;
            }
            else
            {
                next = new SequencePosition(nextSegment, 0);
            }

            return next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(in SequencePosition start, in SequencePosition end, int count, bool checkEndReachable = true)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startSegment = start.GetObject();
            bool notInRange = endIndex - startIndex < count;
            if (type == SequenceType.IMemoryList)
            {
                object endSegment = end.GetObject();
                if (notInRange || startSegment != endSegment)
                {
                    return SeekMultiSegment(startSegment, startIndex, endSegment, endIndex, count, checkEndReachable);
                }
            }
            else if (notInRange)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            return new SequencePosition(startSegment, startIndex + count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(in SequencePosition start, in SequencePosition end, long count, bool checkEndReachable = true)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startSegment = start.GetObject();
            bool notInRange = endIndex - startIndex < count;
            if (type == SequenceType.IMemoryList)
            {
                object endSegment = end.GetObject();
                if (notInRange || startSegment != endSegment)
                {
                    return SeekMultiSegment(startSegment, startIndex, endSegment, endIndex, count, checkEndReachable);
                }
            }
            else if (notInRange)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            return new SequencePosition(startSegment, startIndex + (int)count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SequencePosition SeekMultiSegment(object startSegment, int startIndex, object endSegment, int endPosition, long count, bool checkEndReachable)
        {
            var current = (IMemoryList<T>)startSegment;
            var end = (IMemoryList<T>)endSegment;

            SequencePosition result = default;
            int currentIndex = startIndex;

            do
            {
                bool isCurrentAtEnd = current == end;

                int memoryLength = isCurrentAtEnd ? endPosition - currentIndex : current.Memory.Length - currentIndex;

                if (memoryLength > count || (memoryLength == count && isCurrentAtEnd))
                {
                    result = new SequencePosition(current, currentIndex + (int)count);
                    break;
                }

                count -= memoryLength;
                currentIndex = 0;
                current = current.Next;
            } while (current != null);

            if (current == null)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            if (checkEndReachable)
            {
                while (current.Next != null)
                {
                    current = current.Next;
                }
                if (current != end)
                    ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetLength(in SequencePosition start, in SequencePosition end)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            if (type == SequenceType.IMemoryList)
            {
                object startSegment = start.GetObject();
                object endSegment = end.GetObject();
                if (startSegment != endSegment)
                {
                    return GetLengthMultiSegment(startSegment, startIndex, endSegment, endIndex);
                }
            }

            return endIndex - startIndex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long GetLengthMultiSegment(object startSegment, int startIndex, object endSegment, int endIndex)
        {
            var start = (IMemoryList<T>)startSegment;
            var end = (IMemoryList<T>)endSegment;
            // (end.RunningIndex + endIndex) - (start.RunningIndex + startIndex) // (End offset) - (start offset)
            return end.RunningIndex - start.RunningIndex - startIndex + endIndex; // Rearranged to avoid overflow
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoundsCheck(in SequencePosition start, in SequencePosition end)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            if (type == SequenceType.IMemoryList)
            {
                object startSegment = start.GetObject();
                object endSegment = end.GetObject();

                if (startSegment != endSegment)
                {
                    BoundsCheckMultiSegment(startSegment, startIndex, endSegment, endIndex);
                    return;
                }
            }

            if (startIndex <= endIndex)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BoundsCheckMultiSegment(object startSegment, int startIndex, object endSegment, int endIndex)
        {
            var start = (IMemoryList<T>)startSegment;
            var end = (IMemoryList<T>)endSegment;

            // start.RunningIndex + startIndex <= end.RunningIndex + endIndex
            if (start.RunningIndex - endIndex <= end.RunningIndex - startIndex) // Rearranged to avoid overflow
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
        }

        internal bool TryGetMemoryList(out IMemoryList<T> startSegment, out int startIndex, out IMemoryList<T> endSegment, out int endIndex)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out startIndex, out endIndex);

            if (type != SequenceType.IMemoryList)
            {
                startSegment = null;
                endSegment = null;
                return false;
            }

            startSegment = (IMemoryList<T>)Start.GetObject();
            endSegment = (IMemoryList<T>)End.GetObject();
            return true;
        }

        internal bool TryGetArray(out ArraySegment<T> array)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            if (type != SequenceType.Array)
            {
                array = default;
                return false;
            }

            array = new ArraySegment<T>((T[])Start.GetObject(), startIndex, endIndex - startIndex);
            return true;
        }

        internal bool TryGetOwnedMemory(out OwnedMemory<T> ownedMemory, out int start, out int length)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out start, out int endIndex);

            if (type != SequenceType.OwnedMemory)
            {
                ownedMemory = default;
                length = 0;
                return false;
            }

            ownedMemory = (OwnedMemory<T>)Start.GetObject();
            length = endIndex - start;
            return true;
        }

        internal bool TryGetReadOnlyMemory(out ReadOnlyMemory<T> memory)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startSegment = Start.GetObject();
            object endSegment = End.GetObject();
            int length = endIndex - startIndex;
            if (startSegment != endSegment)
            {
                // Can't get ReadOnlyMemory from multi-block segments
                memory = default;
                return false;
            }
            else if (type == SequenceType.Array)
            {
                memory = new ReadOnlyMemory<T>((T[])startSegment, startIndex, length);
            }
            else if (type == SequenceType.OwnedMemory)
            {
                memory = ((OwnedMemory<T>)startSegment).Memory.Slice(startIndex, length);
            }
            else if (typeof(T) == typeof(char) && type == SequenceType.String)
            {
                memory = (ReadOnlyMemory<T>)(object)((string)startSegment).AsMemory(startIndex, length);
            }
            else // IMemoryList
            {
                memory = ((IMemoryList<T>)startSegment).Memory.Slice(startIndex, length);
            }

            return true;
        }

        internal bool TryGetString(out string text, out int start, out int length)
        {
            if (typeof(T) != typeof(char))
            {
                start = 0;
                length = 0;
                text = null;
                return false;
            }

            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            if (type != SequenceType.String)
            {
                start = 0;
                length = 0;
                text = null;
                return false;
            }

            start = startIndex;
            length = endIndex - startIndex;
            text = (string)Start.GetObject();

            return true;
        }
    }
}
