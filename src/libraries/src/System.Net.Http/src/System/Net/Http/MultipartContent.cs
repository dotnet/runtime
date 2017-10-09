// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Represents a multipart/* content. Even if a collection of HttpContent is stored, " +
        "suffix Collection is not appropriate.")]
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        #region Fields

        private const string CrLf = "\r\n";

        private static readonly int s_crlfLength = GetEncodedLength(CrLf);
        private static readonly int s_dashDashLength = GetEncodedLength("--");
        private static readonly int s_colonSpaceLength = GetEncodedLength(": ");
        private static readonly int s_commaSpaceLength = GetEncodedLength(", ");

        private readonly List<HttpContent> _nestedContent;
        private readonly string _boundary;

        #endregion Fields

        #region Construction

        public MultipartContent()
            : this("mixed", GetDefaultBoundary())
        { }

        public MultipartContent(string subtype)
            : this(subtype, GetDefaultBoundary())
        { }

        public MultipartContent(string subtype, string boundary)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(subtype));
            }
            Contract.EndContractBlock();
            ValidateBoundary(boundary);

            _boundary = boundary;

            string quotedBoundary = boundary;
            if (!quotedBoundary.StartsWith("\"", StringComparison.Ordinal))
            {
                quotedBoundary = "\"" + quotedBoundary + "\"";
            }

            MediaTypeHeaderValue contentType = new MediaTypeHeaderValue("multipart/" + subtype);
            contentType.Parameters.Add(new NameValueHeaderValue(nameof(boundary), quotedBoundary));
            Headers.ContentType = contentType;

            _nestedContent = new List<HttpContent>();
        }

        private static void ValidateBoundary(string boundary)
        {
            // NameValueHeaderValue is too restrictive for boundary.
            // Instead validate it ourselves and then quote it.
            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(boundary));
            }

            // RFC 2046 Section 5.1.1
            // boundary := 0*69<bchars> bcharsnospace
            // bchars := bcharsnospace / " "
            // bcharsnospace := DIGIT / ALPHA / "'" / "(" / ")" / "+" / "_" / "," / "-" / "." / "/" / ":" / "=" / "?"
            if (boundary.Length > 70)
            {
                throw new ArgumentOutOfRangeException(nameof(boundary), boundary,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_content_field_too_long, 70));
            }
            // Cannot end with space.
            if (boundary.EndsWith(" ", StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
            }
            Contract.EndContractBlock();

            const string AllowedMarks = @"'()+_,-./:=? ";

            foreach (char ch in boundary)
            {
                if (('0' <= ch && ch <= '9') || // Digit.
                    ('a' <= ch && ch <= 'z') || // alpha.
                    ('A' <= ch && ch <= 'Z') || // ALPHA.
                    (AllowedMarks.IndexOf(ch) >= 0)) // Marks.
                {
                    // Valid.
                }
                else
                {
                    throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
                }
            }
        }

        private static string GetDefaultBoundary()
        {
            return Guid.NewGuid().ToString();
        }

        public virtual void Add(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Contract.EndContractBlock();

            _nestedContent.Add(content);
        }

        #endregion Construction

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (HttpContent content in _nestedContent)
                {
                    content.Dispose();
                }
                _nestedContent.Clear();
            }
            base.Dispose(disposing);
        }

        #endregion Dispose

        #region IEnumerable<HttpContent> Members

        public IEnumerator<HttpContent> GetEnumerator()
        {
            return _nestedContent.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return _nestedContent.GetEnumerator();
        }

        #endregion

        #region Serialization

        // for-each content
        //   write "--" + boundary
        //   for-each content header
        //     write header: header-value
        //   write content.CopyTo[Async]
        // write "--" + boundary + "--"
        // Can't be canceled directly by the user.  If the overall request is canceled 
        // then the stream will be closed an exception thrown.
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Debug.Assert(stream != null);
            try
            {
                // Write start boundary.
                await EncodeStringToStreamAsync(stream, "--" + _boundary + CrLf).ConfigureAwait(false);

                // Write each nested content.
                var output = new StringBuilder();
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    // Write divider, headers, and content.
                    HttpContent content = _nestedContent[contentIndex];
                    await EncodeStringToStreamAsync(stream, SerializeHeadersToString(output, contentIndex, content)).ConfigureAwait(false);
                    await content.CopyToAsync(stream).ConfigureAwait(false);
                }

                // Write footer boundary.
                await EncodeStringToStreamAsync(stream, CrLf + "--" + _boundary + "--" + CrLf).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, ex);
                throw;
            }
        }

        protected override async Task<Stream> CreateContentReadStreamAsync()
        {
            try
            {
                var streams = new Stream[2 + (_nestedContent.Count*2)];
                var scratch = new StringBuilder();
                int streamIndex = 0;

                // Start boundary.
                streams[streamIndex++] = EncodeStringToNewStream("--" + _boundary + CrLf);

                // Each nested content.
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    HttpContent nestedContent = _nestedContent[contentIndex];
                    streams[streamIndex++] = EncodeStringToNewStream(SerializeHeadersToString(scratch, contentIndex, nestedContent));

                    Stream readStream = (await nestedContent.ReadAsStreamAsync().ConfigureAwait(false)) ?? new MemoryStream();
                    if (!readStream.CanSeek)
                    {
                        // Seekability impacts whether HttpClientHandlers are able to rewind.  To maintain compat
                        // and to allow such use cases when a nested stream isn't seekable (which should be rare),
                        // we fall back to the base behavior.  We don't dispose of the streams already obtained
                        // as we don't necessarily own them yet.
                        return await base.CreateContentReadStreamAsync().ConfigureAwait(false);
                    }
                    streams[streamIndex++] = readStream;
                }

                // Footer boundary.
                streams[streamIndex] = EncodeStringToNewStream(CrLf + "--" + _boundary + "--" + CrLf);

                return new ContentReadStream(streams);
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, ex);
                throw;
            }
        }

        private string SerializeHeadersToString(StringBuilder scratch, int contentIndex, HttpContent content)
        {
            scratch.Clear();

            // Add divider.
            if (contentIndex != 0) // Write divider for all but the first content.
            {
                scratch.Append(CrLf + "--"); // const strings
                scratch.Append(_boundary);
                scratch.Append(CrLf);
            }

            // Add headers.
            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers)
            {
                scratch.Append(headerPair.Key);
                scratch.Append(": ");
                string delim = string.Empty;
                foreach (string value in headerPair.Value)
                {
                    scratch.Append(delim);
                    scratch.Append(value);
                    delim = ", ";
                }
                scratch.Append(CrLf);
            }

            // Extra CRLF to end headers (even if there are no headers).
            scratch.Append(CrLf);

            return scratch.ToString();
        }

        private static Task EncodeStringToStreamAsync(Stream stream, string input)
        {
            byte[] buffer = HttpRuleParser.DefaultHttpEncoding.GetBytes(input);
            return stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static Stream EncodeStringToNewStream(string input)
        {
            return new MemoryStream(HttpRuleParser.DefaultHttpEncoding.GetBytes(input), writable: false);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            int boundaryLength = GetEncodedLength(_boundary);

            long currentLength = 0;
            long internalBoundaryLength = s_crlfLength + s_dashDashLength + boundaryLength + s_crlfLength;

            // Start Boundary.
            currentLength += s_dashDashLength + boundaryLength + s_crlfLength;

            bool first = true;
            foreach (HttpContent content in _nestedContent)
            {
                if (first)
                {
                    first = false; // First boundary already written.
                }
                else
                {
                    // Internal Boundary.
                    currentLength += internalBoundaryLength;
                }

                // Headers.
                foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers)
                {
                    currentLength += GetEncodedLength(headerPair.Key) + s_colonSpaceLength;

                    int valueCount = 0;
                    foreach (string value in headerPair.Value)
                    {
                        currentLength += GetEncodedLength(value);
                        valueCount++;
                    }
                    if (valueCount > 1)
                    {
                        currentLength += (valueCount - 1) * s_commaSpaceLength;
                    }

                    currentLength += s_crlfLength;
                }

                currentLength += s_crlfLength;

                // Content.
                long tempContentLength = 0;
                if (!content.TryComputeLength(out tempContentLength))
                {
                    length = 0;
                    return false;
                }
                currentLength += tempContentLength;
            }

            // Terminating boundary.
            currentLength += s_crlfLength + s_dashDashLength + boundaryLength + s_dashDashLength + s_crlfLength;

            length = currentLength;
            return true;
        }

        private static int GetEncodedLength(string input)
        {
            return HttpRuleParser.DefaultHttpEncoding.GetByteCount(input);
        }

        private sealed class ContentReadStream : Stream
        {
            private readonly Stream[] _streams;
            private readonly long _length;

            private int _next;
            private Stream _current;
            private long _position;

            internal ContentReadStream(Stream[] streams)
            {
                Debug.Assert(streams != null);
                _streams = streams;
                foreach (Stream stream in streams)
                {
                    _length += stream.Length;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    foreach (Stream s in _streams)
                    {
                        s.Dispose();
                    }
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValidateReadArgs(buffer, offset, count);
                if (count == 0)
                {
                    return 0;
                }

                while (true)
                {
                    if (_current != null)
                    {
                        int bytesRead = _current.Read(buffer, offset, count);
                        if (bytesRead != 0)
                        {
                            _position += bytesRead;
                            return bytesRead;
                        }

                        _current = null;
                    }

                    if (_next >= _streams.Length)
                    {
                        return 0;
                    }

                    _current = _streams[_next++];
                }
            }

            public override int Read(Span<byte> destination)
            {
                if (destination.Length == 0)
                {
                    return 0;
                }

                while (true)
                {
                    if (_current != null)
                    {
                        int bytesRead = _current.Read(destination);
                        if (bytesRead != 0)
                        {
                            _position += bytesRead;
                            return bytesRead;
                        }

                        _current = null;
                    }

                    if (_next >= _streams.Length)
                    {
                        return 0;
                    }

                    _current = _streams[_next++];
                }
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateReadArgs(buffer, offset, count);
                return ReadAsyncPrivate(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default) =>
                ReadAsyncPrivate(destination, cancellationToken);

            public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
                TaskToApm.Begin(ReadAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

            public override int EndRead(IAsyncResult asyncResult) =>
                TaskToApm.End<int>(asyncResult);

            public async ValueTask<int> ReadAsyncPrivate(Memory<byte> destination, CancellationToken cancellationToken)
            {
                if (destination.Length == 0)
                {
                    return 0;
                }

                while (true)
                {
                    if (_current != null)
                    {
                        int bytesRead = await _current.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
                        if (bytesRead != 0)
                        {
                            _position += bytesRead;
                            return bytesRead;
                        }

                        _current = null;
                    }

                    if (_next >= _streams.Length)
                    {
                        return 0;
                    }

                    _current = _streams[_next++];
                }
            }

            public override long Position
            {
                get { return _position; }
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    long previousStreamsLength = 0;
                    for (int i = 0; i < _streams.Length; i++)
                    {
                        Stream curStream = _streams[i];
                        long curLength = curStream.Length;

                        if (value < previousStreamsLength + curLength)
                        {
                            _current = curStream;
                            i++;
                            _next = i;

                            curStream.Position = value - previousStreamsLength;
                            for (; i < _streams.Length; i++)
                            {
                                _streams[i].Position = 0;
                            }

                            _position = value;
                            return;
                        }

                        previousStreamsLength += curLength;
                    }

                    _current = null;
                    _next = _streams.Length;
                    _position = value;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;

                    case SeekOrigin.Current:
                        Position += offset;
                        break;

                    case SeekOrigin.End:
                        Position = _length + offset;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }

                return Position;
            }

            public override long Length => _length;

            private static void ValidateReadArgs(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (offset > buffer.Length - count)
                {
                    throw new ArgumentException(SR.net_http_buffer_insufficient_length, nameof(buffer));
                }
            }

            public override void Flush() { }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override void Write(ReadOnlySpan<byte> source) { throw new NotSupportedException(); }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { throw new NotSupportedException(); }
            public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default) { throw new NotSupportedException(); }
        }
        #endregion Serialization
    }
}
