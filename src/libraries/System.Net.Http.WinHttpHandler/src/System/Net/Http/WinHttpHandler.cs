// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    public enum WindowsProxyUsePolicy
    {
        DoNotUseProxy = 0, // Don't use a proxy at all.
        UseWinHttpProxy = 1, // Use configuration as specified by "netsh winhttp" machine config command. Automatic detect not supported.
        UseWinInetProxy = 2, // WPAD protocol and PAC files supported.
        UseCustomProxy = 3 // Use the custom proxy specified in the Proxy property.
    }

    public enum CookieUsePolicy
    {
        IgnoreCookies = 0,
        UseInternalCookieStoreOnly = 1,
        UseSpecifiedCookieContainer = 2
    }

    public class WinHttpHandler : HttpMessageHandler
    {
        // These are normally defined already in System.Net.Primitives as part of the HttpVersion type.
        // However, these are not part of 'netstandard'. WinHttpHandler currently builds against
        // 'netstandard' so we need to add these definitions here.
        internal static readonly Version HttpVersion20 = new Version(2, 0);
        internal static readonly Version HttpVersionUnknown = new Version(0, 0);
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private static readonly StringWithQualityHeaderValue s_gzipHeaderValue = new StringWithQualityHeaderValue("gzip");
        private static readonly StringWithQualityHeaderValue s_deflateHeaderValue = new StringWithQualityHeaderValue("deflate");

        [ThreadStatic]
        private static StringBuilder t_requestHeadersBuilder;

        private readonly object _lockObject = new object();
        private bool _doManualDecompressionCheck;
        private WinInetProxyHelper _proxyHelper;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private CookieUsePolicy _cookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly;
        private CookieContainer _cookieContainer;
        private bool _enableMultipleHttp2Connections;

        private SslProtocols _sslProtocols = SslProtocols.None; // Use most secure protocols available.
        private Func<
            HttpRequestMessage,
            X509Certificate2,
            X509Chain,
            SslPolicyErrors,
            bool> _serverCertificateValidationCallback;
        private bool _checkCertificateRevocationList;
        private ClientCertificateOption _clientCertificateOption = ClientCertificateOption.Manual;
        private X509Certificate2Collection _clientCertificates; // Only create collection when required.
        private ICredentials _serverCredentials;
        private bool _preAuthenticate;
        private WindowsProxyUsePolicy _windowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinHttpProxy;
        private ICredentials _defaultProxyCredentials;
        private IWebProxy _proxy;
        private int _maxConnectionsPerServer = int.MaxValue;
        private TimeSpan _sendTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _receiveHeadersTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _receiveDataTimeout = TimeSpan.FromSeconds(30);

        // Using OS defaults for "Keep-alive timeout" and "keep-alive interval"
        // as documented in https://docs.microsoft.com/en-us/windows/win32/winsock/sio-keepalive-vals#remarks
        private TimeSpan _tcpKeepAliveTime = TimeSpan.FromHours(2);
        private TimeSpan _tcpKeepAliveInterval = TimeSpan.FromSeconds(1);
        private bool _tcpKeepAliveEnabled;

        private int _maxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;
        private int _maxResponseDrainSize = 64 * 1024;
        private IDictionary<string, object> _properties; // Only create dictionary when required.
        private volatile bool _operationStarted;
        private volatile bool _disposed;
        private SafeWinHttpHandle _sessionHandle;
        private readonly WinHttpAuthHelper _authHelper = new WinHttpAuthHelper();

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

        public CookieContainer? CookieContainer
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
                CheckDisposedOrStarted();
                _sslProtocols = value;
            }
        }


        public Func<
            HttpRequestMessage,
            X509Certificate2,
            X509Chain,
            SslPolicyErrors,
            bool>? ServerCertificateValidationCallback
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
                CheckDisposedOrStarted();
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
                if (_clientCertificateOption != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, "ClientCertificateOptions", "Manual"));
                }

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

        public ICredentials? ServerCredentials
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

        public ICredentials? DefaultProxyCredentials
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

        public IWebProxy? Proxy
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

        public TimeSpan SendTimeout
        {
            get
            {
                return _sendTimeout;
            }

            set
            {
                CheckTimeSpanPropertyValue(value);
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
                CheckTimeSpanPropertyValue(value);
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
                CheckTimeSpanPropertyValue(value);
                CheckDisposedOrStarted();
                _receiveDataTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether TCP keep-alive is enabled.
        /// </summary>
        /// <remarks>
        /// Only supported on Windows 10 version 2004 or newer.
        /// If enabled, the values of <see cref="TcpKeepAliveInterval" /> and <see cref="TcpKeepAliveTime"/> will be forwarded
        /// to set WINHTTP_OPTION_TCP_KEEPALIVE, enabling and configuring TCP keep-alive for the backing TCP socket.
        /// </remarks>
        [SupportedOSPlatform("windows10.0.19041")]
        public bool TcpKeepAliveEnabled
        {
            get
            {
                return _tcpKeepAliveEnabled;
            }
            set
            {
                CheckDisposedOrStarted();
                _tcpKeepAliveEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the TCP keep-alive timeout.
        /// </summary>
        /// <remarks>
        /// Only supported on Windows 10 version 2004 or newer.
        /// Has no effect if <see cref="TcpKeepAliveEnabled"/> is <see langword="false" />.
        /// The default value of this property is 2 hours.
        /// </remarks>
        [SupportedOSPlatform("windows10.0.19041")]
        public TimeSpan TcpKeepAliveTime
        {
            get
            {
                return _tcpKeepAliveTime;
            }
            set
            {
                CheckTimeSpanPropertyValue(value);
                CheckDisposedOrStarted();
                _tcpKeepAliveTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the TCP keep-alive interval.
        /// </summary>
        /// <remarks>
        /// Only supported on Windows 10 version 2004 or newer.
        /// Has no effect if <see cref="TcpKeepAliveEnabled"/> is <see langword="false" />.
        /// The default value of this property is 1 second.
        /// </remarks>
        [SupportedOSPlatform("windows10.0.19041")]
        public TimeSpan TcpKeepAliveInterval
        {
            get
            {
                return _tcpKeepAliveInterval;
            }
            set
            {
                CheckTimeSpanPropertyValue(value);
                CheckDisposedOrStarted();
                _tcpKeepAliveInterval = value;
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

        public bool EnableMultipleHttp2Connections
        {
            get
            {
                return _enableMultipleHttp2Connections;
            }
            set
            {
                CheckDisposedOrStarted();
                _enableMultipleHttp2Connections = value;
            }
        }

        public IDictionary<string, object> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new Dictionary<string, object>();
                }

                return _properties;
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            Uri? requestUri = request.RequestUri;
            if (requestUri is null || !requestUri.IsAbsoluteUri)
            {
                throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
            }

            if (requestUri.Scheme != Uri.UriSchemeHttp && requestUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new NotSupportedException(SR.Format(SR.net_http_unsupported_requesturi_scheme, requestUri.Scheme));
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

            Task.Factory.StartNew(s =>
                {
                    var whrs = (WinHttpRequestState)s;
                    _ = whrs.Handler.StartRequestAsync(whrs);
                },
                state,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            return tcs.Task;
        }

        private static WinHttpChunkMode GetChunkedModeForSend(HttpRequestMessage requestMessage)
        {
            WinHttpChunkMode chunkedMode = WinHttpChunkMode.None;

            if (requestMessage.Headers.TransferEncodingChunked.HasValue &&
                requestMessage.Headers.TransferEncodingChunked.Value)
            {
                chunkedMode = WinHttpChunkMode.Manual;
            }

            HttpContent requestContent = requestMessage.Content;
            if (requestContent != null)
            {
                if (requestContent.Headers.ContentLength.HasValue)
                {
                    if (chunkedMode == WinHttpChunkMode.Manual)
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
                    if (chunkedMode == WinHttpChunkMode.None)
                    {
                        // Neither 'Content-Length' nor 'Transfer-Encoding: chunked' semantics was given.

                        if (requestMessage.Version >= HttpVersion20)
                        {
                            // HTTP/2 supports automatic chunking (streaming the request body without a length).
                            chunkedMode = WinHttpChunkMode.Automatic;
                        }
                        else
                        {
                            // Current .NET Desktop HttpClientHandler uses 'Content-Length' semantics and
                            // buffers the content as well in some cases.  But the WinHttpHandler can't access
                            // the protected internal TryComputeLength() method of the content.  So, it
                            // will use 'Transfer-Encoding: chunked' semantics.
                            chunkedMode = WinHttpChunkMode.Manual;
                            requestMessage.Headers.TransferEncodingChunked = true;
                        }
                    }
                }
            }
            else if (chunkedMode == WinHttpChunkMode.Manual)
            {
                throw new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content);
            }

            return chunkedMode;
        }

        private static void AddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            HttpRequestMessage requestMessage,
            CookieContainer cookies,
            DecompressionMethods manuallyProcessedDecompressionMethods)
        {
            // Get a StringBuilder to use for creating the request headers.
            // We cache one in TLS to avoid creating a new one for each request.
            StringBuilder requestHeadersBuffer = t_requestHeadersBuilder;
            if (requestHeadersBuffer != null)
            {
                requestHeadersBuffer.Clear();
            }
            else
            {
                t_requestHeadersBuilder = requestHeadersBuffer = new StringBuilder();
            }

            // Normally WinHttpHandler will let native WinHTTP add 'Accept-Encoding' request headers
            // for gzip and/or default as needed based on whether the handler should do automatic
            // decompression of response content. But on Windows 7, WinHTTP doesn't support this feature.
            // So, we need to manually add these headers since WinHttpHandler still supports automatic
            // decompression (by doing it within the handler).
            if (manuallyProcessedDecompressionMethods != DecompressionMethods.None)
            {
                if ((manuallyProcessedDecompressionMethods & DecompressionMethods.GZip) == DecompressionMethods.GZip &&
                    !requestMessage.Headers.AcceptEncoding.Contains(s_gzipHeaderValue))
                {
                    requestMessage.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
                }

                if ((manuallyProcessedDecompressionMethods & DecompressionMethods.Deflate) == DecompressionMethods.Deflate &&
                    !requestMessage.Headers.AcceptEncoding.Contains(s_deflateHeaderValue))
                {
                    requestMessage.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
                }
            }

            // Manually add cookies.
            if (cookies != null && cookies.Count > 0)
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
                // TODO https://github.com/dotnet/runtime/issues/16162:
                // Content-Length header isn't getting correctly placed using ToString()
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
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpAddRequestHeaders));
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
                        SafeWinHttpHandle sessionHandle;
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

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Proxy accessType={accessType}");

                        sessionHandle = Interop.WinHttp.WinHttpOpen(
                            IntPtr.Zero,
                            accessType,
                            Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                            Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                            (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

                        if (sessionHandle.IsInvalid)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"error={lastError}");
                            if (lastError != Interop.WinHttp.ERROR_INVALID_PARAMETER)
                            {
                                ThrowOnInvalidHandle(sessionHandle, nameof(Interop.WinHttp.WinHttpOpen));
                            }

                            // We must be running on a platform earlier than Win8.1/Win2K12R2 which doesn't support
                            // WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY.  So, we'll need to read the Wininet style proxy
                            // settings ourself using our WinInetProxyHelper object.
                            _proxyHelper = new WinInetProxyHelper();
                            sessionHandle = Interop.WinHttp.WinHttpOpen(
                                IntPtr.Zero,
                                _proxyHelper.ManualSettingsOnly ? Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY : Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY,
                                _proxyHelper.ManualSettingsOnly ? _proxyHelper.Proxy : Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                                _proxyHelper.ManualSettingsOnly ? _proxyHelper.ProxyBypass : Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                                (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);
                            ThrowOnInvalidHandle(sessionHandle, nameof(Interop.WinHttp.WinHttpOpen));
                        }

                        uint optionAssuredNonBlockingTrue = 1; // TRUE

                        if (!Interop.WinHttp.WinHttpSetOption(
                            sessionHandle,
                            Interop.WinHttp.WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS,
                            ref optionAssuredNonBlockingTrue,
                            (uint)sizeof(uint)))
                        {
                            // This option is not available on downlevel Windows versions. While it improves
                            // performance, we can ignore the error that the option is not available.
                            int lastError = Marshal.GetLastWin32Error();
                            if (lastError != Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION)
                            {
                                throw WinHttpException.CreateExceptionUsingError(lastError, nameof(Interop.WinHttp.WinHttpSetOption));
                            }
                        }

                        SetSessionHandleOptions(sessionHandle);
                        _sessionHandle = sessionHandle;
                    }
                }
            }
        }

        private async Task StartRequestAsync(WinHttpRequestState state)
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                state.Tcs.TrySetCanceled(state.CancellationToken);
                state.ClearSendRequestState();
                return;
            }

            Task sendRequestBodyTask = null;
            SafeWinHttpHandle connectHandle = null;
            try
            {
                EnsureSessionHandleExists(state);

                SetEnableHttp2PlusClientCertificate(state.RequestMessage.RequestUri, state.RequestMessage.Version);

                // Specify an HTTP server.
                connectHandle = Interop.WinHttp.WinHttpConnect(
                    _sessionHandle,
                    state.RequestMessage.RequestUri.HostNameType == UriHostNameType.IPv6 ? "[" + state.RequestMessage.RequestUri.IdnHost + "]" : state.RequestMessage.RequestUri.IdnHost,
                    (ushort)state.RequestMessage.RequestUri.Port,
                    0);
                ThrowOnInvalidHandle(connectHandle, nameof(Interop.WinHttp.WinHttpConnect));
                connectHandle.SetParentHandle(_sessionHandle);

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

                OpenRequestHandle(state, connectHandle, httpVersion, out WinHttpChunkMode chunkedModeForSend, out SafeWinHttpHandle requestHandle);
                state.RequestHandle = requestHandle;
                state.RequestHandle.SetParentHandle(connectHandle);

                // Set callback function.
                SetStatusCallback(state.RequestHandle, WinHttpRequestCallback.StaticCallbackDelegate);

                // Set needed options on the request handle.
                SetRequestHandleOptions(state);

                AddRequestHeaders(
                    state.RequestHandle,
                    state.RequestMessage,
                    _cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer ? _cookieContainer : null,
                    _doManualDecompressionCheck ? _automaticDecompression : DecompressionMethods.None);

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

                        await InternalSendRequestAsync(state);

                        RendezvousAwaitable<int> receivedResponseTask;

                        if (chunkedModeForSend == WinHttpChunkMode.Automatic)
                        {
                            // Start waiting to receive response headers before sending request body.
                            // This order is important because the response could be returned immediately
                            // with END_STREAM flag on headers. Trying to send request body after that
                            // can cause the request to go into a bad state.
                            //
                            // We only use this order if chunk mode is automatic because Windows versions
                            // prior to AUTOMATIC_CHUNKING didn't support it.
                            receivedResponseTask = InternalReceiveResponseHeadersAsync(state);

                            if (state.RequestMessage.Content != null)
                            {
                                sendRequestBodyTask = InternalSendRequestBodyAsync(state, chunkedModeForSend);
                            }
                        }
                        else
                        {
                            if (state.RequestMessage.Content != null)
                            {
                                sendRequestBodyTask = InternalSendRequestBodyAsync(state, chunkedModeForSend);
                                await sendRequestBodyTask.ConfigureAwait(false);
                            }

                            receivedResponseTask = InternalReceiveResponseHeadersAsync(state);
                        }

                        bool receivedResponse = await receivedResponseTask != 0;
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

                // Since the headers have been read, set the "receive" timeout to be based on each read
                // call of the response body data. WINHTTP_OPTION_RECEIVE_TIMEOUT sets a timeout on each
                // lower layer winsock read.
                // Timeout.InfiniteTimeSpan will be converted to uint.MaxValue milliseconds (~ 50 days).
                // The result a of double->uint cast is unspecified for -1 and may differ on ARM, returning 0 instead of uint.MaxValue.
                // To handle Timeout.InfiniteTimespan correctly, we need to cast to int first.
                uint optionData = (uint)(int)_receiveDataTimeout.TotalMilliseconds;
                SetWinHttpOption(state.RequestHandle, Interop.WinHttp.WINHTTP_OPTION_RECEIVE_TIMEOUT, ref optionData);

                HttpResponseMessage responseMessage =
                    WinHttpResponseParser.CreateResponseMessage(state, _doManualDecompressionCheck ? _automaticDecompression : DecompressionMethods.None);
                state.Tcs.TrySetResult(responseMessage);

                // HttpStatusCode cast is needed for 308 Moved Permenantly, which we support but is not included in NetStandard status codes.
                if (NetEventSource.Log.IsEnabled() &&
                    ((responseMessage.StatusCode >= HttpStatusCode.MultipleChoices && responseMessage.StatusCode <= HttpStatusCode.SeeOther) ||
                     (responseMessage.StatusCode >= HttpStatusCode.RedirectKeepVerb && responseMessage.StatusCode <= (HttpStatusCode)308)) &&
                    state.RequestMessage.RequestUri.Scheme == Uri.UriSchemeHttps && responseMessage.Headers.Location?.Scheme == Uri.UriSchemeHttp)
                {
                    NetEventSource.Error(this, $"Insecure https to http redirect from {state.RequestMessage.RequestUri} to {responseMessage.Headers.Location} blocked.");
                }
            }
            catch (Exception ex)
            {
                HandleAsyncException(state, state.SavedException ?? ex);
            }
            finally
            {
                SafeWinHttpHandle.DisposeAndClearHandle(ref connectHandle);

                try
                {
                    // Wait for request body to finish sending.
                    if (sendRequestBodyTask != null)
                    {
                        await sendRequestBodyTask.ConfigureAwait(false);
                    }
                }
                finally
                {
                    state.ClearSendRequestState();
                }
            }
        }

        private void OpenRequestHandle(WinHttpRequestState state, SafeWinHttpHandle connectHandle, string httpVersion, out WinHttpChunkMode chunkedModeForSend, out SafeWinHttpHandle requestHandle)
        {
            chunkedModeForSend = GetChunkedModeForSend(state.RequestMessage);

            // Create an HTTP request handle.
            requestHandle = Interop.WinHttp.WinHttpOpenRequest(
                connectHandle,
                state.RequestMessage.Method.Method,
                state.RequestMessage.RequestUri.PathAndQuery,
                httpVersion,
                Interop.WinHttp.WINHTTP_NO_REFERER,
                Interop.WinHttp.WINHTTP_DEFAULT_ACCEPT_TYPES,
                GetRequestFlags(state, chunkedModeForSend));

            // It is possible the request was made with the WINHTTP_FLAG_AUTOMATIC_CHUNKING flag
            // and the platform doesn't support that flag.
            if (requestHandle.IsInvalid)
            {
                int lastError = Marshal.GetLastWin32Error();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"error={lastError}");
                if (lastError != Interop.WinHttp.ERROR_INVALID_PARAMETER || chunkedModeForSend != WinHttpChunkMode.Automatic)
                {
                    ThrowOnInvalidHandle(requestHandle, nameof(Interop.WinHttp.WinHttpOpenRequest));
                }

                // Platform doesn't support WINHTTP_FLAG_AUTOMATIC_CHUNKING. Revert to manual chunking.
                // Note that manual chunking with WinHttp downgrades HTTP/2 requests to HTTP/1.1.
                chunkedModeForSend = WinHttpChunkMode.Manual;
                state.RequestMessage.Headers.TransferEncodingChunked = true;

                requestHandle = Interop.WinHttp.WinHttpOpenRequest(
                    connectHandle,
                    state.RequestMessage.Method.Method,
                    state.RequestMessage.RequestUri.PathAndQuery,
                    httpVersion,
                    Interop.WinHttp.WINHTTP_NO_REFERER,
                    Interop.WinHttp.WINHTTP_DEFAULT_ACCEPT_TYPES,
                    GetRequestFlags(state, chunkedModeForSend));

                ThrowOnInvalidHandle(requestHandle, nameof(Interop.WinHttp.WinHttpOpenRequest));
            }

            static uint GetRequestFlags(WinHttpRequestState state, WinHttpChunkMode chunkedModeForSend)
            {
                // Turn off additional URI reserved character escaping (percent-encoding). This matches
                // .NET Framework behavior. System.Uri establishes the baseline rules for percent-encoding
                // of reserved characters.
                uint flags = Interop.WinHttp.WINHTTP_FLAG_ESCAPE_DISABLE;
                if (state.RequestMessage.RequestUri.Scheme == UriScheme.Https)
                {
                    flags |= Interop.WinHttp.WINHTTP_FLAG_SECURE;
                }
                if (chunkedModeForSend == WinHttpChunkMode.Automatic)
                {
                    flags |= Interop.WinHttp.WINHTTP_FLAG_AUTOMATIC_CHUNKING;
                }

                return flags;
            }
        }

        private void SetSessionHandleOptions(SafeWinHttpHandle sessionHandle)
        {
            SetSessionHandleConnectionOptions(sessionHandle);
            SetSessionHandleTlsOptions(sessionHandle);
            SetSessionHandleTimeoutOptions(sessionHandle);
            SetDisableHttp2StreamQueue(sessionHandle);
            SetTcpKeepalive(sessionHandle);
        }

        private unsafe void SetTcpKeepalive(SafeWinHttpHandle sessionHandle)
        {
            if (_tcpKeepAliveEnabled)
            {
                var tcpKeepalive = new Interop.WinHttp.tcp_keepalive
                {
                    onoff = 1,

                    // Timeout.InfiniteTimeSpan will be converted to uint.MaxValue milliseconds (~ 50 days)
                    // The result a of double->uint cast is unspecified for -1 and may differ on ARM, returning 0 instead of uint.MaxValue.
                    // To handle Timeout.InfiniteTimespan correctly, we need to cast to int first.
                    keepaliveinterval = (uint)(int)_tcpKeepAliveInterval.TotalMilliseconds,
                    keepalivetime = (uint)(int)_tcpKeepAliveTime.TotalMilliseconds
                };

                SetWinHttpOption(
                    sessionHandle,
                    Interop.WinHttp.WINHTTP_OPTION_TCP_KEEPALIVE,
                    (IntPtr)(&tcpKeepalive),
                    (uint)sizeof(Interop.WinHttp.tcp_keepalive));
            }
        }

        private void SetSessionHandleConnectionOptions(SafeWinHttpHandle sessionHandle)
        {
            uint optionData = (uint)_maxConnectionsPerServer;
            SetWinHttpOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_CONNS_PER_SERVER, ref optionData);
            SetWinHttpOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_CONNS_PER_1_0_SERVER, ref optionData);
        }

        private void SetSessionHandleTlsOptions(SafeWinHttpHandle sessionHandle)
        {
            uint optionData = 0;
            SslProtocols sslProtocols =
                (_sslProtocols == SslProtocols.None) ? SecurityProtocol.DefaultSecurityProtocols : _sslProtocols;

#pragma warning disable 0618 // SSL2/SSL3 are deprecated
            if ((sslProtocols & SslProtocols.Ssl2) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_SSL2;
            }

            if ((sslProtocols & SslProtocols.Ssl3) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_SSL3;
            }
#pragma warning restore 0618

            if ((sslProtocols & SslProtocols.Tls) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1;
            }

            if ((sslProtocols & SslProtocols.Tls11) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_1;
            }

            if ((sslProtocols & SslProtocols.Tls12) != 0)
            {
                optionData |= Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_2;
            }

            // As of Win10RS5 there's no public constant for WinHTTP + TLS 1.3
            // This library builds against netstandard, which doesn't define the Tls13 enum field.

            // If only unknown values (e.g. TLS 1.3) were asked for, report ERROR_INVALID_PARAMETER.
            if (optionData == 0)
            {
                throw WinHttpException.CreateExceptionUsingError(
                    unchecked((int)Interop.WinHttp.ERROR_INVALID_PARAMETER),
                    nameof(SetSessionHandleTlsOptions));
            }

            SetWinHttpOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_SECURE_PROTOCOLS, ref optionData);
        }

        private void SetSessionHandleTimeoutOptions(SafeWinHttpHandle sessionHandle)
        {
            if (!Interop.WinHttp.WinHttpSetTimeouts(
                sessionHandle,
                0,
                0,
                (int)_sendTimeout.TotalMilliseconds,
                (int)_receiveHeadersTimeout.TotalMilliseconds))
            {
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSetTimeouts));
            }
        }

        private void SetRequestHandleOptions(WinHttpRequestState state)
        {
            SetRequestHandleProxyOptions(state);
            SetRequestHandleDecompressionOptions(state.RequestHandle);
            SetRequestHandleRedirectionOptions(state.RequestHandle);
            SetRequestHandleCookieOptions(state.RequestHandle);
            SetRequestHandleTlsOptions(state.RequestHandle);
            SetRequestHandleClientCertificateOptions(state.RequestHandle, state.RequestMessage.RequestUri, state.RequestMessage.Version);
            SetRequestHandleCredentialsOptions(state);
            SetRequestHandleBufferingOptions(state.RequestHandle);
            SetRequestHandleHttp2Options(state.RequestHandle, state.RequestMessage.Version);
        }

        private void SetRequestHandleProxyOptions(WinHttpRequestState state)
        {
            // We've already set the proxy on the session handle if we're using no proxy or default proxy settings.
            // We only need to change it on the request handle if we have a specific IWebProxy or need to manually
            // implement Wininet-style auto proxy detection.
            if (state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseCustomProxy ||
                state.WindowsProxyUsePolicy == WindowsProxyUsePolicy.UseWinInetProxy)
            {
                Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo = default;
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
                            string proxyString = proxyUri.Scheme + "://" + proxyUri.Authority;
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

        private void SetRequestHandleClientCertificateOptions(SafeWinHttpHandle requestHandle, Uri requestUri, Version requestVersion)
        {
            if (requestUri.Scheme != UriScheme.Https)
            {
                return;
            }

            X509Certificate2 clientCertificate = null;
            if (_clientCertificateOption == ClientCertificateOption.Manual)
            {
                clientCertificate = CertificateHelper.GetEligibleClientCertificate(ClientCertificates);
            }
            else
            {
                clientCertificate = CertificateHelper.GetEligibleClientCertificate();
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

        private void SetEnableHttp2PlusClientCertificate(Uri requestUri, Version requestVersion)
        {
            if (requestUri.Scheme != UriScheme.Https || requestVersion != HttpVersion20)
            {
                return;
            }

            // Newer versions of WinHTTP fully support HTTP/2 with TLS client certificates.
            // But the support must be opted in.
            uint optionData = Interop.WinHttp.WINHTTP_HTTP2_PLUS_CLIENT_CERT_FLAG;
            if (Interop.WinHttp.WinHttpSetOption(
                _sessionHandle,
                Interop.WinHttp.WINHTTP_OPTION_ENABLE_HTTP2_PLUS_CLIENT_CERT,
                ref optionData))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "HTTP/2 with TLS client cert supported");
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "HTTP/2 with TLS client cert not supported");
            }
        }

        private void SetDisableHttp2StreamQueue(SafeWinHttpHandle sessionHandle)
        {
            if (_enableMultipleHttp2Connections)
            {
                uint optionData = 1;
                if (Interop.WinHttp.WinHttpSetOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_DISABLE_STREAM_QUEUE, ref optionData))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Multiple HTTP/2 connections enabled.");
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Multiple HTTP/2 connections cannot be enabled.");
                }
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
            // By default, WinHTTP sets the default credentials policy such that it automatically sends default credentials
            // (current user's logged on Windows credentials) to a proxy when needed (407 response). It only sends
            // default credentials to a server (401 response) if the server is considered to be on the Intranet.
            // WinHttpHandler uses a more granual opt-in model for using default credentials that can be different between
            // proxy and server credentials. It will explicitly allow default credentials to be sent at a later stage in
            // the request processing (after getting a 401/407 response) when the proxy or server credential is set as
            // CredentialCache.DefaultNetworkCredential. For now, we set the policy to prevent any default credentials
            // from being automatically sent until we get a 401/407 response.
            _authHelper.ChangeDefaultCredentialsPolicy(
                state.RequestHandle,
                Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER,
                allowDefaultCredentials: false);
        }

        private void SetRequestHandleBufferingOptions(SafeWinHttpHandle requestHandle)
        {
            uint optionData = (uint)(_maxResponseHeadersLength * 1024);
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_RESPONSE_HEADER_SIZE, ref optionData);
            optionData = (uint)_maxResponseDrainSize;
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_MAX_RESPONSE_DRAIN_SIZE, ref optionData);
        }

        private void SetRequestHandleHttp2Options(SafeWinHttpHandle requestHandle, Version requestVersion)
        {
            Debug.Assert(requestHandle != null);
            uint optionData = (requestVersion == HttpVersion20) ? Interop.WinHttp.WINHTTP_PROTOCOL_FLAG_HTTP2 : 0;
            if (Interop.WinHttp.WinHttpSetOption(
                requestHandle,
                Interop.WinHttp.WINHTTP_OPTION_ENABLE_HTTP_PROTOCOL,
                ref optionData))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"HTTP/2 option supported, setting to {optionData}");
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "HTTP/2 option not supported");
            }
        }

        private void SetWinHttpOption(SafeWinHttpHandle handle, uint option, ref uint optionData)
        {
            Debug.Assert(handle != null);
            if (!Interop.WinHttp.WinHttpSetOption(
                handle,
                option,
                ref optionData))
            {
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSetOption));
            }
        }

        private static void SetWinHttpOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionSize)
        {
            Debug.Assert(handle != null);
            if (!Interop.WinHttp.WinHttpSetOption(
                handle,
                option,
                optionData,
                optionSize))
            {
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSetOption));
            }
        }

        private void HandleAsyncException(WinHttpRequestState state, Exception ex)
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                // If the exception was due to the cancellation token being canceled, throw cancellation exception.
                state.Tcs.TrySetCanceled(state.CancellationToken);
            }
            else if (ex is WinHttpException || ex is IOException || ex is InvalidOperationException)
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

        private static void CheckTimeSpanPropertyValue(TimeSpan timeSpan)
        {
            if (timeSpan != Timeout.InfiniteTimeSpan && (timeSpan <= TimeSpan.Zero || timeSpan > s_maxTimeout))
            {
                throw new ArgumentOutOfRangeException("value");
            }
        }

        private void SetStatusCallback(
            SafeWinHttpHandle requestHandle,
            Interop.WinHttp.WINHTTP_STATUS_CALLBACK callback)
        {
            const uint notificationFlags =
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
                    throw WinHttpException.CreateExceptionUsingError(lastError, nameof(Interop.WinHttp.WinHttpSetStatusCallback));
                }
            }
        }

        private void ThrowOnInvalidHandle(SafeWinHttpHandle handle, string nameOfCalledFunction)
        {
            if (handle.IsInvalid)
            {
                int lastError = Marshal.GetLastWin32Error();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"error={lastError}");
                throw WinHttpException.CreateExceptionUsingError(lastError, nameOfCalledFunction);
            }
        }

        private RendezvousAwaitable<int> InternalSendRequestAsync(WinHttpRequestState state)
        {
            lock (state.Lock)
            {
                state.Pin();
                if (!Interop.WinHttp.WinHttpSendRequest(
                    state.RequestHandle,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    0,
                    state.ToIntPtr()))
                {
                    // WinHTTP doesn't always associate our context value (state object) to the request handle.
                    // And thus we might not get a HANDLE_CLOSING notification which would normally cause the
                    // state object to be unpinned and disposed. So, we manually dispose the request handle and
                    // state object here.
                    state.RequestHandle.Dispose();
                    state.Dispose();
                    WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSendRequest));
                }
            }

            return state.LifecycleAwaitable;
        }

        private async Task InternalSendRequestBodyAsync(WinHttpRequestState state, WinHttpChunkMode chunkedModeForSend)
        {
            using (var requestStream = new WinHttpRequestStream(state, chunkedModeForSend))
            {
                await state.RequestMessage.Content.CopyToAsync(requestStream, state.TransportContext).ConfigureAwait(false);
                await requestStream.EndUploadAsync(state.CancellationToken).ConfigureAwait(false);
            }
        }

        private RendezvousAwaitable<int> InternalReceiveResponseHeadersAsync(WinHttpRequestState state)
        {
            lock (state.Lock)
            {
                if (!Interop.WinHttp.WinHttpReceiveResponse(state.RequestHandle, IntPtr.Zero))
                {
                    throw WinHttpException.CreateExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpReceiveResponse));
                }
            }

            return state.LifecycleAwaitable;
        }
    }
}
