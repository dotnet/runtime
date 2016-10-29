// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public abstract class HttpContent : IDisposable
    {
        private HttpContentHeaders _headers;
        private MemoryStream _bufferedContent;
        private bool _disposed;
        private Stream _contentReadStream;
        private bool _canCalculateLength;

        internal const long MaxBufferSize = Int32.MaxValue;
        internal static readonly Encoding DefaultStringEncoding = Encoding.UTF8;

        private const int UTF8CodePage = 65001;
        private const int UTF8PreambleLength = 3;
        private const byte UTF8PreambleByte0 = 0xEF;
        private const byte UTF8PreambleByte1 = 0xBB;
        private const byte UTF8PreambleByte2 = 0xBF;
        private const int UTF8PreambleFirst2Bytes = 0xEFBB;

#if !NETNative
        // UTF32 not supported on Phone
        private const int UTF32CodePage = 12000;
        private const int UTF32PreambleLength = 4;
        private const byte UTF32PreambleByte0 = 0xFF;
        private const byte UTF32PreambleByte1 = 0xFE;
        private const byte UTF32PreambleByte2 = 0x00;
        private const byte UTF32PreambleByte3 = 0x00;
#endif
        private const int UTF32OrUnicodePreambleFirst2Bytes = 0xFFFE;

        private const int UnicodeCodePage = 1200;
        private const int UnicodePreambleLength = 2;
        private const byte UnicodePreambleByte0 = 0xFF;
        private const byte UnicodePreambleByte1 = 0xFE;

        private const int BigEndianUnicodeCodePage = 1201;
        private const int BigEndianUnicodePreambleLength = 2;
        private const byte BigEndianUnicodePreambleByte0 = 0xFE;
        private const byte BigEndianUnicodePreambleByte1 = 0xFF;
        private const int BigEndianUnicodePreambleFirst2Bytes = 0xFEFF;

#if DEBUG
        static HttpContent()
        {
            // Ensure the encoding constants used in this class match the actual data from the Encoding class
            AssertEncodingConstants(Encoding.UTF8, UTF8CodePage, UTF8PreambleLength, UTF8PreambleFirst2Bytes,
                UTF8PreambleByte0,
                UTF8PreambleByte1,
                UTF8PreambleByte2);

#if !NETNative
            // UTF32 not supported on Phone
            AssertEncodingConstants(Encoding.UTF32, UTF32CodePage, UTF32PreambleLength, UTF32OrUnicodePreambleFirst2Bytes,
                UTF32PreambleByte0,
                UTF32PreambleByte1,
                UTF32PreambleByte2,
                UTF32PreambleByte3);
#endif

            AssertEncodingConstants(Encoding.Unicode, UnicodeCodePage, UnicodePreambleLength, UTF32OrUnicodePreambleFirst2Bytes,
                UnicodePreambleByte0,
                UnicodePreambleByte1);

            AssertEncodingConstants(Encoding.BigEndianUnicode, BigEndianUnicodeCodePage, BigEndianUnicodePreambleLength, BigEndianUnicodePreambleFirst2Bytes,
                BigEndianUnicodePreambleByte0,
                BigEndianUnicodePreambleByte1);
        }

        private static void AssertEncodingConstants(Encoding encoding, int codePage, int preambleLength, int first2Bytes, params byte[] preamble)
        {
            Debug.Assert(encoding != null);
            Debug.Assert(preamble != null);

            Debug.Assert(codePage == encoding.CodePage,
                "Encoding code page mismatch for encoding: " + encoding.EncodingName,
                "Expected (constant): {0}, Actual (Encoding.CodePage): {1}", codePage, encoding.CodePage);

            byte[] actualPreamble = encoding.GetPreamble();

            Debug.Assert(preambleLength == actualPreamble.Length,
                "Encoding preamble length mismatch for encoding: " + encoding.EncodingName,
                "Expected (constant): {0}, Actual (Encoding.GetPreamble().Length): {1}", preambleLength, actualPreamble.Length);

            Debug.Assert(actualPreamble.Length >= 2);
            int actualFirst2Bytes = actualPreamble[0] << 8 | actualPreamble[1];

            Debug.Assert(first2Bytes == actualFirst2Bytes,
                "Encoding preamble first 2 bytes mismatch for encoding: " + encoding.EncodingName,
                "Expected (constant): {0}, Actual: {1}", first2Bytes, actualFirst2Bytes);

            Debug.Assert(preamble.Length == actualPreamble.Length,
                "Encoding preamble mismatch for encoding: " + encoding.EncodingName,
                "Expected (constant): {0}, Actual (Encoding.GetPreamble()): {1}",
                BitConverter.ToString(preamble),
                BitConverter.ToString(actualPreamble));

            for (int i = 0; i < preamble.Length; i++)
            {
                Debug.Assert(preamble[i] == actualPreamble[i],
                    "Encoding preamble mismatch for encoding: " + encoding.EncodingName,
                    "Expected (constant): {0}, Actual (Encoding.GetPreamble()): {1}",
                    BitConverter.ToString(preamble),
                    BitConverter.ToString(actualPreamble));
            }
        }
#endif

        public HttpContentHeaders Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new HttpContentHeaders(GetComputedOrBufferLength);
                }
                return _headers;
            }
        }

        private bool IsBuffered
        {
            get { return _bufferedContent != null; }
        }

        internal void SetBuffer(byte[] buffer, int offset, int count)
        {
            _bufferedContent = new MemoryStream(buffer, offset, count, writable: false, publiclyVisible: true);
        }

        internal bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
