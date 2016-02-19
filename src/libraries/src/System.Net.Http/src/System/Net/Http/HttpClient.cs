// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClient : HttpMessageInvoker
    {
        #region Fields

        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static readonly TimeSpan s_infiniteTimeout = Threading.Timeout.InfiniteTimeSpan;
        private const HttpCompletionOption defaultCompletionOption = HttpCompletionOption.ResponseContentRead;

        private volatile bool _operationStarted;
        private volatile bool _disposed;

        private CancellationTokenSource _pendingRequestsCts;
        private HttpRequestHeaders _defaultRequestHeaders;

        private Uri _baseAddress;
        private TimeSpan _timeout;
        private long _maxResponseContentBufferSize;

        #endregion Fields

        #region Properties

        public HttpRequestHeaders DefaultRequestHeaders
        {
            get
            {
                if (_defaultRequestHeaders == null)
                {
                    _defaultRequestHeaders = new HttpRequestHeaders();
                }
                return _defaultRequestHeaders;
            }
        }

        public Uri BaseAddress
        {
            get { return _baseAddress; }
            set
            {
                CheckBaseAddress(value, "value");
                CheckDisposedOrStarted();

                if (HttpEventSource.Log.IsEnabled()) HttpEventSource.UriBaseAddress(this, _baseAddress.ToString());

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
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        SR.net_http_content_buffersize_limit, HttpContent.MaxBufferSize));
                }
                CheckDisposedOrStarted();
                _maxResponseContentBufferSize = value;
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Http, this, ".ctor", handler);

            _timeout = s_defaultTimeout;
            _maxResponseContentBufferSize = HttpContent.MaxBufferSize;
            _pendingRequestsCts = new CancellationTokenSource();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Http, this, ".ctor", null);
        }

        #endregion Constructors

        #region Public Send

        #region Simple Get Overloads

        public Task<string> GetStringAsync(string requestUri)
        {
            return GetStringAsync(CreateUri(requestUri));
        }

        public Task<string> GetStringAsync(Uri requestUri)
        {
            return GetContentAsync(requestUri, HttpCompletionOption.ResponseContentRead, string.Empty,
                content => content.ReadAsStringAsync());
        }

        public Task<byte[]> GetByteArrayAsync(string requestUri)
        {
            return GetByteArrayAsync(CreateUri(requestUri));
        }

        public Task<byte[]> GetByteArrayAsync(Uri requestUri)
        {
            return GetContentAsync(requestUri, HttpCompletionOption.ResponseContentRead, Array.Empty<byte>(),
                content => content.ReadAsByteArrayAsync());
        }

        // Unbuffered by default
        public Task<Stream> GetStreamAsync(string requestUri)
        {
            return GetStreamAsync(CreateUri(requestUri));
        }

        // Unbuffered by default
        public Task<Stream> GetStreamAsync(Uri requestUri)
        {
            return GetContentAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, Stream.Null,
                content => content.ReadAsStreamAsync());
        }

        private Task<T> GetContentAsync<T>(Uri requestUri, HttpCompletionOption completionOption, T defaultValue,
            Func<HttpContent, Task<T>> readAs)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            GetAsync(requestUri, completionOption).ContinueWithStandard(requestTask =>
            {
                if (HandleRequestFaultsAndCancelation(requestTask, tcs))
                {
                    return;
                }
                HttpResponseMessage response = requestTask.Result;
                if (response.Content == null)
                {
                    tcs.TrySetResult(defaultValue);
                    return;
                }

                try
                {
                    readAs(response.Content).ContinueWithStandard(contentTask =>
                    {
                        if (!HttpUtilities.HandleFaultsAndCancelation(contentTask, tcs))
                        {
                            tcs.TrySetResult(contentTask.Result);
                        }
                    });
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        #endregion Simple Get Overloads

        #region REST Send Overloads

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return GetAsync(CreateUri(requestUri));
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            return GetAsync(requestUri, defaultCompletionOption);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(CreateUri(requestUri), completionOption);
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(requestUri, completionOption, CancellationToken.None);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(requestUri, defaultCompletionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), completionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return PostAsync(CreateUri(requestUri), content);
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            return PostAsync(requestUri, content, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PostAsync(CreateUri(requestUri), content, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            return PutAsync(CreateUri(requestUri), content);
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            return PutAsync(requestUri, content, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PutAsync(CreateUri(requestUri), content, cancellationToken);
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = content;
            return SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            return DeleteAsync(CreateUri(requestUri));
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            return DeleteAsync(requestUri, CancellationToken.None);
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken)
        {
            return DeleteAsync(CreateUri(requestUri), cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
        }

        #endregion REST Send Overloads

        #region Advanced Send Overloads

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
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            CheckDisposed();
            CheckRequestMessage(request);

            SetOperationStarted();
            PrepareRequestMessage(request);
            // PrepareRequestMessage will resolve the request address against the base address.

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                _pendingRequestsCts.Token);

            SetTimeout(linkedCts);

            TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();

            base.SendAsync(request, linkedCts.Token).ContinueWithStandard(task =>
            {
                try
                {
                    // The request is completed. Dispose the request content.
                    DisposeRequestContent(request);

                    if (task.IsFaulted)
                    {
                        SetTaskFaulted(request, linkedCts, tcs, task.Exception.GetBaseException());
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        SetTaskCanceled(request, linkedCts, tcs);
                        return;
                    }

                    HttpResponseMessage response = task.Result;
                    if (response == null)
                    {
                        SetTaskFaulted(request, linkedCts, tcs,
                            new InvalidOperationException(SR.net_http_handler_noresponse));
                        return;
                    }

                    // If we don't have a response content, just return the response message.
                    if ((response.Content == null) || (completionOption == HttpCompletionOption.ResponseHeadersRead))
                    {
                        SetTaskCompleted(request, linkedCts, tcs, response);
                        return;
                    }
                    Debug.Assert(completionOption == HttpCompletionOption.ResponseContentRead,
                        "Unknown completion option.");

                    // We have an assigned content. Start loading it into a buffer and return response message once
                    // the whole content is buffered.
                    StartContentBuffering(request, linkedCts, tcs, response);
                }
                catch (Exception e)
                {
                    // Make sure we catch any exception, otherwise the task will catch it and throw in the finalizer.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.Http, this, "SendAsync", e);
                    tcs.TrySetException(e);
                }
            });
            return tcs.Task;
        }

        public void CancelPendingRequests()
        {
            CheckDisposed();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Http, this, "CancelPendingRequests", "");

            // With every request we link this cancellation token source.
            CancellationTokenSource currentCts = Interlocked.Exchange(ref _pendingRequestsCts,
                new CancellationTokenSource());

            currentCts.Cancel();
            currentCts.Dispose();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Http, this, "CancelPendingRequests", "");
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

        private void DisposeRequestContent(HttpRequestMessage request)
        {
            Contract.Requires(request != null);

            // When a request completes, HttpClient disposes the request content so the user doesn't have to. This also
            // ensures that a HttpContent object is only sent once using HttpClient (similar to HttpRequestMessages
            // that can also be sent only once).
            HttpContent content = request.Content;
            if (content != null)
            {
                content.Dispose();
            }
        }

        private void StartContentBuffering(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource,
            TaskCompletionSource<HttpResponseMessage> tcs, HttpResponseMessage response)
        {
            response.Content.LoadIntoBufferAsync(_maxResponseContentBufferSize).ContinueWithStandard(contentTask =>
            {
                try
                {
                    // Make sure to dispose the CTS _before_ setting TaskCompletionSource. Otherwise the task will be
                    // completed and the user may dispose the user CTS on the continuation task leading to a race cond.
                    bool isCancellationRequested = cancellationTokenSource.Token.IsCancellationRequested;

                    // contentTask.Exception is always != null if IsFaulted is true. However, we need to access the
                    // Exception property, otherwise the Task considers the excpetion as "unhandled" and will throw in
                    // its finalizer.
                    if (contentTask.IsFaulted)
                    {
                        response.Dispose();
                        // If the cancellation token was canceled, we consider the exception to be caused by the
                        // cancellation (e.g. WebException when reading from canceled response stream).
                        if (isCancellationRequested && (contentTask.Exception.GetBaseException() is HttpRequestException))
                        {
                            SetTaskCanceled(request, cancellationTokenSource, tcs);
                        }
                        else
                        {
                            SetTaskFaulted(request, cancellationTokenSource, tcs, contentTask.Exception.GetBaseException());
                        }
                        return;
                    }

                    if (contentTask.IsCanceled)
                    {
                        response.Dispose();
                        SetTaskCanceled(request, cancellationTokenSource, tcs);
                        return;
                    }

                    // When buffering content is completed, set the Task as completed.
                    SetTaskCompleted(request, cancellationTokenSource, tcs, response);
                }
                catch (Exception e)
                {
                    // Make sure we catch any exception, otherwise the task will catch it and throw in the finalizer.
                    response.Dispose();
                    tcs.TrySetException(e);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.Http, this, "SendAsync", e);
                }
            });
        }

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
            Uri requestUri = null;
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

        private static void CheckBaseAddress(Uri baseAddress, string parameterName)
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

        private void SetTaskFaulted(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource,
            TaskCompletionSource<HttpResponseMessage> tcs, Exception e)
        {
            LogSendError(request, cancellationTokenSource, "SendAsync", e);
            tcs.TrySetException(e);
            cancellationTokenSource.Dispose();
        }

        private void SetTaskCanceled(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource,
            TaskCompletionSource<HttpResponseMessage> tcs)
        {
            LogSendError(request, cancellationTokenSource, "SendAsync", null);
            tcs.TrySetCanceled(cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();
        }

        private void SetTaskCompleted(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource,
            TaskCompletionSource<HttpResponseMessage> tcs, HttpResponseMessage response)
        {
            if (HttpEventSource.Log.IsEnabled()) HttpEventSource.ClientSendCompleted(this, response, request);
            tcs.TrySetResult(response);
            cancellationTokenSource.Dispose();
        }

        private void SetTimeout(CancellationTokenSource cancellationTokenSource)
        {
            Contract.Requires(cancellationTokenSource != null);

            if (_timeout != s_infiniteTimeout)
            {
                cancellationTokenSource.CancelAfter(_timeout);
            }
        }

        private void LogSendError(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource,
            string method, Exception e)
        {
            Contract.Requires(request != null);

            if (cancellationTokenSource.IsCancellationRequested)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.Http, this, method, string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_client_send_canceled, LoggingHash.GetObjectLogHash(request)));
            }
            else
            {
                Debug.Assert(e != null);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.Http, this, method, string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_client_send_error, LoggingHash.GetObjectLogHash(request), e));
            }
        }

        private Uri CreateUri(String uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return null;
            }
            return new Uri(uri, UriKind.RelativeOrAbsolute);
        }

        // Returns true if the task was faulted or canceled and sets tcs accordingly. Non-success status codes count as
        // faults in cases where the HttpResponseMessage object will not be returned to the developer.  
        private static bool HandleRequestFaultsAndCancelation<T>(Task<HttpResponseMessage> task,
            TaskCompletionSource<T> tcs)
        {
            if (HttpUtilities.HandleFaultsAndCancelation(task, tcs))
            {
                return true;
            }

            HttpResponseMessage response = task.Result;
            if (!response.IsSuccessStatusCode)
            {
                if (response.Content != null)
                {
                    response.Content.Dispose();
                }

                tcs.TrySetException(new HttpRequestException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        SR.net_http_message_not_success_statuscode, (int)response.StatusCode,
                        response.ReasonPhrase)));
                return true;
            }
            return false;
        }
        #endregion Private Helpers
    }
}
