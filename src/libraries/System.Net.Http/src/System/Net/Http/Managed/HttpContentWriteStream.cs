// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpContentWriteStream : HttpContentStream
    {
        protected HttpConnection _connection;

        public HttpContentWriteStream(HttpConnection connection, CancellationToken cancellationToken)
        {
            Debug.Assert(connection != null);
            _connection = connection;
            RequestCancellationToken = cancellationToken;
        }

        /// <summary>Cancellation token associated with the send operation.</summary>
        /// <remarks>
        /// Because of how this write stream is used, the CancellationToken passed into the individual
        /// stream operations will be the default non-cancelable token and can be ignored.  Instead,
        /// this token is used.
        /// </remarks>
        internal CancellationToken RequestCancellationToken { get; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public abstract Task FinishAsync();

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
