// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed partial class WrappedStream : Stream
    {
        public override void Write(ReadOnlySpan<byte> source)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            _baseStream.Write(source);
        }
    }

    internal sealed partial class SubReadStream : Stream
    {
        public override int Read(Span<byte> destination)
        {
            // parameter validation sent to _superStream.Read
            int origCount = destination.Length;
            int count = destination.Length;

            ThrowIfDisposed();
            ThrowIfCantRead();

            if (_superStream.Position != _positionInSuperStream)
                _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
            if (_positionInSuperStream + count > _endInSuperStream)
                count = (int)(_endInSuperStream - _positionInSuperStream);

            Debug.Assert(count >= 0);
            Debug.Assert(count <= origCount);

            int ret = _superStream.Read(destination.Slice(0, count));

            _positionInSuperStream += ret;
            return ret;
        }
    }

    internal sealed partial class CheckSumAndSizeWriteStream : Stream
    {
        public override void Write(ReadOnlySpan<byte> source)
        {
            // if we're not actually writing anything, we don't want to trigger as if we did write something
            ThrowIfDisposed();
            Debug.Assert(CanWrite);

            if (source.Length == 0)
                return;

            if (!_everWritten)
            {
                _initialPosition = _baseBaseStream.Position;
                _everWritten = true;
            }

            _checksum = Crc32Helper.UpdateCrc32(_checksum, source);
            _baseStream.Write(source);
            _position += source.Length;
        }
    }
}
