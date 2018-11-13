// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public class GZipStream : Stream
    {
        private DeflateStream _deflateStream;

        public GZipStream(Stream stream, CompressionMode mode): this(stream, mode, leaveOpen: false)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
             _deflateStream = new DeflateStream(stream, mode, leaveOpen, ZLibNative.GZip_DefaultWindowBits);
        }

        // Implies mode = Compress
        public GZipStream(Stream stream, CompressionLevel compressionLevel): this(stream, compressionLevel, leaveOpen: false)
        {
        }

        // Implies mode = Compress
        public GZipStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            _deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen, ZLibNative.GZip_DefaultWindowBits);
        }

        public override bool CanRead => _deflateStream?.CanRead ?? false;

        public override bool CanWrite => _deflateStream?.CanWrite ?? false;

        public override bool CanSeek => _deflateStream?.CanSeek ?? false;

        public override long Length
        {
            get { throw new NotSupportedException(SR.NotSupported); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.NotSupported); }
            set { throw new NotSupportedException(SR.NotSupported); }
        }

        public override void Flush()
        {
            CheckDeflateStream();
            _deflateStream.Flush();
            return;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override int ReadByte()
        {
            CheckDeflateStream();
            return _deflateStream.ReadByte();
        }

        public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(ReadAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override int Read(byte[] array, int offset, int count)
        {
            CheckDeflateStream();
            return _deflateStream.Read(array, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.ReadCore(buffer);
            }
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(WriteAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override void Write(byte[] array, int offset, int count)
        {
            CheckDeflateStream();
            _deflateStream.Write(array, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(ReadOnlySpan<byte>) overload being introduced.  In that case, this Write(ReadOnlySpan<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
            }
            else
            {
                CheckDeflateStream();
                _deflateStream.WriteCore(buffer);
            }
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            CheckDeflateStream();
            _deflateStream.CopyTo(destination, bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _deflateStream != null)
                {
                    _deflateStream.Dispose();
                }
                _deflateStream = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (GetType() != typeof(GZipStream))
            {
                return base.DisposeAsync();
            }

            DeflateStream ds = _deflateStream;
            if (ds != null)
            {
                _deflateStream = null;
                return ds.DisposeAsync();
            }

            return default;
        }

        public Stream BaseStream => _deflateStream?.BaseStream;

        public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.ReadAsync(array, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden ReadAsync(byte[], int, int) prior
                // to this ReadAsync(Memory<byte>) overload being introduced.  In that case, this ReadAsync(Memory<byte>) overload
                // should use the behavior of ReadAsync(byte[],int,int) overload.
                return base.ReadAsync(buffer, cancellationToken);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.ReadAsyncMemory(buffer, cancellationToken);
            }
        }

        public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.WriteAsync(array, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden WriteAsync(byte[], int, int) prior
                // to this WriteAsync(ReadOnlyMemory<byte>) overload being introduced.  In that case, this
                // WriteAsync(ReadOnlyMemory<byte>) overload should use the behavior of Write(byte[],int,int) overload.
                return base.WriteAsync(buffer, cancellationToken);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.WriteAsyncMemory(buffer, cancellationToken);
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.FlushAsync(cancellationToken);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        private void CheckDeflateStream()
        {
            if (_deflateStream == null)
            {
                ThrowStreamClosedException();
            }
        }

        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }
    }
}
