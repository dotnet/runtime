// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <summary>Provides a stream that notifies an event when the Close method is called.</summary>
    internal sealed class ClosableStream : DelegatedStream
    {
        private readonly EventHandler? _onClose;
        private int _closed;

        internal ClosableStream(Stream stream, EventHandler? onClose) : base(stream)
        {
            _onClose = onClose;
        }

        public override bool CanRead => BaseStream.CanRead;
        public override bool CanWrite => BaseStream.CanWrite;

        public override void Close()
        {
            if (Interlocked.Increment(ref _closed) == 1)
            {
                _onClose?.Invoke(this, new EventArgs());
            }
        }

        protected override void WriteInternal(ReadOnlySpan<byte> buffer)
        {
            BaseStream.Write(buffer);
        }

        protected override ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return BaseStream.WriteAsync(buffer, cancellationToken);
        }

        protected override int ReadInternal(Span<byte> buffer)
        {
            return BaseStream.Read(buffer);
        }

        protected override ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return BaseStream.ReadAsync(buffer, cancellationToken);
        }
    }
}
