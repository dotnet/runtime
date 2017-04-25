// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed partial class HttpResponseStream : Stream
    {
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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override int EndRead(IAsyncResult asyncResult) => throw new InvalidOperationException(SR.net_writeonlystream);

        public override void Write(byte[] buffer, int offset, int size)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(this);
                NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            WriteCore(buffer, offset, size);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return BeginWriteCore(buffer, offset, size, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(this);
                NetEventSource.Info(this, $"asyncResult:{asyncResult}");
            }
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            EndWriteCore(asyncResult);
        }
    }
}
