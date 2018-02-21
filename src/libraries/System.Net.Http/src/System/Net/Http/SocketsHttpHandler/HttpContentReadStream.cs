// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpContentReadStream : HttpContentStream
    {
        public HttpContentReadStream(HttpConnection connection) : base(connection)
        {
        }

        public sealed override bool CanRead => true;
        public sealed override bool CanWrite => false;

        public sealed override void Flush() { }
        public sealed override void WriteByte(byte value) => throw new NotSupportedException();
        public sealed override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public sealed override void Write(ReadOnlySpan<byte> source) => throw new NotSupportedException();
        public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
        public sealed override Task WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), CancellationToken.None).GetAwaiter().GetResult();
        }

        public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public sealed override void CopyTo(Stream destination, int bufferSize) =>
            CopyToAsync(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();

        public virtual Task DrainAsync(int maxDrainBytes) => Task.CompletedTask;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connection != null)
                {
                    // Start the asynchronous drain.
                    // It may complete synchronously, in which case the connection will be put back in the pool synchronously.
                    // Skip the call to base.Dispose -- it will be deferred until DrainOnDispose finishes.
                    DrainOnDispose();
                    return;
                }
            }

            base.Dispose(disposing);
        }

        // Maximum request drain size, 1MB.
        private const int MaxDrainBytes = 1024 * 1024;

        private async void DrainOnDispose()
        {
            try
            {
                await DrainAsync(MaxDrainBytes).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Eat any exceptions and just Dispose.
            }

            base.Dispose(true);
        }
    }
}
