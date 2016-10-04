// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.WindowsRuntime.Internal;
using System.Threading;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// A BufferedStream wrapper which expose BufferedStream's UnderlyingStream and BufferSize
    /// </summary>
    internal sealed class BufferedStreamWrapper : Stream
    {
        private BufferedStream _bufferedStream;
        private Stream _stream;                               // Underlying stream.  Close sets _stream to null.
        private readonly Int32 _bufferSize;                   // Length of internal buffer (not counting the shadow buffer).

        public BufferedStreamWrapper(Stream stream, Int32 bufferSize)
        {
            _bufferedStream = new BufferedStream(stream, bufferSize);
            _stream = stream;
            _bufferSize = bufferSize;
        }

        private void EnsureNotClosed()
        {
            if (_bufferedStream == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        internal BufferedStream bufferedStream
        {
            [Pure]
            get
            { return _bufferedStream; }
        }

        internal Stream UnderlyingStream
        {
            [Pure]
            get
            { return _stream; }
        }

        internal Int32 BufferSize
        {
            [Pure]
            get
            { return _bufferSize; }
        }

        public override bool CanRead
        {
            [Pure]
            get
            { return _bufferedStream != null && _bufferedStream.CanRead; }
        }


        public override bool CanWrite
        {
            [Pure]
            get
            { return _bufferedStream != null && _bufferedStream.CanWrite; }
        }


        public override bool CanSeek
        {
            [Pure]
            get
            { return _bufferedStream != null && _bufferedStream.CanSeek; }
        }

        public override Int64 Length
        {
            get
            {
                EnsureNotClosed();
                return _bufferedStream.Length;
            }
        }

        public override Int64 Position
        {
            get
            {
                EnsureNotClosed();
                return _bufferedStream.Position;
            }
            set
            {
                EnsureNotClosed();
                _bufferedStream.Position = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _bufferedStream != null)
                {
                    _bufferedStream.Dispose();
                }
            }
            finally
            {
                _bufferedStream = null;
                _stream = null;
            }
        }

        public override void Flush()
        {
            EnsureNotClosed();
            _bufferedStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) 
        {
             EnsureNotClosed();
             return _bufferedStream.FlushAsync();
        }

        public override IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            EnsureNotClosed();
            return _bufferedStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override Int32 EndRead(IAsyncResult asyncResult)
        {
            EnsureNotClosed();
            return _bufferedStream.EndRead(asyncResult);
        }

        public override int Read(byte[] array, int offset, int count)
        {
            EnsureNotClosed();
            return _bufferedStream.Read(array, offset, count);
        }

        public override Task<int> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            EnsureNotClosed();
            return _bufferedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Int32 ReadByte()
        {
            EnsureNotClosed();
            return _bufferedStream.ReadByte();
        }

        public override void Write(Byte[] array, Int32 offset, Int32 count)
        {
            EnsureNotClosed();
            _bufferedStream.Write(array, offset, count);
        }

        public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            EnsureNotClosed();
            return _bufferedStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
           EnsureNotClosed();
            _bufferedStream.EndWrite(asyncResult);
        }

        public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            EnsureNotClosed();
            return _bufferedStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(Byte value)
        {
            EnsureNotClosed();
            _bufferedStream.WriteByte(value);
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            EnsureNotClosed();
            return _bufferedStream.Seek(offset, origin);
        }

        public override void SetLength(Int64 value)
        {
            EnsureNotClosed();
            _bufferedStream.SetLength(value);
        }
    }  // class BufferedStreamWrapper
}  // namespace
