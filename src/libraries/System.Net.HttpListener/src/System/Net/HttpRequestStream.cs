// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal partial class HttpRequestStream : Stream
    {
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset);

            if (count == 0 || _closed)
            {
                return 0;
            }

            return ReadCore(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset);

            return BeginReadCore(buffer, offset, count, callback, state)!;
        }

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

        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException(SR.net_readonlystream);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            throw new InvalidOperationException(SR.net_readonlystream);
        }

        public override void EndWrite(IAsyncResult asyncResult) => throw new InvalidOperationException(SR.net_readonlystream);

        internal bool Closed => _closed;

        protected override void Dispose(bool disposing)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "_closed:" + _closed);

            _closed = true;
            base.Dispose(disposing);
        }
    }
}
