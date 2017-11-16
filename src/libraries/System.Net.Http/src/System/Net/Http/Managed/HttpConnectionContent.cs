// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class HttpConnectionContent : HttpContent
        {
            private readonly CancellationToken _cancellationToken;
            private HttpContentStream _stream;

            public HttpConnectionContent(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public void SetStream(HttpContentStream stream)
            {
                Debug.Assert(stream != null);
                Debug.Assert(stream.CanRead);

                _stream = stream;
            }

            private HttpContentStream ConsumeStream()
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException(SR.net_http_content_stream_already_read);
                }

                HttpContentStream stream = _stream;
                _stream = null;
                return stream;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Debug.Assert(stream != null);

                using (HttpContentStream contentStream = ConsumeStream())
                {
                    const int BufferSize = 8192;
                    await contentStream.CopyToAsync(stream, BufferSize, _cancellationToken).ConfigureAwait(false);
                }
            }

            protected internal override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            protected override Task<Stream> CreateContentReadStreamAsync() =>
                Task.FromResult<Stream>(ConsumeStream());

            internal override Stream TryCreateContentReadStream() =>
                ConsumeStream();

            protected override void Dispose(bool disposing)
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
}
