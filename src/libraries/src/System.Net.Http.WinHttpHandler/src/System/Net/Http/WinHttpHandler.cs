// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
#if HTTP_DLL
    internal enum WindowsProxyUsePolicy
#else
    public enum WindowsProxyUsePolicy
#endif
    {
        DoNotUseProxy = 0, // Don't use a proxy at all.
        UseWinHttpProxy = 1, // Use configuration as specified by "netsh winhttp" machine config command. Automatic detect not supported.
        UseWinInetProxy = 2, // WPAD protocol and PAC files supported.
        UseCustomProxy = 3 // Use the custom proxy specified in the Proxy property.
    }

#if HTTP_DLL
    internal enum CookieUsePolicy
#else
    public enum CookieUsePolicy
#endif
    {
        IgnoreCookies = 0,
        UseInternalCookieStoreOnly = 1,
        UseSpecifiedCookieContainer = 2
    }

#if HTTP_DLL
    internal class WinHttpHandler : HttpMessageHandler
#else
    public class WinHttpHandler : HttpMessageHandler
#endif
    {
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private object _lockObject = new object();
        private bool _doManualDecompressionCheck = false;
        private WinInetProxyHelper _proxyHelper = null;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private CookieUsePolicy _cookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly;
        private CookieContainer _cookieContainer = null;

        private SslProtocols _sslProtocols = SecurityProtocol.DefaultSecurityProtocols;
        private Func<
            HttpRequestMessage,
            X509Certificate2,
            X509Chain,
            SslPolicyErrors,
            bool> _serverCertificateValidationCallback = null;
        private bool _checkCertificateRevocationList = false;
        private ClientCertificateOption _clientCertificateOption = ClientCertificateOption.Manual;
        private X509Certificate2Collection _clientCertificates = null; // Only create collection when required.
        private ICredentials _serverCredentials = null;
        private bool _preAuthenticate = false;
        private WindowsProxyUsePolicy _windowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinHttpProxy;
        private ICredentials _defaultProxyCredentials = CredentialCache.DefaultCredentials;
        private IWebProxy _proxy = null;
        private int _maxConnectionsPerServer = int.MaxValue;
        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(60);
        private TimeSpan _sendTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _receiveHeadersTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _receiveDataTimeout = TimeSpan.FromSeconds(30);
        private int _maxResponseHeadersLength = 64 * 1024;
        private int _maxResponseDrainSize = 64 * 1024;
        private volatile bool _operationStarted;
        private volatile bool _disposed;
        private SafeWinHttpHandle _sessionHandle;
        private WinHttpAuthHelper _authHelper = new WinHttpAuthHelper();
        private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener(HttpHandlerLoggingStrings.DiagnosticListenerName);

        public WinHttpHandler()
        {
        }

        #region Properties
        public bool AutomaticRedirection
        {
            get
            {
                return _automaticRedirection;
            }

            set
            {
                CheckDisposedOrStarted();
                _automaticRedirection = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get
            {
                return _maxAutomaticRedirections;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
nameof(value),
                        value,
                        SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxAutomaticRedirections = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                return _automaticDecompression;
            }

            set
            {
                CheckDisposedOrStarted();
                _automaticDecompression = value;
            }
        }

        public CookieUsePolicy CookieUsePolicy
        {
            get
            {
                return _cookieUsePolicy;
            }

            set
            {
                if (value != CookieUsePolicy.IgnoreCookies
                    && value != CookieUsePolicy.UseInternalCookieStoreOnly
                    && value != CookieUsePolicy.UseSpecifiedCookieContainer)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _cookieUsePolicy = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return _cookieContainer;
            }

            set
            {
                CheckDisposedOrStarted();
                _cookieContainer = value;
            }
        }

        public SslProtocols SslProtocols
        {
            get
            {
                return _sslProtocols;
            }

            set
            {
                SecurityProtocol.ThrowOnNotAllowed(value, allowNone: false);

                CheckDisposedOrStarted();
                _sslProtocols = value;
            }
        }

        public Func<
            HttpRequestMessage,
            X509Certificate2,
            X509Chain,
            SslPolicyErrors,
            bool> ServerCertificateValidationCallback
        {
            get
            {
                return _serverCertificateValidationCallback;
            }

            set
            {
                CheckDisposedOrStarted();

                _serverCertificateValidationCallback = value;
            }
        }

        public bool CheckCertificateRevocationList
        {
            get
            {
                return _checkCertificateRevocationList;
            }

            set
            {
                _checkCertificateRevocationList = value;
            }
        }

        public ClientCertificateOption ClientCertificateOption
        {
            get
            {
                return _clientCertificateOption;
            }

            set
            {
                if (value != ClientCertificateOption.Manual
                    && value != ClientCertificateOption.Automatic)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _clientCertificateOption = value;
            }
        }

        public X509Certificate2Collection ClientCertificates
        {
            get
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509Certificate2Collection();
                }

                return _clientCertificates;
            }
        }

        public bool PreAuthenticate
        {
            get
            {
                return _preAuthenticate;
            }

            set
            {
                _preAuthenticate = value;
            }
        }

        public ICredentials ServerCredentials
        {
            get
            {
                return _serverCredentials;
            }

            set
            {
                _serverCredentials = value;
            }
        }

        public WindowsProxyUsePolicy WindowsProxyUsePolicy
        {
            get
            {
                return _windowsProxyUsePolicy;
            }

            set
            {
                if (value != WindowsProxyUsePolicy.DoNotUseProxy &&
                    value != WindowsProxyUsePolicy.UseWinHttpProxy &&
                    value != WindowsProxyUsePolicy.UseWinInetProxy &&
                    value != WindowsProxyUsePolicy.UseCustomProxy)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _windowsProxyUsePolicy = value;
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get
            {
                return _defaultProxyCredentials;
            }

            set
            {
                CheckDisposedOrStarted();
                _defaultProxyCredentials = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return _proxy;
            }

            set
            {
                CheckDisposedOrStarted();
                _proxy = value;
            }
        }

        public int MaxConnectionsPerServer
        {
            get
            {
                return _maxConnectionsPerServer;
            }

            set
            {
                if (value < 1)
                {
                    // In WinHTTP, setting this to 0 results in it being reset to 2.
                    // So, we'll only allow settings above 0.
                    throw new ArgumentOutOfRangeException(
nameof(value),
                        value,
                        SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxConnectionsPerServer = value;
            }
        }

        public TimeSpan ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }

            set
            {
                if (value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value > s_maxTimeout))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _connectTimeout = value;
            }
        }

        public TimeSpan SendTimeout
        {
            get
            {
                return _sendTimeout;
            }

            set
            {
                if (value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value > s_maxTimeout))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _sendTimeout = value;
            }
        }

        public TimeSpan ReceiveHeadersTimeout
        {
            get
            {
                return _receiveHeadersTimeout;
            }

            set
            {
                if (value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value > s_maxTimeout))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _receiveHeadersTimeout = value;
            }
        }

        public TimeSpan ReceiveDataTimeout
        {
            get
            {
                return _receiveDataTimeout;
            }

            set
            {
                if (value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value > s_maxTimeout))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _receiveDataTimeout = value;
            }
        }

        public int MaxResponseHeadersLength
        {
            get
            {
                return _maxResponseHeadersLength;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
nameof(value),
                        value,
                        SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxResponseHeadersLength = value;
            }
        }

        public int MaxResponseDrainSize
        {
            get
            {
                return _maxResponseDrainSize;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
nameof(value),
                        value,
                        SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxResponseDrainSize = value;
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing && _sessionHandle != null)
                {
                    SafeWinHttpHandle.DisposeAndClearHandle(ref _sessionHandle);
                }
            }

            base.Dispose(disposing);
        }

