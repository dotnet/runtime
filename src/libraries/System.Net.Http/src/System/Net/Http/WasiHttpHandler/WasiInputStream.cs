﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WasiHttpWorld;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_1;
using WasiHttpWorld.wit.imports.wasi.io.v0_2_1;
using static WasiHttpWorld.wit.imports.wasi.http.v0_2_1.ITypes;
using static WasiHttpWorld.wit.imports.wasi.io.v0_2_1.IStreams;

namespace System.Net.Http
{
    // on top of https://github.com/WebAssembly/wasi-io/blob/main/wit/streams.wit
    internal sealed class WasiInputStream : Stream
    {
        private HttpResponseMessage response;
        private WasiRequestWrapper wrapper; // owned by this instance
        private IncomingBody body; // owned by this instance
        private InputStream stream; // owned by this instance

        private int offset;
        private byte[]? buffer;
        private bool otherSideClosed;
        internal bool isClosed;

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => false;

        public WasiInputStream(
            WasiRequestWrapper wrapper,
            IncomingBody body,
            HttpResponseMessage response
        )
        {
            this.wrapper = wrapper;
            this.body = body;
            this.stream = body.Stream();
            this.response = response;
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
                            await WasiHttpInterop
                                .RegisterWasiPollable(stream.Subscribe(), cancellationToken)
                                .ConfigureAwait(false);
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
                        if (((StreamError)e.Value).Tag == StreamError.CLOSED)
                        {
                            otherSideClosed = true;
                            await ReadTrailingHeaders(cancellationToken).ConfigureAwait(false);
                            return 0;
                        }
                        else
                        {
                            // TODO translate error ?
                            throw;
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

        private async Task ReadTrailingHeaders(CancellationToken cancellationToken)
        {
            isClosed = true;
            stream.Dispose();
            using var futureTrailers = IncomingBody.Finish(body);
            while (true)
            {
                var trailers = futureTrailers.Get();
                if (trailers is null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await WasiHttpInterop
                        .RegisterWasiPollable(futureTrailers.Subscribe(), cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    var inner = ((Result<Result<Fields?, ErrorCode>, None>)trailers!).AsOk;
                    if (inner.IsOk)
                    {
                        using var headers = inner.AsOk;
                        if (headers is not null)
                        {
                            response.StoreReceivedTrailingHeaders(
                                WasiHttpInterop.ConvertTrailingResponseHeaders(headers)
                            );
                        }

                        break;
                    }
                    else if (inner.AsErr.Tag == ErrorCode.CONNECTION_TERMINATED)
                    {
                        // TODO: As of this writing, `wasmtime-wasi-http`
                        // returns this error when no headers are present.  I
                        // *think* that's a bug, since the `wasi-http` WIT docs
                        // say it should return `none` rather than an error in
                        // that case.  If it turns out that, yes, it's a bug, we
                        // can remove this case once a fix is available.
                        break;
                    }
                    else
                    {
                        throw new HttpRequestException(
                            WasiHttpInterop.ErrorCodeToString(inner.AsErr)
                        );
                    }
                }
            }
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
