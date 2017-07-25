// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

namespace System.Net.Http
{
    internal abstract class HttpContentReadStream : HttpContentStream
    {
        protected HttpConnection _connection;

        public HttpContentReadStream(HttpConnection connection)
        {
            _connection = connection;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override void CopyTo(Stream destination, int bufferSize) =>
            CopyToAsync(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();

        protected override void Dispose(bool disposing)
        {
            if (_connection != null)
            {
                // We haven't finished reading the body, so close the connection.
                _connection.Dispose();
                _connection = null;
            }

            base.Dispose(disposing);
        }
    }
}
