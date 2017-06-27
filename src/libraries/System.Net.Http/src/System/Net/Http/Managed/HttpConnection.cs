// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnection : IDisposable
    {
        private const int BufferSize =
#if DEBUG
            10;
#else
            4096;
#endif

        private static readonly byte[] s_contentLength0NewlineAsciiBytes = Encoding.ASCII.GetBytes("Content-Length: 0\r\n");
        private static readonly byte[] s_spaceHttp11NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.1\r\n");
        private static readonly byte[] s_hostKeyAndSeparator = Encoding.ASCII.GetBytes(HttpKnownHeaderNames.Host + ": ");

        private readonly HttpConnectionPool _pool;
        private readonly HttpConnectionKey _key;
        private readonly Stream _stream;
        private readonly TransportContext _transportContext;
        private readonly bool _usingProxy;

        private ValueStringBuilder _sb; // mutable struct, do not make this readonly

        private readonly byte[] _writeBuffer;
        private int _writeOffset;

        private readonly byte[] _readBuffer;
        private int _readOffset;
        private int _readLength;

        private bool _disposed;

        private sealed class HttpConnectionContent : HttpContent
        {
            private readonly CancellationToken _cancellationToken;
            private HttpContentReadStream _stream;

            public HttpConnectionContent(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public void SetStream(HttpContentReadStream stream)
            {
                Debug.Assert(stream != null);
                Debug.Assert(stream.CanRead);

                _stream = stream;
            }

            private HttpContentReadStream ConsumeStream()
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException("content already consumed");
                }

                HttpContentReadStream stream = _stream;
                _stream = null;
                return stream;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Debug.Assert(stream != null);

                using (HttpContentReadStream contentStream = ConsumeStream())
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

        private sealed class ContentLengthReadStream : HttpContentReadStream
        {
            private long _contentBytesRemaining;

            public ContentLengthReadStream(HttpConnection connection, long contentLength)
                : base(connection)
            {
                if (contentLength == 0)
                {
                    _connection = null;
                    _contentBytesRemaining = 0;
                    connection.PutConnectionInPool();
                }
                else
                {
                    _contentBytesRemaining = contentLength;
                }
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (count < 0 || count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return 0;
                }

                Debug.Assert(_contentBytesRemaining > 0);

                count = (int)Math.Min(count, _contentBytesRemaining);

                int bytesRead = await _connection.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // Unexpected end of response stream
                    throw new IOException("Unexpected end of content stream");
                }

                Debug.Assert(bytesRead <= _contentBytesRemaining);
                _contentBytesRemaining -= bytesRead;

                if (_contentBytesRemaining == 0)
                {
                    // End of response body
                    _connection.PutConnectionInPool();
                    _connection = null;
                }

                return bytesRead;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return;
                }

                await _connection.CopyChunkToAsync(destination, _contentBytesRemaining, cancellationToken).ConfigureAwait(false);

                _contentBytesRemaining = 0;
                _connection.PutConnectionInPool();
                _connection = null;
            }
        }

        private sealed class ChunkedEncodingReadStream : HttpContentReadStream
        {
            private int _chunkBytesRemaining;

            public ChunkedEncodingReadStream(HttpConnection connection)
                : base(connection)
            {
                _chunkBytesRemaining = 0;
            }

            private async Task<bool> TryGetNextChunk(CancellationToken cancellationToken)
            {
                Debug.Assert(_chunkBytesRemaining == 0);

                // Start of chunk, read chunk size
                int chunkSize = 0;
                char c = await _connection.ReadCharAsync(cancellationToken).ConfigureAwait(false);
                while (true)
                {
                    // Get hex digit
                    if ((uint)(c - '0') <= '9' - '0')
                    {
                        chunkSize = chunkSize * 16 + (c - '0');
                    }
                    else if ((uint)(c - 'a') <= ('f' - 'a'))
                    {
                        chunkSize = chunkSize * 16 + (c - 'a' + 10);
                    }
                    else if ((uint)(c - 'A') <= ('F' - 'A'))
                    {
                        chunkSize = chunkSize * 16 + (c - 'A' + 10);
                    }
                    else
                    {
                        throw new IOException("Invalid chunk size in response stream");
                    }

                    c = await _connection.ReadCharAsync(cancellationToken).ConfigureAwait(false);
                    if (c == '\r')
                    {
                        if (await _connection.ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\n')
                        {
                            throw new IOException("Saw CR without LF while parsing chunk size");
                        }

                        break;
                    }
                }

                _chunkBytesRemaining = chunkSize;
                if (chunkSize == 0)
                {
                    // Indicates end of response body

                    // We expect final CRLF after this
                    if (await _connection.ReadByteAsync(cancellationToken).ConfigureAwait(false) != (byte)'\r' ||
                        await _connection.ReadByteAsync(cancellationToken).ConfigureAwait(false) != (byte)'\n')
                    {
                        throw new IOException("missing final CRLF for chunked encoding");
                    }

                    _connection.PutConnectionInPool();
                    _connection = null;
                    return false;
                }

                return true;
            }

            private async Task ConsumeChunkBytes(int bytesConsumed, CancellationToken cancellationToken)
            {
                Debug.Assert(bytesConsumed <= _chunkBytesRemaining);
                _chunkBytesRemaining -= bytesConsumed;

                if (_chunkBytesRemaining == 0)
                {
                    // Parse CRLF at end of chunk
                    if (await _connection.ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\r' ||
                        await _connection.ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\n')
                    {
                        throw new IOException("missing CRLF for end of chunk");
                    }
                }
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (count < 0 || count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return 0;
                }

                if (_chunkBytesRemaining == 0)
                {
                    if (!await TryGetNextChunk(cancellationToken).ConfigureAwait(false))
                    {
                        // End of response body
                        return 0;
                    }
                }

                count = Math.Min(count, _chunkBytesRemaining);

                int bytesRead = await _connection.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // Unexpected end of response stream
                    throw new IOException("Unexpected end of content stream while processing chunked response body");
                }

                await ConsumeChunkBytes(bytesRead, cancellationToken).ConfigureAwait(false);

                return bytesRead;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return;
                }

                if (_chunkBytesRemaining > 0)
                {
                    await _connection.CopyChunkToAsync(destination, _chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                    await ConsumeChunkBytes(_chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                }

                while (await TryGetNextChunk(cancellationToken).ConfigureAwait(false))
                {
                    await _connection.CopyChunkToAsync(destination, _chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                    await ConsumeChunkBytes(_chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private sealed class ConnectionCloseReadStream : HttpContentReadStream
        {
            public ConnectionCloseReadStream(HttpConnection connection)
                : base(connection)
            {
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (count < 0 || count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return 0;
                }

                int bytesRead = await _connection.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // We cannot reuse this connection, so close it.
                    _connection.Dispose();
                    _connection = null;
                    return 0;
                }

                return bytesRead;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return;
                }

                await _connection.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

                // We cannot reuse this connection, so close it.
                _connection.Dispose();
                _connection = null;
            }
        }

        private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
        {
            public ChunkedEncodingWriteStream(HttpConnection connection)
                : base (connection)
            {
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (count < 0 || count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                // Don't write if nothing was given
                // Especially since we don't want to accidentally send a 0 chunk, which would indicate end of body
                if (count == 0)
                {
                    return;
                }

                // Write chunk length -- hex representation of count
                bool digitWritten = false;
                for (int i = 7; i >= 0; i--)
                {
                    int shift = i * 4;
                    int mask = 0xF << shift;
                    int digit = (count & mask) >> shift;
                    if (digitWritten || digit != 0)
                    {
                        await _connection.WriteByteAsync((byte)(digit < 10 ? '0' + digit : 'A' + digit - 10), cancellationToken).ConfigureAwait(false);
                        digitWritten = true;
                    }
                }

                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

                // Write chunk contents
                await _connection.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _connection.FlushAsync(cancellationToken);
            }

            public override async Task FinishAsync(CancellationToken cancellationToken)
            {
                // Send 0 byte chunk to indicate end
                await _connection.WriteByteAsync((byte)'0', cancellationToken).ConfigureAwait(false);
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

                // Send final _CRLF
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

                _connection = null;
            }
        }

        public sealed class ContentLengthWriteStream : HttpContentWriteStream
        {
            public ContentLengthWriteStream(HttpConnection connection)
                : base(connection)
            {
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (count < 0 || count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                return _connection.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _connection.FlushAsync(cancellationToken);
            }

            public override Task FinishAsync(CancellationToken cancellationToken)
            {
                _connection = null;
                return Task.CompletedTask;
            }
        }

        public HttpConnection(
            HttpConnectionPool pool, 
            HttpConnectionKey key, 
            Stream stream, 
            TransportContext transportContext, 
            bool usingProxy)
        {
            _pool = pool;
            _key = key;
            _stream = stream;
            _transportContext = transportContext;
            _usingProxy = usingProxy;

            const int DefaultCapacity = 16;
            _sb = new ValueStringBuilder(DefaultCapacity);

            _writeBuffer = new byte[BufferSize];
            _writeOffset = 0;

            _readBuffer = new byte[BufferSize];
            _readLength = 0;
            _readOffset = 0;

            _pool.AddConnection(this);
        }

        public HttpConnectionKey Key
        {
            get { return _key; }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _stream.Dispose();
            }
        }

        private async ValueTask<HttpResponseMessage> ParseResponseAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.RequestMessage = request;

            if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != 'H' ||
                await ReadCharAsync(cancellationToken).ConfigureAwait(false) != 'T' ||
                await ReadCharAsync(cancellationToken).ConfigureAwait(false) != 'T' ||
                await ReadCharAsync(cancellationToken).ConfigureAwait(false) != 'P' ||
                await ReadCharAsync(cancellationToken).ConfigureAwait(false) != '/')
            {
                throw new HttpRequestException("could not read response HTTP version");
            }

            // Set the response HttpVersion.
            char majorVersion = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            if (!char.IsDigit(majorVersion) ||
                await ReadCharAsync(cancellationToken).ConfigureAwait(false) != '.')
            {
                throw new HttpRequestException("could not read response HTTP version");
            }
            char minorVersion = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            if (!char.IsDigit(minorVersion))
            {
                throw new HttpRequestException("could not read response HTTP version");
            }
            response.Version =
                (majorVersion == '1' && minorVersion == '1') ? HttpVersionInternal.Version11 :
                (majorVersion == '1' && minorVersion == '0') ? HttpVersionInternal.Version10 :
                (majorVersion == '2' && minorVersion == '0') ? HttpVersionInternal.Version20 :
                HttpVersionInternal.Unknown;

            if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != ' ')
            {
                throw new HttpRequestException("Invalid characters in response");
            }

            char status1 = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            char status2 = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            char status3 = await ReadCharAsync(cancellationToken).ConfigureAwait(false);

            if (!char.IsDigit(status1) ||
                !char.IsDigit(status2) ||
                !char.IsDigit(status3))
            {
                throw new HttpRequestException("could not read response status code");
            }

            int status = 100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0');
            response.StatusCode = (HttpStatusCode)status;

            if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != ' ')
            {
                throw new HttpRequestException("Invalid characters in response line");
            }

            _sb.Clear();

            // Parse reason phrase
            char c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            while (c != '\r')
            {
                _sb.Append(c);
                c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            }

            if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\n')
            {
                throw new HttpRequestException("Saw CR without LF while parsing response line");
            }

            string knownReasonPhrase = HttpStatusDescription.Get(response.StatusCode);
            response.ReasonPhrase = CharArrayHelpers.EqualsOrdinal(knownReasonPhrase, _sb.Chars, 0, _sb.Length) ?
                knownReasonPhrase :
                _sb.ToString();

            var responseContent = new HttpConnectionContent(CancellationToken.None);

            // Parse headers
            _sb.Clear();
            c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            while (true)
            {
                if (c == '\r')
                {
                    if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\n')
                    {
                        throw new HttpRequestException("Saw CR without LF while parsing headers");
                    }

                    break;
                }

                // Get header name
                while (c != ':')
                {
                    _sb.Append(c);
                    c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                }

                string headerName;
                if (!HttpKnownHeaderNames.TryGetHeaderName(_sb.Chars, 0, _sb.Length, out headerName))
                {
                    headerName = _sb.ToString();
                }

                _sb.Clear();

                // Get header value
                c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                while (c == ' ')
                {
                    c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                }

                while (c != '\r')
                {
                    _sb.Append(c);
                    c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                }

                if (await ReadCharAsync(cancellationToken).ConfigureAwait(false) != '\n')
                {
                    throw new HttpRequestException("Saw CR without LF while parsing headers");
                }

                string headerValue = HttpKnownHeaderNames.GetHeaderValue(headerName, _sb.Chars, 0, _sb.Length);

                // TryAddWithoutValidation will fail if the header name has trailing whitespace.
                // So, trim it here.
                // TODO: Not clear to me from the RFC that this is really correct; RFC seems to indicate this should be an error.
                // However, tests claim this is important for compat in practice.
                headerName = headerName.TrimEnd();

                // Add header to appropriate collection
                if (!response.Headers.TryAddWithoutValidation(headerName, headerValue))
                {
                    // The existing handlers ignore headers that couldn't be added.  Do the same here.
                    responseContent.Headers.TryAddWithoutValidation(headerName, headerValue);
                }

                _sb.Clear();

                c = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            }

            // Instantiate responseStream
            HttpContentReadStream responseStream;

            if (request.Method == HttpMethod.Head ||
                status == 204 ||
                status == 304)
            {
                // There is implicitly no response body
                // TODO: I don't understand why there's any content here at all --
                // i.e. why not just set response.Content = null?
                // This is legal for request bodies (e.g. GET).
                // However, setting response.Content = null causes a bunch of tests to fail.
                responseStream = new ContentLengthReadStream(this, 0);
            }
            else
            { 
                if (responseContent.Headers.ContentLength != null)
                {
                    responseStream = new ContentLengthReadStream(this, responseContent.Headers.ContentLength.Value);
                }
                else if (response.Headers.TransferEncodingChunked == true)
                {
                    responseStream = new ChunkedEncodingReadStream(this);
                }
                else
                {
                    responseStream = new ConnectionCloseReadStream(this);
                }
            }

            responseContent.SetStream(responseStream);
            response.Content = responseContent;
            return response;
        }

        private async Task WriteHeadersAsync(HttpHeaders headers, CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                await WriteAsciiStringAsync(header.Key, cancellationToken).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)':', (byte)' ', cancellationToken).ConfigureAwait(false);

                bool first = true;
                foreach (string headerValue in header.Value)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        await WriteTwoBytesAsync((byte)',', (byte)' ', cancellationToken).ConfigureAwait(false);
                    }
                    await WriteStringAsync(headerValue, cancellationToken).ConfigureAwait(false);
                }

                Debug.Assert(!first, "No values for header??");

                await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteHostHeaderAsync(Uri uri, CancellationToken cancellationToken)
        {
            await WriteBytesAsync(s_hostKeyAndSeparator, cancellationToken).ConfigureAwait(false);

            await WriteStringAsync(uri.Host, cancellationToken).ConfigureAwait(false);
            if (!uri.IsDefaultPort)
            {
                await WriteByteAsync((byte)':', cancellationToken).ConfigureAwait(false);
                await WriteAsciiStringAsync(uri.Port.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            }

            await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Version.Major != 1 || request.Version.Minor != 1)
            {
                throw new PlatformNotSupportedException($"Only HTTP 1.1 supported -- request.Version was {request.Version}");
            }

            HttpContent requestContent = request.Content;

            // Add headers to define content transfer, if not present
            if (requestContent != null &&
                request.Headers.TransferEncodingChunked != true &&
                requestContent.Headers.ContentLength == null)
            {
                // We have content, but neither Transfer-Encoding or Content-Length is set.
                // TODO: Tests expect Transfer-Encoding here always.
                // This seems wrong to me; if we can compute the content length,
                // why not use it instead of falling back to Transfer-Encoding?
#if false
                if (requestContent.TryComputeLength(out contentLength))
                {
                    // We know the content length, so set the header
                    requestContent.Headers.ContentLength = contentLength;
                }
                else
#endif
                {
                    request.Headers.TransferEncodingChunked = true;
                }
            }

            // Write request line
            await WriteStringAsync(request.Method.Method, cancellationToken).ConfigureAwait(false);
            await WriteByteAsync((byte)' ', cancellationToken).ConfigureAwait(false);

            await WriteStringAsync(
                _usingProxy ? request.RequestUri.AbsoluteUri : request.RequestUri.PathAndQuery,
                cancellationToken).ConfigureAwait(false);

            await WriteBytesAsync(s_spaceHttp11NewlineAsciiBytes, cancellationToken).ConfigureAwait(false);

            // Write request headers
            await WriteHeadersAsync(request.Headers, cancellationToken).ConfigureAwait(false);

            if (requestContent == null)
            {
                // Write out Content-Length: 0 header to indicate no body, 
                // unless this is a method that never has a body.
                if (request.Method != HttpMethod.Get &&
                    request.Method != HttpMethod.Head)
                {
                    await WriteBytesAsync(s_contentLength0NewlineAsciiBytes, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                // Write content headers
                await WriteHeadersAsync(requestContent.Headers, cancellationToken).ConfigureAwait(false);
            }

            // Write special additional headers.  If a host isn't in the headers list, then a Host header
            // wasn't sent, so as it's required by HTTP 1.1 spec, send one based on the Request Uri.
            if (request.Headers.Host == null)
            {
                await WriteHostHeaderAsync(request.RequestUri, cancellationToken).ConfigureAwait(false);
            }

            // CRLF for end of headers.
            await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

            // Write body, if any
            if (requestContent != null)
            {
                HttpContentWriteStream stream = (request.Headers.TransferEncodingChunked == true ?
                    (HttpContentWriteStream)new ChunkedEncodingWriteStream(this) : 
                    (HttpContentWriteStream)new ContentLengthWriteStream(this));

                // TODO: CopyToAsync doesn't take a CancellationToken, how do we deal with Cancellation here?
                await request.Content.CopyToAsync(stream, _transportContext).ConfigureAwait(false);
                await stream.FinishAsync(cancellationToken).ConfigureAwait(false);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);

            return await ParseResponseAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private void WriteToBuffer(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count <= BufferSize - _writeOffset);

            Buffer.BlockCopy(buffer, offset, _writeBuffer, _writeOffset, count);
            _writeOffset += count;
        }

        private async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int remaining = BufferSize - _writeOffset;

            if (count <= remaining)
            {
                // Fits in current write buffer.  Just copy and return.
                WriteToBuffer(buffer, offset, count);
                return;
            }

            if (_writeOffset != 0)
            {
                // Fit what we can in the current write buffer and flush it.
                WriteToBuffer(buffer, offset, remaining);
                await FlushAsync(cancellationToken).ConfigureAwait(false);

                // Update offset and count to reflect the write we just did.
                offset += remaining;
                count -= remaining;
            }

            if (count >= BufferSize)
            {
                // Large write.  No sense buffering this.  Write directly to stream.
                // CONSIDER: May want to be a bit smarter here?  Think about how large writes should work...
                await _stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Copy remainder into buffer
                WriteToBuffer(buffer, offset, count);
            }
        }

        private Task WriteByteAsync(byte b, CancellationToken cancellationToken)
        {
            if (_writeOffset < BufferSize)
            {
                _writeBuffer[_writeOffset++] = b;
                return Task.CompletedTask;
            }
            return WriteByteSlowAsync(b, cancellationToken);
        }

        private async Task WriteByteSlowAsync(byte b, CancellationToken cancellationToken)
        {
            await _stream.WriteAsync(_writeBuffer, 0, BufferSize, cancellationToken).ConfigureAwait(false);

            _writeBuffer[0] = b;
            _writeOffset = 1;
        }

        private Task WriteTwoBytesAsync(byte b1, byte b2, CancellationToken cancellationToken)
        {
            if (_writeOffset <= BufferSize - 2)
            {
                byte[] buffer = _writeBuffer;
                buffer[_writeOffset++] = b1;
                buffer[_writeOffset++] = b2;
                return Task.CompletedTask;
            }
            return WriteTwoBytesSlowAsync(b1, b2, cancellationToken);
        }

        private async Task WriteTwoBytesSlowAsync(byte b1, byte b2, CancellationToken cancellationToken)
        {
            await WriteByteAsync(b1, cancellationToken).ConfigureAwait(false);
            await WriteByteAsync(b2, cancellationToken).ConfigureAwait(false);
        }

        private Task WriteBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (_writeOffset <= BufferSize - bytes.Length)
            {
                Buffer.BlockCopy(bytes, 0, _writeBuffer, _writeOffset, bytes.Length);
                _writeOffset += bytes.Length;
                return Task.CompletedTask;
            }
            return WriteBytesSlowAsync(bytes, cancellationToken);
        }

        private async Task WriteBytesSlowAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (true)
            {
                int remaining = bytes.Length - offset;
                int toCopy = Math.Min(remaining, BufferSize - _writeOffset);
                Buffer.BlockCopy(bytes, offset, _writeBuffer, _writeOffset, toCopy);
                _writeOffset += toCopy;
                offset += toCopy;

                Debug.Assert(offset <= bytes.Length, $"Expected {nameof(offset)} to be <= {bytes.Length}, got {offset}");
                Debug.Assert(_writeOffset <= BufferSize, $"Expected {nameof(_writeOffset)} to be <= {BufferSize}, got {_writeOffset}");
                if (offset == bytes.Length)
                {
                    break;
                }
                else if (_writeOffset == BufferSize)
                {
                    await _stream.WriteAsync(_writeBuffer, 0, BufferSize, cancellationToken).ConfigureAwait(false);
                    _writeOffset = 0;
                }
            }
        }

        private Task WriteStringAsync(string s, CancellationToken cancellationToken)
        {
            // If there's enough space in the buffer to just copy all of the string's bytes, do so.
            // Unlike WriteAsciiStringAsync, validate each char along the way.
            int offset = _writeOffset;
            if (s.Length <= BufferSize - offset)
            {
                byte[] writeBuffer = _writeBuffer;
                foreach (char c in s)
                {
                    if ((c & 0xFF80) != 0)
                    {
                        throw new HttpRequestException("Non-ASCII characters found");
                    }
                    writeBuffer[offset++] = (byte)c;
                }
                _writeOffset = offset;
                return Task.CompletedTask;
            }

            // Otherwise, fall back to doing a normal slow string write; we could optimize away
            // the extra checks later, but the case where we cross a buffer boundary should be rare.
            return WriteStringAsyncSlow(s, cancellationToken);
        }

        private Task WriteAsciiStringAsync(string s, CancellationToken cancellationToken)
        {
            // If there's enough space in the buffer to just copy all of the string's bytes, do so.
            int offset = _writeOffset;
            if (s.Length <= BufferSize - offset)
            {
                byte[] writeBuffer = _writeBuffer;
                foreach (char c in s)
                {
                    writeBuffer[offset++] = (byte)c;
                }
                _writeOffset = offset;
                return Task.CompletedTask;
            }

            // Otherwise, fall back to doing a normal slow string write; we could optimize away
            // the extra checks later, but the case where we cross a buffer boundary should be rare.
            return WriteStringAsyncSlow(s, cancellationToken);
        }

        private async Task WriteStringAsyncSlow(string s, CancellationToken cancellationToken)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c & 0xFF80) != 0)
                {
                    throw new HttpRequestException("Non-ASCII characters found");
                }
                await WriteByteAsync((byte)c, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_writeOffset > 0)
            {
                Task t = _stream.WriteAsync(_writeBuffer, 0, _writeOffset, cancellationToken);
                _writeOffset = 0;
                return t;
            }
            return Task.CompletedTask;
        }

        private Task FillAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_readOffset == _readLength);

            _readOffset = 0;
            Task<int> t = _stream.ReadAsync(_readBuffer, 0, BufferSize, cancellationToken);
            if (t.IsCompleted)
            {
                _readLength = t.GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            else
            {
                // Using async/await results in slightly higher allocations for the case of a single await,
                // and it's simple to transform this one into ContinueWith.
                return t.ContinueWith((completed, state) =>
                    ((HttpConnection)state)._readLength = completed.GetAwaiter().GetResult(),
                    this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private async ValueTask<byte> ReadByteSlowAsync(CancellationToken cancellationToken)
        {
            await FillAsync(cancellationToken).ConfigureAwait(false);

            if (_readLength == 0)
            {
                // End of stream
                throw new IOException("unexpected end of stream");
            }

            return _readBuffer[_readOffset++];
        }

        // TODO: Revisit perf characteristics of this approach
        private ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken)
        {
            if (_readOffset < _readLength)
            {
                return new ValueTask<byte>(_readBuffer[_readOffset++]);
            }

            return ReadByteSlowAsync(cancellationToken);
        }

        private async ValueTask<char> ReadCharSlowAsync(CancellationToken cancellationToken)
        {
            await FillAsync(cancellationToken).ConfigureAwait(false);

            if (_readLength == 0)
            {
                // End of stream
                throw new IOException("unexpected end of stream");
            }

            byte b = _readBuffer[_readOffset++];
            if ((b & 0x80) != 0)
            {
                throw new HttpRequestException("Invalid character read from stream");
            }

            return (char)b;
        }

        private ValueTask<char> ReadCharAsync(CancellationToken cancellationToken)
        {
            if (_readOffset < _readLength)
            {
                byte b = _readBuffer[_readOffset++];
                if ((b & 0x80) != 0)
                {
                    return new ValueTask<char>(Task.FromException<char>(new HttpRequestException("Invalid character read from stream")));
                }

                return new ValueTask<char>((char)b);
            }

            return ReadCharSlowAsync(cancellationToken);
        }

        private void ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count <= _readLength - _readOffset);

            Buffer.BlockCopy(_readBuffer, _readOffset, buffer, offset, count);
            _readOffset += count;
        }

        private async ValueTask<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // This is called when reading the response body

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                count = Math.Min(count, remaining);
                ReadFromBuffer(buffer, offset, count);
                return count;
            }

            // No data in read buffer. 
            if (count < BufferSize / 2)
            {
                // Caller requested a small read size (less than half the read buffer size).
                // Read into the buffer, so that we read as much as possible, hopefully.
                await FillAsync(cancellationToken).ConfigureAwait(false);

                count = Math.Min(count, _readLength);
                ReadFromBuffer(buffer, offset, count);
                return count;
            }

            // Large read size, and no buffered data.
            // Do an unbuffered read directly against the underlying stream.
            count = await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            return count;
        }

        private async Task CopyFromBufferAsync(Stream destination, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count <= _readLength - _readOffset);

            await destination.WriteAsync(_readBuffer, _readOffset, count, cancellationToken).ConfigureAwait(false);
            _readOffset += count;
        }

        private async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);
            }

            while (true)
            {
                await FillAsync(cancellationToken).ConfigureAwait(false);
                if (_readLength == 0)
                {
                    // End of stream
                    break;
                }

                await CopyFromBufferAsync(destination, _readLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Copy *exactly* [length] bytes into destination; throws on end of stream.
        private async Task CopyChunkToAsync(Stream destination, long length, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);
            Debug.Assert(length > 0);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                remaining = (int)Math.Min(remaining, length);
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

                length -= remaining;
                if (length == 0)
                {
                    return;
                }
            }

            while (true)
            {
                await FillAsync(cancellationToken).ConfigureAwait(false);
                if (_readLength == 0)
                {
                    throw new HttpRequestException("unexpected end of stream");
                }

                remaining = (int)Math.Min(_readLength, length);
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

                length -= remaining;
                if (length == 0)
                {
                    return;
                }
            }
        }

        private void PutConnectionInPool()
        {
            // Make sure there's nothing in the write buffer that should have been flushed
            Debug.Assert(_writeOffset == 0);

            _pool.PutConnection(this);
        }

        private struct ValueStringBuilder
        {
            public char[] Chars;
            public int Length;

            public ValueStringBuilder(int initialCapacity)
            {
                Chars = new char[initialCapacity];
                Length = 0;
            }

            public void Append(char c)
            {
                if (Length == Chars.Length)
                {
                    Grow();
                }
                Chars[Length++] = c;
            }

            private void Grow() => Array.Resize(ref Chars, Chars.Length * 2);

            public void Clear() => Length = 0;

            public override string ToString() => new string(Chars, 0, Length);
        }
    }
}
