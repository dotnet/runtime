// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class WasiOutputStream : Stream
    {
        private OutputStream stream; // owned by this instance
        private OutgoingBody body; // owned by this instance
        internal bool isClosed;

        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override bool CanSeek => false;

        public WasiOutputStream(OutgoingBody body)
        {
            this.body = body;
            this.stream = body.Write();
        }

        ~WasiOutputStream()
        {
            Dispose(false);
        }

        public override void Close()
        {
            Console.WriteLine("WasiOutputStream.Close " + isClosed);
            if (!isClosed)
            {
                Console.WriteLine("WasiOutputStream.Close A");
                isClosed = true;
                Console.WriteLine("WasiOutputStream.Close B");
                stream.Dispose();
                Console.WriteLine("WasiOutputStream.Close C");
                OutgoingBody.Finish(body, null);
                Console.WriteLine("WasiOutputStream.Close D");
            }
            base.Close();
            Console.WriteLine("WasiOutputStream.Close E");
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine("WasiOutputStream.Dispose" + isClosed);
            if (!isClosed)
            {
                Console.WriteLine("WasiOutputStream.Dispose A");
                isClosed = true;
                stream.Dispose();
                Console.WriteLine("WasiOutputStream.Dispose B");
                body.Dispose();
                Console.WriteLine("WasiOutputStream.Dispose C");
            }
            base.Dispose(disposing);
            Console.WriteLine("WasiOutputStream.Dispose E");
        }

        public override async Task WriteAsync(
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
                    await WasiHttpInterop.RegisterWasiPollable(stream.Subscribe(), cancellationToken).ConfigureAwait(false);
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

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            // TODO: avoid copy when possible and use ArrayPool when not
            var copy = new byte[buffer.Length];
            buffer.Span.CopyTo(copy);
            return new ValueTask(WriteAsync(copy, 0, buffer.Length, cancellationToken));
        }

        #region PlatformNotSupported

        public override void Flush()
        {
            // ignore
            //
            // Note that flushing a `wasi:io/streams/output-stream` is an
            // asynchronous operation, so it's not clear how we would
            // implement it here instead of taking care of it as part of
            // `WriteAsync`.
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new PlatformNotSupportedException();
        }

        public override void SetLength(long length)
        {
            throw new PlatformNotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int length)
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
