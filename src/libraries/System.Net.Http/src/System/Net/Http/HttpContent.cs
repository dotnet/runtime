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

#if NETNative        
        internal void SetBuffer(byte[] buffer, int offset, int count)
        {
            _bufferedContent = new MemoryStream(buffer, offset, count, false, true);
        }
        
        internal bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            if (_bufferedContent == null)
            {
                return false;
            }
            
            return _bufferedContent.TryGetBuffer(out buffer);
        }
#endif
     
        protected HttpContent()
        {
            // Log to get an ID for the current content. This ID is used when the content gets associated to a message.
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Http, this, ".ctor", null);

            // We start with the assumption that we can calculate the content length.
            _canCalculateLength = true;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Http, this, ".ctor", null);
        }

        public Task<string> ReadAsStringAsync()
        {
            CheckDisposed();

            var tcs = new TaskCompletionSource<string>(this);

            LoadIntoBufferAsync().ContinueWithStandard(tcs, (task, state) =>
            {
                var innerTcs = (TaskCompletionSource<string>)state;
                var innerThis = (HttpContent)innerTcs.Task.AsyncState;
                if (HttpUtilities.HandleFaultsAndCancelation(task, innerTcs))
                {
                    return;
                }

                if (innerThis._bufferedContent.Length == 0)
                {
                    innerTcs.TrySetResult(string.Empty);
                    return;
                }

                // We don't validate the Content-Encoding header: If the content was encoded, it's the caller's 
                // responsibility to make sure to only call ReadAsString() on already decoded content. E.g. if the 
                // Content-Encoding is 'gzip' the user should set HttpClientHandler.AutomaticDecompression to get a 
                // decoded response stream.

                Encoding encoding = null;
                int bomLength = -1;

                byte[] data = innerThis.GetDataBuffer(innerThis._bufferedContent);

                int dataLength = (int)innerThis._bufferedContent.Length; // Data is the raw buffer, it may not be full.

                // If we do have encoding information in the 'Content-Type' header, use that information to convert
                // the content to a string.
                if ((innerThis.Headers.ContentType != null) && (innerThis.Headers.ContentType.CharSet != null))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(innerThis.Headers.ContentType.CharSet);

                        // Byte-order-mark (BOM) characters may be present even if a charset was specified.
                        bomLength = GetPreambleLength(data, dataLength, encoding);
                    }
                    catch (ArgumentException e)
                    {
                        innerTcs.TrySetException(new InvalidOperationException(SR.net_http_content_invalid_charset, e));
                        return;
                    }
                }

                // If no content encoding is listed in the ContentType HTTP header, or no Content-Type header present, 
                // then check for a BOM in the data to figure out the encoding.
                if (encoding == null)
                {
                    if (!TryDetectEncoding(data, dataLength, out encoding, out bomLength))
                    {
                        // Use the default encoding (UTF8) if we couldn't detect one.
                        encoding = DefaultStringEncoding;

                        // We already checked to see if the data had a UTF8 BOM in TryDetectEncoding
                        // and DefaultStringEncoding is UTF8, so the bomLength is 0.
                        bomLength = 0;
                    }
                }

                try
                {
                    // Drop the BOM when decoding the data.
                    string result = encoding.GetString(data, bomLength, dataLength - bomLength);
                    innerTcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    innerTcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task<byte[]> ReadAsByteArrayAsync()
        {
            CheckDisposed();

            var tcs = new TaskCompletionSource<byte[]>(this);

            LoadIntoBufferAsync().ContinueWithStandard(tcs, (task, state) =>
            {
                var innerTcs = (TaskCompletionSource<byte[]>)state;
                var innerThis = (HttpContent)innerTcs.Task.AsyncState;
                if (!HttpUtilities.HandleFaultsAndCancelation(task, innerTcs))
                {
                    innerTcs.TrySetResult(innerThis._bufferedContent.ToArray());
                }
            });

            return tcs.Task;
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            CheckDisposed();

            TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>(this);

            if (_contentReadStream == null && IsBuffered)
            {
                byte[] data = this.GetDataBuffer(_bufferedContent);

                // We can cast bufferedContent.Length to 'int' since the length will always be in the 'int' range
                // The .NET Framework doesn't support array lengths > int.MaxValue.
                Debug.Assert(_bufferedContent.Length <= (long)int.MaxValue);
                _contentReadStream = new MemoryStream(data, 0,
                    (int)_bufferedContent.Length, false);
            }

            if (_contentReadStream != null)
            {
                tcs.TrySetResult(_contentReadStream);
                return tcs.Task;
            }

            CreateContentReadStreamAsync().ContinueWithStandard(tcs, (task, state) =>
            {
                var innerTcs = (TaskCompletionSource<Stream>)state;
                var innerThis = (HttpContent)innerTcs.Task.AsyncState;
                if (!HttpUtilities.HandleFaultsAndCancelation(task, innerTcs))
                {
                    innerThis._contentReadStream = task.Result;
                    innerTcs.TrySetResult(innerThis._contentReadStream);
                }
            });

            return tcs.Task;
        }

        protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext context);

        public Task CopyToAsync(Stream stream, TransportContext context)
        {
            CheckDisposed();
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            try
            {
                Task task = null;
                if (IsBuffered)
                {
                    byte[] data = this.GetDataBuffer(_bufferedContent);
                    task = stream.WriteAsync(data, 0, (int)_bufferedContent.Length);
                }
                else
                {
                    task = SerializeToStreamAsync(stream, context);
                    CheckTaskNotNull(task);
                }

                // If the copy operation fails, wrap the exception in an HttpRequestException() if appropriate.
                task.ContinueWithStandard(tcs, (copyTask, state) =>
                {
                    var innerTcs = (TaskCompletionSource<object>)state;
                    if (copyTask.IsFaulted)
                    {
                        innerTcs.TrySetException(GetStreamCopyException(copyTask.Exception.GetBaseException()));
                    }
                    else if (copyTask.IsCanceled)
                    {
                        innerTcs.TrySetCanceled();
                    }
                    else
                    {
                        innerTcs.TrySetResult(null);
                    }
                });
            }
            catch (IOException e)
            {
                tcs.TrySetException(GetStreamCopyException(e));
            }
            catch (ObjectDisposedException e)
            {
                tcs.TrySetException(GetStreamCopyException(e));
            }

            return tcs.Task;
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
                return CreateCompletedTask();
            }

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            Exception error = null;
            MemoryStream tempBuffer = CreateMemoryStream(maxBufferSize, out error);

            if (tempBuffer == null)
            {
                // We don't throw in LoadIntoBufferAsync(): set the task as faulted and return the task.
                Debug.Assert(error != null);
                tcs.TrySetException(error);
            }
            else
            {
                try
                {
                    Task task = SerializeToStreamAsync(tempBuffer, null);
                    CheckTaskNotNull(task);

                    task.ContinueWithStandard(copyTask =>
                    {
                        try
                        {
                            if (copyTask.IsFaulted)
                            {
                                tempBuffer.Dispose(); // Cleanup partially filled stream.
                                tcs.TrySetException(GetStreamCopyException(copyTask.Exception.GetBaseException()));
                                return;
                            }

                            if (copyTask.IsCanceled)
                            {
                                tempBuffer.Dispose(); // Cleanup partially filled stream.
                                tcs.TrySetCanceled();
                                return;
                            }

                            tempBuffer.Seek(0, SeekOrigin.Begin); // Rewind after writing data.
                            _bufferedContent = tempBuffer;
                            tcs.TrySetResult(null);
                        }
                        catch (Exception e)
                        {
                            // Make sure we catch any exception, otherwise the task will catch it and throw in the finalizer.
                            tcs.TrySetException(e);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.Http, this, "LoadIntoBufferAsync", e);
                        }
                    });
                }
                catch (IOException e)
                {
                    tcs.TrySetException(GetStreamCopyException(e));
                }
                catch (ObjectDisposedException e)
                {
                    tcs.TrySetException(GetStreamCopyException(e));
                }
            }

            return tcs.Task;
        }

        protected virtual Task<Stream> CreateContentReadStreamAsync()
        {
            var tcs = new TaskCompletionSource<Stream>(this);
            // By default just buffer the content to a memory stream. Derived classes can override this behavior
            // if there is a better way to retrieve the content as stream (e.g. byte array/string use a more efficient
            // way, like wrapping a read-only MemoryStream around the bytes/string)
            LoadIntoBufferAsync().ContinueWithStandard(tcs, (task, state) =>
            {
                var innerTcs = (TaskCompletionSource<Stream>)state;
                var innerThis = (HttpContent)innerTcs.Task.AsyncState;
                if (!HttpUtilities.HandleFaultsAndCancelation(task, innerTcs))
                {
                    innerTcs.TrySetResult(innerThis._bufferedContent);
                }
            });

            return tcs.Task;
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

        private byte[] GetDataBuffer(MemoryStream stream)
        {
            // TODO: Use TryGetBuffer() instead of ToArray().
            return stream.ToArray();
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.Http, string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_log_content_no_task_returned_copytoasync, this.GetType().ToString()));
                throw new InvalidOperationException(SR.net_http_content_no_task_returned);
            }
        }

        private static Task CreateCompletedTask()
        {
            TaskCompletionSource<object> completed = new TaskCompletionSource<object>();
            bool resultSet = completed.TrySetResult(null);
            Debug.Assert(resultSet, "Can't set Task as completed.");
            return completed.Task;
        }

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
            Exception result = originalException;
            if ((result is IOException) || (result is ObjectDisposedException))
            {
                result = new HttpRequestException(SR.net_http_content_stream_copy_error, result);
            }
            return result;
        }

        private static int GetPreambleLength(byte[] data, int dataLength, Encoding encoding)
        {
            Debug.Assert(data != null);
            Debug.Assert(dataLength <= data.Length);
            Debug.Assert(encoding != null);

            switch (encoding.CodePage)
            {
                case UTF8CodePage:
                    return (dataLength >= UTF8PreambleLength
                        && data[0] == UTF8PreambleByte0
                        && data[1] == UTF8PreambleByte1
                        && data[2] == UTF8PreambleByte2) ? UTF8PreambleLength : 0;
#if !NETNative
                // UTF32 not supported on Phone
                case UTF32CodePage:
                    return (dataLength >= UTF32PreambleLength
                        && data[0] == UTF32PreambleByte0
                        && data[1] == UTF32PreambleByte1
                        && data[2] == UTF32PreambleByte2
                        && data[3] == UTF32PreambleByte3) ? UTF32PreambleLength : 0;
#endif
                case UnicodeCodePage:
                    return (dataLength >= UnicodePreambleLength
                        && data[0] == UnicodePreambleByte0
                        && data[1] == UnicodePreambleByte1) ? UnicodePreambleLength : 0;

                case BigEndianUnicodeCodePage:
                    return (dataLength >= BigEndianUnicodePreambleLength
                        && data[0] == BigEndianUnicodePreambleByte0
                        && data[1] == BigEndianUnicodePreambleByte1) ? BigEndianUnicodePreambleLength : 0;

                default:
                    byte[] preamble = encoding.GetPreamble();
                    return ByteArrayHasPrefix(data, dataLength, preamble) ? preamble.Length : 0;
            }
        }

        private static bool TryDetectEncoding(byte[] data, int dataLength, out Encoding encoding, out int preambleLength)
        {
            Debug.Assert(data != null);
            Debug.Assert(dataLength <= data.Length);

            if (dataLength >= 2)
            {
                int first2Bytes = data[0] << 8 | data[1];

                switch (first2Bytes)
                {
                    case UTF8PreambleFirst2Bytes:
                        if (dataLength >= UTF8PreambleLength && data[2] == UTF8PreambleByte2)
                        {
                            encoding = Encoding.UTF8;
                            preambleLength = UTF8PreambleLength;
                            return true;
                        }
                        break;

                    case UTF32OrUnicodePreambleFirst2Bytes:
#if !NETNative
                        // UTF32 not supported on Phone
                        if (dataLength >= UTF32PreambleLength && data[2] == UTF32PreambleByte2 && data[3] == UTF32PreambleByte3)
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

        private static bool ByteArrayHasPrefix(byte[] byteArray, int dataLength, byte[] prefix)
        {
            if (prefix == null || byteArray == null || prefix.Length > dataLength || prefix.Length == 0)
                return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] != byteArray[i])
                    return false;
            }
            return true;
        }

        #endregion Helpers

        private class LimitMemoryStream : MemoryStream
        {
            private int _maxSize;

            public LimitMemoryStream(int maxSize, int capacity)
                : base(capacity)
            {
                _maxSize = maxSize;
            }

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
