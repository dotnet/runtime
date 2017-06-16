// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal abstract class HttpContentWriteStream : Stream
    {
        protected HttpConnection _connection;

        public HttpContentWriteStream(HttpConnection connection)
        {
            Debug.Assert(connection != null);
            _connection = connection;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            FlushAsync().Wait();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CancellationToken.None).Wait();
        }

        public abstract override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        public abstract override Task FlushAsync(CancellationToken cancellationToken);
        public abstract Task FinishAsync(CancellationToken cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}