// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed partial class HttpResponseStream : Stream
    {
        private bool _closed;
        internal bool Closed => _closed;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override long Length => throw new NotSupportedException(SR.net_noseek);

        public override long Position
        {
            get => throw new NotSupportedException(SR.net_noseek);
            set => throw new NotSupportedException(SR.net_noseek);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.net_noseek);

        public override void SetLength(long value) => throw new NotSupportedException(SR.net_noseek);

        public override int Read(byte[] buffer, int offset, int size) => throw new InvalidOperationException(SR.net_writeonlystream);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override int EndRead(IAsyncResult asyncResult) => throw new InvalidOperationException(SR.net_writeonlystream);

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset);

            if (_closed)
            {
                return;
            }

            WriteCore(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset);

            return BeginWriteCore(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"asyncResult:{asyncResult}");

            ArgumentNullException.ThrowIfNull(asyncResult);

            EndWriteCore(asyncResult);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "_closed:" + _closed);
                    if (_closed)
                    {
                        return;
                    }
                    _closed = true;
                    DisposeCore();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