#if NET46
            buffer = default(ArraySegment<byte>);
#endif
            return _bufferedContent != null && _bufferedContent.TryGetBuffer(out buffer);
        }

        protected HttpContent()
        {
            // Log to get an ID for the current content. This ID is used when the content gets associated to a message.
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // We start with the assumption that we can calculate the content length.
            _canCalculateLength = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public Task<string> ReadAsStringAsync()
        {
            CheckDisposed();
            return WaitAndReturnAsync(LoadIntoBufferAsync(), this, s => s.ReadBufferedContentAsString());
        }

        internal string ReadBufferedContentAsString()
        {
            Debug.Assert(IsBuffered);

            if (_bufferedContent.Length == 0)
            {
                return string.Empty;
            }

            // We don't validate the Content-Encoding header: If the content was encoded, it's the caller's 
            // responsibility to make sure to only call ReadAsString() on already decoded content. E.g. if the 
            // Content-Encoding is 'gzip' the user should set HttpClientHandler.AutomaticDecompression to get a 
            // decoded response stream.

            Encoding encoding = null;
            int bomLength = -1;

            ArraySegment<byte> buffer;
            if (!TryGetBuffer(out buffer))
            {
                buffer = new ArraySegment<byte>(_bufferedContent.ToArray());
            }

            // If we do have encoding information in the 'Content-Type' header, use that information to convert
            // the content to a string.
            if ((Headers.ContentType != null) && (Headers.ContentType.CharSet != null))
            {
                try
                {
                    encoding = Encoding.GetEncoding(Headers.ContentType.CharSet);

                    // Byte-order-mark (BOM) characters may be present even if a charset was specified.
                    bomLength = GetPreambleLength(buffer, encoding);
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException(SR.net_http_content_invalid_charset, e);
                }
            }

            // If no content encoding is listed in the ContentType HTTP header, or no Content-Type header present, 
            // then check for a BOM in the data to figure out the encoding.
            if (encoding == null)
            {
                if (!TryDetectEncoding(buffer, out encoding, out bomLength))
                {
                    // Use the default encoding (UTF8) if we couldn't detect one.
                    encoding = DefaultStringEncoding;

                    // We already checked to see if the data had a UTF8 BOM in TryDetectEncoding
                    // and DefaultStringEncoding is UTF8, so the bomLength is 0.
                    bomLength = 0;
                }
            }

            // Drop the BOM when decoding the data.
            return encoding.GetString(buffer.Array, buffer.Offset + bomLength, buffer.Count - bomLength);
        }

        public Task<byte[]> ReadAsByteArrayAsync()
        {
            CheckDisposed();
            return WaitAndReturnAsync(LoadIntoBufferAsync(), this, s => s.ReadBufferedContentAsByteArray());
        }

        internal byte[] ReadBufferedContentAsByteArray()
        {
            // The returned array is exposed out of the library, so use ToArray rather 
            // than TryGetBuffer in order to make a copy.
            return _bufferedContent.ToArray(); 
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            CheckDisposed();

            ArraySegment<byte> buffer;
            if (_contentReadStream == null && TryGetBuffer(out buffer))
            {
                _contentReadStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, writable: false);
            }

            return _contentReadStream != null ?
                Task.FromResult(_contentReadStream) :
                ReadAsStreamAsyncCore(CreateContentReadStreamAsync());
        }

        private async Task<Stream> ReadAsStreamAsyncCore(Task<Stream> createContentStreamTask)
        {
            _contentReadStream = await createContentStreamTask.ConfigureAwait(false);
            return _contentReadStream;
        }

        protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext context);

        public Task CopyToAsync(Stream stream, TransportContext context)
        {
            CheckDisposed();
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                Task task = null;
                ArraySegment<byte> buffer;
                if (TryGetBuffer(out buffer))
                {
                    task = stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
                }
                else
                {
                    task = SerializeToStreamAsync(stream, context);
                    CheckTaskNotNull(task);
                }

                return CopyToAsyncCore(task);
            }
            catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
            {
                return Task.FromException(GetStreamCopyException(e));
            }
        }

        private static async Task CopyToAsyncCore(Task copyTask)
        {
            try
            {
                await copyTask.ConfigureAwait(false);
            }
            catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
            {
                throw GetStreamCopyException(e);
            }
        }

        public Task CopyToAsync(Stream stream)
        {
            return CopyToAsync(stream, null);
        }

        public Task LoadIntoBufferAsync()
        {
            return LoadIntoBufferAsync(MaxBufferSize);
        }

        // No "CancellationToken" parameter needed since canceling the CTS will close the connection, resulting
        // in an exception being thrown while we're buffering.
        // If buffering is used without a connection, it is supposed to be fast, thus no cancellation required.
        public Task LoadIntoBufferAsync(long maxBufferSize)
        {
            CheckDisposed();
            if (maxBufferSize > HttpContent.MaxBufferSize)
            {
                // This should only be hit when called directly; HttpClient/HttpClientHandler 
                // will not exceed this limit.
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize), maxBufferSize,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    SR.net_http_content_buffersize_limit, HttpContent.MaxBufferSize));
            }

            if (IsBuffered)
            {
                // If we already buffered the content, just return a completed task.
                return Task.CompletedTask;
            }

            Exception error = null;
            MemoryStream tempBuffer = CreateMemoryStream(maxBufferSize, out error);
            if (tempBuffer == null)
            {
                // We don't throw in LoadIntoBufferAsync(): return a faulted task.
                return Task.FromException(error);
            }

            try
            {
                Task task = SerializeToStreamAsync(tempBuffer, null);
                CheckTaskNotNull(task);
                return LoadIntoBufferAsyncCore(task, tempBuffer);
            }
            catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
            {
                return Task.FromException(GetStreamCopyException(e));
            }
            // other synchronous exceptions from SerializeToStreamAsync/CheckTaskNotNull will propagate
        }

        private async Task LoadIntoBufferAsyncCore(Task serializeToStreamTask, MemoryStream tempBuffer)
        {
            try
            {
                await serializeToStreamTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tempBuffer.Dispose(); // Cleanup partially filled stream.
                Exception we = GetStreamCopyException(e);
                if (we != e) throw we;
                throw;
            }

            try
            {
                tempBuffer.Seek(0, SeekOrigin.Begin); // Rewind after writing data.
                _bufferedContent = tempBuffer;
            }
            catch (Exception e)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, e);
                throw;
            }
        }

        protected virtual Task<Stream> CreateContentReadStreamAsync()
        {
            // By default just buffer the content to a memory stream. Derived classes can override this behavior
            // if there is a better way to retrieve the content as stream (e.g. byte array/string use a more efficient
            // way, like wrapping a read-only MemoryStream around the bytes/string)
            return WaitAndReturnAsync(LoadIntoBufferAsync(), this, s => (Stream)s._bufferedContent);
        }

        // Derived types return true if they're able to compute the length. It's OK if derived types return false to
        // indicate that they're not able to compute the length. The transport channel needs to decide what to do in
        // that case (send chunked, buffer first, etc.).
        protected internal abstract bool TryComputeLength(out long length);

        private long? GetComputedOrBufferLength()
        {
            CheckDisposed();

            if (IsBuffered)
            {
                return _bufferedContent.Length;
            }

            // If we already tried to calculate the length, but the derived class returned 'false', then don't try
            // again; just return null.
            if (_canCalculateLength)
            {
                long length = 0;
                if (TryComputeLength(out length))
                {
                    return length;
                }

                // Set flag to make sure next time we don't try to compute the length, since we know that we're unable
                // to do so.
                _canCalculateLength = false;
            }
            return null;
        }

        private MemoryStream CreateMemoryStream(long maxBufferSize, out Exception error)
        {
            Contract.Ensures((Contract.Result<MemoryStream>() != null) ||
                (Contract.ValueAtReturn<Exception>(out error) != null));

            error = null;

            // If we have a Content-Length allocate the right amount of buffer up-front. Also check whether the
            // content length exceeds the max. buffer size.
            long? contentLength = Headers.ContentLength;

            if (contentLength != null)
            {
                Debug.Assert(contentLength >= 0);

                if (contentLength > maxBufferSize)
                {
                    error = new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_content_buffersize_exceeded, maxBufferSize));
                    return null;
                }

                // We can safely cast contentLength to (int) since we just checked that it is <= maxBufferSize.
                return new LimitMemoryStream((int)maxBufferSize, (int)contentLength);
            }

            // We couldn't determine the length of the buffer. Create a memory stream with an empty buffer.
            return new LimitMemoryStream((int)maxBufferSize, 0);
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (_contentReadStream != null)
                {
                    _contentReadStream.Dispose();
                }

                if (IsBuffered)
                {
                    _bufferedContent.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helpers

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        private void CheckTaskNotNull(Task task)
        {
            if (task == null)
            {
                var e = new InvalidOperationException(SR.net_http_content_no_task_returned);
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, e);
                throw e;
            }
        }

        private static bool StreamCopyExceptionNeedsWrapping(Exception e) => e is IOException || e is ObjectDisposedException;

        private static Exception GetStreamCopyException(Exception originalException)
        {
            // HttpContent derived types should throw HttpRequestExceptions if there is an error. However, since the stream
            // provided by CopyToAsync() can also throw, we wrap such exceptions in HttpRequestException. This way custom content
            // types don't have to worry about it. The goal is that users of HttpContent don't have to catch multiple
            // exceptions (depending on the underlying transport), but just HttpRequestExceptions
            // Custom stream should throw either IOException or HttpRequestException.
            // We don't want to wrap other exceptions thrown by Stream (e.g. InvalidOperationException), since we
            // don't want to hide such "usage error" exceptions in HttpRequestException.
            // ObjectDisposedException is also wrapped, since aborting HWR after a request is complete will result in
            // the response stream being closed.
            return StreamCopyExceptionNeedsWrapping(originalException) ?
                new HttpRequestException(SR.net_http_content_stream_copy_error, originalException) :
                originalException;
        }

        private static int GetPreambleLength(ArraySegment<byte> buffer, Encoding encoding)
        {
            byte[] data = buffer.Array;
            int offset = buffer.Offset;
            int dataLength = buffer.Count;

            Debug.Assert(data != null);
            Debug.Assert(encoding != null);

            switch (encoding.CodePage)
            {
                case UTF8CodePage:
                    return (dataLength >= UTF8PreambleLength
                        && data[offset + 0] == UTF8PreambleByte0
                        && data[offset + 1] == UTF8PreambleByte1
                        && data[offset + 2] == UTF8PreambleByte2) ? UTF8PreambleLength : 0;
#if !NETNative
                // UTF32 not supported on Phone
                case UTF32CodePage:
                    return (dataLength >= UTF32PreambleLength
                        && data[offset + 0] == UTF32PreambleByte0
                        && data[offset + 1] == UTF32PreambleByte1
                        && data[offset + 2] == UTF32PreambleByte2
                        && data[offset + 3] == UTF32PreambleByte3) ? UTF32PreambleLength : 0;
#endif
                case UnicodeCodePage:
                    return (dataLength >= UnicodePreambleLength
                        && data[offset + 0] == UnicodePreambleByte0
                        && data[offset + 1] == UnicodePreambleByte1) ? UnicodePreambleLength : 0;

                case BigEndianUnicodeCodePage:
                    return (dataLength >= BigEndianUnicodePreambleLength
                        && data[offset + 0] == BigEndianUnicodePreambleByte0
                        && data[offset + 1] == BigEndianUnicodePreambleByte1) ? BigEndianUnicodePreambleLength : 0;

                default:
                    byte[] preamble = encoding.GetPreamble();
                    return BufferHasPrefix(buffer, preamble) ? preamble.Length : 0;
            }
        }

        private static bool TryDetectEncoding(ArraySegment<byte> buffer, out Encoding encoding, out int preambleLength)
        {
            byte[] data = buffer.Array;
            int offset = buffer.Offset;
            int dataLength = buffer.Count;

            Debug.Assert(data != null);

            if (dataLength >= 2)
            {
                int first2Bytes = data[offset + 0] << 8 | data[offset + 1];

                switch (first2Bytes)
                {
                    case UTF8PreambleFirst2Bytes:
                        if (dataLength >= UTF8PreambleLength && data[offset + 2] == UTF8PreambleByte2)
                        {
                            encoding = Encoding.UTF8;
                            preambleLength = UTF8PreambleLength;
                            return true;
                        }
                        break;

                    case UTF32OrUnicodePreambleFirst2Bytes:
#if !NETNative
                        // UTF32 not supported on Phone
                        if (dataLength >= UTF32PreambleLength && data[offset + 2] == UTF32PreambleByte2 && data[offset + 3] == UTF32PreambleByte3)
                        {
                            encoding = Encoding.UTF32;
                            preambleLength = UTF32PreambleLength;
                        }
                        else
#endif
                        {
                            encoding = Encoding.Unicode;
                            preambleLength = UnicodePreambleLength;
                        }
                        return true;

                    case BigEndianUnicodePreambleFirst2Bytes:
                        encoding = Encoding.BigEndianUnicode;
                        preambleLength = BigEndianUnicodePreambleLength;
                        return true;
                }
            }

            encoding = null;
            preambleLength = 0;
            return false;
        }

        private static bool BufferHasPrefix(ArraySegment<byte> buffer, byte[] prefix)
        {
            byte[] byteArray = buffer.Array;
            if (prefix == null || byteArray == null || prefix.Length > buffer.Count || prefix.Length == 0)
                return false;

            for (int i = 0, j = buffer.Offset; i < prefix.Length; i++, j++)
            {
                if (prefix[i] != byteArray[j])
                    return false;
            }

            return true;
        }

        #endregion Helpers

        private static async Task<TResult> WaitAndReturnAsync<TState, TResult>(Task waitTask, TState state, Func<TState, TResult> returnFunc)
        {
            await waitTask.ConfigureAwait(false);
            return returnFunc(state);
        }

        internal sealed class LimitMemoryStream : MemoryStream
        {
            private readonly int _maxSize;

            public LimitMemoryStream(int maxSize, int capacity)
                : base(capacity)
            {
                Debug.Assert(capacity <= maxSize);
                _maxSize = maxSize;
            }

            public int MaxSize => _maxSize;

            public override void Write(byte[] buffer, int offset, int count)
            {
                CheckSize(count);
                base.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                CheckSize(1);
                base.WriteByte(value);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                CheckSize(count);
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            private void CheckSize(int countToAdd)
            {
                if (_maxSize - Length < countToAdd)
                {
                    throw new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_content_buffersize_exceeded, _maxSize));
                }
            }
        }
    }
}
