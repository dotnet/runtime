// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
** Purpose: Create a Memorystream over an UnmanagedMemoryStream
**
===========================================================*/

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // Needed for backwards compatibility with V1.x usages of the
    // ResourceManager, where a MemoryStream is now returned as an
    // UnmanagedMemoryStream from ResourceReader.
    internal sealed class UnmanagedMemoryStreamWrapper : MemoryStream
    {
        private readonly UnmanagedMemoryStream _unmanagedStream;

        internal UnmanagedMemoryStreamWrapper(UnmanagedMemoryStream stream)
        {
            _unmanagedStream = stream;
        }

        public override bool CanRead => _unmanagedStream.CanRead;

        public override bool CanSeek => _unmanagedStream.CanSeek;

        public override bool CanWrite => _unmanagedStream.CanWrite;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                    _unmanagedStream.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            _unmanagedStream.Flush();
        }

        public override byte[] GetBuffer()
        {
            throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
        }

        public override bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            buffer = default;
            return false;
        }

        public override int Capacity
        {
            get => (int)_unmanagedStream.Capacity;
            set => throw new IOException(SR.IO_FixedCapacity);
        }

        public override long Length => _unmanagedStream.Length;

        public override long Position
        {
            get => _unmanagedStream.Position;
            set => _unmanagedStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _unmanagedStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            return _unmanagedStream.Read(buffer);
        }

        public override int ReadByte()
        {
            return _unmanagedStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            return _unmanagedStream.Seek(offset, loc);
        }

        public override unsafe byte[] ToArray()
        {
            byte[] buffer = new byte[_unmanagedStream.Length];
            _unmanagedStream.Read(buffer, 0, (int)_unmanagedStream.Length);
            return buffer;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _unmanagedStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _unmanagedStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            _unmanagedStream.WriteByte(value);
        }

        // Writes this MemoryStream to another stream.
        public override unsafe void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            byte[] buffer = ToArray();

            stream.Write(buffer, 0, buffer.Length);
        }

        public override void SetLength(long value)
        {
            // This was probably meant to call _unmanagedStream.SetLength(value), but it was forgotten in V.4.0.
            // Now this results in a call to the base which touches the underlying array which is never actually used.
            // We cannot fix it due to compat now, but we should fix this at the next SxS release oportunity.
            base.SetLength(value);
        }


        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(destination);

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            if (!CanRead && !CanWrite)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);

            if (!destination.CanRead && !destination.CanWrite)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(nameof(destination));

            if (!CanRead)
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();

            if (!destination.CanWrite)
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();

            return _unmanagedStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }


        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _unmanagedStream.FlushAsync(cancellationToken);
        }


        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _unmanagedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _unmanagedStream.ReadAsync(buffer, cancellationToken);
        }


        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _unmanagedStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _unmanagedStream.WriteAsync(buffer, cancellationToken);
        }
    }  // class UnmanagedMemoryStreamWrapper
}  // namespace
