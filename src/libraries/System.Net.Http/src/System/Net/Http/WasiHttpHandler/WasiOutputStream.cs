// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
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
    internal sealed class WasiOutputStream : HttpBaseStream
    {
        private OutputStream stream; // owned by this instance
        private OutgoingBody body; // owned by this instance
        internal bool isClosed;

        // Canceled before the stream is dropped, so that a pending write's pollable (a child of the stream) is released first.
        private readonly CancellationTokenSource closeCts = new CancellationTokenSource();

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public WasiOutputStream(OutgoingBody body)
        {
            this.body = body;
            this.stream = body.Write();
        }

        ~WasiOutputStream()
        {
            Dispose(false);
        }

        // Completes the request body. Disposing the stream without finishing it aborts the request body instead.
        public void Finish()
        {
            ObjectDisposedException.ThrowIf(isClosed, this);
            isClosed = true;
            closeCts.Cancel();
            stream.Dispose();
            OutgoingBody.Finish(body, null); // passing body ownership
            GC.SuppressFinalize(this);
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
            base.Dispose(disposing);
        }

        private async Task WriteAsyncCore(
            byte[] bytes,
            int offset,
            int length,
            CancellationToken cancellationToken
        )
        {
            ObjectDisposedException.ThrowIf(isClosed, this);
            var limit = offset + length;
            var flushing = false;
            while (true)
            {
                var count = (int)stream.CheckWrite();
                if (count == 0)
                {
                    await WaitAsync(stream.Subscribe(), cancellationToken).ConfigureAwait(false);
                    ObjectDisposedException.ThrowIf(isClosed, this);
                }
                else if (offset == limit)
                {
                    if (flushing)
                    {
                        return;
                    }
                    else
                    {
                        stream.Flush();
                        flushing = true;
                    }
                }
                else
                {
                    var min = Math.Min(count, limit - offset);
                    if (offset == 0 && min == bytes.Length)
                    {
                        stream.Write(bytes);
                    }
                    else
                    {
                        // TODO: is there a more efficient option than copying here?
                        // Do we need to change the binding generator to accept
                        // e.g. `Span`s?
                        var copy = new byte[min];
                        Array.Copy(bytes, offset, copy, 0, min);
                        stream.Write(copy);
                    }
                    offset += min;
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

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                segment = buffer.ToArray();
            }

            return new ValueTask(WriteAsyncCore(segment.Array!, segment.Offset, segment.Count, cancellationToken));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            // A synchronous write would block, which isn't possible on single-threaded WASI.
            throw new PlatformNotSupportedException();
        }

        public override int Read(Span<byte> buffer) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            ValueTask.FromException<int>(new NotSupportedException());
    }
}
