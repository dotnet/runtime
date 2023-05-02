// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public partial class BlobBuilder
    {
        // The implementation is akin to StringBuilder.
        // The differences:
        // - BlobBuilder allows efficient sequential write of the built content to a stream.
        // - BlobBuilder allows for chunk allocation customization. A custom allocator can use pooling strategy, for example.

        internal const int DefaultChunkSize = 256;

        // Must be at least the size of the largest primitive type we write atomically (Guid).
        internal const int MinChunkSize = 16;

        // Builders are linked like so:
        //
        // [1:first]->[2]->[3:last]<-[4:head]
        //     ^_______________|
        //
        // In this case the content represented is a sequence (1,2,3,4).
        // This structure optimizes for append write operations and sequential enumeration from the start of the chain.
        // Data can only be written to the head node. Other nodes are "frozen".
        private BlobBuilder _nextOrPrevious;
        private BlobBuilder FirstChunk => _nextOrPrevious._nextOrPrevious;

        // The sum of lengths of all preceding chunks (not including the current chunk),
        // or a difference between original buffer length of a builder that was linked as a suffix to another builder,
        // and the current length of the buffer (not that the buffers are swapped when suffix linking).
        private int _previousLengthOrFrozenSuffixLengthDelta;

        private byte[] _buffer;

        // The length of data in the buffer in lower 31 bits.
        // Head: highest bit is 0, length may be 0.
        // Non-head: highest bit is 1, lower 31 bits are not all 0.
        private uint _length;

        private const uint IsFrozenMask = 0x80000000;
        private bool IsHead => (_length & IsFrozenMask) == 0;
        private int Length => (int)(_length & ~IsFrozenMask);
        private uint FrozenLength => _length | IsFrozenMask;
        private Span<byte> Span => _buffer.AsSpan(0, Length);

        public BlobBuilder(int capacity = DefaultChunkSize)
        {
            if (capacity < 0)
            {
                Throw.ArgumentOutOfRange(nameof(capacity));
            }

            _nextOrPrevious = this;
            _buffer = new byte[Math.Max(MinChunkSize, capacity)];
        }

        protected virtual BlobBuilder AllocateChunk(int minimalSize)
        {
            return new BlobBuilder(Math.Max(_buffer.Length, minimalSize));
        }

        protected virtual void FreeChunk()
        {
            // nop
        }

        public void Clear()
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            // Swap buffer with the first chunk.
            // Note that we need to keep holding on all allocated buffers,
            // so that builders with custom allocator can release them.
            var first = FirstChunk;
            if (first != this)
            {
                var firstBuffer = first._buffer;
                first._length = FrozenLength;
                first._buffer = _buffer;
                _buffer = firstBuffer;
            }

            // free all chunks except for the current one
            foreach (BlobBuilder chunk in GetChunks())
            {
                if (chunk != this)
                {
                    chunk.ClearChunk();
                    chunk.FreeChunk();
                }
            }

            ClearChunk();
        }

        protected void Free()
        {
            Clear();
            FreeChunk();
        }

        // internal for testing
        internal void ClearChunk()
        {
            _length = 0;
            _previousLengthOrFrozenSuffixLengthDelta = 0;
            _nextOrPrevious = this;
        }

        [Conditional("DEBUG")]
        private void CheckInvariants()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(Length >= 0 && Length <= _buffer.Length);
            Debug.Assert(_nextOrPrevious != null);

            if (IsHead)
            {
                Debug.Assert(_previousLengthOrFrozenSuffixLengthDelta >= 0);

                // last chunk:
                int totalLength = 0;
                foreach (var chunk in GetChunks())
                {
                    Debug.Assert(chunk.IsHead || chunk.Length > 0);
                    totalLength += chunk.Length;
                }

                Debug.Assert(totalLength == Count);
            }
        }

        public int Count => _previousLengthOrFrozenSuffixLengthDelta + Length;

        private int PreviousLength
        {
            get
            {
                Debug.Assert(IsHead);
                return _previousLengthOrFrozenSuffixLengthDelta;
            }
            set
            {
                Debug.Assert(IsHead);
                _previousLengthOrFrozenSuffixLengthDelta = value;
            }
        }

        protected int FreeBytes => _buffer.Length - Length;

        // internal for testing
        protected internal int ChunkCapacity => _buffer.Length;

        // internal for testing
        internal Chunks GetChunks()
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            return new Chunks(this);
        }

        /// <summary>
        /// Returns a sequence of all blobs that represent the content of the builder.
        /// </summary>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public Blobs GetBlobs()
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            return new Blobs(this);
        }

        /// <summary>
        /// Compares the current content of this writer with another one.
        /// </summary>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public bool ContentEquals(BlobBuilder other)
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            if (!other.IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            if (Count != other.Count)
            {
                return false;
            }

            var leftEnumerator = GetChunks();
            var rightEnumerator = other.GetChunks();
            int leftStart = 0;
            int rightStart = 0;

            bool leftContinues = leftEnumerator.MoveNext();
            bool rightContinues = rightEnumerator.MoveNext();

            while (leftContinues && rightContinues)
            {
                Debug.Assert(leftStart == 0 || rightStart == 0);

                var left = leftEnumerator.Current;
                var right = rightEnumerator.Current;

                int minLength = Math.Min(left.Length - leftStart, right.Length - rightStart);
                if (!left._buffer.AsSpan(leftStart, minLength).SequenceEqual(right._buffer.AsSpan(rightStart, minLength)))
                {
                    return false;
                }

                leftStart += minLength;
                rightStart += minLength;

                // nothing remains in left chunk to compare:
                if (leftStart == left.Length)
                {
                    leftContinues = leftEnumerator.MoveNext();
                    leftStart = 0;
                }

                // nothing remains in left chunk to compare:
                if (rightStart == right.Length)
                {
                    rightContinues = rightEnumerator.MoveNext();
                    rightStart = 0;
                }
            }

            return leftContinues == rightContinues;
        }

        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public byte[] ToArray()
        {
            return ToArray(0, Count);
        }

        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the buffer content.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public byte[] ToArray(int start, int byteCount)
        {
            BlobUtilities.ValidateRange(Count, start, byteCount, nameof(byteCount));

            var result = new byte[byteCount];

            int chunkStart = 0;
            int bufferStart = start;
            int bufferEnd = start + byteCount;
            foreach (var chunk in GetChunks())
            {
                int chunkEnd = chunkStart + chunk.Length;
                Debug.Assert(bufferStart >= chunkStart);

                if (chunkEnd > bufferStart)
                {
                    int bytesToCopy = Math.Min(bufferEnd, chunkEnd) - bufferStart;
                    Debug.Assert(bytesToCopy >= 0);

                    Array.Copy(chunk._buffer, bufferStart - chunkStart, result, bufferStart - start, bytesToCopy);
                    bufferStart += bytesToCopy;

                    if (bufferStart == bufferEnd)
                    {
                        break;
                    }
                }

                chunkStart = chunkEnd;
            }

            Debug.Assert(bufferStart == bufferEnd);

            return result;
        }

        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public ImmutableArray<byte> ToImmutableArray()
        {
            return ToImmutableArray(0, Count);
        }

        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the buffer content.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public ImmutableArray<byte> ToImmutableArray(int start, int byteCount)
        {
            byte[]? array = ToArray(start, byteCount);
            return ImmutableCollectionsMarshal.AsImmutableArray(array);
        }

        internal bool TryGetSpan(out ReadOnlySpan<byte> buffer)
        {
            if (_nextOrPrevious == this)
            {
                // If the blob builder has one chunk, we can just return it and avoid copies.
                buffer = Span;
                return true;
            }

            buffer = default;
            return false;
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(Stream destination)
        {
            if (destination is null)
            {
                Throw.ArgumentNull(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.Write(chunk._buffer, 0, chunk.Length);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is default(<see cref="BlobWriter"/>).</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(ref BlobWriter destination)
        {
            if (destination.IsDefault)
            {
                Throw.ArgumentNull(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.WriteBytes(chunk.Span);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(BlobBuilder destination)
        {
            if (destination is null)
            {
                Throw.ArgumentNull(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.WriteBytes(chunk.Span);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void LinkPrefix(BlobBuilder prefix)
        {
            if (prefix is null)
            {
                Throw.ArgumentNull(nameof(prefix));
            }

            // TODO: consider copying data from right to left while there is space

            if (!prefix.IsHead || !IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            // avoid chaining empty chunks:
            if (prefix.Count == 0)
            {
                return;
            }

            PreviousLength += prefix.Count;

            // prefix is not a head anymore:
            prefix._length = prefix.FrozenLength;

            // First and last chunks:
            //
            // [PrefixFirst]->[]->[PrefixLast] <- [prefix]    [First]->[]->[Last] <- [this]
            //       ^_________________|                          ^___________|
            //
            // Degenerate cases:
            // this == First == Last and/or prefix == PrefixFirst == PrefixLast.
            var first = FirstChunk;
            var prefixFirst = prefix.FirstChunk;
            var last = _nextOrPrevious;
            var prefixLast = prefix._nextOrPrevious;

            // Relink like so:
            // [PrefixFirst]->[]->[PrefixLast] -> [prefix] -> [First]->[]->[Last] <- [this]
            //      ^________________________________________________________|

            _nextOrPrevious = (last != this) ? last : prefix;
            prefix._nextOrPrevious = (first != this) ? first : (prefixFirst != prefix) ? prefixFirst : prefix;

            if (last != this)
            {
                last._nextOrPrevious = (prefixFirst != prefix) ? prefixFirst : prefix;
            }

            if (prefixLast != prefix)
            {
                prefixLast._nextOrPrevious = prefix;
            }

            prefix.CheckInvariants();
            CheckInvariants();
        }

        /// <exception cref="ArgumentNullException"><paramref name="suffix"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void LinkSuffix(BlobBuilder suffix)
        {
            if (suffix is null)
            {
                Throw.ArgumentNull(nameof(suffix));
            }

            // TODO: consider copying data from right to left while there is space

            if (!IsHead || !suffix.IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            // avoid chaining empty chunks:
            if (suffix.Count == 0)
            {
                return;
            }

            bool isEmpty = Count == 0;

            // swap buffers of the heads:
            var suffixBuffer = suffix._buffer;
            uint suffixLength = suffix._length;
            int suffixPreviousLength = suffix.PreviousLength;
            int oldSuffixLength = suffix.Length;
            suffix._buffer = _buffer;
            suffix._length = FrozenLength; // suffix is not a head anymore
            _buffer = suffixBuffer;
            _length = suffixLength;

            PreviousLength += suffix.Length + suffixPreviousLength;

            // Update the _previousLength of the suffix so that suffix.Count = suffix._previousLength + suffix.Length doesn't change.
            // Note that the resulting previous length might be negative.
            // The value is not used, other than for calculating the value of Count property.
            suffix._previousLengthOrFrozenSuffixLengthDelta = suffixPreviousLength + oldSuffixLength - suffix.Length;

            if (!isEmpty)
            {
                // First and last chunks:
                //
                // [First]->[]->[Last] <- [this]    [SuffixFirst]->[]->[SuffixLast]  <- [suffix]
                //    ^___________|                       ^_________________|
                //
                // Degenerate cases:
                // this == First == Last and/or suffix == SuffixFirst == SuffixLast.
                var first = FirstChunk;
                var suffixFirst = suffix.FirstChunk;
                var last = _nextOrPrevious;
                var suffixLast = suffix._nextOrPrevious;

                // Relink like so:
                // [First]->[]->[Last] -> [suffix] -> [SuffixFirst]->[]->[SuffixLast]  <- [this]
                //    ^_______________________________________________________|
                _nextOrPrevious = suffixLast;
                suffix._nextOrPrevious = (suffixFirst != suffix) ? suffixFirst : (first != this) ? first : suffix;

                if (last != this)
                {
                    last._nextOrPrevious = suffix;
                }

                if (suffixLast != suffix)
                {
                    suffixLast._nextOrPrevious = (first != this) ? first : suffix;
                }
            }

            CheckInvariants();
            suffix.CheckInvariants();
        }

        private void AddLength(int value)
        {
            _length += (uint)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Expand(int newLength)
        {
            // TODO: consider converting the last chunk to a smaller one if there is too much empty space left

            // May happen only if the derived class attempts to write to a builder that is not last,
            // or if a builder prepended to another one is not discarded.
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            var newChunk = AllocateChunk(Math.Max(newLength, MinChunkSize));
            if (newChunk.ChunkCapacity < newLength)
            {
                // The overridden allocator didn't provide large enough buffer:
                throw new InvalidOperationException(SR.Format(SR.ReturnedBuilderSizeTooSmall, GetType(), nameof(AllocateChunk)));
            }

            var newBuffer = newChunk._buffer;

            if (_length == 0)
            {
                // If the first write into an empty buffer needs more space than the buffer provides, swap the buffers.
                newChunk._buffer = _buffer;
                _buffer = newBuffer;
            }
            else
            {
                // Otherwise append the new buffer.
                var last = _nextOrPrevious;
                var first = FirstChunk;

                if (last == this)
                {
                    // single chunk in the chain
                    _nextOrPrevious = newChunk;
                }
                else
                {
                    newChunk._nextOrPrevious = first;
                    last._nextOrPrevious = newChunk;
                    _nextOrPrevious = newChunk;
                }

                newChunk._buffer = _buffer;
                newChunk._length = FrozenLength;
                newChunk._previousLengthOrFrozenSuffixLengthDelta = PreviousLength;

                _buffer = newBuffer;
                PreviousLength += Length;
                _length = 0;
            }

            CheckInvariants();
        }

        /// <summary>
        /// Reserves a contiguous block of bytes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public Blob ReserveBytes(int byteCount)
        {
            if (byteCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(byteCount));
            }

            int start = ReserveBytesImpl(byteCount);
            return new Blob(_buffer, start, byteCount);
        }

        private int ReserveBytesImpl(int byteCount)
        {
            Debug.Assert(byteCount >= 0);

            // If write is attempted to a frozen builder we fall back
            // to expand where an exception is thrown:
            uint result = _length;
            if (result > _buffer.Length - byteCount)
            {
                Expand(byteCount);
                result = 0;
            }

            _length = result + (uint)byteCount;
            return (int)result;
        }

        private int ReserveBytesPrimitive(int byteCount)
        {
            // If the primitive doesn't fit to the current chuck we'll allocate a new chunk that is at least MinChunkSize.
            // That chunk has to fit the primitive otherwise we might keep allocating new chunks and never end up with one that fits.
            Debug.Assert(byteCount <= MinChunkSize);
            return ReserveBytesImpl(byteCount);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(byte value, int byteCount)
        {
            if (byteCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(byteCount));
            }

            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            int bytesToCurrent = Math.Min(FreeBytes, byteCount);

            _buffer.WriteBytes(Length, value, bytesToCurrent);
            AddLength(bytesToCurrent);

            int remaining = byteCount - bytesToCurrent;
            if (remaining > 0)
            {
                Expand(remaining);

                _buffer.WriteBytes(0, value, remaining);
                AddLength(remaining);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public unsafe void WriteBytes(byte* buffer, int byteCount)
        {
            if (buffer is null)
            {
                Throw.ArgumentNull(nameof(buffer));
            }

            if (byteCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(byteCount));
            }

            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            WriteBytesUnchecked(new ReadOnlySpan<byte>(buffer, byteCount));
        }

        private void WriteBytesUnchecked(ReadOnlySpan<byte> buffer)
        {
            int bytesToCurrent = Math.Min(FreeBytes, buffer.Length);

            buffer.Slice(0, bytesToCurrent).CopyTo(_buffer.AsSpan(Length));

            AddLength(bytesToCurrent);

            ReadOnlySpan<byte> remaining = buffer.Slice(bytesToCurrent);
            if (!remaining.IsEmpty)
            {
                Expand(remaining.Length);

                remaining.CopyTo(_buffer);
                AddLength(remaining.Length);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        /// <returns>Bytes successfully written from the <paramref name="source" />.</returns>
        public int TryWriteBytes(Stream source, int byteCount)
        {
            if (source is null)
            {
                Throw.ArgumentNull(nameof(source));
            }

            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (byteCount == 0)
            {
                return 0;
            }

            int bytesRead = 0;
            int bytesToCurrent = Math.Min(FreeBytes, byteCount);

            if (bytesToCurrent > 0)
            {
                bytesRead = source.TryReadAll(_buffer, Length, bytesToCurrent);
                AddLength(bytesRead);

                if (bytesRead != bytesToCurrent)
                {
                    return bytesRead;
                }
            }

            int remaining = byteCount - bytesToCurrent;
            if (remaining > 0)
            {
                Expand(remaining);
                bytesRead = source.TryReadAll(_buffer, 0, remaining);
                AddLength(bytesRead);

                bytesRead += bytesToCurrent;
            }

            return bytesRead;
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(ImmutableArray<byte> buffer)
        {
            if (buffer.IsDefault)
            {
                Throw.ArgumentNull(nameof(buffer));
            }

            WriteBytes(buffer.AsSpan());
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the <paramref name="buffer"/>.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(ImmutableArray<byte> buffer, int start, int byteCount)
        {
            if (buffer.IsDefault)
            {
                Throw.ArgumentNull(nameof(buffer));
            }

            BlobUtilities.ValidateRange(buffer.Length, start, byteCount, nameof(byteCount));

            WriteBytes(buffer.AsSpan(start, byteCount));
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(byte[] buffer)
        {
            if (buffer is null)
            {
                Throw.ArgumentNull(nameof(buffer));
            }

            WriteBytes(buffer.AsSpan());
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the <paramref name="buffer"/>.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(byte[] buffer, int start, int byteCount)
        {
            if (buffer is null)
            {
                Throw.ArgumentNull(nameof(buffer));
            }

            BlobUtilities.ValidateRange(buffer.Length, start, byteCount, nameof(byteCount));

            WriteBytes(buffer.AsSpan(start, byteCount));
        }

        private void WriteBytes(ReadOnlySpan<byte> buffer)
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            WriteBytesUnchecked(buffer);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void PadTo(int position)
        {
            WriteBytes(0, position - Count);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void Align(int alignment)
        {
            int position = Count;
            WriteBytes(0, BitArithmetic.Align(position, alignment) - position);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBoolean(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteByte(byte value)
        {
            int start = ReserveBytesPrimitive(sizeof(byte));
            _buffer.WriteByte(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDouble(double value)
        {
            int start = ReserveBytesPrimitive(sizeof(double));
            _buffer.WriteDouble(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSingle(float value)
        {
            int start = ReserveBytesPrimitive(sizeof(float));
            _buffer.WriteSingle(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt16(short value)
        {
            WriteUInt16(unchecked((ushort)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt16(ushort value)
        {
            int start = ReserveBytesPrimitive(sizeof(ushort));
            _buffer.WriteUInt16(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt16BE(short value)
        {
            WriteUInt16BE(unchecked((ushort)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt16BE(ushort value)
        {
            int start = ReserveBytesPrimitive(sizeof(ushort));
            _buffer.WriteUInt16BE(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt32BE(int value)
        {
            WriteUInt32BE(unchecked((uint)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt32BE(uint value)
        {
            int start = ReserveBytesPrimitive(sizeof(uint));
            _buffer.WriteUInt32BE(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt32(uint value)
        {
            int start = ReserveBytesPrimitive(sizeof(uint));
            _buffer.WriteUInt32(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt64(ulong value)
        {
            int start = ReserveBytesPrimitive(sizeof(ulong));
            _buffer.WriteUInt64(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDecimal(decimal value)
        {
            int start = ReserveBytesPrimitive(BlobUtilities.SizeOfSerializedDecimal);
            _buffer.WriteDecimal(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteGuid(Guid value)
        {
            int start = ReserveBytesPrimitive(BlobUtilities.SizeOfGuid);
            _buffer.WriteGuid(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        /// <summary>
        /// Writes a reference to a heap (heap offset) or a table (row number).
        /// </summary>
        /// <param name="reference">Heap offset or table row number.</param>
        /// <param name="isSmall">True to encode the reference as 16-bit integer, false to encode as 32-bit integer.</param>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteReference(int reference, bool isSmall)
        {
            // This code is a very hot path, hence we don't check if the reference actually fits 2B.

            if (isSmall)
            {
                Debug.Assert(unchecked((ushort)reference) == reference);
                WriteUInt16((ushort)reference);
            }
            else
            {
                WriteInt32(reference);
            }
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF16(char[] value)
        {
            if (value is null)
            {
                Throw.ArgumentNull(nameof(value));
            }

            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            WriteUTF16(value.AsSpan());
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF16(string value)
        {
            if (value is null)
            {
                Throw.ArgumentNull(nameof(value));
            }

            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            WriteUTF16(value.AsSpan());
        }

        private void WriteUTF16(ReadOnlySpan<char> value)
        {
            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            if (BitConverter.IsLittleEndian)
            {
                WriteBytesUnchecked(MemoryMarshal.AsBytes(value));
            }
            else
            {
                foreach (char c in value)
                {
                    WriteUInt16(c);
                }
            }
        }

        /// <summary>
        /// Writes string in SerString format (see ECMA-335-II 23.3 Custom attributes).
        /// </summary>
        /// <remarks>
        /// The string is UTF8 encoded and prefixed by the its size in bytes.
        /// Null string is represented as a single byte 0xFF.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSerializedString(string? value)
        {
            if (value == null)
            {
                WriteByte(0xff);
                return;
            }

            WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates: true, prependSize: true);
        }

        /// <summary>
        /// Writes string in User String (#US) heap format (see ECMA-335-II 24.2.4 #US and #Blob heaps):
        /// </summary>
        /// <remarks>
        /// The string is UTF16 encoded and prefixed by the its size in bytes.
        ///
        /// This final byte holds the value 1 if and only if any UTF16 character within the string has any bit set in its top byte,
        /// or its low byte is any of the following: 0x01-0x08, 0x0E-0x1F, 0x27, 0x2D, 0x7F. Otherwise, it holds 0.
        /// The 1 signifies Unicode characters that require handling beyond that normally provided for 8-bit encoding sets.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUserString(string value)
        {
            if (value is null)
            {
                Throw.ArgumentNull(nameof(value));
            }

            WriteCompressedInteger(BlobUtilities.GetUserStringByteLength(value.Length));
            WriteUTF16(value);
            WriteByte(BlobUtilities.GetUserStringTrailingByte(value));
        }

        /// <summary>
        /// Writes UTF8 encoded string at the current position.
        /// </summary>
        /// <param name="value">Constant value.</param>
        /// <param name="allowUnpairedSurrogates">
        /// True to encode unpaired surrogates as specified, otherwise replace them with U+FFFD character.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF8(string value, bool allowUnpairedSurrogates = true)
        {
            if (value is null)
            {
                Throw.ArgumentNull(nameof(value));
            }

            WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates, prependSize: false);
        }

        internal unsafe void WriteUTF8(string str, int start, int length, bool allowUnpairedSurrogates, bool prependSize)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(start + length <= str.Length);

            if (!IsHead)
            {
                Throw.InvalidOperationBuilderAlreadyLinked();
            }

            fixed (char* strPtr = str)
            {
                char* currentPtr = strPtr + start;
                char* nextPtr;

                // the max size of compressed int is 4B:
                int byteLimit = FreeBytes - (prependSize ? sizeof(uint) : 0);

                int bytesToCurrent = BlobUtilities.GetUTF8ByteCount(currentPtr, length, byteLimit, out nextPtr);
                int charsToCurrent = (int)(nextPtr - currentPtr);
                int charsToNext = length - charsToCurrent;
                int bytesToNext = BlobUtilities.GetUTF8ByteCount(nextPtr, charsToNext);

                if (prependSize)
                {
                    WriteCompressedInteger(bytesToCurrent + bytesToNext);
                }

                _buffer.WriteUTF8(Length, currentPtr, charsToCurrent, bytesToCurrent, allowUnpairedSurrogates);
                AddLength(bytesToCurrent);

                if (bytesToNext > 0)
                {
                    Expand(bytesToNext);

                    _buffer.WriteUTF8(0, nextPtr, charsToNext, bytesToNext, allowUnpairedSurrogates);
                    AddLength(bytesToNext);
                }
            }
        }

        /// <summary>
        /// Implements compressed signed integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between -64 (0xFFFFFFC0) and 63 (0x3F), inclusive, encode as a one-byte integer:
        /// bit 7 clear, value bits 5 through 0 held in bits 6 through 1, sign bit (value bit 31) in bit 0.
        ///
        /// If the value lies between -8192 (0xFFFFE000) and 8191 (0x1FFF), inclusive, encode as a two-byte integer:
        /// 15 set, bit 14 clear, value bits 12 through 0 held in bits 13 through 1, sign bit(value bit 31) in bit 0.
        ///
        /// If the value lies between -268435456 (0xF000000) and 268435455 (0x0FFFFFFF), inclusive, encode as a four-byte integer:
        /// 31 set, 30 set, bit 29 clear, value bits 27 through 0 held in bits 28 through 1, sign bit(value bit 31) in bit 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be represented as a compressed signed integer.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteCompressedSignedInteger(int value)
        {
            BlobWriterImpl.WriteCompressedSignedInteger(this, value);
        }

        /// <summary>
        /// Implements compressed unsigned integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between 0 (0x00) and 127 (0x7F), inclusive,
        /// encode as a one-byte integer (bit 7 is clear, value held in bits 6 through 0).
        ///
        /// If the value lies between 28 (0x80) and 214 - 1 (0x3FFF), inclusive,
        /// encode as a 2-byte integer with bit 15 set, bit 14 clear (value held in bits 13 through 0).
        ///
        /// Otherwise, encode as a 4-byte integer, with bit 31 set, bit 30 set, bit 29 clear (value held in bits 28 through 0).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be represented as a compressed unsigned integer.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteCompressedInteger(int value)
        {
            BlobWriterImpl.WriteCompressedInteger(this, unchecked((uint)value));
        }

        /// <summary>
        /// Writes a constant value (see ECMA-335 Partition II section 22.9) at the current position.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not of a constant type.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteConstant(object? value)
        {
            BlobWriterImpl.WriteConstant(this, value);
        }

        internal string GetDebuggerDisplay()
        {
            return IsHead ?
                string.Join("->", GetChunks().Select(chunk => $"[{Display(chunk._buffer, chunk.Length)}]")) :
                $"<{Display(_buffer, Length)}>";
        }

        private static string Display(byte[] bytes, int length)
        {
            const int MaxDisplaySize = 64;

            return (length <= MaxDisplaySize) ?
                BitConverter.ToString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, MaxDisplaySize / 2) + "-...-" + BitConverter.ToString(bytes, length - MaxDisplaySize / 2, MaxDisplaySize / 2);
        }
    }
}
