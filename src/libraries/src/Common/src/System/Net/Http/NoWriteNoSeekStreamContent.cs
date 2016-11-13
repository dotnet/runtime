// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides an HttpContent for a Stream that is inherently read-only without support for writing or seeking.</summary>
    /// <remarks>Same as StreamContent, but specialized for no-write, no-seek, and without being constrained by its public API.</remarks>
    internal sealed class NoWriteNoSeekStreamContent : HttpContent
    {
        private readonly Stream _content;
        private readonly CancellationToken _cancellationToken;
        private bool _contentConsumed;

        internal NoWriteNoSeekStreamContent(Stream content, CancellationToken cancellationToken)
        {
            Debug.Assert(content != null);
            Debug.Assert(content.CanRead);
            Debug.Assert(!content.CanWrite);
            Debug.Assert(!content.CanSeek);

            _content = content;
            _cancellationToken = cancellationToken;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Debug.Assert(stream != null);

            if (_contentConsumed)
            {
                throw new InvalidOperationException(SR.net_http_content_stream_already_read);
            }
            _contentConsumed = true;

            // If the stream can't be re-read, make sure that it gets disposed once it is consumed.
            const int BufferSize = 8192;
            return StreamToStreamCopy.CopyAsync(_content, stream, BufferSize, disposeSource: true, cancellationToken: _cancellationToken);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(_content);
    }
}
