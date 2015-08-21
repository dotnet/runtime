// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SafeCurlHandle = Interop.libcurl.SafeCurlHandle;
using SafeCurlMultiHandle = Interop.libcurl.SafeCurlMultiHandle;
using SafeCurlSlistHandle = Interop.libcurl.SafeCurlSlistHandle;
using CURLoption = Interop.libcurl.CURLoption;
using CURLMoption = Interop.libcurl.CURLMoption;
using CURLcode = Interop.libcurl.CURLcode;
using CURLMcode = Interop.libcurl.CURLMcode;
using CURLINFO = Interop.libcurl.CURLINFO;
using CURLAUTH = Interop.libcurl.CURLAUTH;
using CurlVersionInfoData = Interop.libcurl.curl_version_info_data;
using CurlFeatures = Interop.libcurl.CURL_VERSION_Features;
using CURLProxyType = Interop.libcurl.curl_proxytype;
using size_t = System.IntPtr;


namespace System.Net.Http
{  
    internal partial class CurlHandler : HttpMessageHandler
    {
        #region Constants

        private const string UriSchemeHttp = "http";
        private const string UriSchemeHttps = "https";
        private const string EncodingNameGzip = "gzip";
        private const string EncodingNameDeflate = "deflate";
        private readonly static string[] AuthenticationSchemes = { "Negotiate", "Digest", "Basic" }; // the order in which libcurl goes over authentication schemes
        private readonly static ulong[]  AuthSchemePriorityOrder = { CURLAUTH.Negotiate, CURLAUTH.Digest, CURLAUTH.Basic };

        private const int s_requestBufferSize = 16384; // Default used by libcurl
        private const string NoTransferEncoding = HttpKnownHeaderNames.TransferEncoding + ":";
        private readonly static CurlVersionInfoData curlVersionInfoData;
        private const int CurlAge = 5;
        private const int MinCurlAge = 3;

        #endregion

        #region Fields

        private static readonly bool _supportsAutomaticDecompression;
        private static readonly bool _supportsSSL;
        private volatile bool _anyOperationStarted;
        private volatile bool _disposed;
        private IWebProxy _proxy = null;
        private ICredentials _serverCredentials = null;
        private ProxyUsePolicy _proxyPolicy = ProxyUsePolicy.UseDefaultProxy;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private SafeCurlMultiHandle _multiHandle;
        private GCHandle _multiHandlePtr = new GCHandle();
        private bool _preAuthenticate = false;
        private CredentialCache _credentialCache = null;
        private CookieContainer _cookieContainer = null;
        private bool _useCookie = false;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;

        #endregion        

        static CurlHandler()
        {
            int result = Interop.libcurl.curl_global_init(Interop.libcurl.CurlGlobalFlags.CURL_GLOBAL_ALL);
            if (result != CURLcode.CURLE_OK)
            {
                throw new InvalidOperationException("Cannot use libcurl in this process");
            }
            curlVersionInfoData = Marshal.PtrToStructure<CurlVersionInfoData>(Interop.libcurl.curl_version_info(CurlAge));
            if (curlVersionInfoData.age < MinCurlAge)
            {
                throw new InvalidOperationException(SR.net_http_unix_https_libcurl_too_old);
            }
            _supportsSSL = (CurlFeatures.CURL_VERSION_SSL & curlVersionInfoData.features) != 0;
            _supportsAutomaticDecompression = (CurlFeatures.CURL_VERSION_LIBZ & curlVersionInfoData.features) != 0;
        }

