// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a zero-length HttpContent implementation.</summary>
    internal sealed class EmptyContent : HttpContent
    {
        protected internal override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        { }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.CompletedTask;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
            SerializeToStreamAsync(stream, context);

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken) =>
            EmptyReadStream.Instance;

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(EmptyReadStream.Instance);

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled<Stream>(cancellationToken) :
            CreateContentReadStreamAsync();

        internal override Stream? TryCreateContentReadStream() => EmptyReadStream.Instance;

        internal override bool AllowDuplex => false;
    }
}
