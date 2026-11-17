// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WasiHttpWorld;
using WasiHttpWorld.wit.Imports.wasi.http.v0_2_8;
using WasiHttpWorld.wit.Imports.wasi.io.v0_2_8;
using static WasiHttpWorld.wit.Imports.wasi.http.v0_2_8.ITypesImports;
using static WasiHttpWorld.wit.Imports.wasi.io.v0_2_8.IStreamsImports;

namespace System.Net.Http
{
    // on top of https://github.com/WebAssembly/wasi-io/blob/main/wit/streams.wit
    internal sealed class WasiInputStream : HttpBaseStream
    {
        private WasiRequestWrapper wrapper; // owned by this instance
        private IncomingBody body; // owned by this instance
        private InputStream stream; // owned by this instance

        private byte[]? pending; // data read from the stream but not yet returned to the caller
        private int pendingOffset;
        private bool otherSideClosed;
        internal bool isClosed;

        // Canceled before the stream is dropped, so that a pending read's pollable (a child of the stream) is released first.
        private readonly CancellationTokenSource closeCts = new CancellationTokenSource();

        public override bool CanRead => !isClosed;
        public override bool CanWrite => false;

        public WasiInputStream(WasiRequestWrapper wrapper, IncomingBody body)
        {
            this.wrapper = wrapper;
            this.body = body;
            this.stream = body.Stream();
        }

        ~WasiInputStream()
        {
            Dispose(false);
        }

        public override void Close()
        {
            if (!isClosed)
            {
                isClosed = true;
                closeCts.Cancel();
                stream.Dispose();
                var futureTrailers = IncomingBody.Finish(body); // we just passed body ownership to Finish
                futureTrailers.Dispose();
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (!isClosed)
            {
                isClosed = true;
                closeCts.Cancel();
                stream.Dispose();
                body.Dispose();
            }

            if (disposing)
            {
                // this helps with disposing WIT resources at the Close() time of this stream, instead of waiting for the GC
                wrapper.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(isClosed, this);
            cancellationToken.ThrowIfCancellationRequested();
            while (pending is null)
            {
                if (otherSideClosed)
                {
                    return 0;
                }

                try
                {
                    // TODO: should we add a special case to the bindings generator
                    // to allow passing a buffer to InputStream.Read and
                    // avoid the extra copy?
                    byte[] result = stream.Read(16 * 1024);
                    if (result.Length == 0)
                    {
                        await WaitAsync(stream.Subscribe(), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        pending = result;
                        pendingOffset = 0;
                    }
                }
                catch (WitException e)
                {
                    if (((StreamError)e.Value).Tag == StreamError.Tags.Closed)
                    {
                        otherSideClosed = true;
                        return 0;
                    }

                    throw new HttpIOException(HttpRequestError.ResponseEnded, SR.net_http_invalid_response_premature_eof, e);
                }
            }

            ReadOnlySpan<byte> available = pending.AsSpan(pendingOffset);
            int count = Math.Min(available.Length, buffer.Length);
            available.Slice(0, count).CopyTo(buffer.Span);
            pendingOffset += count;
            if (pendingOffset == pending.Length)
            {
                pending = null;
            }

            return count;
        }

        private async Task WaitAsync(IPollImports.Pollable pollable, CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, closeCts.Token);
            try
            {
                await WasiHttpInterop.RegisterWasiPollable(pollable, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                ObjectDisposedException.ThrowIf(isClosed, this);
                throw;
            }
            ObjectDisposedException.ThrowIf(isClosed, this);
        }

        // WASI HTTP streams currently support only non-blocking (asynchronous) I/O. TODO https://github.com/dotnet/runtime/issues/137564
        public override int Read(Span<byte> buffer) => throw new PlatformNotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            ValueTask.FromException(new NotSupportedException());
    }
}
