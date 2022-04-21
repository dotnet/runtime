// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Stream that allows wrapping a super stream and specify the lower and upper limits that can be read from it.
    // It is meant to be used when the super stream is seekable.
    // Does not support writing.
    internal sealed class SeekableSubReadStream : SubReadStream
    {
        public SeekableSubReadStream(Stream superStream, long startPosition, long maxLength)
            : base(superStream, startPosition, maxLength)
        {
            if (!superStream.CanSeek)
            {
                throw new InvalidOperationException(SR.IO_NotSupported_UnseekableStream);
            }
        }

        public override bool CanSeek => !_isDisposed;

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _positionInSuperStream - _startInSuperStream;
            }
            set
            {
                ThrowIfDisposed();
                if (value < 0 || value >= _endInSuperStream)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _positionInSuperStream = _startInSuperStream + value;
            }
        }

        public override int Read(Span<byte> destination)
        {
            ThrowIfDisposed();
            VerifyPositionInSuperStream();

            // parameter validation sent to _superStream.Read
            int origCount = destination.Length;
            int count = destination.Length;

            if (_positionInSuperStream + count > _endInSuperStream)
            {
                count = (int)(_endInSuperStream - _positionInSuperStream);
            }

            Debug.Assert(count >= 0);
            Debug.Assert(count <= origCount);

            if (count > 0)
            {
                int bytesRead = _superStream.Read(destination.Slice(0, count));
                _positionInSuperStream += bytesRead;
                return bytesRead;
            }

            return 0;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            VerifyPositionInSuperStream();
            return ReadAsyncCore(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();

            long newPosition = origin switch
            {
                SeekOrigin.Begin => _startInSuperStream + offset,
                SeekOrigin.Current => _positionInSuperStream + offset,
                SeekOrigin.End => _endInSuperStream + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
            if (newPosition < _startInSuperStream || newPosition > _endInSuperStream)
            {
                throw new IndexOutOfRangeException(nameof(offset));
            }

            _superStream.Position = newPosition;
            _positionInSuperStream = newPosition;

            return _superStream.Position;
        }

        private void VerifyPositionInSuperStream()
        {
            if (_positionInSuperStream != _superStream.Position)
            {
                // Since we can seek, if the stream had its position pointer moved externally,
                // we must bring it back to the last read location on this stream
                _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
            }
        }
    }
}
