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
    internal sealed class WasiInputStream : Stream
    {
        private WasiRequestWrapper wrapper; // owned by this instance
        private IncomingBody body; // owned by this instance
        private InputStream stream; // owned by this instance

        private int offset;
        private byte[]? buffer;
        private bool otherSideClosed;
        internal bool isClosed;

        // Canceled before the stream is dropped, so that a pending read's pollable (a child of the stream) is released first.
        private readonly CancellationTokenSource closeCts = new CancellationTokenSource();

        public override bool CanRead => !isClosed;
        public override bool CanWrite => false;
        public override bool CanSeek => false;

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

        public override async Task<int> ReadAsync(
            byte[] bytes,
            int offset,
            int length,
            CancellationToken cancellationToken
        )
        {
            ObjectDisposedException.ThrowIf(isClosed, this);
            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                if (otherSideClosed)
                {
                    return 0;
                }
                else if (this.buffer == null)
                {
                    try
                    {
                        // TODO: should we add a special case to the bindings generator
                        // to allow passing a buffer to InputStream.Read and
                        // avoid the extra copy?
                        var result = stream.Read(16 * 1024);
                        var buffer = result;
                        if (buffer.Length == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await WaitAsync(stream.Subscribe(), cancellationToken).ConfigureAwait(false);
                            ObjectDisposedException.ThrowIf(isClosed, this);
                        }
                        else
                        {
                            this.buffer = buffer;
                            this.offset = 0;
                        }
                    }
                    catch (WitException e)
                    {
                        if (((StreamError)e.Value).Tag == StreamError.Tags.Closed)
                        {
                            otherSideClosed = true;
                            return 0;
                        }
                        else
                        {
                            throw new HttpIOException(HttpRequestError.ResponseEnded, SR.net_http_invalid_response_premature_eof, e);
                        }
                    }
                }
                else
                {
                    var min = Math.Min(this.buffer.Length - this.offset, length);
                    Array.Copy(this.buffer, this.offset, bytes, offset, min);
                    if (min < buffer.Length - this.offset)
                    {
                        this.offset += min;
                    }
                    else
                    {
                        this.buffer = null;
                    }
                    return min;
                }
            }
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

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            // TODO: avoid copy when possible and use ArrayPool when not
            var dst = new byte[buffer.Length];
            // We disable "CA1835: Prefer the memory-based overloads of
            // ReadAsync/WriteAsync methods in stream-based classes" for
            // now, since `ReadyAsync(byte[], int, int, CancellationToken)`
            // is where the implementation currently resides, but we should
            // revisit this if/when `wit-bindgen` learns to generate
            // memory-based bindings.
#pragma warning disable CA1835
            var result = await ReadAsync(dst, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);
#pragma warning restore CA1835
            new ReadOnlySpan<byte>(dst, 0, result).CopyTo(buffer.Span);
            return result;
        }

        #region PlatformNotSupported

        public override void Flush()
        {
            // ignore
        }

        public override void SetLength(long length)
        {
            throw new PlatformNotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int length)
        {
            throw new PlatformNotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new PlatformNotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int length)
        {
            throw new PlatformNotSupportedException();
        }

        public override long Length => throw new PlatformNotSupportedException();
        public override long Position
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        #endregion
    }
}
