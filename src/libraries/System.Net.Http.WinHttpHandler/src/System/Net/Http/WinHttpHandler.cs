// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
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
        private static Interop.WinHttp.WINHTTP_STATUS_CALLBACK s_staticCallback = 
            new Interop.WinHttp.WINHTTP_STATUS_CALLBACK(WinHttpStatusCallback);

        // TODO:  This looks messy but it is fast. Research a cleaner way
        // to do this which keeps high performance lookup.
        //
        // Fast lookup table to convert WINHTTP_AUTH constants to strings.
        // WINHTTP_AUTH_SCHEME_BASIC = 0x00000001;
        // WINHTTP_AUTH_SCHEME_DIGEST = 0x00000008;
        // WINHTTP_AUTH_SCHEME_NEGOTIATE = 0x00000010;
        private static readonly string[] s_authSchemeStringMapping =
        {
            null,
            "Basic",
            null,
            null,
            null,
            null,
            null,
            null,
            "Digest",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Negotiate"
        };

        private static readonly uint[] s_authSchemePriorityOrder =
        {
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_NEGOTIATE,
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_DIGEST,
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_BASIC
        };

        #region Constants
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";
        private const string UriSchemeHttps = "https";
        private const string HeaderNameContentLength = "Content-Length";
        private const string HeaderNameContentEncoding = "Content-Encoding";
        private const string HeaderNameCookie = "Cookie";
        private const string HeaderNameSetCookie = "Set-Cookie";
        private const string EncodingNameDeflate = "DEFLATE";
        private const string EncodingNameGzip = "GZIP";
        private const string EmptyCookieHeader = HeaderNameCookie + ":";
        private static readonly string[] s_httpHeadersSeparator = { "\r\n" };
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        #endregion

        #region Fields
        private object _lockObject = new object();
        private bool _doManualDecompressionCheck = false;
        private WinInetProxyHelper _proxyHelper = null;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private CookieUsePolicy _cookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly;
        private CookieContainer _cookieContainer = null;

        // TODO: This current design uses a handler-wide lock to Add/Retrieve
        // from the cache.  Need to improve this for next iteration in order
        // to boost performance and scalability.
        private CredentialCache _credentialCache = new CredentialCache();
        private object _credentialCacheLock = new object();

        private SslProtocols _sslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
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
        #endregion

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
                        "value",
                        value,
                        string.Format(SR.net_http_value_must_be_greater_than, 0));
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
                    throw new ArgumentOutOfRangeException("value");
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
                CheckDisposedOrStarted();

                SslProtocols allowedSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
                if (value == SslProtocols.None || (value & ~allowedSslProtocols) != 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

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
                    throw new ArgumentOutOfRangeException("value");
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
                    throw new ArgumentOutOfRangeException("value");
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
                        "value",
                        value,
                        string.Format(SR.net_http_value_must_be_greater_than, 0));
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
                    throw new ArgumentOutOfRangeException("value");
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
                    throw new ArgumentOutOfRangeException("value");
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
                    throw new ArgumentOutOfRangeException("value");
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
                    throw new ArgumentOutOfRangeException("value");
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
                        "value",
                        value,
                        string.Format(SR.net_http_value_must_be_greater_than, 0));
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
                        "value",
                        value,
                        string.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxResponseDrainSize = value;
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (_sessionHandle != null)
                {
                    _sessionHandle.DangerousRelease();
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
                throw new ArgumentNullException("request", SR.net_http_handler_norequest);
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

            // Create RequestState object and save current values of handler settings.
            RequestState state = new RequestState();
            state.Tcs = tcs;
            state.CancellationToken = cancellationToken;
            state.RequestMessage = request;
            state.Handler = this;
            state.CheckCertificateRevocationList = _checkCertificateRevocationList;
            state.ServerCertificateValidationCallback = _serverCertificateValidationCallback;
            state.WindowsProxyUsePolicy = _windowsProxyUsePolicy;
            state.Proxy = _proxy;
            state.ServerCredentials = _serverCredentials;

            try
            {
                Task.Factory.StartNew(
                    StartRequest,
                    state,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);                
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("SendAsync", ex);
            }

            return tcs.Task;
        }

        private static uint ChooseAuthScheme(uint supportedSchemes)
        {
            foreach (uint authScheme in s_authSchemePriorityOrder)
            {
                if ((supportedSchemes & authScheme) != 0)
                {
                    return authScheme;
                }
            }

            return 0;
        }

        private static void WinHttpStatusCallback(
            IntPtr handle,
            IntPtr context,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            RequestState state;

            // TODO: Use logging.

            if (internetStatus != Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REDIRECT &&
                internetStatus != Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDING_REQUEST)
            {
                return;
            }

            // Get request state object.
            try
            {
                GCHandle gch = GCHandle.FromIntPtr(context);
                state = (RequestState)gch.Target;
            }
            catch (Exception ex)
            {
                // TODO: We should log this but an exception here is pretty rare and
                // is probably the result of a programming error.
                Debug.Fail("Unhandled exception in WinHTTP callback: " + ex);                

                // Closing the WinHTTP request handle will cancel the operation.
                Interop.WinHttp.WinHttpCloseHandle(handle);

                return;
            }

            if (internetStatus == Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REDIRECT)
            {
                try
                {
                    string redirectUriString = Marshal.PtrToStringUni(statusInformation);
                    var redirectUri = new Uri(redirectUriString);

                    // If we're manually handling cookies, we need to reset them
                    // based on the new URI.
                    if (state.Handler._cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
                    {
                        // Clear cookies.
                        if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                            state.RequestHandle,
                            EmptyCookieHeader,
                            (uint)EmptyCookieHeader.Length,
                            Interop.WinHttp.WINHTTP_ADDREQ_FLAG_REPLACE))
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            if (lastError != Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
                            {
                                throw WinHttpException.CreateExceptionUsingError(lastError);
                            }
                        }

                        // Re-add cookies. The GetCookieHeader() method will return the correct set of
                        // cookies based on the redirectUri.
                        string cookieHeader = GetCookieHeader(redirectUri, state.Handler._cookieContainer);
                        if (!string.IsNullOrEmpty(cookieHeader))
                        {
                            if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                                state.RequestHandle,
                                cookieHeader,
                                (uint)cookieHeader.Length,
                                Interop.WinHttp.WINHTTP_ADDREQ_FLAG_ADD))
                            {
                                WinHttpException.ThrowExceptionUsingLastError();
                            }
                        }
                    }

                    state.RequestMessage.RequestUri = redirectUri;
                    
                    // Redirection to a new uri may require a new connection through a potentially different proxy.
                    // If so, we will need to respond to additional 407 proxy auth demands and re-attach any
                    // proxy credentials. The ProcessResponse() method looks at the state.LastStatusCode
                    // before attaching proxy credentials and marking the HTTP request to be re-submitted.
                    // So we need to reset the LastStatusCode remembered. Otherwise, it will see additional 407
                    // responses as an indication that proxy auth failed and won't retry the HTTP request.
                    if (state.LastStatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                    {
                        state.LastStatusCode = 0;
                    }

                    // For security reasons, we drop the server credential if it is a 
                    // NetworkCredential.  But we allow credentials in a CredentialCache
                    // since they are specifically tied to URI's.
                    if (!(state.ServerCredentials is CredentialCache))
                    {
                        state.ServerCredentials = null;
                    }
                }
                catch (Exception ex)
                {
                    Interop.WinHttp.WinHttpCloseHandle(handle);
                    state.SavedException = ex;
                }
            }
            else if (internetStatus == Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDING_REQUEST)
            {
                if (state.RequestMessage.RequestUri.Scheme != UriSchemeHttps)
                {
                    // Not SSL/TLS.
                    return;
                }

                // Grab the channel binding token (CBT) information from the request handle and put it into
                // the TransportContext object.
                try
                {
                    state.TransportContext.SetChannelBinding(state.RequestHandle);
                }
                catch (Exception ex)
                {
                    Interop.WinHttp.WinHttpCloseHandle(handle);
                    state.SavedException = ex;

                    return;
                }

                if (state.ServerCertificateValidationCallback != null)
                {
                    IntPtr certHandle = IntPtr.Zero;
                    uint certHandleSize = (uint)IntPtr.Size;

                    if (Interop.WinHttp.WinHttpQueryOption(
                        state.RequestHandle,
                        Interop.WinHttp.WINHTTP_OPTION_SERVER_CERT_CONTEXT,
                        ref certHandle,
                        ref certHandleSize))
                    {
                        // Create a managed wrapper around the certificate handle. Since this results in duplicating
                        // the handle, we will close the original handle after creating the wrapper.
                        var serverCertificate = new X509Certificate2(certHandle);
                        Interop.Crypt32.CertFreeCertificateContext(certHandle);

                        X509Chain chain = null;
                        SslPolicyErrors sslPolicyErrors;

                        try
                        {
                            CertificateHelper.BuildChain(
                                serverCertificate,
                                state.RequestMessage.RequestUri.Host,
                                state.CheckCertificateRevocationList,
                                out chain,
                                out sslPolicyErrors);

                            bool result = state.ServerCertificateValidationCallback(
                                state.RequestMessage,
                                serverCertificate,
                                chain,
                                sslPolicyErrors);
                            if (!result)
                            {
                                Interop.WinHttp.WinHttpCloseHandle(handle);
                                state.SavedException = 
                                    WinHttpException.CreateExceptionUsingError((int)Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interop.WinHttp.WinHttpCloseHandle(handle);
                            state.SavedException = ex;
                        }
                        finally
                        {
                            if (chain != null)
                            {
                                chain.Dispose();
                            }
                        }
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Interop.WinHttp.WinHttpCloseHandle(handle);
                        state.SavedException = WinHttpException.CreateExceptionUsingError(lastError);
                    }
                }
            }
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

        // TODO: Refactor to smaller files. Perhaps split cookie handling into separate class.
        private static string GetCookieHeader(Uri uri, CookieContainer cookies)
        {
            string cookieHeader = null;

            Debug.Assert(cookies != null);

            string cookieValues = cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieValues))
            {
                cookieHeader = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", HeaderNameCookie, cookieValues);
            }

            return cookieHeader;
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
                string cookieHeader = GetCookieHeader(requestMessage.RequestUri, cookies);
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

        private void EnsureSessionHandleExists(RequestState state)
        {
            bool ignore = false;

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
                            0);
                        if (!_sessionHandle.IsInvalid)
                        {
                            _sessionHandle.DangerousAddRef(ref ignore);

                            return;
                        }

                        int lastError = Marshal.GetLastWin32Error();
                        if (lastError != Interop.WinHttp.ERROR_INVALID_PARAMETER)
                        {
                            throw new HttpRequestException(
                                SR.net_http_client_execution_error,
                                WinHttpException.CreateExceptionUsingError(lastError));
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
                            0);
                        if (_sessionHandle.IsInvalid)
                        {
                            throw new HttpRequestException(
                                SR.net_http_client_execution_error,
                                WinHttpException.CreateExceptionUsingLastError());
                        }

                        _sessionHandle.DangerousAddRef(ref ignore);
                    }
                }
            }
        }

        private bool GetServerCredentialsFromCache(
            Uri uri,
            out uint serverAuthScheme,
            out NetworkCredential serverCredentials)
        {
            serverAuthScheme = 0;
            serverCredentials = null;

            NetworkCredential cred = null;

            lock (_credentialCacheLock)
            {
                foreach (uint authScheme in s_authSchemePriorityOrder)
                {
                    cred = _credentialCache.GetCredential(uri, s_authSchemeStringMapping[authScheme]);
                    if (cred != null)
                    {
                        serverAuthScheme = authScheme;
                        serverCredentials = cred;

                        return true;
                    }
                }
            }

            return false;
        }

        private void SaveServerCredentialsToCache(Uri uri, uint authScheme, ICredentials serverCredentials)
        {
            string authType = s_authSchemeStringMapping[authScheme];
            Debug.Assert(!string.IsNullOrEmpty(authType));

            NetworkCredential cred = serverCredentials.GetCredential(uri, authType);
            if (cred != null)
            {
                lock (_credentialCacheLock)
                {
                    try
                    {
                        _credentialCache.Add(uri, authType, cred);
                    }
                    catch (ArgumentException)
                    {
                        // The credential was already added.
                    }
                }
            }
        }

        private async void StartRequest(object obj)
        {
            RequestState state = (RequestState)obj;
            bool secureConnection = false;
            HttpResponseMessage responseMessage = null;
            Exception savedException = null;
            SafeWinHttpHandle connectHandle = null;
            SafeWinHttpHandle requestHandle = null;
            WinHttpRequestStream requestStream = null;
            GCHandle requestStateHandle = new GCHandle();

            if (state.CancellationToken.IsCancellationRequested)
            {
                state.Tcs.TrySetCanceled(state.CancellationToken);
                return;
            }

            var cancellationTokenRegistration = state.CancellationToken.Register(s =>
            {
                RequestState rs = (RequestState)s;
                rs.Tcs.TrySetCanceled(rs.CancellationToken);
            }, state);

            try
            {
                // Prepare context object.
                requestStateHandle = GCHandle.Alloc(state);

                EnsureSessionHandleExists(state);

                SetSessionHandleOptions();

                // Specify an HTTP server.
                connectHandle = Interop.WinHttp.WinHttpConnect(
                    _sessionHandle,
                    state.RequestMessage.RequestUri.Host,
                    (ushort)state.RequestMessage.RequestUri.Port,
                    0);
                if (connectHandle.IsInvalid)
                {
                    throw new HttpRequestException(
                        SR.net_http_client_execution_error,
                        WinHttpException.CreateExceptionUsingLastError());
                }

                if (state.RequestMessage.RequestUri.Scheme == UriSchemeHttps)
                {
                    secureConnection = true;
                }
                else
                {
                    secureConnection = false;
                }

                // Create an HTTP request handle.
                requestHandle = Interop.WinHttp.WinHttpOpenRequest(
                    connectHandle,
                    state.RequestMessage.Method.Method,
                    state.RequestMessage.RequestUri.PathAndQuery,
                    null,
                    Interop.WinHttp.WINHTTP_NO_REFERER,
                    Interop.WinHttp.WINHTTP_DEFAULT_ACCEPT_TYPES,
                    secureConnection ? Interop.WinHttp.WINHTTP_FLAG_SECURE : 0);
                if (requestHandle.IsInvalid)
                {
                    throw new HttpRequestException(
                        SR.net_http_client_execution_error,
                        WinHttpException.CreateExceptionUsingLastError());
                }

                state.RequestHandle = requestHandle;

                // Set callback function.
                SetStatusCallback(requestHandle, s_staticCallback);

                // Set needed options on the request handle.
                SetRequestHandleOptions(state);

                bool chunkedModeForSend = IsChunkedModeForSend(state.RequestMessage);

                AddRequestHeaders(
                    requestHandle,
                    state.RequestMessage,
                    _cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer ? _cookieContainer : null);

                uint proxyAuthScheme = 0;
                uint serverAuthScheme = 0;
                bool retryRequest = false;

                do
                {
                    state.CancellationToken.ThrowIfCancellationRequested();

                    PreAuthenticateRequest(state, requestHandle, proxyAuthScheme);

                    // Send a request.
                    if (!Interop.WinHttp.WinHttpSendRequest(
                        requestHandle,
                        null,
                        0,
                        IntPtr.Zero,
                        0,
                        0,
                        GCHandle.ToIntPtr(requestStateHandle)))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    // Send request body if present.
                    if (state.RequestMessage.Content != null)
                    {
                        requestStream = new WinHttpRequestStream(requestHandle, chunkedModeForSend);
                        await state.RequestMessage.Content.CopyToAsync(
                            requestStream,
                            state.TransportContext).ConfigureAwait(false);
                        requestStream.EndUpload();
                    }

                    state.CancellationToken.ThrowIfCancellationRequested();

                    // End the request and wait for the response.
                    if (!Interop.WinHttp.WinHttpReceiveResponse(requestHandle, IntPtr.Zero))
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        if (lastError == (int)Interop.WinHttp.ERROR_WINHTTP_RESEND_REQUEST)
                        {
                            retryRequest = true;
                        }
                        else if (lastError == (int)Interop.WinHttp.ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED)
                        {
                            // WinHttp will automatically drop any client SSL certificates that we
                            // have pre-set into the request handle.  For security reasons, we don't
                            // allow the certificate to be re-applied. But we need to tell WinHttp
                            // that we don't have any certificate.
                            SetNoClientCertificate(requestHandle);
                            retryRequest = true;
                        }

                        else
                        {
                            throw WinHttpException.CreateExceptionUsingError(lastError);
                        }
                    }
                    else
                    {
                        ProcessResponse(
                            state,
                            requestHandle,
                            ref proxyAuthScheme,
                            ref serverAuthScheme,
                            out retryRequest);
                    }
                } while (retryRequest);

                // Clear callback function in WinHTTP once we have a final response
                // and are ready to create the response message.
                SetStatusCallback(requestHandle, null);

                state.CancellationToken.ThrowIfCancellationRequested();

                // Create HttpResponseMessage object.
                responseMessage = CreateResponseMessage(connectHandle, requestHandle, state.RequestMessage);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    savedException = ex;
                }
                else if (state.SavedException != null)
                {
                    savedException = state.SavedException;
                }
                else
                {
                    savedException = ex;
                }

                // Clear callback function in WinHTTP to prevent
                // further native callbacks as we clean up the objects.
                if (requestHandle != null)
                {
                    SetStatusCallback(requestHandle, null);
                }
            }

            if (requestStateHandle.IsAllocated)
            {
                requestStateHandle.Free();
            }

            if (requestStream != null)
            {
                requestStream.Dispose();
            }

            SafeWinHttpHandle.DisposeAndClearHandle(ref requestHandle);
            SafeWinHttpHandle.DisposeAndClearHandle(ref connectHandle);

            // Move the task to a terminal state.
            if (responseMessage != null)
            {
                state.Tcs.TrySetResult(responseMessage);
            }
            else
            {
                HandleAsyncException(state, savedException);
            }

            cancellationTokenRegistration.Dispose();
        }

        private void ProcessResponse(
            RequestState state,
            SafeWinHttpHandle requestHandle,
            ref uint proxyAuthScheme,
            ref uint serverAuthScheme,
            out bool retryRequest)
        {
            retryRequest = false;

            // Check the status code and retry the request applying credentials if needed.
            var statusCode = (HttpStatusCode)GetResponseHeaderNumberInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE);
            uint supportedSchemes = 0;
            uint firstSchemeIgnored = 0;
            uint authTarget = 0;
            Uri uri = state.RequestMessage.RequestUri;

            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    if (state.ServerCredentials == null || state.LastStatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Either we don't have server credentials or we already tried 
                        // to set the credentials and it failed before.
                        // So we will let the 401 be the final status code returned.
                        break;
                    }
                    state.LastStatusCode = statusCode;

                    // Determine authorization scheme to use. We ignore the firstScheme
                    // parameter which is included in the supportedSchemes flags already.
                    // We pass the schemes to ChooseAuthScheme which will pick the scheme
                    // based on most secure scheme to least secure scheme ordering.
                    if (!Interop.WinHttp.WinHttpQueryAuthSchemes(
                        requestHandle,
                        out supportedSchemes,
                        out firstSchemeIgnored,
                        out authTarget))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    // Verify the authTarget is for server authentication only.
                    if (authTarget != Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER)
                    {
                        // TODO: Protocol violation. Add detailed error message.
                        throw new InvalidOperationException();
                    }

                    serverAuthScheme = ChooseAuthScheme(supportedSchemes);
                    if (serverAuthScheme != 0)
                    {
                        SetWinHttpCredential(
                            requestHandle,
                            state.ServerCredentials,
                            uri,
                            serverAuthScheme,
                            authTarget);

                        retryRequest = true;
                    }
                    break;

                case HttpStatusCode.ProxyAuthenticationRequired:
                    if (state.LastStatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                    {
                        // We tried already to set the credentials.
                        break;
                    }
                    state.LastStatusCode = statusCode;

                    // Determine authorization scheme to use. We ignore the firstScheme
                    // parameter which is included in the supportedSchemes flags already.
                    // We pass the schemes to ChooseAuthScheme which will pick the scheme
                    // based on most secure scheme to least secure scheme ordering.
                    if (!Interop.WinHttp.WinHttpQueryAuthSchemes(
                        requestHandle,
                        out supportedSchemes,
                        out firstSchemeIgnored,
                        out authTarget))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    // Verify the authTarget is for proxy authentication only.
                    if (authTarget != Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY)
                    {
                        // TODO: Protocol violation. Add detailed error message.
                        throw new InvalidOperationException();
                    }

                    proxyAuthScheme = ChooseAuthScheme(supportedSchemes);

                    retryRequest = true;
                    break;

                default:
                    if (_preAuthenticate && serverAuthScheme != 0)
                    {
                        SaveServerCredentialsToCache(uri, serverAuthScheme, state.ServerCredentials);
                    }
                    break;
            }
        }

        private void PreAuthenticateRequest(
            RequestState state,
            SafeWinHttpHandle requestHandle,
            uint proxyAuthScheme)
        {
            // Set proxy credentials if we have them.
            // If a proxy authentication challenge was responded to, reset
            // those credentials before each SendRequest, because the proxy  
            // may require re-authentication after responding to a 401 or  
            // to a redirect. If you don't, you can get into a 
            // 407-401-407-401- loop.
            if (proxyAuthScheme != 0)
            {
                SetWinHttpCredential(
                    requestHandle,
                    state.Proxy == null ? _defaultProxyCredentials : state.Proxy.Credentials,
                    state.RequestMessage.RequestUri,
                    proxyAuthScheme,
                    Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY);
            }

            // Apply pre-authentication headers for server authentication?
            if (_preAuthenticate)
            {
                uint authScheme;
                NetworkCredential serverCredentials;
                if (GetServerCredentialsFromCache(
                    state.RequestMessage.RequestUri,
                    out authScheme,
                    out serverCredentials))
                {
                    SetWinHttpCredential(
                        requestHandle,
                        serverCredentials,
                        state.RequestMessage.RequestUri,
                        authScheme,
                        Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);
                    state.LastStatusCode = HttpStatusCode.Unauthorized; // Remember we already set the creds.
                }
            }
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

        private void SetRequestHandleOptions(RequestState state)
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

        private void SetRequestHandleProxyOptions(RequestState state)
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
                        updateProxySettings = true;
                        _proxyHelper.GetProxyForUrl(_sessionHandle, uri, out proxyInfo);
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
            // Must be HTTPS scheme to use client certificates.
            if (requestUri.Scheme != UriSchemeHttps)
            {
                return;
            }

            // Get candidate list for client certificates.
            X509Certificate2Collection certs;
            if (_clientCertificateOption == ClientCertificateOption.Manual)
            {
                certs = ClientCertificates;
            }
            else
            {
                using (var myStore = new X509Store())
                {
                    myStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                    certs = myStore.Certificates;
                }
            }

            // Check for no certs now as a performance optimization.
            if (certs.Count == 0)
            {
                SetNoClientCertificate(requestHandle);
                return;
            }

            // Reduce the set of certificates to match the proper 'Client Authentication' criteria.
            certs = certs.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, true);
            certs = certs.Find(X509FindType.FindByApplicationPolicy, ClientAuthenticationOID, true);

            // Build a new collection with certs that have a private key. Need to do this
            // manually because there is no X509FindType to match this criteria.
            var clientCerts = new X509Certificate2Collection();
            foreach (var cert in certs)
            {
                if (cert.HasPrivateKey)
                {
                    clientCerts.Add(cert);
                }
            }

            // TOOD: Filter the list based on TrustedIssuerList info from WINHTTP.

            // Set the client certificate.
            if (certs.Count == 0)
            {
                SetNoClientCertificate(requestHandle);
            }
            else
            {
                SetWinHttpOption(
                    requestHandle,
                    Interop.WinHttp.WINHTTP_OPTION_CLIENT_CERT_CONTEXT,
                    clientCerts[0].Handle,
                    (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CONTEXT>());
            }
        }

        private static void SetNoClientCertificate(SafeWinHttpHandle requestHandle)
        {
            SetWinHttpOption(
                requestHandle,
                Interop.WinHttp.WINHTTP_OPTION_CLIENT_CERT_CONTEXT,
                IntPtr.Zero,
                0);
        }

        private void SetRequestHandleCredentialsOptions(RequestState state)
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

        private void SetWinHttpCredential(
            SafeWinHttpHandle requestHandle,
            ICredentials credentials,
            Uri uri,
            uint authScheme,
            uint authTarget)
        {
            Debug.Assert(credentials != null);
            Debug.Assert(authScheme != 0);
            Debug.Assert(authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY || 
                         authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);

            NetworkCredential networkCredential = credentials.GetCredential(uri, s_authSchemeStringMapping[authScheme]);

            // Skip if no credentials or this is the default credential.
            if (networkCredential == null || networkCredential == CredentialCache.DefaultNetworkCredentials)
            {
                return;
            }

            string userName = networkCredential.UserName;
            string password = networkCredential.Password;
            string domain = networkCredential.Domain;

            // WinHTTP does not support a blank username.  So, we will throw an exception.
            if (string.IsNullOrEmpty(userName))
            {
                // TODO: Add error message.
                throw new InvalidOperationException();
            }

            if (!string.IsNullOrEmpty(domain))
            {
                userName = domain + "\\" + userName;
            }

            if (!Interop.WinHttp.WinHttpSetCredentials(
                requestHandle,
                authTarget,
                authScheme,
                userName,
                password,
                IntPtr.Zero))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
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

        private HttpResponseMessage CreateResponseMessage(
            SafeWinHttpHandle connectHandle,
            SafeWinHttpHandle requestHandle,
            HttpRequestMessage request)
        {
            var response = new HttpResponseMessage();
            bool useDeflateDecompression = false;
            bool useGzipDecompression = false;

            // Get HTTP version, status code, reason phrase from the response headers.
            string version = GetResponseHeaderStringInfo(requestHandle, Interop.WinHttp.WINHTTP_QUERY_VERSION);
            if (string.Compare("HTTP/1.1", version, StringComparison.OrdinalIgnoreCase) == 0)
            {
                response.Version = new Version(1, 1);
            }
            else if (string.Compare("HTTP/1.0", version, StringComparison.OrdinalIgnoreCase) == 0)
            {
                response.Version = new Version(1, 0);
            }
            else
            {
                response.Version = null;
            }

            response.StatusCode = (HttpStatusCode)GetResponseHeaderNumberInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE);
            response.ReasonPhrase = GetResponseHeaderStringInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_TEXT);

            if (_doManualDecompressionCheck)
            {
                string contentEncoding = GetResponseHeaderStringInfo(
                    requestHandle,
                    Interop.WinHttp.WINHTTP_QUERY_CONTENT_ENCODING);
                if (!string.IsNullOrEmpty(contentEncoding))
                {
                    if (contentEncoding.IndexOf(EncodingNameDeflate, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        useDeflateDecompression = true;
                    }
                    else if (contentEncoding.IndexOf(EncodingNameGzip, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        useGzipDecompression = true;
                    }
                }
            }

            // Create response stream and wrap it in a StreamContent object.
            var responseStream = new WinHttpResponseStream(_sessionHandle, connectHandle, requestHandle);
            Stream decompressedStream = responseStream;
            if (_doManualDecompressionCheck)
            {
                if (useDeflateDecompression)
                {
                    decompressedStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                }
                else if (useGzipDecompression)
                {
                    decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress);
                }
            }

            var content = new StreamContent(decompressedStream);

            response.Content = content;
            response.RequestMessage = request;

            // Parse raw response headers and place them into response message.
            ParseResponseHeaders(requestHandle, response, useDeflateDecompression || useGzipDecompression);

            // Store response header cookies into custom CookieContainer.
            if (_cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
            {
                Debug.Assert(_cookieContainer != null);

                if (response.Headers.Contains(HeaderNameSetCookie))
                {
                    IEnumerable<string> cookieHeaders = response.Headers.GetValues(HeaderNameSetCookie);
                    foreach (var cookieHeader in cookieHeaders)
                    {
                        try
                        {
                            _cookieContainer.SetCookies(request.RequestUri, cookieHeader);
                        }
                        catch (CookieException)
                        {
                            // We ignore malformed cookies in the response.
                        }
                    }
                }
            }

            // Since the headers have been read, set the "receive" timeout to be based on each read
            // call of the response body data. WINHTTP_OPTION_RECEIVE_TIMEOUT sets a timeout on each
            // lower layer winsock read.
            uint optionData = (uint)_receiveDataTimeout.TotalMilliseconds;
            SetWinHttpOption(requestHandle, Interop.WinHttp.WINHTTP_OPTION_RECEIVE_TIMEOUT, ref optionData);

            return response;
        }

        private uint GetResponseHeaderNumberInfo(SafeWinHttpHandle requestHandle, uint infoLevel)
        {
            uint result = 0;
            uint resultSize = sizeof(uint);

            if (!Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel | Interop.WinHttp.WINHTTP_QUERY_FLAG_NUMBER,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                ref result,
                ref resultSize,
                IntPtr.Zero))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return result;
        }

        private string GetResponseHeaderStringInfo(SafeWinHttpHandle requestHandle, uint infoLevel)
        {
            uint bytesNeeded = 0;
            bool results = false;

            // Call WinHttpQueryHeaders once to obtain the size of the buffer needed.  The size is returned in
            // bytes but the API actually returns Unicode characters.
            if (!Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                null,
                ref bytesNeeded,
                IntPtr.Zero))
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
                {
                    return null;
                }

                if (lastError != Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw WinHttpException.CreateExceptionUsingError(lastError);
                }
            }

            // Allocate space for the buffer.
            int charsNeeded = (int)bytesNeeded / 2;
            var buffer = new StringBuilder(charsNeeded, charsNeeded);

            results = Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                buffer,
                ref bytesNeeded,
                IntPtr.Zero);
            if (!results)
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return buffer.ToString();
        }

        private void ParseResponseHeaders(
            SafeWinHttpHandle requestHandle,
            HttpResponseMessage response,
            bool stripEncodingHeaders)
        {
            string rawResponseHeaders = GetResponseHeaderStringInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_RAW_HEADERS_CRLF);
            string[] responseHeaderArray = rawResponseHeaders.Split(
                s_httpHeadersSeparator,
                StringSplitOptions.RemoveEmptyEntries);

            // Parse the array of headers and split them between Content headers and Response headers.
            // Skip the first line which contains status code, etc. information that we already parsed.
            for (int i = 1; i < responseHeaderArray.Length; i++)
            {
                int colonIndex = responseHeaderArray[i].IndexOf(':');

                // Skip malformed header lines that are missing the colon character.
                if (colonIndex > 0)
                {
                    string headerName = responseHeaderArray[i].Substring(0, colonIndex);
                    string headerValue = responseHeaderArray[i].Substring(colonIndex + 1).Trim(); // Normalize header value by trimming white space.

                    if (!response.Headers.TryAddWithoutValidation(headerName, headerValue))
                    {
                        if (stripEncodingHeaders)
                        {
                            // Remove Content-Length and Content-Encoding headers if we are
                            // decompressing the response stream in the handler (due to 
                            // WINHTTP not supporting it in a particular downlevel platform). 
                            // This matches the behavior of WINHTTP when it does decompression iself.
                            if (string.Equals(
                                HeaderNameContentLength,
                                headerName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (string.Equals(
                                HeaderNameContentEncoding,
                                headerName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        // TODO: Should we log if there is an error here?
                        response.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                    }
                }
            }
        }

        private void HandleAsyncException(RequestState state, Exception ex)
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
            IntPtr oldCallback = Interop.WinHttp.WinHttpSetStatusCallback(
                requestHandle,
                callback,
                Interop.WinHttp.WINHTTP_CALLBACK_FLAG_ALL_NOTIFICATIONS,
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

        private class RequestState
        {
            public RequestState()
            {
                TransportContext = new WinHttpTransportContext();
            }

            public TaskCompletionSource<HttpResponseMessage> Tcs { get; set; }

            public CancellationToken CancellationToken { get; set; }

            public HttpRequestMessage RequestMessage { get; set; }

            public WinHttpHandler Handler { get; set; }

            public SafeWinHttpHandle RequestHandle { get; set; }

            public Exception SavedException { get; set; }

            public bool CheckCertificateRevocationList { get; set; }

            public Func<
                HttpRequestMessage,
                X509Certificate2,
                X509Chain,
                SslPolicyErrors,
                bool> ServerCertificateValidationCallback { get; set; }

            public WinHttpTransportContext TransportContext { get; private set; }

            public WindowsProxyUsePolicy WindowsProxyUsePolicy { get; set; }

            public IWebProxy Proxy { get; set; }

            public ICredentials ServerCredentials { get; set; }
            
            public HttpStatusCode LastStatusCode { get; set; }
        }
    }
}
