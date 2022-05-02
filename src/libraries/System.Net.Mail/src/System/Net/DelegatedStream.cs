// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        protected Stream BaseStream
        {
            get
            {
                return _stream;
            }
        }

        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _stream.CanWrite;
            }
        }

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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        //This calls close on the inner stream
        //however, the stream may not be actually closed, but simpy flushed
        public override void Close()
        {
            _stream.Close();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return _stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            _stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return _stream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            return _stream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            _stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            return _stream.WriteAsync(buffer, cancellationToken);
        }
    }
}
