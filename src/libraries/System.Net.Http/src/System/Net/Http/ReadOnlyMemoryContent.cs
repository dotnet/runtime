// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public sealed class ReadOnlyMemoryContent : HttpContent
    {
        private readonly ReadOnlyMemory<byte> _content;

        public ReadOnlyMemoryContent(ReadOnlyMemory<byte> content) =>
            _content = content;

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            stream.Write(_content.Span);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            await stream.WriteAsync(_content).ConfigureAwait(false);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            await stream.WriteAsync(_content, cancellationToken).ConfigureAwait(false);

        protected internal override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken) =>
            new ReadOnlyMemoryStream(_content);

        protected override async Task<Stream> CreateContentReadStreamAsync() =>
            new ReadOnlyMemoryStream(_content);

        internal override Stream TryCreateContentReadStream() =>
            new ReadOnlyMemoryStream(_content);

        internal override bool AllowDuplex => false;
    }
}
