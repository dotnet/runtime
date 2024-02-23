// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
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

        private static bool EncodingExists(HttpHeaderValueCollection<StringWithQualityHeaderValue> acceptEncodingHeader, string encoding)
        {
            foreach (StringWithQualityHeaderValue existingEncoding in acceptEncodingHeader)
            {
                if (string.Equals(existingEncoding.Value, encoding, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (GZipEnabled && !EncodingExists(request.Headers.AcceptEncoding, Gzip))
            {
                request.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
            }

            if (DeflateEnabled && !EncodingExists(request.Headers.AcceptEncoding, Deflate))
            {
                request.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
            }

            if (BrotliEnabled && !EncodingExists(request.Headers.AcceptEncoding, Brotli))
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

                if (GZipEnabled && string.Equals(last, Gzip, StringComparison.OrdinalIgnoreCase))
                {
                    response.Content = new GZipDecompressedContent(response.Content);
                }
                else if (DeflateEnabled && string.Equals(last, Deflate, StringComparison.OrdinalIgnoreCase))
                {
                    response.Content = new DeflateDecompressedContent(response.Content);
                }
                else if (BrotliEnabled && string.Equals(last, Brotli, StringComparison.OrdinalIgnoreCase))
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
                new ZLibOrDeflateStream(originalStream);

            /// <summary>Stream that wraps either <see cref="ZLibStream"/> or <see cref="DeflateStream"/> for decompression.</summary>
            private sealed class ZLibOrDeflateStream : HttpBaseStream
            {
                // As described in RFC 2616, the deflate content-coding is the "zlib" format (RFC 1950) in combination with
                // the "deflate" compression algrithm (RFC 1951). Thus, the right stream to use here is ZLibStream.  However,
                // some servers incorrectly interpret "deflate" to mean the raw, unwrapped deflate protocol.  To account for
                // that, this switches between using ZLibStream (correct) and DeflateStream (incorrect) in order to maximize
                // compatibility with servers.

                private readonly PeekFirstByteReadStream _stream;
                private Stream? _decompressionStream;

                public ZLibOrDeflateStream(Stream stream) => _stream = new PeekFirstByteReadStream(stream);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _decompressionStream?.Dispose();
                        _stream.Dispose();
                    }
                    base.Dispose(disposing);
                }

                public override bool CanRead => true;
                public override bool CanWrite => false;
                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

                // On the first read request, peek at the first nibble of the response. If it's an 8, use ZLibStream, otherwise
                // use DeflateStream. This heuristic works because we're deciding only between raw deflate and zlib wrapped around
                // deflate, in which case the first nibble will always be 8 for zlib and never be 8 for deflate.
                // https://stackoverflow.com/a/37528114 provides an explanation for why.

                public override int Read(Span<byte> buffer)
                {
                    if (_decompressionStream is null)
                    {
                        int firstByte = _stream.PeekFirstByte();
                        _decompressionStream = CreateDecompressionStream(firstByte, _stream);
                    }

                    return _decompressionStream.Read(buffer);
                }

                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
                {
                    if (_decompressionStream is null)
                    {
                        return CreateAndReadAsync(this, buffer, cancellationToken);

                        static async ValueTask<int> CreateAndReadAsync(ZLibOrDeflateStream thisRef, Memory<byte> buffer, CancellationToken cancellationToken)
                        {
                            int firstByte = await thisRef._stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(false);
                            thisRef._decompressionStream = CreateDecompressionStream(firstByte, thisRef._stream);
                            return await thisRef._decompressionStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    return _decompressionStream.ReadAsync(buffer, cancellationToken);
                }

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                {
                    ValidateCopyToArguments(destination, bufferSize);
                    return Core(destination, bufferSize, cancellationToken);
                    async Task Core(Stream destination, int bufferSize, CancellationToken cancellationToken)
                    {
                        if (_decompressionStream is null)
                        {
                            int firstByte = await _stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(false);
                            _decompressionStream = CreateDecompressionStream(firstByte, _stream);
                        }

                        await _decompressionStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }

                private static Stream CreateDecompressionStream(int firstByte, Stream stream) =>
                    (firstByte & 0xF) == 8 ?
                        new ZLibStream(stream, CompressionMode.Decompress) :
                        new DeflateStream(stream, CompressionMode.Decompress);

                private sealed class PeekFirstByteReadStream : HttpBaseStream
                {
                    private readonly Stream _stream;
                    private byte _firstByte;
                    private FirstByteStatus _firstByteStatus;

                    public PeekFirstByteReadStream(Stream stream) => _stream = stream;

                    protected override void Dispose(bool disposing)
                    {
                        if (disposing)
                        {
                            _stream.Dispose();
                        }
                        base.Dispose(disposing);
                    }

                    public override bool CanRead => true;
                    public override bool CanWrite => false;
                    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

                    public int PeekFirstByte()
                    {
                        Debug.Assert(_firstByteStatus == FirstByteStatus.None);

                        int value = _stream.ReadByte();
                        if (value == -1)
                        {
                            _firstByteStatus = FirstByteStatus.Consumed;
                            return -1;
                        }

                        _firstByte = (byte)value;
                        _firstByteStatus = FirstByteStatus.Available;
                        return value;
                    }

                    public async ValueTask<int> PeekFirstByteAsync(CancellationToken cancellationToken)
                    {
                        Debug.Assert(_firstByteStatus == FirstByteStatus.None);

                        var buffer = new byte[1];

                        int bytesRead = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            _firstByteStatus = FirstByteStatus.Consumed;
                            return -1;
                        }

                        _firstByte = buffer[0];
                        _firstByteStatus = FirstByteStatus.Available;
                        return buffer[0];
                    }

                    public override int Read(Span<byte> buffer)
                    {
                        if (_firstByteStatus == FirstByteStatus.Available)
                        {
                            if (buffer.Length != 0)
                            {
                                buffer[0] = _firstByte;
                                _firstByteStatus = FirstByteStatus.Consumed;
                                return 1;
                            }

                            return 0;
                        }

                        Debug.Assert(_firstByteStatus == FirstByteStatus.Consumed);
                        return _stream.Read(buffer);
                    }

                    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
                    {
                        if (_firstByteStatus == FirstByteStatus.Available)
                        {
                            if (buffer.Length != 0)
                            {
                                buffer.Span[0] = _firstByte;
                                _firstByteStatus = FirstByteStatus.Consumed;
                                return new ValueTask<int>(1);
                            }

                            return new ValueTask<int>(0);
                        }

                        Debug.Assert(_firstByteStatus == FirstByteStatus.Consumed);
                        return _stream.ReadAsync(buffer, cancellationToken);
                    }

                    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                    {
                        Debug.Assert(_firstByteStatus != FirstByteStatus.None);

                        ValidateCopyToArguments(destination, bufferSize);
                        if (_firstByteStatus == FirstByteStatus.Available)
                        {
                            await destination.WriteAsync(new byte[] { _firstByte }, cancellationToken).ConfigureAwait(false);
                            _firstByteStatus = FirstByteStatus.Consumed;
                        }

                        await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
                    }

                    private enum FirstByteStatus : byte
                    {
                        None = 0,
                        Available = 1,
                        Consumed = 2
                    }
                }
            }
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
