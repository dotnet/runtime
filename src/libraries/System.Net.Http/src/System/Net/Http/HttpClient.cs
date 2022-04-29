// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClient : HttpMessageInvoker
    {
        #region Fields

        private static IWebProxy? s_defaultProxy;
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static readonly TimeSpan s_infiniteTimeout = Threading.Timeout.InfiniteTimeSpan;
        private const HttpCompletionOption DefaultCompletionOption = HttpCompletionOption.ResponseContentRead;

        private volatile bool _operationStarted;
        private volatile bool _disposed;

        private CancellationTokenSource _pendingRequestsCts;
        private HttpRequestHeaders? _defaultRequestHeaders;
        private Version _defaultRequestVersion = HttpRequestMessage.DefaultRequestVersion;
        private HttpVersionPolicy _defaultVersionPolicy = HttpRequestMessage.DefaultVersionPolicy;

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
                ArgumentNullException.ThrowIfNull(value);
                s_defaultProxy = value;
            }
        }

        public HttpRequestHeaders DefaultRequestHeaders =>
            _defaultRequestHeaders ??= new HttpRequestHeaders();

        public Version DefaultRequestVersion
        {
            get => _defaultRequestVersion;
            set
            {
                CheckDisposedOrStarted();
                ArgumentNullException.ThrowIfNull(value);
                _defaultRequestVersion = value;
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
            get => _baseAddress;
            set
            {
                // It's OK to not have a base address specified, but if one is, it needs to be absolute.
                if (value is not null && !value.IsAbsoluteUri)
                {
                    throw new ArgumentException(SR.net_http_client_absolute_baseaddress_required, nameof(value));
                }

                CheckDisposedOrStarted();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.UriBaseAddress(this, value);

                _baseAddress = value;
            }
        }

        public TimeSpan Timeout
        {
            get => _timeout;
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
            get => _maxResponseContentBufferSize;
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

        public HttpClient() : this(new HttpClientHandler())
        {
        }

        public HttpClient(HttpMessageHandler handler) : this(handler, true)
        {
        }

        public HttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
            _timeout = s_defaultTimeout;
            _maxResponseContentBufferSize = HttpContent.MaxBufferSize;
            _pendingRequestsCts = new CancellationTokenSource();
        }

        #endregion Constructors

        #region Public Send

        #region Simple Get Overloads

        public Task<string> GetStringAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri) =>
            GetStringAsync(CreateUri(requestUri));

        public Task<string> GetStringAsync(Uri? requestUri) =>
            GetStringAsync(requestUri, CancellationToken.None);

        public Task<string> GetStringAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken) =>
            GetStringAsync(CreateUri(requestUri), cancellationToken);

        public Task<string> GetStringAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);

            // Called outside of async state machine to propagate certain exception even without awaiting the returned task.
            CheckRequestBeforeSend(request);

            return GetStringAsyncCore(request, cancellationToken);
        }

        private async Task<string> GetStringAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool telemetryStarted = StartSend(request);
            bool responseContentTelemetryStarted = false;

            (CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);
            HttpResponseMessage? response = null;
            try
            {
                // Wait for the response message and make sure it completed successfully.
                response = await base.SendAsync(request, cts.Token).ConfigureAwait(false);
                ThrowForNullResponse(response);
                response.EnsureSuccessStatusCode();

                // Get the response content.
                HttpContent c = response.Content;
                if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
                {
                    HttpTelemetry.Log.ResponseContentStart();
                    responseContentTelemetryStarted = true;
                }

                // Since the underlying byte[] will never be exposed, we use an ArrayPool-backed
                // stream to which we copy all of the data from the response.
                using Stream responseStream = c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var buffer = new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize, (int)c.Headers.ContentLength.GetValueOrDefault());

                try
                {
                    await responseStream.CopyToAsync(buffer, cts.Token).ConfigureAwait(false);
                }
                catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
                {
                    throw HttpContent.WrapStreamCopyException(e);
                }

                if (buffer.Length > 0)
                {
                    // Decode and return the data from the buffer.
                    return HttpContent.ReadBufferAsString(buffer.GetBuffer(), c.Headers);
                }

                // No content to return.
                return string.Empty;
            }
            catch (Exception e)
            {
                HandleFailure(e, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
                throw;
            }
            finally
            {
                FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
            }
        }

        public Task<byte[]> GetByteArrayAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri) =>
            GetByteArrayAsync(CreateUri(requestUri));

        public Task<byte[]> GetByteArrayAsync(Uri? requestUri) =>
            GetByteArrayAsync(requestUri, CancellationToken.None);

        public Task<byte[]> GetByteArrayAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken) =>
            GetByteArrayAsync(CreateUri(requestUri), cancellationToken);

        public Task<byte[]> GetByteArrayAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);

            // Called outside of async state machine to propagate certain exception even without awaiting the returned task.
            CheckRequestBeforeSend(request);

            return GetByteArrayAsyncCore(request, cancellationToken);
        }

        private async Task<byte[]> GetByteArrayAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool telemetryStarted = StartSend(request);
            bool responseContentTelemetryStarted = false;

            (CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);
            HttpResponseMessage? response = null;
            try
            {
                // Wait for the response message and make sure it completed successfully.
                response = await base.SendAsync(request, cts.Token).ConfigureAwait(false);
                ThrowForNullResponse(response);
                response.EnsureSuccessStatusCode();

                // Get the response content.
                HttpContent c = response.Content;
                if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
                {
                    HttpTelemetry.Log.ResponseContentStart();
                    responseContentTelemetryStarted = true;
                }

                // If we got a content length, then we assume that it's correct and create a MemoryStream
                // to which the content will be transferred.  That way, assuming we actually get the exact
                // amount we were expecting, we can simply return the MemoryStream's underlying buffer.
                // If we didn't get a content length, then we assume we're going to have to grow
                // the buffer potentially several times and that it's unlikely the underlying buffer
                // at the end will be the exact size needed, in which case it's more beneficial to use
                // ArrayPool buffers and copy out to a new array at the end.
                long? contentLength = c.Headers.ContentLength;
                using Stream buffer = contentLength.HasValue ?
                    new HttpContent.LimitMemoryStream(_maxResponseContentBufferSize, (int)contentLength.GetValueOrDefault()) :
                    new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize);

                using Stream responseStream = c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                try
                {
                    await responseStream.CopyToAsync(buffer, cts.Token).ConfigureAwait(false);
                }
                catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
                {
                    throw HttpContent.WrapStreamCopyException(e);
                }

                return
                    buffer.Length == 0 ? Array.Empty<byte>() :
                    buffer is HttpContent.LimitMemoryStream lms ? lms.GetSizedBuffer() :
                    ((HttpContent.LimitArrayPoolWriteStream)buffer).ToArray();
            }
            catch (Exception e)
            {
                HandleFailure(e, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
                throw;
            }
            finally
            {
                FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
            }
        }

        public Task<Stream> GetStreamAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri) =>
            GetStreamAsync(CreateUri(requestUri));

        public Task<Stream> GetStreamAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken) =>
            GetStreamAsync(CreateUri(requestUri), cancellationToken);

        public Task<Stream> GetStreamAsync(Uri? requestUri) =>
            GetStreamAsync(requestUri, CancellationToken.None);

        public Task<Stream> GetStreamAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);

            // Called outside of async state machine to propagate certain exception even without awaiting the returned task.
            CheckRequestBeforeSend(request);

            return GetStreamAsyncCore(request, cancellationToken);
        }

        private async Task<Stream> GetStreamAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool telemetryStarted = StartSend(request);

            (CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);
            HttpResponseMessage? response = null;
            try
            {
                // Wait for the response message and make sure it completed successfully.
                response = await base.SendAsync(request, cts.Token).ConfigureAwait(false);
                ThrowForNullResponse(response);
                response.EnsureSuccessStatusCode();

                HttpContent c = response.Content;
                return c.TryReadAsStream() ?? await c.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleFailure(e, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
                throw;
            }
            finally
            {
                FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted: false);
            }
        }

        #endregion Simple Get Overloads

        #region REST Send Overloads

        public Task<HttpResponseMessage> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri) =>
            GetAsync(CreateUri(requestUri));

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri) =>
            GetAsync(requestUri, DefaultCompletionOption);

        public Task<HttpResponseMessage> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpCompletionOption completionOption) =>
            GetAsync(CreateUri(requestUri), completionOption);

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption) =>
            GetAsync(requestUri, completionOption, CancellationToken.None);

        public Task<HttpResponseMessage> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken) =>
            GetAsync(CreateUri(requestUri), cancellationToken);

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken) =>
            GetAsync(requestUri, DefaultCompletionOption, cancellationToken);

        public Task<HttpResponseMessage> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
            GetAsync(CreateUri(requestUri), completionOption, cancellationToken);

        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
            SendAsync(CreateRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);

        public Task<HttpResponseMessage> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content) =>
            PostAsync(CreateUri(requestUri), content);

        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content) =>
            PostAsync(requestUri, content, CancellationToken.None);

        public Task<HttpResponseMessage> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content, CancellationToken cancellationToken) =>
            PostAsync(CreateUri(requestUri), content, cancellationToken);

        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PutAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content) =>
            PutAsync(CreateUri(requestUri), content);

        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content) =>
            PutAsync(requestUri, content, CancellationToken.None);

        public Task<HttpResponseMessage> PutAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content, CancellationToken cancellationToken) =>
            PutAsync(CreateUri(requestUri), content, cancellationToken);

        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Put, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content) =>
            PatchAsync(CreateUri(requestUri), content);

        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content) =>
            PatchAsync(requestUri, content, CancellationToken.None);

        public Task<HttpResponseMessage> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content, CancellationToken cancellationToken) =>
            PatchAsync(CreateUri(requestUri), content, cancellationToken);

        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(HttpMethod.Patch, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri) =>
            DeleteAsync(CreateUri(requestUri));

        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri) =>
            DeleteAsync(requestUri, CancellationToken.None);

        public Task<HttpResponseMessage> DeleteAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken) =>
            DeleteAsync(CreateUri(requestUri), cancellationToken);

        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken) =>
            SendAsync(CreateRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);

        #endregion REST Send Overloads

        #region Advanced Send Overloads

        [UnsupportedOSPlatform("browser")]
        public HttpResponseMessage Send(HttpRequestMessage request) =>
            Send(request, DefaultCompletionOption, cancellationToken: default);

        [UnsupportedOSPlatform("browser")]
        public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption) =>
            Send(request, completionOption, cancellationToken: default);

        [UnsupportedOSPlatform("browser")]
        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Send(request, DefaultCompletionOption, cancellationToken);

        [UnsupportedOSPlatform("browser")]
        public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            CheckRequestBeforeSend(request);
            (CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);

            bool telemetryStarted = StartSend(request);
            bool responseContentTelemetryStarted = false;
            HttpResponseMessage? response = null;
            try
            {
                // Wait for the send request to complete, getting back the response.
                response = base.Send(request, cts.Token);
                ThrowForNullResponse(response);

                // Buffer the response content if we've been asked to.
                if (ShouldBufferResponse(completionOption, request))
                {
                    if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
                    {
                        HttpTelemetry.Log.ResponseContentStart();
                        responseContentTelemetryStarted = true;
                    }

                    response.Content.LoadIntoBuffer(_maxResponseContentBufferSize, cts.Token);
                }

                return response;
            }
            catch (Exception e)
            {
                HandleFailure(e, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
                throw;
            }
            finally
            {
                FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
            }
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
            SendAsync(request, DefaultCompletionOption, CancellationToken.None);

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsync(request, DefaultCompletionOption, cancellationToken);

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption) =>
            SendAsync(request, completionOption, CancellationToken.None);

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            // Called outside of async state machine to propagate certain exception even without awaiting the returned task.
            CheckRequestBeforeSend(request);
            (CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);

            return Core(request, completionOption, cts, disposeCts, pendingRequestsCts, cancellationToken);

            async Task<HttpResponseMessage> Core(
                HttpRequestMessage request, HttpCompletionOption completionOption,
                CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts, CancellationToken originalCancellationToken)
            {
                bool telemetryStarted = StartSend(request);
                bool responseContentTelemetryStarted = false;
                HttpResponseMessage? response = null;
                try
                {
                    // Wait for the send request to complete, getting back the response.
                    response = await base.SendAsync(request, cts.Token).ConfigureAwait(false);
                    ThrowForNullResponse(response);

                    // Buffer the response content if we've been asked to.
                    if (ShouldBufferResponse(completionOption, request))
                    {
                        if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
                        {
                            HttpTelemetry.Log.ResponseContentStart();
                            responseContentTelemetryStarted = true;
                        }

                        await response.Content.LoadIntoBufferAsync(_maxResponseContentBufferSize, cts.Token).ConfigureAwait(false);
                    }

                    return response;
                }
                catch (Exception e)
                {
                    HandleFailure(e, telemetryStarted, response, cts, originalCancellationToken, pendingRequestsCts);
                    throw;
                }
                finally
                {
                    FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
                }
            }
        }

        private void CheckRequestBeforeSend(HttpRequestMessage request)
        {
            ArgumentNullException.ThrowIfNull(request);

            CheckDisposed();
            CheckRequestMessage(request);

            SetOperationStarted();

            // PrepareRequestMessage will resolve the request address against the base address.
            PrepareRequestMessage(request);
        }

        private static void ThrowForNullResponse([NotNull] HttpResponseMessage? response)
        {
            if (response is null)
            {
                throw new InvalidOperationException(SR.net_http_handler_noresponse);
            }
        }

        private static bool ShouldBufferResponse(HttpCompletionOption completionOption, HttpRequestMessage request) =>
            completionOption == HttpCompletionOption.ResponseContentRead &&
            !string.Equals(request.Method.Method, "HEAD", StringComparison.OrdinalIgnoreCase);

        private void HandleFailure(Exception e, bool telemetryStarted, HttpResponseMessage? response, CancellationTokenSource cts, CancellationToken cancellationToken, CancellationTokenSource pendingRequestsCts)
        {
            LogRequestFailed(telemetryStarted);

            response?.Dispose();

            Exception? toThrow = null;

            if (e is OperationCanceledException oce)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    if (oce.CancellationToken != cancellationToken)
                    {
                        // We got a cancellation exception, and the caller requested cancellation, but the exception doesn't contain that token.
                        // Massage things so that the cancellation exception we propagate appropriately contains the caller's token (it's possible
                        // multiple things caused cancellation, in which case we can attribute it to the caller's token, or it's possible the
                        // exception contains the linked token source, in which case that token isn't meaningful to the caller).
                        e = toThrow = new TaskCanceledException(oce.Message, oce.InnerException, cancellationToken);
                    }
                }
                else if (!pendingRequestsCts.IsCancellationRequested)
                {
                    // If this exception is for cancellation, but cancellation wasn't requested, either by the caller's token or by the pending requests source,
                    // the only other cause could be a timeout.  Treat it as such.
                    e = toThrow = new TaskCanceledException(SR.Format(SR.net_http_request_timedout, _timeout.TotalSeconds), new TimeoutException(e.Message, e), oce.CancellationToken);
                }
            }
            else if (e is HttpRequestException && cts.IsCancellationRequested) // if cancellationToken is canceled, cts will also be canceled
            {
                // If the cancellation token source was canceled, race conditions abound, and we consider the failure to be
                // caused by the cancellation (e.g. WebException when reading from canceled response stream).
                e = toThrow = new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : cts.Token);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, e);

            if (toThrow != null)
            {
                throw toThrow;
            }
        }

        private static bool StartSend(HttpRequestMessage request)
        {
            if (HttpTelemetry.Log.IsEnabled())
            {
                HttpTelemetry.Log.RequestStart(request);
                return true;
            }

            return false;
        }

        private static void FinishSend(CancellationTokenSource cts, bool disposeCts, bool telemetryStarted, bool responseContentTelemetryStarted)
        {
            // Log completion.
            if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
            {
                if (responseContentTelemetryStarted)
                {
                    HttpTelemetry.Log.ResponseContentStop();
                }

                HttpTelemetry.Log.RequestStop();
            }

            // Dispose of the CancellationTokenSource if it was created specially for this request
            // rather than being used across multiple requests.
            if (disposeCts)
            {
                cts.Dispose();
            }

            // This method used to also dispose of the request content, e.g.:
            //     request.Content?.Dispose();
            // This has multiple problems:
            //   1. It prevents code from reusing request content objects for subsequent requests,
            //      as disposing of the object likely invalidates it for further use.
            //   2. It prevents the possibility of partial or full duplex communication, even if supported
            //      by the handler, as the request content may still be in use even if the response
            //      (or response headers) has been received.
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

        private (CancellationTokenSource TokenSource, bool DisposeTokenSource, CancellationTokenSource PendingRequestsCts) PrepareCancellationTokenSource(CancellationToken cancellationToken)
        {
            // We need a CancellationTokenSource to use with the request.  We always have the global
            // _pendingRequestsCts to use, plus we may have a token provided by the caller, and we may
            // have a timeout.  If we have a timeout or a caller-provided token, we need to create a new
            // CTS (we can't, for example, timeout the pending requests CTS, as that could cancel other
            // unrelated operations).  Otherwise, we can use the pending requests CTS directly.

            // Snapshot the current pending requests cancellation source. It can change concurrently due to cancellation being requested
            // and it being replaced, and we need a stable view of it: if cancellation occurs and the caller's token hasn't been canceled,
            // it's either due to this source or due to the timeout, and checking whether this source is the culprit is reliable whereas
            // it's more approximate checking elapsed time.
            CancellationTokenSource pendingRequestsCts = _pendingRequestsCts;

            bool hasTimeout = _timeout != s_infiniteTimeout;
            if (hasTimeout || cancellationToken.CanBeCanceled)
            {
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
                if (hasTimeout)
                {
                    cts.CancelAfter(_timeout);
                }

                return (cts, DisposeTokenSource: true, pendingRequestsCts);
            }

            return (pendingRequestsCts, DisposeTokenSource: false, pendingRequestsCts);
        }

        private static Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);

        private HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri? uri) =>
            new HttpRequestMessage(method, uri) { Version = _defaultRequestVersion, VersionPolicy = _defaultVersionPolicy };
        #endregion Private Helpers
    }
}
