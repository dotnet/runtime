// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        public sealed override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                Task.CompletedTask;

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

        public virtual bool NeedsDrain => false;

        public virtual Task<bool> DrainAsync(int maxDrainBytes)
        {
            Debug.Assert(false, "DrainAsync should not be called for this response stream");
            return Task.FromResult(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (NeedsDrain)
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
            HttpConnection connection = _connection;        // Will be null after drain succeeds

            try
            {
                bool drained = await DrainAsync(MaxDrainBytes).ConfigureAwait(false);

                if (NetEventSource.IsEnabled)
                {
                    connection.Trace(drained ? "Connection drain succeeded" : "Connection drain failed because MaxDrainSize was exceeded");
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.IsEnabled)
                {
                    connection.Trace($"Connection drain failed due to exception: {e}");
                }

                // Eat any exceptions and just Dispose.
            }

            base.Dispose(true);
        }
    }
}
