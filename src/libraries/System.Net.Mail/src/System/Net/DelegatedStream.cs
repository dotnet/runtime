// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace System.Net
{
    internal abstract class DelegatedStream : Stream
    {
        private readonly Stream _stream;

        protected DelegatedStream(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _stream = stream;
        }

        protected Stream BaseStream => _stream;

        public override bool CanSeek => _stream.CanSeek;

        public abstract override bool CanRead { get; }

        public abstract override bool CanWrite { get; }

        public override long Length
        {
            get
            {
                if (!CanSeek)
                    throw new NotSupportedException(SR.SeekNotSupported);

                return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek)
                    throw new NotSupportedException(SR.SeekNotSupported);

                return _stream.Position;
            }
            set
            {
                if (!CanSeek)
                    throw new NotSupportedException(SR.SeekNotSupported);

                _stream.Position = value;
            }
        }

        public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
        }
        public sealed override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<int>(asyncResult);
        }

        public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
        }

        public sealed override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        public override void Close()
        {
            _stream.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        // Abstract methods for derived classes to implement core logic
        protected abstract int ReadInternal(Span<byte> buffer);

        protected abstract ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken);

        protected abstract void WriteInternal(ReadOnlySpan<byte> buffer);

        protected abstract ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        // Sealed methods implementing the Stream Read/Write methods
        public sealed override int Read(Span<byte> buffer)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return ReadInternal(buffer);
        }

        public sealed override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return ReadAsyncInternal(buffer, cancellationToken);
        }

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return ReadInternal(buffer.AsSpan(offset, count));
        }

        public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public sealed override int ReadByte()
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            byte b = 0;
            return ReadInternal(new Span<byte>(ref b)) != 0 ? b : -1;
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            return _stream.Seek(offset, origin);
        }

        public sealed override void SetLength(long value)
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            _stream.SetLength(value);
        }

        public sealed override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            WriteInternal(buffer);
        }

        public sealed override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            return WriteAsyncInternal(buffer, cancellationToken);
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            WriteInternal(buffer.AsSpan(offset, count));
        }

        public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            return WriteAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }
    }
}
