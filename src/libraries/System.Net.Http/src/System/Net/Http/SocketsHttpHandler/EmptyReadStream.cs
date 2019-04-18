// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class EmptyReadStream : HttpBaseStream
    {
        internal static EmptyReadStream Instance { get; } = new EmptyReadStream();

        private EmptyReadStream() { }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        protected override void Dispose(bool disposing) {  /* nop */ }
        public override void Close() { /* nop */ }

        public override int Read(Span<byte> buffer) => 0;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken)) :
            new ValueTask<int>(0);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArgs(this, destination, bufferSize);
            return NopAsync(cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.net_http_content_readonly_stream);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
} 