#if HTTP_DLL
        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
#else
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
#endif
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            // Check for invalid combinations of properties.
            if (_proxy != null && _windowsProxyUsePolicy != WindowsProxyUsePolicy.UseCustomProxy)
            {
                throw new InvalidOperationException(SR.net_http_invalid_proxyusepolicy);
            }

            if (_windowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy && _proxy == null)
            {
                throw new InvalidOperationException(SR.net_http_invalid_proxy);
            }

            if (_cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer &&
                _cookieContainer == null)
            {
                throw new InvalidOperationException(SR.net_http_invalid_cookiecontainer);
            }

            CheckDisposed();

            Guid loggingRequestId = s_diagnosticListener.LogHttpRequest(request);

            SetOperationStarted();

            TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();

            // Create state object and save current values of handler settings.
            var state = new WinHttpRequestState();
            state.Tcs = tcs;
            state.CancellationToken = cancellationToken;
            state.RequestMessage = request;
            state.Handler = this;
            state.CheckCertificateRevocationList = _checkCertificateRevocationList;
            state.ServerCertificateValidationCallback = _serverCertificateValidationCallback;
            state.WindowsProxyUsePolicy = _windowsProxyUsePolicy;
            state.Proxy = _proxy;
            state.ServerCredentials = _serverCredentials;
            state.DefaultProxyCredentials = _defaultProxyCredentials;
            state.PreAuthenticate = _preAuthenticate;

            Task.Factory.StartNew(
                s => ((WinHttpRequestState)s).Handler.StartRequest(s),
                state,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            s_diagnosticListener.LogHttpResponse(tcs.Task, loggingRequestId);

            return tcs.Task;
        }

        private static bool IsChunkedModeForSend(HttpRequestMessage requestMessage)
        {
            bool chunkedMode = requestMessage.Headers.TransferEncodingChunked.HasValue &&
                requestMessage.Headers.TransferEncodingChunked.Value;

            HttpContent requestContent = requestMessage.Content;
            if (requestContent != null)
            {
                if (requestContent.Headers.ContentLength.HasValue)
                {
                    if (chunkedMode)
                    {
                        // Deal with conflict between 'Content-Length' vs. 'Transfer-Encoding: chunked' semantics.
                        // Current .NET Desktop HttpClientHandler allows both headers to be specified but ends up
                        // stripping out 'Content-Length' and using chunked semantics.  WinHttpHandler will maintain
                        // the same behavior.
                        requestContent.Headers.ContentLength = null;
                    }
                }
                else
                {
                    if (!chunkedMode)
                    {
                        // Neither 'Content-Length' nor 'Transfer-Encoding: chunked' semantics was given.
                        // Current .NET Desktop HttpClientHandler uses 'Content-Length' semantics and
                        // buffers the content as well in some cases.  But the WinHttpHandler can't access
                        // the protected internal TryComputeLength() method of the content.  So, it
                        // will use'Transfer-Encoding: chunked' semantics.
                        chunkedMode = true;
                        requestMessage.Headers.TransferEncodingChunked = true;
                    }
                }
            }
            else if (chunkedMode)
            {
                throw new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content);
            }

            return chunkedMode;
        }

        private static void AddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            HttpRequestMessage requestMessage,
            CookieContainer cookies)
        {
            var requestHeadersBuffer = new StringBuilder();

            // Manually add cookies.
            if (cookies != null)
            {
                string cookieHeader = WinHttpCookieContainerAdapter.GetCookieHeader(requestMessage.RequestUri, cookies);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    requestHeadersBuffer.AppendLine(cookieHeader);
                }
            }

            // Serialize general request headers.
            requestHeadersBuffer.AppendLine(requestMessage.Headers.ToString());

            // Serialize entity-body (content) headers.
            if (requestMessage.Content != null)
            {
                // TODO: Content-Length header isn't getting correctly placed using ToString()
                // This is a bug in HttpContentHeaders that needs to be fixed.
                if (requestMessage.Content.Headers.ContentLength.HasValue)
                {
                    long contentLength = requestMessage.Content.Headers.ContentLength.Value;
                    requestMessage.Content.Headers.ContentLength = null;
                    requestMessage.Content.Headers.ContentLength = contentLength;
                }

                requestHeadersBuffer.AppendLine(requestMessage.Content.Headers.ToString());
            }

            // Add request headers to WinHTTP request handle.
            if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                requestHandle,
                requestHeadersBuffer,
                (uint)requestHeadersBuffer.Length,
                Interop.WinHttp.WINHTTP_ADDREQ_FLAG_ADD))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private void EnsureSessionHandleExists(WinHttpRequestState state)
        {
            if (_sessionHandle == null)
            {
                lock (_lockObject)
                {
                    if (_sessionHandle == null)
                    {
                        uint accessType;

                        // If a custom proxy is specified and it is really the system web proxy
                        // (initial WebRequest.DefaultWebProxy) then we need to update the settings
                        // since that object is only a sentinel.
                        if (state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy)
                        {
                            Debug.Assert(state.Proxy != null);
                            try
                            {
                                state.Proxy.GetProxy(state.RequestMessage.RequestUri);
                            }
                            catch (PlatformNotSupportedException)
                            {
                                // This is the system web proxy.
                                state.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                                state.Proxy = null;
                            }
                        }

                        if (state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.DoNotUseProxy ||
                            state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy)
                        {
                            // Either no proxy at all or a custom IWebProxy proxy is specified.
                            // For a custom IWebProxy, we'll need to calculate and set the proxy
                            // on a per request handle basis using the request Uri.  For now,
                            // we set the session handle to have no proxy.
                            accessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY;
                        }
                        else if (state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseWinHttpProxy)
                        {
                            // Use WinHTTP per-machine proxy settings which are set using the "netsh winhttp" command.
                            accessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY;
                        }
                        else
                        {
                            // Use WinInet per-user proxy settings.
                            accessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY;
                        }

                        _sessionHandle = Interop.WinHttp.WinHttpOpen(
                            IntPtr.Zero,
                            accessType,
                            Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                            Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                            (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);
                            
                        if (_sessionHandle.IsInvalid)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            if (lastError != Interop.WinHttp.ERROR_INVALID_PARAMETER)
                            {
                                ThrowOnInvalidHandle(_sessionHandle);
                            }

                            // We must be running on a platform earlier than Win8.1/Win2K12R2 which doesn't support
                            // WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY.  So, we'll need to read the Wininet style proxy
                            // settings ourself using our WinInetProxyHelper object.
                            _proxyHelper = new WinInetProxyHelper();
                            _sessionHandle = Interop.WinHttp.WinHttpOpen(
                                IntPtr.Zero,
                                _proxyHelper.ManualSettingsOnly ? Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY : Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY,
                                _proxyHelper.ManualSettingsOnly ? _proxyHelper.Proxy : Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                                _proxyHelper.ManualSettingsOnly ? _proxyHelper.ProxyBypass : Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                                (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);
                            ThrowOnInvalidHandle(_sessionHandle);
                        }

                        uint optionAssuredNonBlockingTrue = 1; // TRUE

                        if (!Interop.WinHttp.WinHttpSetOption(
                            _sessionHandle,
                            Interop.WinHttp.WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS,
                            ref optionAssuredNonBlockingTrue,
                            (uint)Marshal.SizeOf<uint>()))
                        {
                            // This option is not available on downlevel Windows versions. While it improves
                            // performance, we can ignore the error that the option is not available.
                            int lastError = Marshal.GetLastWin32Error();
                            if (lastError != Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION)
                            {
                                throw WinHttpException.CreateExceptionUsingError(lastError);
                            }
                        }
                    }
                }
            }
        }

        private async void StartRequest(object obj)
        {
            WinHttpRequestState state = (WinHttpRequestState)obj;
            bool secureConnection = false;
            HttpResponseMessage responseMessage = null;
            Exception savedException = null;
            SafeWinHttpHandle connectHandle = null;

            if (state.CancellationToken.IsCancellationRequested)
            {
                state.Tcs.TrySetCanceled(state.CancellationToken);
                state.ClearSendRequestState();
                return;
            }

            try
            {
                EnsureSessionHandleExists(state);

                SetSessionHandleOptions();

                // Specify an HTTP server.
                connectHandle = Interop.WinHttp.WinHttpConnect(
                    _sessionHandle,
                    state.RequestMessage.RequestUri.Host,
                    (ushort)state.RequestMessage.RequestUri.Port,
                    0);
                ThrowOnInvalidHandle(connectHandle);
                connectHandle.SetParentHandle(_sessionHandle);

                if (state.RequestMessage.RequestUri.Scheme == UriScheme.Https)
                {
                    secureConnection = true;
                }
                else
                {
                    secureConnection = false;
                }

                // Try to use the requested version if a known/supported version was explicitly requested.
                // Otherwise, we simply use winhttp's default.
                string httpVersion = null;
                if (state.RequestMessage.Version == HttpVersion.Version10)
                {
                    httpVersion = "HTTP/1.0";
                }
                else if (state.RequestMessage.Version == HttpVersion.Version11)
                {
                    httpVersion = "HTTP/1.1";
                }

                // Create an HTTP request handle.
                state.RequestHandle = Interop.WinHttp.WinHttpOpenRequest(
                    connectHandle,
                    state.RequestMessage.Method.Method,
                    state.RequestMessage.RequestUri.PathAndQuery,
                    httpVersion,
                    Interop.WinHttp.WINHTTP_NO_REFERER,
                    Interop.WinHttp.WINHTTP_DEFAULT_ACCEPT_TYPES,
                    secureConnection ? Interop.WinHttp.WINHTTP_FLAG_SECURE : 0);
                ThrowOnInvalidHandle(state.RequestHandle);
                state.RequestHandle.SetParentHandle(connectHandle);

                // Set callback function.
                SetStatusCallback(state.RequestHandle, WinHttpRequestCallback.StaticCallbackDelegate);

                // Set needed options on the request handle.
                SetRequestHandleOptions(state);

                bool chunkedModeForSend = IsChunkedModeForSend(state.RequestMessage);

                AddRequestHeaders(
                    state.RequestHandle,
                    state.RequestMessage,
                    _cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer ? _cookieContainer : null);

                uint proxyAuthScheme = 0;
                uint serverAuthScheme = 0;
                state.RetryRequest = false;

                // The only way to abort pending async operations in WinHTTP is to close the WinHTTP handle.
                // We will detect a cancellation request on the cancellation token by registering a callback.
                // If the callback is invoked, then we begin the abort process by disposing the handle. This
                // will have the side-effect of WinHTTP cancelling any pending I/O and accelerating its callbacks
                // on the handle and thus releasing the awaiting tasks in the loop below. This helps to provide
                // a more timely, cooperative, cancellation pattern.
                using (state.CancellationToken.Register(s => ((WinHttpRequestState)s).RequestHandle.Dispose(), state))                
                {
                    do
                    {
                        _authHelper.PreAuthenticateRequest(state, proxyAuthScheme);

                        await InternalSendRequestAsync(state).ConfigureAwait(false);

                        if (state.RequestMessage.Content != null)
                        {
                            await InternalSendRequestBodyAsync(state, chunkedModeForSend).ConfigureAwait(false);
                        }

                        bool receivedResponse = await InternalReceiveResponseHeadersAsync(state).ConfigureAwait(false);
                        if (receivedResponse)
                        {
                            // If we're manually handling cookies, we need to add them to the container after
                            // each response has been received.
                            if (state.Handler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
                            {
                                WinHttpCookieContainerAdapter.AddResponseCookiesToContainer(state);
                            }

                            _authHelper.CheckResponseForAuthentication(
                                state,
                                ref proxyAuthScheme,
                                ref serverAuthScheme);
                        }
                    } while (state.RetryRequest);
                }

                state.CancellationToken.ThrowIfCancellationRequested();

                responseMessage = WinHttpResponseParser.CreateResponseMessage(state, _doManualDecompressionCheck);

                // Since the headers have been read, set the "receive" timeout to be based on each read
                // call of the response body data. WINHTTP_OPTION_RECEIVE_TIMEOUT sets a timeout on each
                // lower layer winsock read.
                uint optionData = (uint)_receiveDataTimeout.TotalMilliseconds;
                SetWinHttpOption(state.RequestHandle, Interop.WinHttp.WINHTTP_OPTION_RECEIVE_TIMEOUT, ref optionData);
            }
            catch (Exception ex)
            {
                if (state.SavedException != null)
                {
                    savedException = state.SavedException;
                }
                else
                {
                    savedException = ex;
                }
            }
            finally
            {
                SafeWinHttpHandle.DisposeAndClearHandle(ref connectHandle);
            }

            // Move the main task to a terminal state. This releases any callers of SendAsync() that are awaiting.
            if (responseMessage != null)
            {
                state.Tcs.TrySetResult(responseMessage);
            }
            else
            {
                HandleAsyncException(state, savedException);
            }
            
            state.ClearSendRequestState();
        }

        private void SetSessionHandleOptions()
        {
            SetSessionHandleConnectionOptions();
            SetSessionHandleTlsOptions();
            SetSessionHandleTimeoutOptions();
        }

        private void SetSessionHandleConnectionOptions()
        {
            uint optionData = (uint)_maxConnectionsPerServer;
            SetWinHttpOption(_sessionHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_CONNS_PER_SERVER, ref optionData);
            SetWinHttpOption(_sessionHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_CONNS_PER_1_0_SERVER, ref optionData);
        }

        private void SetSessionHandleTlsOptions()
        {
            uint optionData = 0;
            if ((_sslProtocols & SslProtocols.Tls) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1;
            }

            if ((_sslProtocols & SslProtocols.Tls11) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_1;
            }

            if ((_sslProtocols & SslProtocols.Tls12) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_2;
            }

            SetWinHttpOption(_sessionHandle, Interop.WinHttp.WINHTTP_OPTION_SECURE_PROTOCOLS, ref optionData);
        }

        private void SetSessionHandleTimeoutOptions()
        {
            if (!Interop.WinHttp.WinHttpSetTimeouts(
                _sessionHandle,
                0,
                (int)_connectTimeout.TotalMilliseconds,
                (int)_sendTimeout.TotalMilliseconds,
                (int)_receiveHeadersTimeout.TotalMilliseconds))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private void SetRequestHandleOptions(WinHttpRequestState state)
        {
            SetRequestHandleProxyOptions(state);
            SetRequestHandleDecompressionOptions(state.RequestHandle);
            SetRequestHandleRedirectionOptions(state.RequestHandle);
            SetRequestHandleCookieOptions(state.RequestHandle);
            SetRequestHandleTlsOptions(state.RequestHandle);
            SetRequestHandleClientCertificateOptions(state.RequestHandle, state.RequestMessage.RequestUri);
            SetRequestHandleCredentialsOptions(state);
            SetRequestHandleBufferingOptions(state.RequestHandle);
        }

        private void SetRequestHandleProxyOptions(WinHttpRequestState state)
        {
            // We've already set the proxy on the session handle if we're using no proxy or default proxy settings.
            // We only need to change it on the request handle if we have a specific IWebProxy or need to manually
            // implement Wininet-style auto proxy detection.
            if (state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy ||
                state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseWinInetProxy)
            {
                var proxyInfo = new Interop.WinHttp.WINHTTP_PROXY_INFO();
                bool updateProxySettings = false;
                Uri uri = state.RequestMessage.RequestUri;

                try
                {
                    if (state.Proxy != null)
                    {
                        Debug.Assert(state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy);
                        updateProxySettings = true;
                        if (state.Proxy.IsBypassed(uri))
                        {
                            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY;
                        }
                        else
                        {
                            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY;
                            Uri proxyUri = state.Proxy.GetProxy(uri);
                            string proxyString = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}://{1}",
                                proxyUri.Scheme,
                                proxyUri.Authority);
                            proxyInfo.Proxy = Marshal.StringToHGlobalUni(proxyString);
                        }
                    }
                    else if (_proxyHelper != null && _proxyHelper.AutoSettingsUsed)
                    {
                        if (_proxyHelper.GetProxyForUrl(_sessionHandle, uri, out proxyInfo))
                        {
                            updateProxySettings = true;
                        }
                    }

                    if (updateProxySettings)
                    {
                        GCHandle pinnedHandle = GCHandle.Alloc(proxyInfo, GCHandleType.Pinned);

                        try
                        {
                            SetWinHttpOption(
                                state.RequestHandle,
                                Interop.WinHttp.WINHTTP_OPTION_PROXY,
                                pinnedHandle.AddrOfPinnedObject(),
                                (uint)Marshal.SizeOf(proxyInfo));
                        }
                        finally
                        {
                            pinnedHandle.Free();
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(proxyInfo.Proxy);
                    Marshal.FreeHGlobal(proxyInfo.ProxyBypass);
                }
            }
        }

        private void SetRequestHandleDecompressionOptions(SafeWinHttpHandle requestHandle)
        {
            uint optionData = 0;

            if (_automaticDecompression != DecompressionMethods.None)
            {
                if ((_automaticDecompression & DecompressionMethods.GZip) != 0)
                {
                    optionData |= Interop.WinHttp.WINHTTP_DECOMPRESSION_FLAG_GZIP;
                }

                if ((_automaticDecompression & DecompressionMethods.Deflate) != 0)
                {
                    optionData |= Interop.WinHttp.WINHTTP_DECOMPRESSION_FLAG_DEFLATE;
                }

                try
                {
                    SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_DECOMPRESSION, ref optionData);
                }
                catch (WinHttpException ex)
                {
                    if (ex.NativeErrorCode != (int)Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION)
                    {
                        throw;
                    }

                    // We are running on a platform earlier than Win8.1 for which WINHTTP.DLL
                    // doesn't support this option.  So, we'll have to do the decompression
                    // manually.
                    _doManualDecompressionCheck = true;
                }
            }
        }

        private void SetRequestHandleRedirectionOptions(SafeWinHttpHandle requestHandle)
        {
            uint optionData = 0;

            if (_automaticRedirection)
            {
                optionData = (uint)_maxAutomaticRedirections;
                SetWinHttpOption(
                    requestHandle,
                    Interop.WinHttp.WINHTTP_OPTION_MAX_HTTP_AUTOMATIC_REDIRECTS,
                    ref optionData);
            }

            optionData = _automaticRedirection ? 
                Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY_DISALLOW_HTTPS_TO_HTTP :
                Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY_NEVER;
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY, ref optionData);
        }

        private void SetRequestHandleCookieOptions(SafeWinHttpHandle requestHandle)
        {
            if (_cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer ||
                _cookieUsePolicy == CookieUsePolicy.IgnoreCookies)
            {
                uint optionData = Interop.WinHttp.WINHTTP_DISABLE_COOKIES;
                SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_DISABLE_FEATURE, ref optionData);
            }
        }

        private void SetRequestHandleTlsOptions(SafeWinHttpHandle requestHandle)
        {
            // If we have a custom server certificate validation callback method then 
            // we need to have WinHTTP ignore some errors so that the callback method
            // will have a chance to be called.
            uint optionData;
            if (_serverCertificateValidationCallback != null)
            {
                optionData =
                    Interop.WinHttp.SECURITY_FLAG_IGNORE_UNKNOWN_CA |
                    Interop.WinHttp.SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE |
                    Interop.WinHttp.SECURITY_FLAG_IGNORE_CERT_CN_INVALID |
                    Interop.WinHttp.SECURITY_FLAG_IGNORE_CERT_DATE_INVALID;
                SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_SECURITY_FLAGS, ref optionData);
            }
            else if (_checkCertificateRevocationList)
            {
                // If no custom validation method, then we let WinHTTP do the revocation check itself.
                optionData = Interop.WinHttp.WINHTTP_ENABLE_SSL_REVOCATION;
                SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_ENABLE_FEATURE, ref optionData);
            }
        }

        private void SetRequestHandleClientCertificateOptions(SafeWinHttpHandle requestHandle, Uri requestUri)
        {
            if (requestUri.Scheme != UriScheme.Https)
            {
                return;
            }

            X509Certificate2 clientCertificate = null;
            if (_clientCertificateOption == ClientCertificateOption.Manual)
            {
                clientCertificate = WinHttpCertificateHelper.GetEligibleClientCertificate(ClientCertificates);
            }
            else
            {
                clientCertificate = WinHttpCertificateHelper.GetEligibleClientCertificate();
            }

            if (clientCertificate != null)
            {
                SetWinHttpOption(
                    requestHandle,
                    Interop.WinHttp.WINHTTP_OPTION_CLIENT_CERT_CONTEXT,
                    clientCertificate.Handle,
                    (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CONTEXT>());
            }
            else
            {
                SetNoClientCertificate(requestHandle);
            }
        }

        internal static void SetNoClientCertificate(SafeWinHttpHandle requestHandle)
        {
            SetWinHttpOption(
                requestHandle,
                Interop.WinHttp.WINHTTP_OPTION_CLIENT_CERT_CONTEXT,
                IntPtr.Zero,
                0);
        }

        private void SetRequestHandleCredentialsOptions(WinHttpRequestState state)
        {
            // Set WinHTTP to send/prevent default credentials for either proxy or server auth.
            bool useDefaultCredentials = false;
            if (state.ServerCredentials == CredentialCache.DefaultCredentials)
            {
                useDefaultCredentials = true;
            }
            else if (state.WindowsProxyUsePolicy != WindowsProxyUsePolicy.DoNotUseProxy)
            {
                if (state.Proxy == null && _defaultProxyCredentials == CredentialCache.DefaultCredentials)
                {
                    useDefaultCredentials = true;
                }
                else if (state.Proxy != null && state.Proxy.Credentials == CredentialCache.DefaultCredentials)
                {
                    useDefaultCredentials = true;
                }
            }

            uint optionData = useDefaultCredentials ? 
                Interop.WinHttp.WINHTTP_AUTOLOGON_SECURITY_LEVEL_LOW : 
                Interop.WinHttp.WINHTTP_AUTOLOGON_SECURITY_LEVEL_HIGH;
            SetWinHttpOption(state.RequestHandle, Interop.WinHttp.WINHTTP_OPTION_AUTOLOGON_POLICY, ref optionData);
        }

        private void SetRequestHandleBufferingOptions(SafeWinHttpHandle requestHandle)
        {
            uint optionData = (uint)_maxResponseHeadersLength;
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_RESPONSE_HEADER_SIZE, ref optionData);
            optionData = (uint)_maxResponseDrainSize;
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_RESPONSE_DRAIN_SIZE, ref optionData);
        }

        private void SetWinHttpOption(SafeWinHttpHandle handle, uint option, ref uint optionData)
        {
            if (!Interop.WinHttp.WinHttpSetOption(
                handle,
                option,
                ref optionData))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private void SetWinHttpOption(SafeWinHttpHandle handle, uint option, string optionData)
        {
            if (!Interop.WinHttp.WinHttpSetOption(
                handle,
                option,
                optionData,
                (uint)optionData.Length))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private static void SetWinHttpOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionSize)
        {
            if (!Interop.WinHttp.WinHttpSetOption(
                handle,
                option,
                optionData,
                optionSize))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private void HandleAsyncException(WinHttpRequestState state, Exception ex)
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                // If the exception was due to the cancellation token being canceled, throw cancellation exception.
                state.Tcs.TrySetCanceled(state.CancellationToken);
            }
            else if (ex is WinHttpException || ex is IOException)
            {
                // Wrap expected exceptions as HttpRequestExceptions since this is considered an error during 
                // execution. All other exception types, including ArgumentExceptions and ProtocolViolationExceptions
                // are 'unexpected' or caused by user error and should not be wrapped.
                state.Tcs.TrySetException(new HttpRequestException(SR.net_http_client_execution_error, ex));
            }
            else
            {
                state.Tcs.TrySetException(ex);
            }
        }

        private void SetOperationStarted()
        {
            if (!_operationStarted)
            {
                _operationStarted = true;
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
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

        private void SetStatusCallback(
            SafeWinHttpHandle requestHandle,
            Interop.WinHttp.WINHTTP_STATUS_CALLBACK callback)
        {
            uint notificationFlags =
                Interop.WinHttp.WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
                    Interop.WinHttp.WINHTTP_CALLBACK_FLAG_HANDLES |
                    Interop.WinHttp.WINHTTP_CALLBACK_FLAG_REDIRECT |
                    Interop.WinHttp.WINHTTP_CALLBACK_FLAG_SEND_REQUEST;

            IntPtr oldCallback = Interop.WinHttp.WinHttpSetStatusCallback(
                requestHandle,
                callback,
                notificationFlags,
                IntPtr.Zero);

            if (oldCallback == new IntPtr(Interop.WinHttp.WINHTTP_INVALID_STATUS_CALLBACK))
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError != Interop.WinHttp.ERROR_INVALID_HANDLE) // Ignore error if handle was already closed.
                {
                    throw WinHttpException.CreateExceptionUsingError(lastError);
                }
            }
        }
        
        private void ThrowOnInvalidHandle(SafeWinHttpHandle handle)
        {
            if (handle.IsInvalid)
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }
        
        private Task<bool> InternalSendRequestAsync(WinHttpRequestState state)
        {
            state.TcsSendRequest = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (state.Lock)
            {
                if (!Interop.WinHttp.WinHttpSendRequest(
                    state.RequestHandle,
                    null,
                    0,
                    IntPtr.Zero,
                    0,
                    0,
                    state.ToIntPtr()))
                {
                    WinHttpException.ThrowExceptionUsingLastError();
                }
            }

            return state.TcsSendRequest.Task;
        }
        
        private async Task InternalSendRequestBodyAsync(WinHttpRequestState state, bool chunkedModeForSend)
        {
            using (var requestStream = new WinHttpRequestStream(state, chunkedModeForSend))
            {
                await state.RequestMessage.Content.CopyToAsync(
                    requestStream,
                    state.TransportContext).ConfigureAwait(false);
                await requestStream.EndUploadAsync(state.CancellationToken).ConfigureAwait(false);
            }
        }
        
        private Task<bool> InternalReceiveResponseHeadersAsync(WinHttpRequestState state)
        {
            state.TcsReceiveResponseHeaders =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            
            lock (state.Lock)
            {
                if (!Interop.WinHttp.WinHttpReceiveResponse(state.RequestHandle, IntPtr.Zero))
                {
                    throw WinHttpException.CreateExceptionUsingLastError();
                }
            }

            return state.TcsReceiveResponseHeaders.Task;
        }
    }
}
