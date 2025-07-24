// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public abstract class HttpContent : IDisposable
    {
        private HttpContentHeaders? _headers;
        private LimitArrayPoolWriteStream? _bufferedContent;
        private object? _contentReadStream; // Stream or Task<Stream>
        private bool _disposed;
        private bool _canCalculateLength;

        internal const int MaxBufferSize = int.MaxValue;
        internal static readonly Encoding DefaultStringEncoding = Encoding.UTF8;

        private const int UTF8CodePage = 65001;
        private const int UTF32CodePage = 12000;
        private const int UnicodeCodePage = 1200;
        private const int BigEndianUnicodeCodePage = 1201;

        private static ReadOnlySpan<byte> UTF8Preamble => [0xEF, 0xBB, 0xBF];
        private static ReadOnlySpan<byte> UTF32Preamble => [0xFF, 0xFE, 0x00, 0x00];
        private static ReadOnlySpan<byte> UnicodePreamble => [0xFF, 0xFE];
        private static ReadOnlySpan<byte> BigEndianUnicodePreamble => [0xFE, 0xFF];

#if DEBUG
        static HttpContent()
        {
            // Ensure the encoding constants used in this class match the actual data from the Encoding class
            AssertEncodingConstants(Encoding.UTF8, UTF8CodePage, UTF8Preamble);

            // UTF32 not supported on Phone
            AssertEncodingConstants(Encoding.UTF32, UTF32CodePage, UTF32Preamble);

            AssertEncodingConstants(Encoding.Unicode, UnicodeCodePage, UnicodePreamble);

            AssertEncodingConstants(Encoding.BigEndianUnicode, BigEndianUnicodeCodePage, BigEndianUnicodePreamble);
        }

        private static void AssertEncodingConstants(Encoding encoding, int codePage, ReadOnlySpan<byte> preamble)
        {
            Debug.Assert(encoding != null);

            Debug.Assert(codePage == encoding.CodePage,
                $"Encoding code page mismatch for encoding: {encoding.EncodingName}",
                $"Expected (constant): {codePage}, Actual (Encoding.CodePage): {encoding.CodePage}");

            byte[] actualPreamble = encoding.GetPreamble();

            Debug.Assert(preamble.SequenceEqual(actualPreamble),
                $"Encoding preamble mismatch for encoding: {encoding.EncodingName}",
                $"Expected (constant): {BitConverter.ToString(preamble.ToArray())}, Actual (Encoding.GetPreamble()): {BitConverter.ToString(actualPreamble)}");
        }
#endif

        public HttpContentHeaders Headers => _headers ??= new HttpContentHeaders(this);

        [MemberNotNullWhen(true, nameof(_bufferedContent))]
        private bool IsBuffered => _bufferedContent is not null;

        protected HttpContent()
        {
            // Log to get an ID for the current content. This ID is used when the content gets associated to a message.
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            // We start with the assumption that we can calculate the content length.
            _canCalculateLength = true;
        }

        private MemoryStream CreateMemoryStreamFromBufferedContent()
        {
            Debug.Assert(IsBuffered);
            return new MemoryStream(_bufferedContent.GetSingleBuffer(), 0, (int)_bufferedContent.Length, writable: false);
        }

        public Task<string> ReadAsStringAsync() =>
            ReadAsStringAsync(CancellationToken.None);

        public Task<string> ReadAsStringAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();
            return WaitAndReturnAsync(LoadIntoBufferAsync(cancellationToken), this, static s => s.ReadBufferedContentAsString());
        }

        private string ReadBufferedContentAsString()
        {
            Debug.Assert(IsBuffered);

            return ReadBufferAsString(_bufferedContent, Headers);
        }

        internal static string ReadBufferAsString(LimitArrayPoolWriteStream stream, HttpContentHeaders headers)
        {
            if (stream.Length == 0)
            {
                return string.Empty;
            }

            // We don't validate the Content-Encoding header: If the content was encoded, it's the caller's
            // responsibility to make sure to only call ReadAsString() on already decoded content. E.g. if the
            // Content-Encoding is 'gzip' the user should set HttpClientHandler.AutomaticDecompression to get a
            // decoded response stream.

            ReadOnlySpan<byte> firstBuffer = stream.GetFirstBuffer();
            Debug.Assert(firstBuffer.Length >= 4 || firstBuffer.Length == stream.Length);

            Encoding? encoding = null;
            int bomLength = -1;

            string? charset = headers.ContentType?.CharSet;

            // If we do have encoding information in the 'Content-Type' header, use that information to convert
            // the content to a string.
            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 &&
                        charset.StartsWith('\"') &&
                        charset.EndsWith('\"'))
                    {
                        encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }

                    // Byte-order-mark (BOM) characters may be present even if a charset was specified.
                    bomLength = GetPreambleLength(firstBuffer, encoding);
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
                if (!TryDetectEncoding(firstBuffer, out encoding, out bomLength))
                {
                    // Use the default encoding (UTF8) if we couldn't detect one.
                    encoding = DefaultStringEncoding;

                    // We already checked to see if the data had a UTF8 BOM in TryDetectEncoding
                    // and DefaultStringEncoding is UTF8, so the bomLength is 0.
                    bomLength = 0;
                }
            }

            // Drop the BOM when decoding the data.

            if (firstBuffer.Length == stream.Length)
            {
                return encoding.GetString(firstBuffer[bomLength..]);
            }
            else
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.CopyToCore(buffer);

                string result = encoding.GetString(buffer.AsSpan(0, (int)stream.Length)[bomLength..]);

                ArrayPool<byte>.Shared.Return(buffer);
                return result;
            }
        }

        public Task<byte[]> ReadAsByteArrayAsync() =>
            ReadAsByteArrayAsync(CancellationToken.None);

        public Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();
            return WaitAndReturnAsync(LoadIntoBufferAsync(cancellationToken), this, static s => s.ReadBufferedContentAsByteArray());
        }

        internal byte[] ReadBufferedContentAsByteArray()
        {
            Debug.Assert(_bufferedContent != null);
            // The returned array is exposed out of the library, so use CreateCopy rather than GetSingleBuffer.
            return _bufferedContent.CreateCopy();
        }

        public Stream ReadAsStream() =>
            ReadAsStream(CancellationToken.None);

        public Stream ReadAsStream(CancellationToken cancellationToken)
        {
            CheckDisposed();

            // _contentReadStream will be either null (nothing yet initialized), a Stream (it was previously
            // initialized in TryReadAsStream/ReadAsStream), or a Task<Stream> (it was previously initialized
            // in ReadAsStreamAsync).

            if (_contentReadStream == null) // don't yet have a Stream
            {
                Stream s = IsBuffered ?
                    CreateMemoryStreamFromBufferedContent() :
                    CreateContentReadStream(cancellationToken);
                _contentReadStream = s;
                return s;
            }
            else if (_contentReadStream is Stream stream) // have a Stream
            {
                return stream;
            }
            else // have a Task<Stream>
            {
                // Throw if ReadAsStreamAsync has been called previously since _contentReadStream contains a cached task.
                throw new HttpRequestException(SR.net_http_content_read_as_stream_has_task);
            }
        }

        public Task<Stream> ReadAsStreamAsync() =>
            ReadAsStreamAsync(CancellationToken.None);

        public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();

            // _contentReadStream will be either null (nothing yet initialized), a Stream (it was previously
            // initialized in TryReadAsStream/ReadAsStream), or a Task<Stream> (it was previously initialized here
            // in ReadAsStreamAsync).

            if (_contentReadStream == null) // don't yet have a Stream
            {
                Task<Stream> t = IsBuffered ?
                    Task.FromResult<Stream>(CreateMemoryStreamFromBufferedContent()) :
                    CreateContentReadStreamAsync(cancellationToken);
                _contentReadStream = t;
                return t;
            }
            else if (_contentReadStream is Task<Stream> t) // have a Task<Stream>
            {
                return t;
            }
            else
            {
                Debug.Assert(_contentReadStream is Stream, $"Expected a Stream, got ${_contentReadStream}");
                Task<Stream> ts = Task.FromResult((Stream)_contentReadStream);
                _contentReadStream = ts;
                return ts;
            }
        }

        internal Stream? TryReadAsStream()
        {
            CheckDisposed();

            // _contentReadStream will be either null (nothing yet initialized), a Stream (it was previously
            // initialized in TryReadAsStream/ReadAsStream), or a Task<Stream> (it was previously initialized here
            // in ReadAsStreamAsync).

            if (_contentReadStream == null) // don't yet have a Stream
            {
                Stream? s = IsBuffered ?
                    CreateMemoryStreamFromBufferedContent() :
                    TryCreateContentReadStream();
                _contentReadStream = s;
                return s;
            }
            else if (_contentReadStream is Stream s) // have a Stream
            {
                return s;
            }
            else // have a Task<Stream>
            {
                Debug.Assert(_contentReadStream is Task<Stream>, $"Expected a Task<Stream>, got ${_contentReadStream}");
                Task<Stream> t = (Task<Stream>)_contentReadStream;
                return t.Status == TaskStatus.RanToCompletion ? t.Result : null;
            }
        }

        protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext? context);

        // We cannot add abstract member to a public class in order to not to break already established contract of this class.
        // So we add virtual method, override it everywhere internally and provide proper implementation.
        // Unfortunately we cannot force everyone to implement so in such case we throw NSE.
        protected virtual void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException(SR.Format(SR.net_http_missing_sync_implementation, GetType(), nameof(HttpContent), nameof(SerializeToStream)));
        }

        protected virtual Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            SerializeToStreamAsync(stream, context);

        // TODO https://github.com/dotnet/runtime/issues/31316: Expose something to enable this publicly.  For very specific
        // HTTP/2 scenarios (e.g. gRPC), we need to be able to allow request content to continue sending after SendAsync has
        // completed, which goes against the previous design of content, and which means that with some servers, even outside
        // of desired scenarios we could end up unexpectedly having request content still sending even after the response
        // completes, which could lead to spurious failures in unsuspecting client code.  To mitigate that, we prohibit duplex
        // on all known HttpContent types, waiting for the request content to complete before completing the SendAsync task.
        internal virtual bool AllowDuplex => true;

        public void CopyTo(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            CheckDisposed();
            ArgumentNullException.ThrowIfNull(stream);
            try
            {
                if (IsBuffered)
                {
                    stream.Write(_bufferedContent.GetSingleBuffer(), 0, (int)_bufferedContent.Length);
                }
                else
                {
                    SerializeToStream(stream, context, cancellationToken);
                }
            }
            catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
            {
                throw GetStreamCopyException(e);
            }
        }

        public Task CopyToAsync(Stream stream) =>
            CopyToAsync(stream, CancellationToken.None);

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken) =>
            CopyToAsync(stream, null, cancellationToken);

        public Task CopyToAsync(Stream stream, TransportContext? context) =>
            CopyToAsync(stream, context, CancellationToken.None);

        public Task CopyToAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            CheckDisposed();
            ArgumentNullException.ThrowIfNull(stream);
            try
            {
                return WaitAsync(InternalCopyToAsync(stream, context, cancellationToken));
            }
            catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
            {
                return Task.FromException(GetStreamCopyException(e));
            }

            static async Task WaitAsync(ValueTask copyTask)
            {
                try
                {
                    await copyTask.ConfigureAwait(false);
                }
                catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
                {
                    throw WrapStreamCopyException(e);
                }
            }
        }

        internal ValueTask InternalCopyToAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (IsBuffered)
            {
                return stream.WriteAsync(_bufferedContent.GetSingleBuffer().AsMemory(0, (int)_bufferedContent.Length), cancellationToken);
            }

            Task task = SerializeToStreamAsync(stream, context, cancellationToken);
            CheckTaskNotNull(task);
            return new ValueTask(task);
        }

        internal void LoadIntoBuffer(long maxBufferSize, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (!CreateTemporaryBuffer(maxBufferSize, out LimitArrayPoolWriteStream? tempBuffer, out Exception? error))
            {
                // If we already buffered the content, just return.
                return;
            }

            if (tempBuffer == null)
            {
                throw error!;
            }

            // Register for cancellation and tear down the underlying stream in case of cancellation/timeout.
            // We're only comfortable disposing of the HttpContent instance like this because LoadIntoBuffer is internal and
            // we're only using it on content instances we get back from a handler's Send call that haven't been given out to the user yet.
            // If we were to ever make LoadIntoBuffer public, we'd need to rethink this.
            CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(static s => ((HttpContent)s!).Dispose(), this);

            try
            {
                SerializeToStream(tempBuffer, null, cancellationToken);
            }
            catch (Exception e)
            {
                tempBuffer.Dispose();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

                if (CancellationHelper.ShouldWrapInOperationCanceledException(e, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(e, cancellationToken);
                }

                if (StreamCopyExceptionNeedsWrapping(e))
                {
                    throw GetStreamCopyException(e);
                }

                throw;
            }
            finally
            {
                // Clean up the cancellation registration.
                cancellationRegistration.Dispose();
            }

            tempBuffer.ReallocateIfPooled();
            _bufferedContent = tempBuffer;
        }

        public Task LoadIntoBufferAsync() =>
            LoadIntoBufferAsync(MaxBufferSize);

        // No "CancellationToken" parameter needed since canceling the CTS will close the connection, resulting
        // in an exception being thrown while we're buffering.
        // If buffering is used without a connection, it is supposed to be fast, thus no cancellation required.
        public Task LoadIntoBufferAsync(long maxBufferSize) =>
            LoadIntoBufferAsync(maxBufferSize, CancellationToken.None);

        /// <summary>
        /// Serialize the HTTP content to a memory buffer as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <remarks>
        /// This operation will not block. The returned <see cref="Task"/> object will complete after all of the content has been serialized to the memory buffer.
        /// After content is serialized to a memory buffer, calls to one of the <see cref="CopyToAsync(Stream)"/> methods will copy the content of the memory buffer to the target stream.
        /// </remarks>
        /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public Task LoadIntoBufferAsync(CancellationToken cancellationToken) =>
            LoadIntoBufferAsync(MaxBufferSize, cancellationToken);

        /// <summary>
        /// Serialize the HTTP content to a memory buffer as an asynchronous operation.
        /// </summary>
        /// <param name="maxBufferSize">The maximum size, in bytes, of the buffer to use.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <remarks>
        /// This operation will not block. The returned <see cref="Task"/> object will complete after all of the content has been serialized to the memory buffer.
        /// After content is serialized to a memory buffer, calls to one of the <see cref="CopyToAsync(Stream)"/> methods will copy the content of the memory buffer to the target stream.
        /// </remarks>
        /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public Task LoadIntoBufferAsync(long maxBufferSize, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (!CreateTemporaryBuffer(maxBufferSize, out LimitArrayPoolWriteStream? tempBuffer, out Exception? error))
            {
                // If we already buffered the content, just return a completed task.
                return Task.CompletedTask;
            }

            if (tempBuffer == null)
            {
                // We don't throw in LoadIntoBufferAsync(): return a faulted task.
                return Task.FromException(error!);
            }

            try
            {
#pragma warning disable CA2025
                Task task = SerializeToStreamAsync(tempBuffer, null, cancellationToken);
                CheckTaskNotNull(task);
                return LoadIntoBufferAsyncCore(task, tempBuffer);
#pragma warning restore
            }
            catch (Exception e)
            {
                tempBuffer.Dispose();

                if (StreamCopyExceptionNeedsWrapping(e))
                {
                    return Task.FromException(GetStreamCopyException(e));
                }

                // other synchronous exceptions from SerializeToStreamAsync/CheckTaskNotNull will propagate
                throw;
            }
        }

        private async Task LoadIntoBufferAsyncCore(Task serializeToStreamTask, LimitArrayPoolWriteStream tempBuffer)
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

            tempBuffer.ReallocateIfPooled();
            _bufferedContent = tempBuffer;
        }

        /// <summary>
        /// Serializes the HTTP content to a memory stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The output memory stream which contains the serialized HTTP content.</returns>
        /// <remarks>
        /// Once the operation completes, the returned memory stream represents the HTTP content. The returned stream can then be used to read the content using various stream APIs.
        /// The <see cref="CreateContentReadStream(CancellationToken)"/> method buffers the content to a memory stream.
        /// Derived classes can override this behavior if there is a better way to retrieve the content as stream.
        /// For example, a byte array or a string could use a more efficient method way such as wrapping a read-only MemoryStream around the bytes or string.
        /// </remarks>
        protected virtual Stream CreateContentReadStream(CancellationToken cancellationToken)
        {
            LoadIntoBuffer(MaxBufferSize, cancellationToken);
            return CreateMemoryStreamFromBufferedContent();
        }

        protected virtual Task<Stream> CreateContentReadStreamAsync()
        {
            // By default just buffer the content to a memory stream. Derived classes can override this behavior
            // if there is a better way to retrieve the content as stream (e.g. byte array/string use a more efficient
            // way, like wrapping a read-only MemoryStream around the bytes/string)
            return WaitAndReturnAsync(LoadIntoBufferAsync(), this, static s => (Stream)s.CreateMemoryStreamFromBufferedContent());
        }

        protected virtual Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            // Drops the CT for compatibility reasons, see https://github.com/dotnet/runtime/issues/916#issuecomment-562083237
            return CreateContentReadStreamAsync();
        }

        // As an optimization for internal consumers of HttpContent (e.g. HttpClient.GetStreamAsync), and for
        // HttpContent-derived implementations that override CreateContentReadStreamAsync in a way that always
        // or frequently returns synchronously-completed tasks, we can avoid the task allocation by enabling
        // callers to try to get the Stream first synchronously.
        internal virtual Stream? TryCreateContentReadStream() => null;

        // Derived types return true if they're able to compute the length. It's OK if derived types return false to
        // indicate that they're not able to compute the length. The transport channel needs to decide what to do in
        // that case (send chunked, buffer first, etc.).
        protected internal abstract bool TryComputeLength(out long length);

        internal long? GetComputedOrBufferLength()
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
                long length;
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

        private bool CreateTemporaryBuffer(long maxBufferSize, out LimitArrayPoolWriteStream? tempBuffer, out Exception? error)
        {
            if (maxBufferSize > HttpContent.MaxBufferSize)
            {
                // This should only be hit when called directly; HttpClient/HttpClientHandler
                // will not exceed this limit.
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize), maxBufferSize,
                    SR.Format(CultureInfo.InvariantCulture,
                        SR.net_http_content_buffersize_limit, HttpContent.MaxBufferSize));
            }

            if (IsBuffered)
            {
                // If we already buffered the content, just return false.
                tempBuffer = default;
                error = default;
                return false;
            }

            // If we have a Content-Length allocate the right amount of buffer up-front. Also check whether the
            // content length exceeds the max. buffer size.
            long contentLength = Headers.ContentLength.GetValueOrDefault();
            Debug.Assert(contentLength >= 0);

            if (contentLength > maxBufferSize)
            {
                tempBuffer = null;
                error = CreateOverCapacityException(maxBufferSize);
            }
            else
            {
                tempBuffer = new LimitArrayPoolWriteStream((int)maxBufferSize, contentLength, getFinalSizeFromPool: false);
                error = null;
            }

            return true;
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (_contentReadStream != null)
                {
                    Stream? s = _contentReadStream as Stream ??
                        (_contentReadStream is Task<Stream> t && t.Status == TaskStatus.RanToCompletion ? t.Result : null);
                    s?.Dispose();
                    _contentReadStream = null;
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
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void CheckTaskNotNull(Task task)
        {
            if (task == null)
            {
                var e = new InvalidOperationException(SR.net_http_content_no_task_returned);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);
                throw e;
            }
        }

        internal static bool StreamCopyExceptionNeedsWrapping(Exception e) => e is IOException || e is ObjectDisposedException;

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
                WrapStreamCopyException(originalException) :
                originalException;
        }

        internal static Exception WrapStreamCopyException(Exception e)
        {
            Debug.Assert(StreamCopyExceptionNeedsWrapping(e));
            HttpRequestError error = e is HttpIOException ioEx ? ioEx.HttpRequestError : HttpRequestError.Unknown;
            return ExceptionDispatchInfo.SetCurrentStackTrace(new HttpRequestException(error, SR.net_http_content_stream_copy_error, e));
        }

        private static int GetPreambleLength(ReadOnlySpan<byte> data, Encoding encoding)
        {
            Debug.Assert(encoding != null);

            switch (encoding.CodePage)
            {
                case UTF8CodePage:
                    return data.StartsWith(UTF8Preamble) ? UTF8Preamble.Length : 0;

                case UTF32CodePage:
                    return data.StartsWith(UTF32Preamble) ? UTF32Preamble.Length : 0;

                case UnicodeCodePage:
                    return data.StartsWith(UnicodePreamble) ? UnicodePreamble.Length : 0;

                case BigEndianUnicodeCodePage:
                    return data.StartsWith(BigEndianUnicodePreamble) ? BigEndianUnicodePreamble.Length : 0;

                default:
                    byte[] preamble = encoding.GetPreamble();
                    return preamble is not null && data.StartsWith(preamble) ? preamble.Length : 0;
            }
        }

        private static bool TryDetectEncoding(ReadOnlySpan<byte> data, [NotNullWhen(true)] out Encoding? encoding, out int preambleLength)
        {
            if (data.StartsWith(UTF8Preamble))
            {
                encoding = Encoding.UTF8;
                preambleLength = UTF8Preamble.Length;
                return true;
            }

            if (data.StartsWith(UTF32Preamble))
            {
                encoding = Encoding.UTF32;
                preambleLength = UTF32Preamble.Length;
                return true;
            }

            if (data.StartsWith(UnicodePreamble))
            {
                encoding = Encoding.Unicode;
                preambleLength = UnicodePreamble.Length;
                return true;
            }

            if (data.StartsWith(BigEndianUnicodePreamble))
            {
                encoding = Encoding.BigEndianUnicode;
                preambleLength = BigEndianUnicodePreamble.Length;
                return true;
            }

            encoding = null;
            preambleLength = 0;
            return false;
        }

        #endregion Helpers

        private static async Task<TResult> WaitAndReturnAsync<TState, TResult>(Task waitTask, TState state, Func<TState, TResult> returnFunc)
        {
            await waitTask.ConfigureAwait(false);
            return returnFunc(state);
        }

        private static HttpRequestException CreateOverCapacityException(long maxBufferSize)
        {
            return (HttpRequestException)ExceptionDispatchInfo.SetCurrentStackTrace(new HttpRequestException(HttpRequestError.ConfigurationLimitExceeded, SR.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_exceeded, maxBufferSize)));
        }

        /// <summary>
        /// A write-only stream that limits the total length of the content to <see cref="_maxBufferSize"/>.
        /// It uses pooled buffers for the content, but can switch to a regular array allocation, which is useful when the caller
        /// already knows the final size and needs a new array anyway (e.g. <see cref="HttpClient.GetByteArrayAsync(string?)"/>).
        /// <para>Since we can't rely on users to reliably dispose content objects, any pooled buffers must be returned before leaving
        /// the execution path we control. In practice this means <see cref="LoadIntoBufferAsync()"/> must call <see cref="ReturnAllPooledBuffers"/>
        /// before storing the stream in <see cref="_bufferedContent"/>.</para>
        /// </summary>
        internal sealed class LimitArrayPoolWriteStream : Stream
        {
            /// <summary>Applies when a Content-Length header was not specified.</summary>
            private const int MinInitialBufferSize = 16 * 1024; // 16 KB

            /// <summary>Applies when a Content-Length header was set. If it's &lt;= this limit, we'll allocate an exact-sized buffer upfront.</summary>
            private const int MaxInitialBufferSize = 16 * 1024 * 1024; // 16 MB

            private const int ResizeFactor = 2;

            /// <summary>Controls how quickly we're willing to expand up to <see cref="_expectedFinalSize"/> when a caller requested that the last buffer should not be pooled.
            /// <para>The factor is higher than usual to lower the number of memory copies when the caller already committed to allocating a large buffer.</para></summary>
            private const int LastResizeFactor = 4;

            /// <summary><see cref="_totalLength"/> may not exceed this limit, or we'll throw a <see cref="CreateOverCapacityException"/>.</summary>
            private readonly int _maxBufferSize;
            /// <summary>The value of the Content-Length header or 0. <see cref="_totalLength"/> may exceed this value if the Content-Length isn't being enforced by the content.</summary>
            private readonly int _expectedFinalSize;
            /// <summary>Indicates whether the caller will need an exactly-sized, non-pooled, buffer. The implementation will switch away from pooled buffers earlier to reduce memory copies.</summary>
            private readonly bool _shouldPoolFinalSize;

            private bool _lastBufferIsPooled;
            private byte[] _lastBuffer;
            private byte[]?[]? _pooledBuffers;
            private int _lastBufferOffset;
            private int _totalLength;

            public LimitArrayPoolWriteStream(int maxBufferSize, long expectedFinalSize, bool getFinalSizeFromPool)
            {
                Debug.Assert(maxBufferSize >= 0);
                Debug.Assert(expectedFinalSize >= 0);

                if (expectedFinalSize > maxBufferSize)
                {
                    throw CreateOverCapacityException(maxBufferSize);
                }

                _maxBufferSize = maxBufferSize;
                _expectedFinalSize = (int)expectedFinalSize;
                _shouldPoolFinalSize = getFinalSizeFromPool || expectedFinalSize == 0;
                _lastBufferIsPooled = false;
                _lastBuffer = [];
            }

#if DEBUG
            ~LimitArrayPoolWriteStream()
            {
                // Ensure that we're not leaking pooled buffers.
                Debug.Assert(_pooledBuffers is null);
                Debug.Assert(!_lastBufferIsPooled);
            }
#endif

            protected override void Dispose(bool disposing)
            {
                ReturnAllPooledBuffers();
                base.Dispose(disposing);
            }

            /// <summary>Should only be called once.</summary>
            public byte[] ToArray()
            {
                Debug.Assert(!_shouldPoolFinalSize || _expectedFinalSize == 0);

                if (!_lastBufferIsPooled && _totalLength == _lastBuffer.Length)
                {
                    Debug.Assert(_pooledBuffers is null);
                    return _lastBuffer;
                }

                if (_totalLength == 0)
                {
                    return [];
                }

                byte[] buffer = new byte[_totalLength];
                CopyToCore(buffer);
                return buffer;
            }

            /// <summary>Should only be called if <see cref="ReallocateIfPooled"/> was used to avoid exposing pooled buffers.</summary>
            public byte[] GetSingleBuffer()
            {
                Debug.Assert(!_lastBufferIsPooled);
                Debug.Assert(_pooledBuffers is null);

                return _lastBuffer;
            }

            public ReadOnlySpan<byte> GetFirstBuffer()
            {
                return _pooledBuffers is byte[]?[] buffers
                    ? buffers[0]
                    : _lastBuffer.AsSpan(0, _totalLength);
            }

            public byte[] CreateCopy()
            {
                Debug.Assert(!_lastBufferIsPooled);
                Debug.Assert(_pooledBuffers is null);
                Debug.Assert(_lastBufferOffset == _totalLength);
                Debug.Assert(_lastBufferOffset <= _lastBuffer.Length);

                return _lastBuffer.AsSpan(0, _totalLength).ToArray();
            }

            public void ReallocateIfPooled()
            {
                Debug.Assert(_lastBufferIsPooled || _pooledBuffers is null);

                if (_lastBufferIsPooled)
                {
                    byte[] newBuffer = new byte[_totalLength];
                    CopyToCore(newBuffer);
                    ReturnAllPooledBuffers();
                    _lastBuffer = newBuffer;
                    _lastBufferOffset = newBuffer.Length;
                }
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                if (_maxBufferSize - _totalLength < buffer.Length)
                {
                    throw CreateOverCapacityException(_maxBufferSize);
                }

                byte[] lastBuffer = _lastBuffer;
                int offset = _lastBufferOffset;

                if (lastBuffer.Length - offset >= buffer.Length)
                {
                    buffer.CopyTo(lastBuffer.AsSpan(offset));
                    _lastBufferOffset = offset + buffer.Length;
                    _totalLength += buffer.Length;
                }
                else
                {
                    GrowAndWrite(buffer);
                }
            }

            private void GrowAndWrite(ReadOnlySpan<byte> buffer)
            {
                Debug.Assert(_totalLength + buffer.Length <= _maxBufferSize);

                int lastBufferCapacity = _lastBuffer.Length;

                // Start by doubling the current array size.
                int newBufferCapacity = (int)Math.Min((uint)lastBufferCapacity * ResizeFactor, Array.MaxLength);

                // If the required length is longer than Array.MaxLength, we'll let the runtime throw.
                newBufferCapacity = Math.Max(newBufferCapacity, _totalLength + buffer.Length);

                // If this is the first write, set an initial minimum size.
                if (lastBufferCapacity == 0)
                {
                    int minCapacity = _expectedFinalSize == 0
                        ? MinInitialBufferSize
                        : Math.Min(_expectedFinalSize, MaxInitialBufferSize / LastResizeFactor);

                    newBufferCapacity = Math.Max(newBufferCapacity, minCapacity);
                }

                // Avoid having the last buffer expand beyond the size limit too much.
                // It may still go beyond the limit somewhat due to the ArrayPool's buffer sizes being powers of 2.
                int currentTotalCapacity = _totalLength - _lastBufferOffset + lastBufferCapacity;
                int remainingUntilMaxCapacity = _maxBufferSize - currentTotalCapacity;
                newBufferCapacity = Math.Min(newBufferCapacity, remainingUntilMaxCapacity);

                int newTotalCapacity = currentTotalCapacity + newBufferCapacity;

                Debug.Assert(newBufferCapacity > 0);
                byte[] newBuffer;

                // If we don't want to pool our last buffer and we're getting close to its size, allocate the exact length.
                if (!_shouldPoolFinalSize && newTotalCapacity >= _expectedFinalSize / LastResizeFactor)
                {
                    // We knew the Content-Length upfront, and the caller needs an exactly-sized, non-pooled, buffer.
                    // It's almost certain that the final length will match the expected size,
                    // so we switch from pooled buffers to a regular array now to reduce memory copies.

                    // It's possible we're writing more bytes than the expected final size if the handler/content is not
                    // enforcing Content-Length correctness. Such requests will likely throw later on anyway.
                    newBuffer = new byte[_totalLength + buffer.Length <= _expectedFinalSize ? _expectedFinalSize : newTotalCapacity];

                    CopyToCore(newBuffer);

                    ReturnAllPooledBuffers();

                    buffer.CopyTo(newBuffer.AsSpan(_totalLength));

                    _totalLength += buffer.Length;
                    _lastBufferOffset = _totalLength;
                    _lastBufferIsPooled = false;
                }
                else if (lastBufferCapacity == 0)
                {
                    // This is the first write call, allocate the initial buffer.
                    Debug.Assert(_pooledBuffers is null);
                    Debug.Assert(_lastBufferOffset == 0);
                    Debug.Assert(_totalLength == 0);

                    newBuffer = ArrayPool<byte>.Shared.Rent(newBufferCapacity);
                    Debug.Assert(_shouldPoolFinalSize || newBuffer.Length != _expectedFinalSize);

                    buffer.CopyTo(newBuffer);
                    _totalLength = _lastBufferOffset = buffer.Length;
                    _lastBufferIsPooled = true;
                }
                else
                {
                    Debug.Assert(_lastBufferIsPooled);

                    _totalLength += buffer.Length;

                    // When buffers are stored in '_pooledBuffers', they are assumed to be full.
                    // Copy as many bytes as we can fit into the current buffer now.
                    Span<byte> remainingInCurrentBuffer = _lastBuffer.AsSpan(_lastBufferOffset);
                    Debug.Assert(remainingInCurrentBuffer.Length < buffer.Length);
                    buffer.Slice(0, remainingInCurrentBuffer.Length).CopyTo(remainingInCurrentBuffer);
                    buffer = buffer.Slice(remainingInCurrentBuffer.Length);

                    newBuffer = ArrayPool<byte>.Shared.Rent(newBufferCapacity);
                    buffer.CopyTo(newBuffer);
                    _lastBufferOffset = buffer.Length;

                    // Find the first empty slot in '_pooledBuffers', resizing the array if needed.
                    int bufferCount = 0;
                    if (_pooledBuffers is null)
                    {
                        // Starting with 4 buffers means we'll have capacity for at least
                        // 16 KB + 32 KB + 64 KB + 128 KB + 256 KB (last buffer) = 496 KB
                        _pooledBuffers = new byte[]?[4];
                    }
                    else
                    {
                        byte[]?[] buffers = _pooledBuffers;
                        while (bufferCount < buffers.Length && buffers[bufferCount] is not null)
                        {
                            bufferCount++;
                        }

                        if (bufferCount == buffers.Length)
                        {
                            Debug.Assert(bufferCount <= 16);

                            // After the first resize, we should have enough capacity for at least ~8 MB.
                            // ~128 MB after the second, ~2 GB after the third.
                            Array.Resize(ref _pooledBuffers, bufferCount + 4);
                        }
                    }

                    _pooledBuffers[bufferCount] = _lastBuffer;
                }

                _lastBuffer = newBuffer;
            }

            public void CopyToCore(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= _totalLength);

                if (_pooledBuffers is byte[]?[] buffers)
                {
                    Debug.Assert(buffers.Length > 0 && buffers[0] is not null);

                    foreach (byte[]? buffer in buffers)
                    {
                        if (buffer is null)
                        {
                            break;
                        }

                        Debug.Assert(destination.Length >= buffer.Length);

                        buffer.CopyTo(destination);
                        destination = destination.Slice(buffer.Length);
                    }
                }

                Debug.Assert(_lastBufferOffset <= _lastBuffer.Length);
                Debug.Assert(_lastBufferOffset <= destination.Length);

                _lastBuffer.AsSpan(0, _lastBufferOffset).CopyTo(destination);
            }

            private void ReturnAllPooledBuffers()
            {
                if (_pooledBuffers is byte[]?[] buffers)
                {
                    _pooledBuffers = null;

                    foreach (byte[]? buffer in buffers)
                    {
                        if (buffer is null)
                        {
                            break;
                        }

                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                Debug.Assert(_lastBuffer is not null);
                byte[] lastBuffer = _lastBuffer;
                _lastBuffer = null!;

                if (_lastBufferIsPooled)
                {
                    _lastBufferIsPooled = false;
                    ArrayPool<byte>.Shared.Return(lastBuffer);
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);

                Write(buffer.AsSpan(offset, count));
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Write(buffer.Span);
                return default;
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
                TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

            public override void EndWrite(IAsyncResult asyncResult) =>
                TaskToAsyncResult.End(asyncResult);

            public override void WriteByte(byte value) =>
                Write(new ReadOnlySpan<byte>(ref value));

            public override void Flush() { }
            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public override long Length => _totalLength;
            public override bool CanWrite => true;
            public override bool CanRead => false;
            public override bool CanSeek => false;

            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
        }
    }
}
