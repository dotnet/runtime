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

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(isClosed, this);
            bool flushing = false;
            while (true)
            {
                int count = (int)stream.CheckWrite();
                if (count == 0)
                {
                    await WaitAsync(stream.Subscribe(), cancellationToken).ConfigureAwait(false);
                }
                else if (!buffer.IsEmpty)
                {
                    ReadOnlyMemory<byte> chunk = buffer.Slice(0, Math.Min(count, buffer.Length));
                    // TODO: is there a more efficient option than copying here?
                    // Do we need to change the binding generator to accept
                    // e.g. `Span`s?
                    byte[] bytes = MemoryMarshal.TryGetArray(chunk, out ArraySegment<byte> segment) && segment.Offset == 0 && segment.Count == segment.Array!.Length
                        ? segment.Array
                        : chunk.ToArray();
                    stream.Write(bytes);
                    buffer = buffer.Slice(chunk.Length);
                }
                else if (!flushing)
                {
                    stream.Flush();
                    flushing = true;
                }
                else
                {
                    return;
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
