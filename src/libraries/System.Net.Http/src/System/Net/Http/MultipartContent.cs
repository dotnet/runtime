// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        #region Fields

        private const string CrLf = "\r\n";

        private const int CrLfLength = 2;
        private const int DashDashLength = 2;
        private const int ColonSpaceLength = 2;
        private const int CommaSpaceLength = 2;

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
            ValidateBoundary(boundary);

            _boundary = boundary;

            string quotedBoundary = boundary;
            if (!quotedBoundary.StartsWith('\"'))
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
                    SR.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_content_field_too_long, 70));
            }
            // Cannot end with space.
            if (boundary.EndsWith(' '))
            {
                throw new ArgumentException(SR.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
            }

            const string AllowedMarks = @"'()+_,-./:=? ";

            foreach (char ch in boundary)
            {
                if (('0' <= ch && ch <= '9') || // Digit.
                    ('a' <= ch && ch <= 'z') || // alpha.
                    ('A' <= ch && ch <= 'Z') || // ALPHA.
                    (AllowedMarks.Contains(ch))) // Marks.
                {
                    // Valid.
                }
                else
                {
                    throw new ArgumentException(SR.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
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

        /// <summary>
        /// Gets or sets a callback that returns the <see cref="Encoding"/> to decode the value for the specified response header name,
        /// or <see langword="null"/> to use the default behavior.
        /// </summary>
        public HeaderEncodingSelector<HttpContent>? HeaderEncodingSelector { get; set; }

        // for-each content
        //   write "--" + boundary
        //   for-each content header
        //     write header: header-value
        //   write content.CopyTo[Async]
        // write "--" + boundary + "--"
        // Can't be canceled directly by the user.  If the overall request is canceled
        // then the stream will be closed an exception thrown.
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            Debug.Assert(stream != null);
            try
            {
                // Write start boundary.
                WriteToStream(stream, "--" + _boundary + CrLf);

                // Write each nested content.
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    // Write divider, headers, and content.
                    HttpContent content = _nestedContent[contentIndex];
                    SerializeHeadersToStream(stream, content, writeDivider: contentIndex != 0);
                    content.CopyTo(stream, context, cancellationToken);
                }

                // Write footer boundary.
                WriteToStream(stream, CrLf + "--" + _boundary + "--" + CrLf);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                throw;
            }
        }

        // for-each content
        //   write "--" + boundary
        //   for-each content header
        //     write header: header-value
        //   write content.CopyTo[Async]
        // write "--" + boundary + "--"
        // Can't be canceled directly by the user.  If the overall request is canceled
        // then the stream will be closed an exception thrown.
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsyncCore(stream, context, default);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(MultipartContent) ? SerializeToStreamAsyncCore(stream, context, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        private protected async Task SerializeToStreamAsyncCore(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            Debug.Assert(stream != null);
            try
            {
                // Write start boundary.
                await EncodeStringToStreamAsync(stream, "--" + _boundary + CrLf, cancellationToken).ConfigureAwait(false);

                // Write each nested content.
                var output = new MemoryStream();
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    // Write divider, headers, and content.
                    HttpContent content = _nestedContent[contentIndex];

                    output.SetLength(0);
                    SerializeHeadersToStream(output, content, writeDivider: contentIndex != 0);
                    output.Position = 0;
                    await output.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

                    await content.CopyToAsync(stream, context, cancellationToken).ConfigureAwait(false);
                }

                // Write footer boundary.
                await EncodeStringToStreamAsync(stream, CrLf + "--" + _boundary + "--" + CrLf, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                throw;
            }
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
        {
            ValueTask<Stream> task = CreateContentReadStreamAsyncCore(async: false, cancellationToken);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            CreateContentReadStreamAsyncCore(async: true, CancellationToken.None).AsTask();

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
            // Only skip the original protected virtual CreateContentReadStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(MultipartContent) ? CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask() :
            base.CreateContentReadStreamAsync(cancellationToken);

        private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async, CancellationToken cancellationToken)
        {
            try
            {
                var streams = new Stream[2 + (_nestedContent.Count * 2)];
                int streamIndex = 0;

                // Start boundary.
                streams[streamIndex++] = EncodeStringToNewStream("--" + _boundary + CrLf);

                // Each nested content.
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    HttpContent nestedContent = _nestedContent[contentIndex];
                    streams[streamIndex++] = EncodeHeadersToNewStream(nestedContent, writeDivider: contentIndex != 0);

                    Stream readStream;
                    if (async)
                    {
                        readStream = nestedContent.TryReadAsStream() ?? await nestedContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        readStream = nestedContent.ReadAsStream(cancellationToken);
                    }
                    // Cannot be null, at least an empty stream is necessary.
                    readStream ??= new MemoryStream();

                    if (!readStream.CanSeek)
                    {
                        // Seekability impacts whether HttpClientHandlers are able to rewind. To maintain compat
                        // and to allow such use cases when a nested stream isn't seekable (which should be rare),
                        // we fall back to the base behavior. We don't dispose of the streams already obtained
                        // as we don't necessarily own them yet.

#pragma warning disable CA2016
                        // Do not pass a cancellationToken to base.CreateContentReadStreamAsync() as it would trigger an infinite loop => StackOverflow
                        return async ? await base.CreateContentReadStreamAsync().ConfigureAwait(false) : base.CreateContentReadStream(cancellationToken);
#pragma warning restore CA2016
                    }
                    streams[streamIndex++] = readStream;
                }

                // Footer boundary.
                streams[streamIndex] = EncodeStringToNewStream(CrLf + "--" + _boundary + "--" + CrLf);

                return new ContentReadStream(streams);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                throw;
            }
        }

        private void SerializeHeadersToStream(Stream stream, HttpContent content, bool writeDivider)
        {
            // Add divider.
            if (writeDivider) // Write divider for all but the first content.
            {
                WriteToStream(stream, CrLf + "--"); // const strings
                WriteToStream(stream, _boundary);
                WriteToStream(stream, CrLf);
            }

            // Add headers.
            foreach (KeyValuePair<string, HeaderStringValues> headerPair in content.Headers.NonValidated)
            {
                Encoding headerValueEncoding = HeaderEncodingSelector?.Invoke(headerPair.Key, content) ?? HttpRuleParser.DefaultHttpEncoding;

                WriteToStream(stream, headerPair.Key);
                WriteToStream(stream, ": ");
                string delim = string.Empty;
                foreach (string value in headerPair.Value)
                {
                    WriteToStream(stream, delim);
                    WriteToStream(stream, value, headerValueEncoding);
                    delim = ", ";
                }
                WriteToStream(stream, CrLf);
            }

            // Extra CRLF to end headers (even if there are no headers).
            WriteToStream(stream, CrLf);
        }

        private static ValueTask EncodeStringToStreamAsync(Stream stream, string input, CancellationToken cancellationToken)
        {
            byte[] buffer = HttpRuleParser.DefaultHttpEncoding.GetBytes(input);
            return stream.WriteAsync(new ReadOnlyMemory<byte>(buffer), cancellationToken);
        }

        private static Stream EncodeStringToNewStream(string input)
        {
            return new MemoryStream(HttpRuleParser.DefaultHttpEncoding.GetBytes(input), writable: false);
        }

        private Stream EncodeHeadersToNewStream(HttpContent content, bool writeDivider)
        {
            var stream = new MemoryStream();
            SerializeHeadersToStream(stream, content, writeDivider);
            stream.Position = 0;
            return stream;
        }

        internal override bool AllowDuplex => false;

        protected internal override bool TryComputeLength(out long length)
        {
            // Start Boundary.
            long currentLength = DashDashLength + _boundary.Length + CrLfLength;

            if (_nestedContent.Count > 1)
            {
                // Internal boundaries
                currentLength += (_nestedContent.Count - 1) * (CrLfLength + DashDashLength + _boundary.Length + CrLfLength);
            }

            foreach (HttpContent content in _nestedContent)
            {
                // Headers.
                foreach (KeyValuePair<string, HeaderStringValues> headerPair in content.Headers.NonValidated)
                {
                    currentLength += headerPair.Key.Length + ColonSpaceLength;

                    Encoding headerValueEncoding = HeaderEncodingSelector?.Invoke(headerPair.Key, content) ?? HttpRuleParser.DefaultHttpEncoding;

                    int valueCount = 0;
                    foreach (string value in headerPair.Value)
                    {
                        currentLength += headerValueEncoding.GetByteCount(value);
                        valueCount++;
                    }

                    if (valueCount > 1)
                    {
                        currentLength += (valueCount - 1) * CommaSpaceLength;
                    }

                    currentLength += CrLfLength;
                }

                currentLength += CrLfLength;

                // Content.
                if (!content.TryComputeLength(out long tempContentLength))
                {
                    length = 0;
                    return false;
                }
                currentLength += tempContentLength;
            }

            // Terminating boundary.
            currentLength += CrLfLength + DashDashLength + _boundary.Length + DashDashLength + CrLfLength;

            length = currentLength;
            return true;
        }

        private sealed class ContentReadStream : Stream
        {
            private readonly Stream[] _streams;
            private readonly long _length;

            private int _next;
            private Stream? _current;
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

            public override async ValueTask DisposeAsync()
            {
                foreach (Stream s in _streams)
                {
                    await s.DisposeAsync().ConfigureAwait(false);
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);
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

            public override int Read(Span<byte> buffer)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                while (true)
                {
                    if (_current != null)
                    {
                        int bytesRead = _current.Read(buffer);
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
                ValidateBufferArguments(buffer, offset, count);
                return ReadAsyncPrivate(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                ReadAsyncPrivate(buffer, cancellationToken);

            public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
                TaskToApm.Begin(ReadAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

            public override int EndRead(IAsyncResult asyncResult) =>
                TaskToApm.End<int>(asyncResult);

            public async ValueTask<int> ReadAsyncPrivate(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                while (true)
                {
                    if (_current != null)
                    {
                        int bytesRead = await _current.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
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

            public override void Flush() { }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override void Write(ReadOnlySpan<byte> buffer) { throw new NotSupportedException(); }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { throw new NotSupportedException(); }
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) { throw new NotSupportedException(); }
        }


        private static void WriteToStream(Stream stream, string content) =>
            WriteToStream(stream, content, HttpRuleParser.DefaultHttpEncoding);

        private static void WriteToStream(Stream stream, string content, Encoding encoding)
        {
            const int StackallocThreshold = 1024;

            int maxLength = encoding.GetMaxByteCount(content.Length);

            byte[]? rentedBuffer = null;
            Span<byte> buffer = maxLength <= StackallocThreshold
                ? stackalloc byte[StackallocThreshold]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(maxLength));

            try
            {
                int written = encoding.GetBytes(content, buffer);
                stream.Write(buffer.Slice(0, written));
            }
            finally
            {
                if (rentedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        #endregion Serialization
    }
}
