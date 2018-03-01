// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Buffers
{
    public readonly partial struct ReadOnlySequence<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetBuffer(in SequencePosition start, in SequencePosition end, out ReadOnlyMemory<T> data, out SequencePosition next)
        {
            SequenceType type;
            int startIndex = 0;
            int endIndex = 0;
            next = default;
            object startObject = start.GetObject();
            if (startObject != null)
            {
                GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out type, out startIndex, out endIndex);
            }
            else
            {
                type = SequenceType.Empty;
            }

            int length = endIndex - startIndex;
            object endObject = end.GetObject();

            if (type != SequenceType.MultiSegment && type != SequenceType.Empty && startObject != endObject)
                ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();

            if (type == SequenceType.MultiSegment)
            {
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                ReadOnlySequenceSegment<T> startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);

                if (startSegment != endObject)
                {
                    next = GetBufferCrossSegment(startIndex, endObject, ref startSegment, ref length);
                }

                data = startSegment.Memory;
            }
            else if (type == SequenceType.Array)
            {
                Debug.Assert(startObject is T[]);

                data = new ReadOnlyMemory<T>(Unsafe.As<T[]>(startObject));
            }
            else if (type == SequenceType.OwnedMemory)
            {
                Debug.Assert(startObject is OwnedMemory<T>);

                data = (Unsafe.As<OwnedMemory<T>>(startObject)).Memory;
            }
            else if (typeof(T) == typeof(char) && type == SequenceType.String)
            {
                Debug.Assert(startObject is string);

                data = (ReadOnlyMemory<T>)(object)(Unsafe.As<string>(startObject)).AsMemory();
            }
            else
            {
                data = default;
                return false;
            }

            data = data.Slice(startIndex, length);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlyMemory<T> GetFirstBuffer(in SequencePosition start, in SequencePosition end)
        {
            SequenceType type;
            int startIndex = 0;
            int endIndex = 0;
            object startObject = start.GetObject();
            if (startObject != null)
            {
                GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out type, out startIndex, out endIndex);
            }
            else
            {
                type = SequenceType.Empty;
            }

            int length = endIndex - startIndex;
            object endObject = end.GetObject();

            if (type != SequenceType.MultiSegment && type != SequenceType.Empty && startObject != endObject)
                ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();

            ReadOnlyMemory<T> memory;
            if (type == SequenceType.MultiSegment)
            {
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                ReadOnlySequenceSegment<T> startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);
                memory = startSegment.Memory;
            }
            else if (type == SequenceType.Array)
            {
                Debug.Assert(startObject is T[]);

                memory = new ReadOnlyMemory<T>(Unsafe.As<T[]>(startObject));
            }
            else if (type == SequenceType.OwnedMemory)
            {
                Debug.Assert(startObject is OwnedMemory<T>);

                memory = (Unsafe.As<OwnedMemory<T>>(startObject)).Memory;
            }
            else if (typeof(T) == typeof(char) && type == SequenceType.String)
            {
                Debug.Assert(startObject is string);

                memory = (ReadOnlyMemory<T>)(object)(Unsafe.As<string>(startObject)).AsMemory();
            }
            else
            {
                return default;
            }

            return memory.Slice(startIndex, length);
        }

        private static SequencePosition GetBufferCrossSegment(int startIndex, object endObject, ref ReadOnlySequenceSegment<T> startSegment, ref int length)
        {
            Debug.Assert(startSegment != null);

            ReadOnlySequenceSegment<T> nextSegment = startSegment.Next;
            int currentLength = startSegment.Memory.Length - startIndex;

            while (currentLength == 0 && nextSegment != endObject && nextSegment != null)
            {
                // startSegment is at the end of a segment; move it to start of the next.
                // However, skip any empty segments, else the caller will immedately run out of data and call back
                startSegment = nextSegment;
                nextSegment = nextSegment.Next;
                currentLength = startSegment.Memory.Length;
            }

            length = currentLength;
            while (nextSegment != null && nextSegment.Memory.Length == 0)
            {
                // nextSegment is an empty segment, skip until one is found with data.
                if (nextSegment == endObject)
                {
                    // Reached the end and no segment with data found,
                    // set nextSegment to null as if there were no following segments. 
                    endObject = nextSegment = null;
                    break;
                }

                nextSegment = nextSegment.Next;
            }

            if (nextSegment != null)
            {
                return new SequencePosition(nextSegment, 0);
            }
            else if (endObject != null)
                ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(in SequencePosition start, in SequencePosition end, int count)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startObject = start.GetObject();
            object endObject = end.GetObject();

            bool notInRange = endIndex - startIndex < count;
            if (type == SequenceType.MultiSegment && startObject != endObject)
            {
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                var startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);

                int currentLength = startSegment.Memory.Length - startIndex;
                if (currentLength > count)
                {
                    // Position in start segment, defer to single segment seek
                    notInRange = false;
                }
                else
                {
                    return SeekMultiSegment(startSegment, startIndex, endObject, endIndex, count - currentLength);
                }
            }
            else
            {
                Debug.Assert(startObject == endObject);
            }

            if (notInRange)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            // Single segment Seek
            return new SequencePosition(startObject, startIndex + count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SequencePosition Seek(in SequencePosition start, in SequencePosition end, long count)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startObject = start.GetObject();
            object endObject = end.GetObject();

            bool notInRange = endIndex - startIndex < count;
            if (type == SequenceType.MultiSegment && startObject != endObject)
            {
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                var startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);

                int currentLength = startSegment.Memory.Length - startIndex;
                if (currentLength > count)
                {
                    // Position in start segment, defer to single segment seek
                    notInRange = false;
                }
                else
                {
                    return SeekMultiSegment(startSegment, startIndex, endObject, endIndex, count - currentLength);
                }
            }
            else
            {
                Debug.Assert(startObject == endObject);
            }

            if (notInRange)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            // Single segment Seek
            return new SequencePosition(startObject, startIndex + (int)count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SequencePosition SeekMultiSegment(ReadOnlySequenceSegment<T> currentSegment, int startIndex, object endObject, int endPosition, long count)
        {
            Debug.Assert(currentSegment != null);
            Debug.Assert(currentSegment.Next != null);
            Debug.Assert(count >= 0);

            // End of segment. Move to start of next.
            currentSegment = currentSegment.Next;

            if (count > 0 || currentSegment.Memory.Length == 0)
            {
                while (currentSegment != null)
                {
                    bool isCurrentAtEnd = currentSegment == endObject;
                    int memoryLength = isCurrentAtEnd ? endPosition : currentSegment.Memory.Length;

                    if (memoryLength > count || (memoryLength == count && isCurrentAtEnd))
                    {
                        // Fully contained in this segment; or at end of segment but there isn't a next one to move to
                        break;
                    }

                    // Move to next
                    count -= memoryLength;
                    currentSegment = currentSegment.Next;
                }
            }

            // Hit the end of the segments but didn't reach the count
            if (currentSegment == null)
                ThrowHelper.ThrowArgumentOutOfRangeException_CountOutOfRange();

            return new SequencePosition(currentSegment, (int)count);
        }

        private static void CheckEndReachable(object startSegment, object endSegment)
        {
            Debug.Assert(startSegment != null);
            Debug.Assert(startSegment is ReadOnlySequenceSegment<T>);
            Debug.Assert(endSegment != null);

            var current = Unsafe.As<ReadOnlySequenceSegment<T>>(startSegment);

            while (current.Next != null)
            {
                current = current.Next;
                if (current == endSegment)
                {
                    // Found end
                    return;
                }
            }

            ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetLength(in SequencePosition start, in SequencePosition end)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startObject = start.GetObject();
            object endObject = end.GetObject();
            if (type == SequenceType.MultiSegment && startObject != endObject)
            {
                Debug.Assert(startObject != null);
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                Debug.Assert(endObject != null);
                Debug.Assert(endObject is ReadOnlySequenceSegment<T>);

                var startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);
                var endSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(endObject);
                // (end.RunningIndex + endIndex) - (start.RunningIndex + startIndex) // (End offset) - (start offset)
                return endSegment.RunningIndex - startSegment.RunningIndex - startIndex + endIndex; // Rearranged to avoid overflow
            }

            Debug.Assert(startObject == endObject);
            // Single segment length
            return endIndex - startIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoundsCheck(in SequencePosition start, in SequencePosition end)
        {
            GetTypeAndIndices(start.GetInteger(), end.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startObject = start.GetObject();
            object endObject = end.GetObject();
            if (type == SequenceType.MultiSegment && startObject != endObject)
            {
                Debug.Assert(startObject != null);
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);
                Debug.Assert(endObject != null);
                Debug.Assert(endObject is ReadOnlySequenceSegment<T>);

                var startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject);
                var endSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(endObject);

                // start.RunningIndex + startIndex <= end.RunningIndex + endIndex
                if (startSegment.RunningIndex - endIndex <= endSegment.RunningIndex - startIndex) // Rearranged to avoid overflow
                {
                    // Mult-segment in bounds
                    return;
                }
            }
            else if (startIndex <= endIndex)
            {
                Debug.Assert(startObject == endObject);
                // Single segment in bounds
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
        }

        internal bool TryGetReadOnlySequenceSegment(out ReadOnlySequenceSegment<T> startSegment, out int startIndex, out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out startIndex, out endIndex);

            if (type != SequenceType.MultiSegment)
            {
                startSegment = null;
                endSegment = null;
                return false;
            }

            Debug.Assert(Start.GetObject() != null);
            Debug.Assert(Start.GetObject() is ReadOnlySequenceSegment<T>);
            Debug.Assert(End.GetObject() != null);
            Debug.Assert(End.GetObject() is ReadOnlySequenceSegment<T>);

            startSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(Start.GetObject());
            endSegment = Unsafe.As<ReadOnlySequenceSegment<T>>(End.GetObject());
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

            Debug.Assert(Start.GetObject() != null);
            Debug.Assert(Start.GetObject() is T[]);

            array = new ArraySegment<T>(Unsafe.As<T[]>(Start.GetObject()), startIndex, endIndex - startIndex);
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

            Debug.Assert(Start.GetObject() != null);
            Debug.Assert(Start.GetObject() is OwnedMemory<T>);

            ownedMemory = Unsafe.As<OwnedMemory<T>>(Start.GetObject());
            length = endIndex - start;
            return true;
        }

        internal bool TryGetReadOnlyMemory(out ReadOnlyMemory<T> memory)
        {
            GetTypeAndIndices(Start.GetInteger(), End.GetInteger(), out SequenceType type, out int startIndex, out int endIndex);

            object startObject = Start.GetObject();
            object endObject = End.GetObject();

            Debug.Assert(startObject != null);
            Debug.Assert(endObject != null);

            int length = endIndex - startIndex;
            if (startObject != endObject)
            {
                // Can't get ReadOnlyMemory from multi-block segments
                memory = default;
                return false;
            }
            else if (type == SequenceType.Array)
            {
                Debug.Assert(startObject is T[]);

                memory = new ReadOnlyMemory<T>(Unsafe.As<T[]>(startObject));
            }
            else if (type == SequenceType.OwnedMemory)
            {
                Debug.Assert(startObject is OwnedMemory<T>);

                memory = Unsafe.As<OwnedMemory<T>>(startObject).Memory;
            }
            else if (typeof(T) == typeof(char) && type == SequenceType.String)
            {
                Debug.Assert(startObject is string);

                var text = Unsafe.As<string>(startObject);
                memory = (ReadOnlyMemory<T>)(object)text.AsMemory();
            }
            else // ReadOnlySequenceSegment
            {
                Debug.Assert(startObject is ReadOnlySequenceSegment<T>);

                memory = Unsafe.As<ReadOnlySequenceSegment<T>>(startObject).Memory;
            }

            memory = memory.Slice(startIndex, length);
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