        internal CurlHandler()
        {
            _multiHandle = Interop.libcurl.curl_multi_init();
            if (_multiHandle.IsInvalid)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error);
            }
            _multiHandle.Timer = new Timer(CurlTimerElapsed, _multiHandle, Timeout.Infinite, Timeout.Infinite);
            SetCurlMultiOptions();
        }

        #region Properties

        internal bool AutomaticRedirection
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

        internal bool SupportsProxy
        {
            get
            {
                return true;
            }
        }

        internal bool UseProxy
        {
            get
            {
                return _proxyPolicy != ProxyUsePolicy.DoNotUseProxy;
            }

            set
            {
                CheckDisposedOrStarted();
                if (value)
                {
                    _proxyPolicy = ProxyUsePolicy.UseCustomProxy;
                }
                else
                {
                    _proxyPolicy = ProxyUsePolicy.DoNotUseProxy;
                }
            }
        }

        internal IWebProxy Proxy
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
        
        internal ICredentials Credentials
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

        internal ClientCertificateOption ClientCertificateOptions
        {
            get
            {
                return ClientCertificateOption.Manual;
            }

            set
            {
                if (ClientCertificateOption.Manual != value)
                {
                    throw new PlatformNotSupportedException(SR.net_http_unix_invalid_client_cert_option);
                }
            }
        }

        internal bool SupportsAutomaticDecompression
        {
            get
            {
                return _supportsAutomaticDecompression;
            }
        }

        internal DecompressionMethods AutomaticDecompression
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

        internal bool PreAuthenticate
        {
            get
            {
                return _preAuthenticate;
            }
            set
            {
                CheckDisposedOrStarted();
                _preAuthenticate = value;
                if (value)
                {
                    _credentialCache = new CredentialCache();
                }
            }
        }

        internal bool UseCookie
        {
            get
            {
                return _useCookie;
            }

            set
            {               
                CheckDisposedOrStarted();
                _useCookie = value;
            }
        }

        internal CookieContainer CookieContainer
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

        internal int MaxAutomaticRedirections
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

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_multiHandlePtr.IsAllocated)
                {
                    _multiHandlePtr.Free();
                }
                _multiHandle = null;
            }

            base.Dispose(disposing);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", SR.net_http_handler_norequest);
            }

            if ((request.RequestUri.Scheme != UriSchemeHttp) && (request.RequestUri.Scheme != UriSchemeHttps))
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_http_client_http_baseaddress_required);
            }

            if (request.RequestUri.Scheme == UriSchemeHttps && !_supportsSSL)
            {
                throw new PlatformNotSupportedException(SR.net_http_unix_https_support_unavailable_libcurl);
            }

            if (request.Headers.TransferEncodingChunked.GetValueOrDefault() && (request.Content == null))
            {
                throw new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content);
            }

            // TODO: Check that SendAsync is not being called again for same request object.
            //       Probably fix is needed in WinHttpHandler as well

            CheckDisposed();

            SetOperationStarted();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            // Create RequestCompletionSource object and save current values of handler settings.
            RequestCompletionSource state = new RequestCompletionSource(this, cancellationToken, request);

            BeginRequest(state);
            return state.Task;
        }

        #region Private methods

        private async void BeginRequest(RequestCompletionSource state)
        {
            SafeCurlHandle requestHandle = new SafeCurlHandle();
            GCHandle stateHandle = new GCHandle();
            bool needCleanup = false;

            try
            {
                // Prepare context objects
                state.ResponseMessage = new CurlResponseMessage(state.RequestMessage);
                stateHandle = GCHandle.Alloc(state);
                requestHandle = CreateRequestHandle(state, stateHandle);
                state.RequestHandle = requestHandle;
                needCleanup = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    state.TrySetCanceled(state.CancellationToken);
                    return;
                }

                if (state.RequestMessage.Content != null)
                {
                    Stream requestContentStream =
                        await state.RequestMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (state.CancellationToken.IsCancellationRequested)
                    {
                        state.TrySetCanceled(state.CancellationToken);
                        return;
                    }
                    state.RequestContentStream = requestContentStream;
                    state.RequestContentBuffer = new byte[s_requestBufferSize];
                }

                AddEasyHandle(state);
                needCleanup = false;
            }
            catch (Exception ex)
            {
                HandleAsyncException(state, ex);
            }
            finally
            {
                if (needCleanup)
                {
                    RemoveEasyHandle(_multiHandle, stateHandle, false);
                }
                else if (state.Task.IsCompleted)
                {
                    if (stateHandle.IsAllocated)
                    {
                        stateHandle.Free();
                    }
                    if (!requestHandle.IsInvalid)
                    {
                        SafeCurlHandle.DisposeAndClearHandle(ref requestHandle);
                    }
                }
            }
        }

        private static void EndRequest(SafeCurlMultiHandle multiHandle, IntPtr statePtr, int result)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr(statePtr);
            RequestCompletionSource state = (RequestCompletionSource)stateHandle.Target;
            try
            {
                // No more callbacks so no more data
                state.ResponseMessage.ContentStream.SignalComplete();

                if (CURLcode.CURLE_OK == result)
                {
                    state.TrySetResult(state.ResponseMessage);
                }
                else
                {
                    state.TrySetException(new HttpRequestException(SR.net_http_client_execution_error,
                        GetCurlException(result)));
                }

                if (state.ResponseMessage.StatusCode != HttpStatusCode.Unauthorized && state.Handler.PreAuthenticate)
                {
                    ulong availedAuth;
                    if (Interop.libcurl.curl_easy_getinfo(state.RequestHandle, CURLINFO.CURLINFO_HTTPAUTH_AVAIL, out availedAuth) == CURLcode.CURLE_OK)
                    {
                        state.Handler.AddCredentialToCache(state.RequestMessage.RequestUri, availedAuth, state.NetworkCredential);
                    }
                    // ignoring the exception in this case.
                    // There is no point in killing the request for the sake of putting the credentials into the cache
                }
            }
            catch (Exception ex)
            {
                HandleAsyncException(state, ex);
            }
            finally
            {
                RemoveEasyHandle(multiHandle, stateHandle, true);
            }
        }

        private void SetOperationStarted()
        {
            if (!_anyOperationStarted)
            {
                _anyOperationStarted = true;
            }
        }

        private SafeCurlHandle CreateRequestHandle(RequestCompletionSource state, GCHandle stateHandle)
        {
            // TODO: If this impacts perf, optimize using a handle pool
            SafeCurlHandle requestHandle = Interop.libcurl.curl_easy_init();
            if (requestHandle.IsInvalid)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error);
            }

            SetCurlOption(requestHandle, CURLoption.CURLOPT_URL, state.RequestMessage.RequestUri.AbsoluteUri);
            if (_automaticRedirection)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_FOLLOWLOCATION, 1L);
                
                // Set maximum automatic redirection option
                SetCurlOption(requestHandle, CURLoption.CURLOPT_MAXREDIRS, _maxAutomaticRedirections);
            }

            if (state.RequestMessage.Method == HttpMethod.Put)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_UPLOAD, 1L);
            }
            else if (state.RequestMessage.Method == HttpMethod.Head)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_NOBODY, 1L);
            }
            else if (state.RequestMessage.Method == HttpMethod.Post)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_POST, 1L);
                if (state.RequestMessage.Content == null)
                {
                    SetCurlOption(requestHandle, CURLoption.CURLOPT_POSTFIELDSIZE, 0L);
                    SetCurlOption(requestHandle, CURLoption.CURLOPT_POSTFIELDS, "");
                }
            }

            IntPtr statePtr = GCHandle.ToIntPtr(stateHandle);
            SetCurlOption(requestHandle, CURLoption.CURLOPT_PRIVATE, statePtr);

            SetCurlCallbacks(requestHandle, state.RequestMessage, statePtr);

            if (_supportsAutomaticDecompression)
            {
                SetRequestHandleDecompressionOptions(requestHandle);
            }

            SetProxyOptions(requestHandle, state.RequestMessage.RequestUri);

            SetRequestHandleCredentialsOptions(requestHandle, state);

            SetCookieOption(requestHandle, state.RequestMessage.RequestUri);

            state.RequestHeaderHandle = SetRequestHeaders(requestHandle, state.RequestMessage);

            return requestHandle;
        }

        private void SetRequestHandleDecompressionOptions(SafeCurlHandle requestHandle)
        {
            bool gzip = (AutomaticDecompression & DecompressionMethods.GZip) != 0;
            bool deflate = (AutomaticDecompression & DecompressionMethods.Deflate) != 0;
            if (gzip || deflate)
            {
                string encoding = (gzip && deflate) ?
                                   EncodingNameGzip + "," + EncodingNameDeflate :
                                   gzip ? EncodingNameGzip :
                                   EncodingNameDeflate ;
                SetCurlOption(requestHandle, CURLoption.CURLOPT_ACCEPTENCODING, encoding);
            }
        }

        private void SetProxyOptions(SafeCurlHandle requestHandle, Uri requestUri)
        {
            if (_proxyPolicy == ProxyUsePolicy.DoNotUseProxy)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXY, string.Empty);
                return;
            }

            if ((_proxyPolicy == ProxyUsePolicy.UseDefaultProxy) || (Proxy == null))
            {
                return;
            }

            Debug.Assert( (Proxy != null) && (_proxyPolicy == ProxyUsePolicy.UseCustomProxy));
            if (Proxy.IsBypassed(requestUri))
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXY, string.Empty);
                return;
            }

            var proxyUri = Proxy.GetProxy(requestUri);
            if (proxyUri == null)
            {
                return;
            }

            SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXYTYPE, CURLProxyType.CURLPROXY_HTTP);
            SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXY, proxyUri.AbsoluteUri);
            SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXYPORT, proxyUri.Port);
            NetworkCredential credentials = GetCredentials(Proxy.Credentials, requestUri);
            if (credentials != null)
            {
                if (string.IsNullOrEmpty(credentials.UserName))
                {
                    throw new ArgumentException(SR.net_http_argument_empty_string, "UserName");
                }

                string credentialText;
                if (string.IsNullOrEmpty(credentials.Domain))
                {
                    credentialText = string.Format("{0}:{1}", credentials.UserName, credentials.Password);
                }
                else
                {
                    credentialText = string.Format("{2}\\{0}:{1}", credentials.UserName, credentials.Password, credentials.Domain);
                }
                SetCurlOption(requestHandle, CURLoption.CURLOPT_PROXYUSERPWD, credentialText);
            }
        }

        private void SetRequestHandleCredentialsOptions(SafeCurlHandle requestHandle, RequestCompletionSource state)
        {
            NetworkCredential credentials = GetNetworkCredentials(state.Handler._serverCredentials, state.RequestMessage.RequestUri);
            if (credentials != null)
            {
                string userName = string.IsNullOrEmpty(credentials.Domain) ?
                    credentials.UserName :
                    string.Format("{0}\\{1}", credentials.Domain, credentials.UserName);

                SetCurlOption(requestHandle, CURLoption.CURLOPT_USERNAME, userName);
                SetCurlOption(requestHandle, CURLoption.CURLOPT_HTTPAUTH, CURLAUTH.AuthAny);
                if (credentials.Password != null)
                {
                    SetCurlOption(requestHandle, CURLoption.CURLOPT_PASSWORD, credentials.Password);
                }

                state.NetworkCredential = credentials;
            }
        }

        private NetworkCredential GetNetworkCredentials(ICredentials credentials, Uri requestUri)
        {
            if (_preAuthenticate)
            {
                NetworkCredential nc = null;
                lock (_multiHandle)
                {
                    nc = GetCredentials(_credentialCache, requestUri);
                }
                if (nc != null)
                {
                    return nc;
                }
            }

            return GetCredentials(credentials, requestUri);
        }

        private void SetCookieOption(SafeCurlHandle requestHandle, Uri requestUri)
        {
            if (!_useCookie)
            {
                return;
            }
            else if (_cookieContainer == null)
            {
                throw new InvalidOperationException(SR.net_http_invalid_cookiecontainer);
            }

            string cookieValues = _cookieContainer.GetCookieHeader(requestUri);                    

            if (cookieValues != null)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_COOKIE, cookieValues);
            }           
        }

        private void AddCredentialToCache(Uri serverUri, ulong authAvail, NetworkCredential nc)
        {
            lock (_multiHandle)
            {
                for (int i=0; i < AuthSchemePriorityOrder.Length; i++)
                {
                    if ((authAvail & AuthSchemePriorityOrder[i]) != 0 )
                    {
                        _credentialCache.Add(serverUri, AuthenticationSchemes[i], nc);
                    }
                }
            }
        }

		private static NetworkCredential GetCredentials(ICredentials credentials, Uri requestUri)
        {
            if (credentials == null)
            {
                return null;
            }

            foreach (var authScheme in AuthenticationSchemes)
            {
                NetworkCredential networkCredential = credentials.GetCredential(requestUri, authScheme);
                if (networkCredential != null)
                {
                    return networkCredential;
                }
            }
            return null;
        }

        private static void HandleAsyncException(RequestCompletionSource state, Exception ex)
        {
            if ((null == ex) && state.CancellationToken.IsCancellationRequested)
            {
                state.TrySetCanceled(state.CancellationToken);
            }
            if (null == ex)
            {
                return;
            }
            var oce = (ex as OperationCanceledException);
            if (oce != null)
            {
                // If the exception was due to the cancellation token being canceled, throw cancellation exception.
                Debug.Assert(state.CancellationToken.IsCancellationRequested);
                state.TrySetCanceled(oce.CancellationToken);
            }
            else if (ex is HttpRequestException)
            {
                state.TrySetException(ex);
            }
            else
            {
                state.TrySetException(new HttpRequestException(SR.net_http_client_execution_error, ex));
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
            if (_anyOperationStarted)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        private static string GetCurlErrorString(int code, bool isMulti = false)
        {
            IntPtr ptr = isMulti ? Interop.libcurl.curl_multi_strerror(code) : Interop.libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        private static Exception GetCurlException(int code, bool isMulti = false)
        {
            return new Exception(GetCurlErrorString(code, isMulti));
        }

        private void SetCurlCallbacks(SafeCurlHandle requestHandle, HttpRequestMessage request, IntPtr stateHandle)
        {
            SetCurlOption(requestHandle, CURLoption.CURLOPT_HEADERDATA, stateHandle);
            SetCurlOption(requestHandle, CURLoption.CURLOPT_HEADERFUNCTION, s_receiveHeadersCallback);
            if (request.Method != HttpMethod.Head)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_WRITEDATA, stateHandle);
                unsafe
                {
                    SetCurlOption(requestHandle, CURLoption.CURLOPT_WRITEFUNCTION, s_receiveBodyCallback);
                }
            }
            if (request.Content != null)
            {
                SetCurlOption(requestHandle, CURLoption.CURLOPT_READDATA, stateHandle);
                SetCurlOption(requestHandle, CURLoption.CURLOPT_READFUNCTION, s_sendCallback);
                SetCurlOption(requestHandle, CURLoption.CURLOPT_IOCTLDATA, stateHandle);
                SetCurlOption(requestHandle, CURLoption.CURLOPT_IOCTLFUNCTION, s_sendIoCtlCallback);
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, string value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, long value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, ulong value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, Interop.libcurl.curl_readwrite_callback value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private unsafe void SetCurlOption(SafeCurlHandle handle, int option, Interop.libcurl.curl_unsafe_write_callback value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, Interop.libcurl.curl_ioctl_callback value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlOption(SafeCurlHandle handle, int option, IntPtr value)
        {
            int result = Interop.libcurl.curl_easy_setopt(handle, option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
        }

        private void SetCurlMultiOptions()
        {
            _multiHandlePtr = GCHandle.Alloc(_multiHandle);
            IntPtr callbackContext = GCHandle.ToIntPtr(_multiHandlePtr);
            int result = Interop.libcurl.curl_multi_setopt(_multiHandle, CURLMoption.CURLMOPT_SOCKETFUNCTION, s_socketCallback);
            if (result == CURLMcode.CURLM_OK)
            {
                result = Interop.libcurl.curl_multi_setopt(_multiHandle, CURLMoption.CURLMOPT_TIMERFUNCTION, s_multiTimerCallback);
            }
            if (result == CURLMcode.CURLM_OK)
            {
                result = Interop.libcurl.curl_multi_setopt(_multiHandle, CURLMoption.CURLMOPT_TIMERDATA, callbackContext);
            }
            if (result != CURLMcode.CURLM_OK)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result, true));
            }
        }

        private SafeCurlSlistHandle SetRequestHeaders(SafeCurlHandle handle, HttpRequestMessage request)
        {
            SafeCurlSlistHandle retVal = new SafeCurlSlistHandle();
            if (request.Headers == null)
            {
                return retVal;
            }

            HttpHeaders contentHeaders = null;
            if (request.Content != null)
            {
                SetChunkedModeForSend(request);

                // TODO: Content-Length header isn't getting correctly placed using ToString()
                // This is a bug in HttpContentHeaders that needs to be fixed.
                if (request.Content.Headers.ContentLength.HasValue)
                {
                    long contentLength = request.Content.Headers.ContentLength.Value;
                    request.Content.Headers.ContentLength = null;
                    request.Content.Headers.ContentLength = contentLength;
                }
                contentHeaders = request.Content.Headers;
            }

            bool gotReference = false;
            try
            {
                retVal.DangerousAddRef(ref gotReference);
                IntPtr rawHandle = IntPtr.Zero;

                if (request.Headers != null)
                {
                    // Add request headers
                    AddRequestHeaders(request.Headers, retVal, ref rawHandle);
                }

                if (contentHeaders != null)
                {
                    // Add content request headers
                    AddRequestHeaders(contentHeaders, retVal, ref rawHandle);
                }

                // Since libcurl always adds a Transfer-Encoding header, we need to explicitly block
                // it if caller specifically does not want to set the header
                if (request.Headers.TransferEncodingChunked.HasValue && !request.Headers.TransferEncodingChunked.Value)
                {
                    rawHandle = Interop.libcurl.curl_slist_append(rawHandle, NoTransferEncoding);

                    if (rawHandle == null)
                    {
                        throw new HttpRequestException(SR.net_http_client_execution_error);
                    }

                    retVal.SetHandle(rawHandle);
                }

                if (!retVal.IsInvalid)
                {
                    SetCurlOption(handle, CURLoption.CURLOPT_HTTPHEADER, rawHandle);
                }
            }
            finally
            {
                if (gotReference)
                {
                    retVal.DangerousRelease();
                }
            }

            return retVal;
        }

        /// <summary>
        /// Add request headers to curl API
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="handle"></param>
        /// <param name="rawHandle"></param>
        private static void AddRequestHeaders(HttpHeaders headers, SafeCurlSlistHandle handle, ref IntPtr rawHandle)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                string headerStr = header.Key + ": " + headers.GetHeaderString(header.Key);
                rawHandle = Interop.libcurl.curl_slist_append(rawHandle, headerStr);

                if (rawHandle == null)
                {
                    throw new HttpRequestException(SR.net_http_client_execution_error);
                }

                handle.SetHandle(rawHandle);
            }
        }

        private static void SetChunkedModeForSend(HttpRequestMessage request)
        {
            bool chunkedMode = request.Headers.TransferEncodingChunked.GetValueOrDefault();
            HttpContent requestContent = request.Content;
            Debug.Assert(requestContent != null);

            // Deal with conflict between 'Content-Length' vs. 'Transfer-Encoding: chunked' semantics.
            // libcurl adds a Tranfer-Encoding header by default and the request fails if both are set.
            if (requestContent.Headers.ContentLength.HasValue)
            {
                if (chunkedMode)
                {
                    // Same behaviour as WinHttpHandler
                    requestContent.Headers.ContentLength = null;
                }
                else
                {
                    // Prevent libcurl from adding Transfer-Encoding header
                    request.Headers.TransferEncodingChunked = false;
                }
            }
        }

        private void AddEasyHandle(RequestCompletionSource state)
        {
            bool gotReference = false;
            SafeCurlHandle requestHandle = state.RequestHandle;
            try
            {
                requestHandle.DangerousAddRef(ref gotReference);
                lock (_multiHandle)
                {
                    int result = Interop.libcurl.curl_multi_add_handle(_multiHandle, requestHandle);
                    if (result != CURLcode.CURLE_OK)
                    {
                        throw new HttpRequestException(SR.net_http_client_execution_error,
                            GetCurlException(result, true));
                    }
                    state.SessionHandle = _multiHandle;
                    _multiHandle.RequestCount = _multiHandle.RequestCount + 1;
                    if (_multiHandle.PollCancelled)
                    {
                        // TODO: Create single polling thread for all HttpClientHandler objects
                        Task.Factory.StartNew(s => PollFunction(((CurlHandler)s)._multiHandle), this,
                                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                        _multiHandle.PollCancelled = false;
                    }
                }
                // Note that we are deliberately not decreasing the ref counts of
                // the multi and easy handles since that will be done in RemoveEasyHandle
                // when the request is completed and the handles are used in an
                // unmanaged context till then
                // TODO: Investigate if SafeCurlHandle is really useful since we are not
                //       avoiding any leaks due to the ref count increment
            }
            catch (Exception)
            {
                if (gotReference)
                {
                    requestHandle.DangerousRelease();
                }
                throw;
            }
        }

        private static void RemoveEasyHandle(SafeCurlMultiHandle multiHandle, GCHandle stateHandle, bool onMultiStack)
        {
            RequestCompletionSource state = (RequestCompletionSource)stateHandle.Target;
            SafeCurlHandle requestHandle = state.RequestHandle;

            if (onMultiStack)
            {
                lock (multiHandle)
                {
                    Interop.libcurl.curl_multi_remove_handle(multiHandle, requestHandle);
                    multiHandle.RequestCount = multiHandle.RequestCount - 1;
                }
                state.SessionHandle = null;
                requestHandle.DangerousRelease();
            }

            if (!state.RequestHeaderHandle.IsInvalid)
            {
                SafeCurlSlistHandle headerHandle = state.RequestHeaderHandle;
                SafeCurlSlistHandle.DisposeAndClearHandle(ref headerHandle);
            }

            SafeCurlHandle.DisposeAndClearHandle(ref requestHandle);

            stateHandle.Free();
        }

        #endregion

        private sealed class RequestCompletionSource : TaskCompletionSource<HttpResponseMessage>
        {
            private readonly CurlHandler _handler;
            private readonly CancellationToken _cancellationToken;
            private readonly HttpRequestMessage _requestMessage;


            // TODO: The task completion can sometimes happen under a lock. So we need to ensure
            //       that arbitrary blocking code cannot run when the task is marked for completion
            //       So we are using RunContinuationsAsynchronously to force a diff. thread
            public RequestCompletionSource(
                    CurlHandler handler,
                    CancellationToken cancellationToken,
                    HttpRequestMessage request)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                this._handler = handler;
                this._cancellationToken = cancellationToken;
                this._requestMessage = request;
            }

            public CurlResponseMessage ResponseMessage { get; set; }

            public SafeCurlMultiHandle SessionHandle { get; set; }

            public SafeCurlHandle RequestHandle { get; set; }

            public SafeCurlSlistHandle RequestHeaderHandle { get; set; }

            public Stream RequestContentStream { get; set; }

            public byte[] RequestContentBuffer { get; set; }

            public NetworkCredential NetworkCredential {get; set;}

            public CurlHandler Handler
            {
                get
                {
                    return _handler;
                }
            }

            public CancellationToken CancellationToken
            {
                get
                {
                    return _cancellationToken;
                }
            }

            public HttpRequestMessage RequestMessage
            {
                get
                {
                    return _requestMessage;
                }
            }
        }

        private enum ProxyUsePolicy
        {
            DoNotUseProxy = 0, // Do not use proxy. Ignores the value set in the environment.
            UseDefaultProxy = 1, // Do not set the proxy parameter. Use the value of environment variable, if any.
            UseCustomProxy = 2  // Use The proxy specified by the user.
        }
    }
}

