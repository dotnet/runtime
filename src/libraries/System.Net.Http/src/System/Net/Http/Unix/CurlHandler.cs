// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using CURLAUTH = Interop.Http.CURLAUTH;
using CURLcode = Interop.Http.CURLcode;
using CURLMcode = Interop.Http.CURLMcode;
using CURLoption = Interop.Http.CURLoption;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        #region Constants

        private const char SpaceChar = ' ';
        private const int StatusCodeLength = 3;

        private const string UriSchemeHttp = "http";
        private const string UriSchemeHttps = "https";
        private const string EncodingNameGzip = "gzip";
        private const string EncodingNameDeflate = "deflate";

        private const int RequestBufferSize = 16384; // Default used by libcurl
        private const string NoTransferEncoding = HttpKnownHeaderNames.TransferEncoding + ":";
        private const string NoContentType = HttpKnownHeaderNames.ContentType + ":";
        private const int CurlAge = 5;
        private const int MinCurlAge = 3;

        #endregion

        #region Fields

        // To enable developers to opt-in to support certain auth types that we don't want
        // to enable by default (e.g. NTLM), we allow for such types to be specified explicitly
        // when credentials are added to a credential cache, but we don't enable them
        // when no auth type is specified, e.g. when a NetworkCredential is provided directly
        // to the handler.  As such, we have two different sets of auth types that we use
        // for when the supplied creds are a cache vs not.
        private readonly static KeyValuePair<string,CURLAUTH>[] s_orderedAuthTypesCredentialCache = new KeyValuePair<string, CURLAUTH>[] {
            new KeyValuePair<string,CURLAUTH>("Negotiate", CURLAUTH.Negotiate),
            new KeyValuePair<string,CURLAUTH>("NTLM", CURLAUTH.NTLM), // only available when credentials supplied via a credential cache
            new KeyValuePair<string,CURLAUTH>("Digest", CURLAUTH.Digest),
            new KeyValuePair<string,CURLAUTH>("Basic", CURLAUTH.Basic),
        };
        private readonly static KeyValuePair<string, CURLAUTH>[] s_orderedAuthTypesICredential = new KeyValuePair<string, CURLAUTH>[] {
            new KeyValuePair<string,CURLAUTH>("Negotiate", CURLAUTH.Negotiate),
            new KeyValuePair<string,CURLAUTH>("Digest", CURLAUTH.Digest),
            new KeyValuePair<string,CURLAUTH>("Basic", CURLAUTH.Basic),
        };

        private readonly static char[] s_newLineCharArray = new char[] { HttpRuleParser.CR, HttpRuleParser.LF };
        private readonly static bool s_supportsAutomaticDecompression;
        private readonly static bool s_supportsSSL;
        private readonly static bool s_supportsHttp2Multiplexing;

        private readonly static DiagnosticListener s_diagnosticListener = new DiagnosticListener(HttpHandlerLoggingStrings.DiagnosticListenerName);

        private readonly MultiAgent _agent = new MultiAgent();
        private volatile bool _anyOperationStarted;
        private volatile bool _disposed;

        private IWebProxy _proxy = null;
        private ICredentials _serverCredentials = null;
        private ProxyUsePolicy _proxyPolicy = ProxyUsePolicy.UseDefaultProxy;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private bool _preAuthenticate = HttpHandlerDefaults.DefaultPreAuthenticate;
        private CredentialCache _credentialCache = null; // protected by LockObject
        private CookieContainer _cookieContainer = new CookieContainer();
        private bool _useCookie = HttpHandlerDefaults.DefaultUseCookies;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private ClientCertificateOption _clientCertificateOption = HttpHandlerDefaults.DefaultClientCertificateOption;

        private object LockObject { get { return _agent; } }

        #endregion        

        static CurlHandler()
        {
            // curl_global_init call handled by Interop.LibCurl's cctor

            int age;
            if (!Interop.Http.GetCurlVersionInfo(
                out age, 
                out s_supportsSSL, 
                out s_supportsAutomaticDecompression, 
                out s_supportsHttp2Multiplexing))
            {
                throw new InvalidOperationException(SR.net_http_unix_https_libcurl_no_versioninfo);  
            }

            // Verify the version of curl we're using is new enough
            if (age < MinCurlAge)
            {
                throw new InvalidOperationException(SR.net_http_unix_https_libcurl_too_old);
            }
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

        internal bool SupportsRedirectConfiguration
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
                _proxyPolicy = value ?
                    ProxyUsePolicy.UseCustomProxy :
                    ProxyUsePolicy.DoNotUseProxy;
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
                return _clientCertificateOption;
            }

            set
            {
                CheckDisposedOrStarted();
                _clientCertificateOption = value;
            }
        }

        internal bool SupportsAutomaticDecompression
        {
            get
            {
                return s_supportsAutomaticDecompression;
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
                if (value && _credentialCache == null)
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
                        SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxAutomaticRedirections = value;
            }
        }

        /// <summary>
        ///   <b> UseDefaultCredentials is a no op on Unix </b>
        /// </summary>
        internal bool UseDefaultCredentials
        {
            get
            {
                return false;
            }
            set
            {
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
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

            if (request.RequestUri.Scheme == UriSchemeHttps)
            {
                if (!s_supportsSSL)
                {
                    throw new PlatformNotSupportedException(SR.net_http_unix_https_support_unavailable_libcurl);
                }
            }
            else
            {
                Debug.Assert(request.RequestUri.Scheme == UriSchemeHttp, "HttpClient expected to validate scheme as http or https.");
            }

            if (request.Headers.TransferEncodingChunked.GetValueOrDefault() && (request.Content == null))
            {
                throw new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content);
            }

            if (_useCookie && _cookieContainer == null)
            {
                throw new InvalidOperationException(SR.net_http_invalid_cookiecontainer);
            }

            Guid loggingRequestId = s_diagnosticListener.LogHttpRequest(request);

            CheckDisposed();
            SetOperationStarted();

            // Do an initial cancellation check to avoid initiating the async operation if
            // cancellation has already been requested.  After this, we'll rely on CancellationToken.Register
            // to notify us of cancellation requests and shut down the operation if possible.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            EventSourceTrace("{0}", request);

            // Create the easy request.  This associates the easy request with this handler and configures
            // it based on the settings configured for the handler.
            var easy = new EasyRequest(this, request, cancellationToken);
            try
            {
                easy.InitializeCurl();
                if (easy._requestContentStream != null)
                {
                    easy._requestContentStream.Run();
                }
                _agent.Queue(new MultiAgent.IncomingRequest { Easy = easy, Type = MultiAgent.IncomingRequestType.New });
            }
            catch (Exception exc)
            {
                easy.FailRequest(exc);
                easy.Cleanup(); // no active processing remains, so we can cleanup
            }

            s_diagnosticListener.LogHttpResponse(easy.Task, loggingRequestId);

            return easy.Task;
        }

        #region Private methods

        private void SetOperationStarted()
        {
            if (!_anyOperationStarted)
            {
                _anyOperationStarted = true;
            }
        }

        private KeyValuePair<NetworkCredential, CURLAUTH> GetCredentials(Uri requestUri)
        {
            // Get the auth types (Negotiate, Digest, etc.) that are allowed to be selected automatically
            // based on the type of the ICredentials provided.
            KeyValuePair<string, CURLAUTH>[] validAuthTypes = AuthTypesPermittedByCredentialKind(_serverCredentials);

            // If preauthentication is enabled, we may have populated our internal credential cache,
            // so first check there to see if we have any credentials for this uri.
            if (_preAuthenticate)
            {
                KeyValuePair<NetworkCredential, CURLAUTH> ncAndScheme;
                lock (LockObject)
                {
                    Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                    ncAndScheme = GetCredentials(requestUri, _credentialCache, validAuthTypes);
                }
                if (ncAndScheme.Key != null)
                {
                    return ncAndScheme;
                }
            }

            // We either weren't preauthenticating or we didn't have any cached credentials
            // available, so check the credentials on the handler.
            return GetCredentials(requestUri, _serverCredentials, validAuthTypes);
        }

        private void TransferCredentialsToCache(Uri serverUri, CURLAUTH serverAuthAvail)
        {
            if (_serverCredentials == null)
            {
                // No credentials, nothing to put into the cache.
                return;
            }

            // Get the auth types valid based on the type of the ICredentials used.  We're effectively
            // going to intersect this (the types we allow to be used) with those the server supports
            // and with the credentials we have available.  The best of that resulting intersection
            // will be added to the cache.
            KeyValuePair<string, CURLAUTH>[] validAuthTypes = AuthTypesPermittedByCredentialKind(_serverCredentials);

            lock (LockObject)
            {
                // For each auth type we allow, check whether it's one supported by the server.
                for (int i = 0; i < validAuthTypes.Length; i++)
                {
                    // Is it supported by the server?
                    if ((serverAuthAvail & validAuthTypes[i].Value) != 0)
                    {
                        // And do we have a credential for it?
                        NetworkCredential nc = _serverCredentials.GetCredential(serverUri, validAuthTypes[i].Key);
                        if (nc != null)
                        {
                            // We have a credential for it, so add it, and we're done.
                            Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                            try
                            {
                                _credentialCache.Add(serverUri, validAuthTypes[i].Key, nc);
                            }
                            catch (ArgumentException)
                            {
                                // Ignore the case of key already present
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static KeyValuePair<string, CURLAUTH>[] AuthTypesPermittedByCredentialKind(ICredentials c)
        {
            // Special-case CredentialCache for auth types, as credentials put into the cache are put
            // in explicitly tagged with an auth type, and thus credentials are opted-in to potentially
            // less secure auth types we might otherwise want disabled by default.
            return c is CredentialCache ?
                s_orderedAuthTypesCredentialCache :
                s_orderedAuthTypesICredential;
        }

        private void AddResponseCookies(EasyRequest state, string cookieHeader)
        {
            if (!_useCookie)
            {
                return;
            }

            try
            {
                _cookieContainer.SetCookies(state._targetUri, cookieHeader);
                state.SetCookieOption(state._requestMessage.RequestUri);
            }
            catch (CookieException e)
            {
                EventSourceTrace(
                    "Malformed cookie parsing failed: {0}, server: {1}, cookie: {2}", 
                    e.Message, state._requestMessage.RequestUri, cookieHeader);
            }
        }

        private static KeyValuePair<NetworkCredential, CURLAUTH> GetCredentials(Uri requestUri, ICredentials credentials, KeyValuePair<string, CURLAUTH>[] validAuthTypes)
        {
            NetworkCredential nc = null;
            CURLAUTH curlAuthScheme = CURLAUTH.None;

            if (credentials != null)
            {
                // For each auth type we consider valid, try to get a credential for it.
                // Union together the auth types for which we could get credentials, but validate
                // that the found credentials are all the same, as libcurl doesn't support differentiating
                // by auth type.
                for (int i = 0; i < validAuthTypes.Length; i++)
                {
                    NetworkCredential networkCredential = credentials.GetCredential(requestUri, validAuthTypes[i].Key);
                    if (networkCredential != null)
                    {
                        curlAuthScheme |= validAuthTypes[i].Value;
                        if (nc == null)
                        {
                            nc = networkCredential;
                        }
                        else if(!AreEqualNetworkCredentials(nc, networkCredential))
                        {
                            throw new PlatformNotSupportedException(SR.net_http_unix_invalid_credential);
                        }
                    }
                }
            }

            EventSourceTrace("Authentication scheme: {0}", curlAuthScheme);
            return new KeyValuePair<NetworkCredential, CURLAUTH>(nc, curlAuthScheme); ;
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

        private static void ThrowIfCURLEError(CURLcode error)
        {
            if (error != CURLcode.CURLE_OK)
            {
                var inner = new CurlException((int)error, isMulti: false);
                EventSourceTrace(inner.Message);
                throw inner;
            }
        }

        private static void ThrowIfCURLMError(CURLMcode error)
        {
            if (error != CURLMcode.CURLM_OK)
            {
                string msg = CurlException.GetCurlErrorString((int)error, true);
                EventSourceTrace(msg);
                switch (error)
                {
                    case CURLMcode.CURLM_ADDED_ALREADY:
                    case CURLMcode.CURLM_BAD_EASY_HANDLE:
                    case CURLMcode.CURLM_BAD_HANDLE:
                    case CURLMcode.CURLM_BAD_SOCKET:
                        throw new ArgumentException(msg);
                    case CURLMcode.CURLM_UNKNOWN_OPTION:
                        throw new ArgumentOutOfRangeException(msg);
                    case CURLMcode.CURLM_OUT_OF_MEMORY:
                        throw new OutOfMemoryException(msg);
                    case CURLMcode.CURLM_INTERNAL_ERROR:
                    default:
                        throw new CurlException((int)error, msg);
                }
            }
        }

        private static bool AreEqualNetworkCredentials(NetworkCredential credential1, NetworkCredential credential2)
        {
            Debug.Assert(credential1 != null && credential2 != null, "arguments are non-null in network equality check");
            return credential1.UserName == credential2.UserName &&
                credential1.Domain == credential2.Domain &&
                string.Equals(credential1.Password, credential2.Password, StringComparison.Ordinal);
        }

        private static bool EventSourceTracingEnabled { get { return HttpEventSource.Log.IsEnabled(); } }

        // PERF NOTE:
        // These generic overloads of EventSourceTrace (and similar wrapper methods in some of the other CurlHandler
        // nested types) exist to allow call sites to call EventSourceTrace without boxing and without checking
        // EventSourceTracingEnabled.  Do not remove these without fixing the call sites accordingly.

        private static void EventSourceTrace<TArg0>(
            string formatMessage, TArg0 arg0,
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (EventSourceTracingEnabled)
            {
                EventSourceTraceCore(string.Format(formatMessage, arg0), agent, easy, memberName);
            }
        }

        private static void EventSourceTrace<TArg0, TArg1, TArg2>
            (string formatMessage, TArg0 arg0, TArg1 arg1, TArg2 arg2,
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (EventSourceTracingEnabled)
            {
                EventSourceTraceCore(string.Format(formatMessage, arg0, arg1, arg2), agent, easy, memberName);
            }
        }

        private static void EventSourceTrace(
            string message, 
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (EventSourceTracingEnabled)
            {
                EventSourceTraceCore(message, agent, easy, memberName);
            }
        }

        private static void EventSourceTraceCore(string message, MultiAgent agent, EasyRequest easy, string memberName)
        {
            // If we weren't handed a multi agent, see if we can get one from the EasyRequest
            if (agent == null && easy != null)
            {
                agent = easy._associatedMultiAgent;
            }

            HttpEventSource.Log.HandlerMessage(
                agent != null && agent.RunningWorkerId.HasValue ? agent.RunningWorkerId.GetValueOrDefault() : 0,
                easy != null ? easy.Task.Id : 0,
                memberName,
                message);
        }

        private static Exception CreateHttpRequestException(Exception inner = null)
        {
            return new HttpRequestException(SR.net_http_client_execution_error, inner);
        }

        private static bool TryParseStatusLine(HttpResponseMessage response, string responseHeader, EasyRequest state)
        {
            if (!responseHeader.StartsWith(CurlResponseParseUtils.HttpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Clear the header if status line is recieved again. This signifies that there are multiple response headers (like in redirection).
            response.Headers.Clear();
            response.Content.Headers.Clear();

            CurlResponseParseUtils.ReadStatusLine(response, responseHeader);
            state._isRedirect = state._handler.AutomaticRedirection &&
                         (response.StatusCode == HttpStatusCode.Redirect ||
                         response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                         response.StatusCode == HttpStatusCode.RedirectMethod) ;
            return true;
        }

        private static void HandleRedirectLocationHeader(EasyRequest state, string locationValue)
        {
            Debug.Assert(state._isRedirect);
            Debug.Assert(state._handler.AutomaticRedirection);

            string location = locationValue.Trim();
            //only for absolute redirects
            Uri forwardUri;
            if (Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out forwardUri) && forwardUri.IsAbsoluteUri)
            {
                // Just as with WinHttpHandler, for security reasons, we drop the server credential if it is 
                // anything other than a CredentialCache. We allow credentials in a CredentialCache since they 
                // are specifically tied to URIs.
                var creds = state._handler.Credentials as CredentialCache;
                KeyValuePair<NetworkCredential, CURLAUTH> ncAndScheme = GetCredentials(forwardUri, creds, AuthTypesPermittedByCredentialKind(creds));
                if (ncAndScheme.Key != null)
                {
                    state.SetCredentialsOptions(ncAndScheme);
                }
                else
                {
                    state.SetCurlOption(CURLoption.CURLOPT_USERNAME, IntPtr.Zero);
                    state.SetCurlOption(CURLoption.CURLOPT_PASSWORD, IntPtr.Zero);
                }

                // reset proxy - it is possible that the proxy has different credentials for the new URI
                state.SetProxyOptions(forwardUri);

                if (state._handler._useCookie)
                {
                    // reset cookies.
                    state.SetCurlOption(CURLoption.CURLOPT_COOKIE, IntPtr.Zero);

                    // set cookies again
                    state.SetCookieOption(forwardUri);
                }

                state.SetRedirectUri(forwardUri);
            }

            // set the headers again. This is a workaround for libcurl's limitation in handling headers with empty values
            state.SetRequestHeaders();
        }

        private static void SetChunkedModeForSend(HttpRequestMessage request)
        {
            bool chunkedMode = request.Headers.TransferEncodingChunked.GetValueOrDefault();
            HttpContent requestContent = request.Content;
            Debug.Assert(requestContent != null, "request is null");

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
            else if (!chunkedMode)
            {
                // Make sure Transfer-Encoding: chunked header is set, 
                // as we have content to send but no known length for it.
                request.Headers.TransferEncodingChunked = true;
            }
        }

        #endregion

        private enum ProxyUsePolicy
        {
            DoNotUseProxy = 0, // Do not use proxy. Ignores the value set in the environment.
            UseDefaultProxy = 1, // Do not set the proxy parameter. Use the value of environment variable, if any.
            UseCustomProxy = 2  // Use The proxy specified by the user.
        }
    }
}

