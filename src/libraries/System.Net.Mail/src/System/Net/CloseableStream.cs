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

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            BaseStream.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return BaseStream.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            return BaseStream.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return BaseStream.ReadAsync(buffer, cancellationToken);
        }
    }
}
