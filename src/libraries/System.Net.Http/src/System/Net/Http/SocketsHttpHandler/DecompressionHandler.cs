// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class DecompressionHandler : HttpMessageHandlerStage
    {
        private readonly HttpMessageHandlerStage _innerHandler;
        private readonly DecompressionMethods _decompressionMethods;

        private const string Gzip = "gzip";
        private const string Deflate = "deflate";
        private const string Brotli = "br";
        private static readonly StringWithQualityHeaderValue s_gzipHeaderValue = new StringWithQualityHeaderValue(Gzip);
        private static readonly StringWithQualityHeaderValue s_deflateHeaderValue = new StringWithQualityHeaderValue(Deflate);
        private static readonly StringWithQualityHeaderValue s_brotliHeaderValue = new StringWithQualityHeaderValue(Brotli);

        public DecompressionHandler(DecompressionMethods decompressionMethods, HttpMessageHandlerStage innerHandler)
        {
            Debug.Assert(decompressionMethods != DecompressionMethods.None);
            Debug.Assert(innerHandler != null);

            _decompressionMethods = decompressionMethods;
            _innerHandler = innerHandler;
        }

        internal bool GZipEnabled => (_decompressionMethods & DecompressionMethods.GZip) != 0;
        internal bool DeflateEnabled => (_decompressionMethods & DecompressionMethods.Deflate) != 0;
        internal bool BrotliEnabled => (_decompressionMethods & DecompressionMethods.Brotli) != 0;

        internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (GZipEnabled && !request.Headers.AcceptEncoding.Contains(s_gzipHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
            }

            if (DeflateEnabled && !request.Headers.AcceptEncoding.Contains(s_deflateHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
            }

            if (BrotliEnabled && !request.Headers.AcceptEncoding.Contains(s_brotliHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_brotliHeaderValue);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(false);

            Debug.Assert(response.Content != null);
            ICollection<string> contentEncodings = response.Content.Headers.ContentEncoding;
            if (contentEncodings.Count > 0)
            {
                string? last = null;
                foreach (string encoding in contentEncodings)
                {
                    last = encoding;
                }

                if (GZipEnabled && last == Gzip)
                {
                    response.Content = new GZipDecompressedContent(response.Content);
                }
                else if (DeflateEnabled && last == Deflate)
                {
                    response.Content = new DeflateDecompressedContent(response.Content);
                }
                else if (BrotliEnabled && last == Brotli)
                {
                    response.Content = new BrotliDecompressedContent(response.Content);
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
            private readonly HttpContent _originalContent;
            private bool _contentConsumed;

            public DecompressedContent(HttpContent originalContent)
            {
                _originalContent = originalContent;
                _contentConsumed = false;

                // Copy original response headers, but with the following changes:
                //   Content-Length is removed, since it no longer applies to the decompressed content
                //   The last Content-Encoding is removed, since we are processing that here.
                Headers.AddHeaders(originalContent.Headers);
                Headers.ContentLength = null;
                Headers.ContentEncoding.Clear();
                string? prevEncoding = null;
                foreach (string encoding in originalContent.Headers.ContentEncoding)
                {
                    if (prevEncoding != null)
                    {
                        Headers.ContentEncoding.Add(prevEncoding);
                    }
                    prevEncoding = encoding;
                }
            }

            protected abstract Stream GetDecompressedStream(Stream originalStream);

            protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            {
                using Stream decompressedStream = CreateContentReadStream(cancellationToken);
                decompressedStream.CopyTo(stream);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
                SerializeToStreamAsync(stream, context, CancellationToken.None);

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            {
                using (Stream decompressedStream = TryCreateContentReadStream() ?? await CreateContentReadStreamAsync(cancellationToken).ConfigureAwait(false))
                {
                    await decompressedStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            {
                ValueTask<Stream> task = CreateContentReadStreamAsyncCore(async: false, cancellationToken);
                Debug.Assert(task.IsCompleted);
                return task.GetAwaiter().GetResult();
            }

            protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
                CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask();

            private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async, CancellationToken cancellationToken)
            {
                if (_contentConsumed)
                {
                    throw new InvalidOperationException(SR.net_http_content_stream_already_read);
                }

                _contentConsumed = true;

                Stream originalStream;
                if (async)
                {
                    originalStream = _originalContent.TryReadAsStream() ?? await _originalContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    originalStream = _originalContent.ReadAsStream(cancellationToken);
                }
                return GetDecompressedStream(originalStream);
            }

            internal override Stream? TryCreateContentReadStream()
            {
                Stream? originalStream = _originalContent.TryReadAsStream();
                return originalStream is null ? null : GetDecompressedStream(originalStream);
            }

            protected internal override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            internal override bool AllowDuplex => false;

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

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new GZipStream(originalStream, CompressionMode.Decompress);
        }

        private sealed class DeflateDecompressedContent : DecompressedContent
        {
            public DeflateDecompressedContent(HttpContent originalContent)
                : base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new DeflateStream(originalStream, CompressionMode.Decompress);
        }

        private sealed class BrotliDecompressedContent : DecompressedContent
        {
            public BrotliDecompressedContent(HttpContent originalContent) :
                base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new BrotliStream(originalStream, CompressionMode.Decompress);
        }
    }
}
