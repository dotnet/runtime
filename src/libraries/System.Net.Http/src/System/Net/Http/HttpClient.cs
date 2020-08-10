// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClient : HttpMessageInvoker
    {
        #region Fields

        private static IWebProxy? s_defaultProxy;
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static readonly TimeSpan s_infiniteTimeout = Threading.Timeout.InfiniteTimeSpan;
        private const HttpCompletionOption defaultCompletionOption = HttpCompletionOption.ResponseContentRead;

        private volatile bool _operationStarted;
        private volatile bool _disposed;

        private CancellationTokenSource _pendingRequestsCts;
        private HttpRequestHeaders? _defaultRequestHeaders;
        private Version _defaultRequestVersion = HttpUtilities.DefaultRequestVersion;
        private HttpVersionPolicy _defaultVersionPolicy = HttpUtilities.DefaultVersionPolicy;

        private Uri? _baseAddress;
        private TimeSpan _timeout;
        private int _maxResponseContentBufferSize;

        #endregion Fields

        #region Properties
        public static IWebProxy DefaultProxy
        {
            get => LazyInitializer.EnsureInitialized(ref s_defaultProxy, () => SystemProxyInfo.Proxy);

            set
            {
                s_defaultProxy = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public HttpRequestHeaders DefaultRequestHeaders =>
            _defaultRequestHeaders ??= new HttpRequestHeaders(forceHeaderStoreItems: true);

        public Version DefaultRequestVersion
        {
            get => _defaultRequestVersion;
            set
            {
                CheckDisposedOrStarted();
                _defaultRequestVersion = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the default value of <see cref="HttpRequestMessage.VersionPolicy" /> for implicitly created requests in convenience methods,
        /// e.g.: <see cref="GetAsync(string?)" />, <see cref="PostAsync(string?, HttpContent)" />.
        /// </summary>
        /// <remarks>
        /// Note that this property has no effect on any of the <see cref="Send(HttpRequestMessage)" /> and <see cref="SendAsync(HttpRequestMessage)" /> overloads
        /// since they accept fully initialized <see cref="HttpRequestMessage" />.
        /// </remarks>
        public HttpVersionPolicy DefaultVersionPolicy
        {
            get => _defaultVersionPolicy;
            set
            {
                CheckDisposedOrStarted();
                _defaultVersionPolicy = value;
            }
        }

        public Uri? BaseAddress
        {
            get { return _baseAddress; }
            set
            {
                CheckBaseAddress(value, nameof(value));
                CheckDisposedOrStarted();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.UriBaseAddress(this, value);

                _baseAddress = value;
            }
        }

        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if (value != s_infiniteTimeout && (value <= TimeSpan.Zero || value > s_maxTimeout))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                CheckDisposedOrStarted();
                _timeout = value;
            }
        }

        public long MaxResponseContentBufferSize
        {
            get { return _maxResponseContentBufferSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (value > HttpContent.MaxBufferSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        SR.Format(System.Globalization.CultureInfo.InvariantCulture,
                        SR.net_http_content_buffersize_limit, HttpContent.MaxBufferSize));
                }
                CheckDisposedOrStarted();

                Debug.Assert(HttpContent.MaxBufferSize <= int.MaxValue);
                _maxResponseContentBufferSize = (int)value;
            }
        }

        #endregion Properties

        #region Constructors

        public HttpClient()
            : this(new HttpClientHandler())
        {
        }

        public HttpClient(HttpMessageHandler handler)
            : this(handler, true)
        {
        }

        public HttpClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
            _timeout = s_defaultTimeout;
            _maxResponseContentBufferSize = HttpContent.MaxBufferSize;
            _pendingRequestsCts = new CancellationTokenSource();
        }

        #endregion Constructors

        #region Public Send

        #region Simple Get Overloads

        public Task<string> GetStringAsync(string? requestUri) =>
            GetStringAsync(CreateUri(requestUri));

        public Task<string> GetStringAsync(Uri? requestUri) =>
            GetStringAsync(requestUri, CancellationToken.None);

        public Task<string> GetStringAsync(string? requestUri, CancellationToken cancellationToken) =>
            GetStringAsync(CreateUri(requestUri), cancellationToken);

        public Task<string> GetStringAsync(Uri? requestUri, CancellationToken cancellationToken) =>
            GetStringAsyncCore(GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken), cancellationToken);

        private async Task<string> GetStringAsyncCore(Task<HttpResponseMessage> getTask, CancellationToken cancellationToken)
        {
            // Wait for the response message.
            using (HttpResponseMessage responseMessage = await getTask.ConfigureAwait(false))
            {
                // Make sure it completed successfully.
                responseMessage.EnsureSuccessStatusCode();

                // Get the response content.
                HttpContent? c = responseMessage.Content;
                if (c != null)
                {
#if NET46
                    return await c.ReadAsStringAsync().ConfigureAwait(false);
#else
                    HttpContentHeaders headers = c.Headers;

                    // Since the underlying byte[] will never be exposed, we use an ArrayPool-backed
                    // stream to which we copy all of the data from the response.
                    using (Stream responseStream = c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (var buffer = new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize, (int)headers.ContentLength.GetValueOrDefault()))
                    {
                        try
                        {
                            await responseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
                        {
                            throw HttpContent.WrapStreamCopyException(e);
                        }

                        if (buffer.Length > 0)
                        {
                            // Decode and return the data from the buffer.
                            return HttpContent.ReadBufferAsString(buffer.GetBuffer(), headers);
                        }
                    }
#endif
                }

                // No content to return.
                return string.Empty;
            }
        }

        public Task<byte[]> GetByteArrayAsync(string? requestUri) =>
            GetByteArrayAsync(CreateUri(requestUri));

        public Task<byte[]> GetByteArrayAsync(Uri? requestUri) =>
            GetByteArrayAsync(requestUri, CancellationToken.None);

        public Task<byte[]> GetByteArrayAsync(string? requestUri, CancellationToken cancellationToken) =>
            GetByteArrayAsync(CreateUri(requestUri), cancellationToken);

        public Task<byte[]> GetByteArrayAsync(Uri? requestUri, CancellationToken cancellationToken) =>
            GetByteArrayAsyncCore(GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken), cancellationToken);

        private async Task<byte[]> GetByteArrayAsyncCore(Task<HttpResponseMessage> getTask, CancellationToken cancellationToken)
        {
            // Wait for the response message.
            using (HttpResponseMessage responseMessage = await getTask.ConfigureAwait(false))
            {
                // Make sure it completed successfully.
                responseMessage.EnsureSuccessStatusCode();

                // Get the response content.
                HttpContent? c = responseMessage.Content;
                if (c != null)
                {
#if NET46
                    return await c.ReadAsByteArrayAsync().ConfigureAwait(false);
#else
                    HttpContentHeaders headers = c.Headers;
                    using (Stream responseStream = c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    {
                        long? contentLength = headers.ContentLength;
                        Stream buffer; // declared here to share the state machine field across both if/else branches

                        if (contentLength.HasValue)
                        {
                            // If we got a content length, then we assume that it's correct and create a MemoryStream
                            // to which the content will be transferred.  That way, assuming we actually get the exact
                            // amount we were expecting, we can simply return the MemoryStream's underlying buffer.
                            buffer = new HttpContent.LimitMemoryStream(_maxResponseContentBufferSize, (int)contentLength.GetValueOrDefault());

                            try
                            {
                                await responseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
                            {
                                throw HttpContent.WrapStreamCopyException(e);
                            }

                            if (buffer.Length > 0)
                            {
                                return ((HttpContent.LimitMemoryStream)buffer).GetSizedBuffer();
                            }
                        }
                        else
                        {
                            // If we didn't get a content length, then we assume we're going to have to grow
                            // the buffer potentially several times and that it's unlikely the underlying buffer
                            // at the end will be the exact size needed, in which case it's more beneficial to use
                            // ArrayPool buffers and copy out to a new array at the end.
                            buffer = new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize);
                            try
                            {
                                try
                                {
                                    await responseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
                                {
                                    throw HttpContent.WrapStreamCopyException(e);
                                }

                                if (buffer.Length > 0)
                                {
                                    return ((HttpContent.LimitArrayPoolWriteStream)buffer).ToArray();
                                }
                            }
                            finally { buffer.Dispose(); }
                        }
                    }
#endif
                }

                // No content to return.
                return Array.Empty<byte>();
            }
        }

        public Task<Stream> GetStreamAsync(string? requestUri) =>
            GetStreamAsync(CreateUri(requestUri));

        public Task<Stream> GetStreamAsync(string? requestUri, CancellationToken cancellationToken) =>
            GetStreamAsync(CreateUri(requestUri), cancellationToken);

        public Task<Stream> GetStreamAsync(Uri? requestUri) =>
            GetStreamAsync(requestUri, CancellationToken.None);

        public Task<Stream> GetStreamAsync(Uri? requestUri, CancellationToken cancellationToken) =>
            FinishGetStreamAsync(GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken), cancellationToken);

        private async Task<Stream> FinishGetStreamAsync(Task<HttpResponseMessage> getTask, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await getTask.ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            HttpContent? c = response.Content;
            return c != null ?
                (c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)) :
                Stream.Null;
        }

        #endregion Simple Get Overloads

        #region REST Send Overloads

        public Task<HttpResponseMessage> GetAsync(string? requestUri)
        {
            return GetAsync(CreateUri(requestUri));
        }

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri)
        {
            return GetAsync(requestUri, defaultCompletionOption);
        }

        public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(CreateUri(requestUri), completionOption);
        }

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(requestUri, completionOption, CancellationToken.None);
        }

        public Task<HttpResponseMessage> GetAsync(string? requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(requestUri, defaultCompletionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), completionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return SendAsync(CreateRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent content)
        {
            return PostAsync(CreateUri(requestUri), content);
        }

        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent content)
        {
            return PostAsync(requestUri, content, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PostAsync(CreateUri(requestUri), content, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent content)
        {
            return PutAsync(CreateUri(requestUri), content);
        }

        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent content)
        {
            return PutAsync(requestUri, content, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PutAsync(CreateUri(requestUri), content, cancellationToken);
        }

        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Put, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent content)
        {
            return PatchAsync(CreateUri(requestUri), content);
        }

        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent content)
        {
            return PatchAsync(requestUri, content, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PatchAsync(CreateUri(requestUri), content, cancellationToken);
        }

        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Patch, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteAsync(string? requestUri)
        {
            return DeleteAsync(CreateUri(requestUri));
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri)
        {
            return DeleteAsync(requestUri, CancellationToken.None);
        }

        public Task<HttpResponseMessage> DeleteAsync(string? requestUri, CancellationToken cancellationToken)
        {
            return DeleteAsync(CreateUri(requestUri), cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            return SendAsync(CreateRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
        }

        #endregion REST Send Overloads

        #region Advanced Send Overloads

        public HttpResponseMessage Send(HttpRequestMessage request)
        {
            return Send(request, defaultCompletionOption, cancellationToken: default);
        }

        public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption)
        {
            return Send(request, completionOption, cancellationToken: default);
        }

        public override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Send(request, defaultCompletionOption, cancellationToken);
        }

        public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            ValueTask<HttpResponseMessage> sendTask = SendAsyncCore(request, completionOption, async: false, cancellationToken);
            Debug.Assert(sendTask.IsCompleted);
            return sendTask.GetAwaiter().GetResult();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return SendAsync(request, defaultCompletionOption, CancellationToken.None);
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return SendAsync(request, defaultCompletionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption)
        {
            return SendAsync(request, completionOption, CancellationToken.None);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return SendAsyncCore(request, completionOption, async: true, cancellationToken).AsTask();
        }

        private ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, HttpCompletionOption completionOption,
            bool async, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            CheckDisposed();
            CheckRequestMessage(request);

            SetOperationStarted();
            PrepareRequestMessage(request);
            // PrepareRequestMessage will resolve the request address against the base address.

            // Combines given cancellationToken with the global one and the timeout.
            CancellationTokenSource cts = PrepareCancellationTokenSource(cancellationToken, out bool disposeCts, out long timeoutTime);

            // Initiate the send.
            ValueTask<HttpResponseMessage> responseTask;
            try
            {
                responseTask = async ?
                    new ValueTask<HttpResponseMessage>(base.SendAsync(request, cts.Token)) :
                    new ValueTask<HttpResponseMessage>(base.Send(request, cts.Token));
            }
            catch (Exception e)
            {
                HandleFinishSendCleanup(cts, disposeCts);

                if (e is OperationCanceledException operationException && TimeoutFired(cancellationToken, timeoutTime))
                {
                    throw CreateTimeoutException(operationException);
                }

                throw;
            }

            bool buffered = completionOption == HttpCompletionOption.ResponseContentRead &&
                            !string.Equals(request.Method.Method, "HEAD", StringComparison.OrdinalIgnoreCase);

            return FinishSendAsync(responseTask, request, cts, disposeCts, buffered, async, cancellationToken, timeoutTime);
        }

        private async ValueTask<HttpResponseMessage> FinishSendAsync(ValueTask<HttpResponseMessage> sendTask, HttpRequestMessage request, CancellationTokenSource cts,
            bool disposeCts, bool buffered, bool async, CancellationToken callerToken, long timeoutTime)
        {
            HttpResponseMessage? response = null;
            try
            {
                // In sync scenario the ValueTask must already contains the result.
                Debug.Assert(async || sendTask.IsCompleted, "In synchronous scenario, the sendTask must be already completed.");

                // Wait for the send request to complete, getting back the response.
                response = await sendTask.ConfigureAwait(false);
                if (response == null)
                {
                    throw new InvalidOperationException(SR.net_http_handler_noresponse);
                }

                // Buffer the response content if we've been asked to and we have a Content to buffer.
                if (buffered && response.Content != null)
                {
                    if (async)
                    {
                        await response.Content.LoadIntoBufferAsync(_maxResponseContentBufferSize, cts.Token).ConfigureAwait(false);

                    }
                    else
                    {
                        response.Content.LoadIntoBuffer(_maxResponseContentBufferSize, cts.Token);
                    }
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.ClientSendCompleted(this, response, request);
                return response;
            }
            catch (Exception e)
            {
                response?.Dispose();

                if (e is OperationCanceledException operationException && TimeoutFired(callerToken, timeoutTime))
                {
                    HandleSendTimeout(operationException);
                    throw CreateTimeoutException(operationException);
                }

                HandleFinishSendAsyncError(e, cts);
                throw;
            }
            finally
            {
                HandleFinishSendCleanup(cts, disposeCts);
            }
        }

        private bool TimeoutFired(CancellationToken callerToken, long timeoutTime) => !callerToken.IsCancellationRequested && Environment.TickCount64 >= timeoutTime;

        private TaskCanceledException CreateTimeoutException(OperationCanceledException originalException)
        {
            return new TaskCanceledException(string.Format(SR.net_http_request_timedout, _timeout.TotalSeconds),
                new TimeoutException(originalException.Message, originalException), originalException.CancellationToken);
        }

        private void HandleFinishSendAsyncError(Exception e, CancellationTokenSource cts)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

            // If the cancellation token was canceled, we consider the exception to be caused by the
            // cancellation (e.g. WebException when reading from canceled response stream).
            if (cts.IsCancellationRequested && e is HttpRequestException)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, "Canceled");
                throw new OperationCanceledException(cts.Token);
            }
        }

        private void HandleSendTimeout(OperationCanceledException e)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(this, e);
                NetEventSource.Error(this, "Canceled due to timeout");
            }
        }

        private void HandleFinishSendCleanup(CancellationTokenSource cts, bool disposeCts)
        {
            // Dispose of the CancellationTokenSource if it was created specially for this request
            // rather than being used across multiple requests.
            if (disposeCts)
            {
                cts.Dispose();
            }

            // This method used to also dispose of the request content, e.g.:
            //     request.Content?.Dispose();
            // This has multiple problems:
            // 1. It prevents code from reusing request content objects for subsequent requests,
            //    as disposing of the object likely invalidates it for further use.
            // 2. It prevents the possibility of partial or full duplex communication, even if supported
            //    by the handler, as the request content may still be in use even if the response
            //    (or response headers) has been received.
            // By changing this to not dispose of the request content, disposal may end up being
            // left for the finalizer to handle, or the developer can explicitly dispose of the
            // content when they're done with it.  But it allows request content to be reused,
            // and more importantly it enables handlers that allow receiving of the response before
            // fully sending the request.  Prior to this change, a handler that supported duplex communication
            // would fail trying to access certain sites, if the site sent its response before it had
            // completely received the request: CurlHandler might then find that the request content
            // was disposed of while it still needed to read from it.
        }

        public void CancelPendingRequests()
        {
            CheckDisposed();

            // With every request we link this cancellation token source.
            CancellationTokenSource currentCts = Interlocked.Exchange(ref _pendingRequestsCts, new CancellationTokenSource());

            currentCts.Cancel();
            currentCts.Dispose();
        }

        #endregion Advanced Send Overloads

        #endregion Public Send

        #region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // Cancel all pending requests (if any). Note that we don't call CancelPendingRequests() but cancel
                // the CTS directly. The reason is that CancelPendingRequests() would cancel the current CTS and create
                // a new CTS. We don't want a new CTS in this case.
                _pendingRequestsCts.Cancel();
                _pendingRequestsCts.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Private Helpers

        private void SetOperationStarted()
        {
            // This method flags the HttpClient instances as "active". I.e. we executed at least one request (or are
            // in the process of doing so). This information is used to lock-down all property setters. Once a
            // Send/SendAsync operation started, no property can be changed.
            if (!_operationStarted)
            {
                _operationStarted = true;
            }
        }

        private void CheckDisposedOrStarted()
        {
            CheckDisposed();
            if (_operationStarted)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private static void CheckRequestMessage(HttpRequestMessage request)
        {
            if (!request.MarkAsSent())
            {
                throw new InvalidOperationException(SR.net_http_client_request_already_sent);
            }
        }

        private void PrepareRequestMessage(HttpRequestMessage request)
        {
            Uri? requestUri = null;
            if ((request.RequestUri == null) && (_baseAddress == null))
            {
                throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
            }
            if (request.RequestUri == null)
            {
                requestUri = _baseAddress;
            }
            else
            {
                // If the request Uri is an absolute Uri, just use it. Otherwise try to combine it with the base Uri.
                if (!request.RequestUri.IsAbsoluteUri)
                {
                    if (_baseAddress == null)
                    {
                        throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
                    }
                    else
                    {
                        requestUri = new Uri(_baseAddress, request.RequestUri);
                    }
                }
            }

            // We modified the original request Uri. Assign the new Uri to the request message.
            if (requestUri != null)
            {
                request.RequestUri = requestUri;
            }

            // Add default headers
            if (_defaultRequestHeaders != null)
            {
                request.Headers.AddHeaders(_defaultRequestHeaders);
            }
        }

        private CancellationTokenSource PrepareCancellationTokenSource(CancellationToken cancellationToken, out bool disposeCts, out long timeoutTime)
        {
            // We need a CancellationTokenSource to use with the request.  We always have the global
            // _pendingRequestsCts to use, plus we may have a token provided by the caller, and we may
            // have a timeout.  If we have a timeout or a caller-provided token, we need to create a new
            // CTS (we can't, for example, timeout the pending requests CTS, as that could cancel other
            // unrelated operations).  Otherwise, we can use the pending requests CTS directly.
            bool hasTimeout = _timeout != s_infiniteTimeout;
            timeoutTime = long.MaxValue;
            if (hasTimeout || cancellationToken.CanBeCanceled)
            {
                disposeCts = true;
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _pendingRequestsCts.Token);
                if (hasTimeout)
                {
                    timeoutTime = Environment.TickCount64 + (_timeout.Ticks / TimeSpan.TicksPerMillisecond);
                    cts.CancelAfter(_timeout);
                }

                return cts;
            }

            disposeCts = false;
            return _pendingRequestsCts;
        }

        private static void CheckBaseAddress(Uri? baseAddress, string parameterName)
        {
            if (baseAddress == null)
            {
                return; // It's OK to not have a base address specified.
            }

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.net_http_client_absolute_baseaddress_required, parameterName);
            }

            if (!HttpUtilities.IsHttpUri(baseAddress))
            {
                throw new ArgumentException(SR.net_http_client_http_baseaddress_required, parameterName);
            }
        }

        private Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);

        private HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri? uri) =>
            new HttpRequestMessage(method, uri) { Version = _defaultRequestVersion, VersionPolicy = _defaultVersionPolicy };
        #endregion Private Helpers
    }
}
