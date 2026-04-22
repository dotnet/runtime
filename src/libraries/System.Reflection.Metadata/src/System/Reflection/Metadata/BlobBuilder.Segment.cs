// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata
{
    public partial class BlobBuilder
    {
        /// <summary>
        /// Returns whether the <see cref="BlobBuilder"/> is in a state where <see cref="Segment"/>-addressible
        /// data can be written to it.
        /// </summary>
        /// <remarks>
        /// This is true if the blob builder contains more than one chunk, the head chunk is empty, and the
        /// previous chunk has some minimum free space.
        /// </remarks>
        /// <seealso cref="EnsureCanWriteSegment"/>
        private bool CanWriteSegment => _nextOrPrevious != this && Length == 0 && _nextOrPrevious.FreeBytes > 0;

        // TODO: Move the cunking logic to the main BlobBuilder.cs file, and use it everywhere in BlobBuilder.
        // https://github.com/dotnet/runtime/issues/100418
        internal const int DefaultMaxChunkSize = 8192;

        private int NextChunkLength => Math.Max(MinChunkSize, Math.Min(Count, DefaultMaxChunkSize));

        /// <summary>
        /// Brings the <see cref="BlobBuilder"/> into a state where <see cref="Segment"/>-addressible
        /// data can be written to it.
        /// </summary>
        /// <seealso cref="CanWriteSegment"/>
        private void EnsureCanWriteSegment(int minimumSize = 1)
        {
            Debug.Assert(minimumSize > 0);
            if (!CanWriteSegment || _nextOrPrevious.FreeBytes < minimumSize)
            {
                int chunkSize = NextChunkLength;
                // We need to have some free space in the last chunk. If the head chunk is full,
                // calling Expand() will move it to the last chunk, so we need to call it twice.
                if (FreeBytes == 0)
                {
                    Expand(chunkSize, alwaysAppendBuffer: true);
                }
                Expand(chunkSize, alwaysAppendBuffer: true);
                Debug.Assert(CanWriteSegment && _nextOrPrevious.FreeBytes >= minimumSize);
            }
        }

        private void AddLengthForSegment(int length)
        {
            Debug.Assert(length > 0);
            Debug.Assert(CanWriteSegment && _nextOrPrevious.FreeBytes >= length);
            _nextOrPrevious.AddLength(length);
            PreviousLength += length;
        }

        /// <summary>
        /// Starts writing <see cref="Segment"/>-addressible data to the <see cref="BlobBuilder"/>.
        /// </summary>
        /// <param name="startChunk">The first chunk of the segment.</param>
        /// <param name="startOffset">The offset to the first byte within the first chunk of the segment.</param>
        private void StartSegment(out BlobBuilder startChunk, out int startOffset)
        {
            Debug.Assert(IsHead);
            EnsureCanWriteSegment();
            startChunk = _nextOrPrevious;
            startOffset = startChunk.Length;
        }

        /// <summary>
        /// Writes a compressed integer to the <see cref="BlobBuilder"/>. This method should be preferred
        /// over <see cref="WriteCompressedInteger"/> when the <see cref="BlobBuilder"/> is used to create
        /// <see cref="Segment"/>s.
        /// </summary>
        /// <param name="value">The integer to write.</param>
        private void WriteCompressedIntegerForSegment(int value)
        {
            EnsureCanWriteSegment(minimumSize: sizeof(int));
            Span<byte> writeBuffer = _nextOrPrevious.RemainingSpan;
            int written = BlobWriterImpl.WriteCompressedInteger(writeBuffer, unchecked((uint)value));
            AddLengthForSegment(written);
        }

        /// <summary>
        /// Writes a buffer to the <see cref="BlobBuilder"/>, ensuring that its content can be accessed from
        /// a <see cref="Segment"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="StartSegment"/> must be called before calling this method.
        /// </remarks>
        private void WriteForSegment(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(CanWriteSegment);
            int expectedCount = Count + buffer.Length;
            while (!buffer.IsEmpty)
            {
                Span<byte> writeBuffer = _nextOrPrevious.RemainingSpan;
                int bytesToWrite = Math.Min(writeBuffer.Length, buffer.Length);
                buffer.Slice(0, bytesToWrite).CopyTo(writeBuffer);
                buffer = buffer.Slice(bytesToWrite);
                AddLengthForSegment(bytesToWrite);
                EnsureCanWriteSegment();
            }
            Debug.Assert(Count == expectedCount);
        }

        /// <summary>
        /// Finishes creation of a <see cref="Segment"/>.
        /// </summary>
        private Segment FinishSegment(BlobBuilder startChunk, int startOffset, int size)
        {
            Debug.Assert(CanWriteSegment);
            CheckInvariants();
            return new Segment(startChunk, startOffset, size);
        }

        /// <summary>
        /// Writes a buffer to the <see cref="BlobBuilder"/> and returns a <see cref="Segment"/> over
        /// the written data.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="prependCompressedSize">Whether to prepend <paramref name="buffer"/>'s size as
        /// a compressed integer. The bytes of the size will not be included in the <see cref="Segment"/>.</param>
        internal Segment WriteSegment(ReadOnlySpan<byte> buffer, bool prependCompressedSize = false)
        {
            if (prependCompressedSize)
            {
                WriteCompressedIntegerForSegment(buffer.Length);
            }
            StartSegment(out BlobBuilder startChunk, out int startOffset);
            WriteForSegment(buffer);
            return FinishSegment(startChunk, startOffset, buffer.Length);
        }

        /// <summary>
        /// Writes the contents of a <see cref="BlobBuilder"/> to the <see cref="BlobBuilder"/> and
        /// returns a <see cref="Segment"/> over the written data.
        /// </summary>
        /// <param name="builder">The <see cref="BlobBuilder"/> to write.</param>
        /// <param name="prependCompressedSize">Whether to prepend <paramref name="builder"/>'s size as
        /// a compressed integer. The bytes of the size will not be included in the <see cref="Segment"/>.</param>
        internal Segment WriteSegment(BlobBuilder builder, bool prependCompressedSize = false)
        {
            if (prependCompressedSize)
            {
                WriteCompressedIntegerForSegment(builder.Count);
            }
            StartSegment(out BlobBuilder startChunk, out int startOffset);
            foreach (BlobBuilder chunk in builder.GetChunks())
            {
                WriteForSegment(chunk.Span);
            }
            return FinishSegment(startChunk, startOffset, builder.Count);
        }

        /// <summary>
        /// Represents a contiguous region of data in a <see cref="BlobBuilder"/>.
        /// </summary>
        /// <seealso cref="WriteSegment(ReadOnlySpan{byte}, bool)"/>
        /// <seealso cref="WriteSegment(BlobBuilder, bool)"/>
        [DebuggerDisplay("Count = {Count}")]
        internal readonly struct Segment(BlobBuilder builder, int offset, int size)
        {
            private readonly BlobBuilder _builder = builder;
            private readonly int _offset = offset;

            public int Count { get; } = size;

            public Chunks GetChunks() => new Chunks(this);

            public bool ContentEquals(ReadOnlySpan<byte> other)
            {
                if (Count != other.Length)
                {
                    return false;
                }

                foreach (ReadOnlySpan<byte> chunk in GetChunks())
                {
                    if (!chunk.SequenceEqual(other.Slice(0, chunk.Length)))
                    {
                        return false;
                    }
                    other = other.Slice(chunk.Length);
                }

                return true;
            }

            public bool ContentEquals(BlobBuilder other)
            {
                if (Count != other.Count)
                {
                    return false;
                }

                Chunks leftEnumerator = GetChunks();
                BlobBuilder.Chunks rightEnumerator = other.GetChunks();
                int leftStart = 0;
                int rightStart = 0;

                bool leftContinues = leftEnumerator.MoveNext();
                bool rightContinues = rightEnumerator.MoveNext();

                while (leftContinues && rightContinues)
                {
                    Debug.Assert(leftStart == 0 || rightStart == 0);

                    ReadOnlySpan<byte> left = leftEnumerator.Current;
                    ReadOnlySpan<byte> right = rightEnumerator.Current.Span;

                    int minLength = Math.Min(left.Length - leftStart, right.Length - rightStart);
                    if (!left.Slice(leftStart, minLength).SequenceEqual(right.Slice(rightStart, minLength)))
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

#if NET
            public bool ContentEquals(Segment other)
            {
                if (Count != other.Count)
                {
                    return false;
                }

                Chunks leftEnumerator = GetChunks();
                Chunks rightEnumerator = other.GetChunks();
                int leftStart = 0;
                int rightStart = 0;

                bool leftContinues = leftEnumerator.MoveNext();
                bool rightContinues = rightEnumerator.MoveNext();

                while (leftContinues && rightContinues)
                {
                    Debug.Assert(leftStart == 0 || rightStart == 0);

                    ReadOnlySpan<byte> left = leftEnumerator.Current;
                    ReadOnlySpan<byte> right = rightEnumerator.Current;

                    int minLength = Math.Min(left.Length - leftStart, right.Length - rightStart);
                    if (!left.Slice(leftStart, minLength).SequenceEqual(right.Slice(rightStart, minLength)))
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
#endif

            public int GetContentFNVHashCode()
            {
                int hash = Hash.FnvOffsetBias;
                foreach (ReadOnlySpan<byte> chunk in GetChunks())
                {
                    hash = Hash.AccumulateFNVHashCode(hash, chunk);
                }
                return hash;
            }

            public struct Chunks
            {
                private BlobBuilder _current;
                private int _chunkOffset, _chunkLength;
                private int _remaining;
                private int _steps;

                internal Chunks(Segment segment)
                {
                    _current = segment._builder;
                    _chunkOffset = segment._offset;
                    _chunkLength = Math.Min(segment.Count, _current.Length - _chunkOffset);
                    _remaining = segment.Count - _chunkLength;
                }

                public readonly ReadOnlySpan<byte> Current => _current.Span.Slice(_chunkOffset, _chunkLength);

                public bool MoveNext()
                {
                    if (_remaining == 0 && _steps != 0)
                    {
                        // If the segment was empty, return an empty chunk, to match BlobBuilder.GetChunks().
                        return false;
                    }
                    switch (_steps++)
                    {
                        case 0:
                            // The enumerator is already initialized to the first chunk, so do nothing the first time MoveNext is called.
                            return true;
                        case 1:
                            // Chunks after the first one start at their beginning.
                            _chunkOffset = 0;
                            break;
                    }
                    _current = _current._nextOrPrevious;
                    _chunkLength = Math.Min(_remaining, _current.Length);
                    _remaining -= _chunkLength;
                    return true;
                }

                public readonly Chunks GetEnumerator() => this;
            }
        }
    }
}
