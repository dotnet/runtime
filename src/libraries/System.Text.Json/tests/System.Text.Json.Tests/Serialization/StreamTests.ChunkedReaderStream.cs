// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Text.Json.Serialization.Tests
{
    public partial class StreamTests
    {
        /// <summary>
        /// Defines a readable Stream implementation that lazily returns chunks of data via an enumerator.
        /// Lets us define huge JSON documents without having to materialize them in memory.
        /// </summary>
        public class ChunkedReaderStream : Stream
        {
            private readonly IEnumerator<byte[]> _chunkEnumerator;
            private byte[]? _currentChunk; // Null means EOF.
            private int _currentChunkOffset;
            private long _position;

            public ChunkedReaderStream(IEnumerable<byte[]> chunkProvider)
            {
                _chunkEnumerator = chunkProvider.GetEnumerator();
                MoveToNextChunk();
            }

            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => false;
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer is null || offset < 0 || count < 0 || buffer.Length - offset < count)
                {
                    ThrowArgumentException();
                    static void ThrowArgumentException() => throw new ArgumentException();
                }

                int bytesRead = 0;
                while (count > 0)
                {
                    byte[]? currentChunk = _currentChunk;
                    int currentChunkOffset = _currentChunkOffset;

                    if (currentChunk is null)
                    {
                        break; // EOF.
                    }

                    Debug.Assert(currentChunk.Length - currentChunkOffset > 0);
                    int bytesToCopy = Math.Min(count, currentChunk.Length - currentChunkOffset);
                    Buffer.BlockCopy(currentChunk, currentChunkOffset, buffer, offset, bytesToCopy);

                    if (bytesToCopy < count)
                    {
                        MoveToNextChunk();
                    }
                    else
                    {
                        _currentChunkOffset = currentChunkOffset + bytesToCopy;
                    }

                    bytesRead += bytesToCopy;
                    offset += bytesToCopy;
                    count -= bytesToCopy;
                }

                _position += bytesRead;
                return bytesRead;
            }

            private void MoveToNextChunk()
            {
                IEnumerator<byte[]> chunkEnumerator = _chunkEnumerator;
                while (chunkEnumerator.MoveNext())
                {
                    byte[]? chunk = chunkEnumerator.Current;
                    if (chunk is null || chunk.Length == 0)
                    {
                        // Skip null or empty chunks.
                        continue;
                    }

                    _currentChunk = chunk;
                    _currentChunkOffset = 0;
                    return;
                }

                _currentChunk = null;
                _currentChunkOffset = 0;
            }

            protected override void Dispose(bool _) => _chunkEnumerator.Dispose();

            public override long Length => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush() => throw new NotSupportedException();
        }
    }
}
