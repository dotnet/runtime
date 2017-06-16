// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal class DecompressionHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly DecompressionMethods _decompressionMethods;

        private const string s_gzip = "gzip";
        private const string s_deflate = "deflate";
        private static StringWithQualityHeaderValue s_gzipHeaderValue = new StringWithQualityHeaderValue(s_gzip);
        private static StringWithQualityHeaderValue s_deflateHeaderValue = new StringWithQualityHeaderValue(s_deflate);

        public DecompressionHandler(DecompressionMethods decompressionMethods, HttpMessageHandler innerHandler)
        {
            if (innerHandler == null)
            {
                throw new ArgumentNullException(nameof(innerHandler));
            }

            if (decompressionMethods == DecompressionMethods.None)
            {
                throw new ArgumentOutOfRangeException(nameof(decompressionMethods));
            }

            _innerHandler = innerHandler;
            _decompressionMethods = decompressionMethods;
        }

        internal bool GZipEnabled => (_decompressionMethods & DecompressionMethods.GZip) != 0;
        internal bool DeflateEnabled => (_decompressionMethods & DecompressionMethods.Deflate) != 0;

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (GZipEnabled)
            {
                request.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
            }
            if (DeflateEnabled)
            {
                request.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken);

            while (true)
            {
                // Get first encoding
                using (IEnumerator<string> e = response.Content.Headers.ContentEncoding.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        break;
                    }

                    string encoding = e.Current;
                    if (GZipEnabled && encoding == s_gzip)
                    {
                        response.Content = new GZipDecompressedContent(response.Content);
                    }
                    else if (DeflateEnabled && encoding == s_deflate)
                    {
                        response.Content = new DeflateDecompressedContent(response.Content);
                    }
                    else
                    {
                        // Unknown content encoding.  Stop processing.
                        break;
                    }
                }
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        private abstract class DecompressedContent : HttpContent
        {
            HttpContent _originalContent;
            bool _contentConsumed;

            public DecompressedContent(HttpContent originalContent)
            {
                _originalContent = originalContent;
                _contentConsumed = false;

                // Copy original response headers, but with the following changes:
                //   Content-Length is removed, since it no longer applies to the decompressed content
                //   The first Content-Encoding is removed, since we are processing that here
                Headers.AddHeaders(originalContent.Headers);
                Headers.ContentLength = null;
                Headers.ContentEncoding.Clear();
                bool first = true;
                foreach (var encoding in originalContent.Headers.ContentEncoding)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        Headers.ContentEncoding.Add(encoding);
                    }
                }
            }

            protected abstract Stream GetDecompressedStream(Stream originalStream);

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Stream decompressedStream = await CreateContentReadStreamAsync();
                await decompressedStream.CopyToAsync(stream);
                decompressedStream.Dispose();
            }

            protected override async Task<Stream> CreateContentReadStreamAsync()
            {
                if (_contentConsumed)
                {
                    throw new InvalidOperationException("content already consumed");
                }

                _contentConsumed = true;

                Stream originalStream = await _originalContent.ReadAsStreamAsync();
                return GetDecompressedStream(originalStream);
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
                    _originalContent.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class GZipDecompressedContent : DecompressedContent
        {
            public GZipDecompressedContent(HttpContent originalContent)
                : base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream)
            {
                return new GZipStream(originalStream, CompressionMode.Decompress);
            }
        }

        private sealed class DeflateDecompressedContent : DecompressedContent
        {
            public DeflateDecompressedContent(HttpContent originalContent)
                : base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream)
            {
                return new DeflateStream(originalStream, CompressionMode.Decompress);
            }
        }
    }
}
