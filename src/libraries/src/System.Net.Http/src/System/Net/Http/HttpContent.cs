// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        // These encodings have Byte-Order-Markers that we will use to detect the encoding.
        private static Encoding[] s_encodingsWithBom =
        {
            Encoding.UTF8, // EF BB BF
#if NETNative
            // Not supported on Phone
#else
            // UTF32 Must be before Unicode because its BOM is similar but longer.
            Encoding.UTF32, // FF FE 00 00
#endif
            Encoding.Unicode, // FF FE
            Encoding.BigEndianUnicode, // FE FF
        };

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
            if (Logging.On) Logging.Enter(Logging.Http, this, ".ctor", null);

            // We start with the assumption that we can calculate the content length.
            _canCalculateLength = true;

            if (Logging.On) Logging.Exit(Logging.Http, this, ".ctor", null);
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
                    }
                    catch (ArgumentException e)
                    {
                        innerTcs.TrySetException(new InvalidOperationException(SR.net_http_content_invalid_charset, e));
                        return;
                    }
                }

                // If no content encoding is listed in the ContentType HTTP header, or no Content-Type header present, 
                // then check for a byte-order-mark (BOM) in the data to figure out the encoding.
                if (encoding == null)
                {
                    byte[] preamble;
                    foreach (Encoding testEncoding in s_encodingsWithBom)
                    {
                        preamble = testEncoding.GetPreamble();
                        if (ByteArrayHasPrefix(data, dataLength, preamble))
                        {
                            encoding = testEncoding;
                            bomLength = preamble.Length;
                            break;
                        }
                    }
                }

                // Use the default encoding if we couldn't detect one.
                encoding = encoding ?? DefaultStringEncoding;

                // BOM characters may be present even if a charset was specified.
                if (bomLength == -1)
                {
                    byte[] preamble = encoding.GetPreamble();
                    if (ByteArrayHasPrefix(data, dataLength, preamble))
                        bomLength = preamble.Length;
                    else
                        bomLength = 0;
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
                throw new ArgumentNullException("stream");
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
                throw new ArgumentOutOfRangeException("maxBufferSize", maxBufferSize,
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
                            if (Logging.On) Logging.Exception(Logging.Http, this, "LoadIntoBufferAsync", e);
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
                if (Logging.On) Logging.PrintError(Logging.Http, string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_log_content_no_task_returned_copytoasync, this.GetType().ToString()));
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
