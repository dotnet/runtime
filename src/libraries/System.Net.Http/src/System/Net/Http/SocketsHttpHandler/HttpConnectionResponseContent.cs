// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionResponseContent : HttpContent
    {
        private Stream? _stream;
        private bool _consumedStream; // separate from _stream so that Dispose can drain _stream

        public void SetStream(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            Debug.Assert(!_consumedStream);

            _stream = stream;
        }

        private Stream ConsumeStream()
        {
            if (_consumedStream || _stream == null)
            {
                throw new InvalidOperationException(SR.net_http_content_stream_already_read);
            }
            _consumedStream = true;

            return _stream;
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);

            using (Stream contentStream = ConsumeStream())
            {
                const int BufferSize = 8192;
                contentStream.CopyTo(stream, BufferSize);
            }
        }

        protected sealed override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected sealed override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return Impl(stream, context, cancellationToken);

            async Task Impl(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            {
                using (Stream contentStream = ConsumeStream())
                {
                    const int BufferSize = 8192;
                    await contentStream.CopyToAsync(stream, BufferSize, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        protected internal sealed override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected sealed override Stream CreateContentReadStream(CancellationToken cancellationToken) =>
            ConsumeStream();

        protected sealed override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(ConsumeStream());

        internal sealed override Stream TryCreateContentReadStream() =>
            ConsumeStream();

        internal override bool AllowDuplex => false;

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
