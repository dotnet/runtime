// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using CURLAUTH = Interop.libcurl.CURLAUTH;
using CURLcode = Interop.libcurl.CURLcode;
using CurlFeatures = Interop.libcurl.CURL_VERSION_Features;
using CURLMcode = Interop.libcurl.CURLMcode;
using CURLoption = Interop.libcurl.CURLoption;
using CurlVersionInfoData = Interop.libcurl.curl_version_info_data;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        #region Constants

        private const string VerboseDebuggingConditional = "CURLHANDLER_VERBOSE";
        private const string HttpPrefix = "HTTP/";
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

        private readonly static char[] s_newLineCharArray = new char[] { HttpRuleParser.CR, HttpRuleParser.LF };
        private readonly static string[] s_authenticationSchemes = { "Negotiate", "Digest", "Basic" }; // the order in which libcurl goes over authentication schemes
        private readonly static ulong[] s_authSchemePriorityOrder = { CURLAUTH.Negotiate, CURLAUTH.Digest, CURLAUTH.Basic };

        private readonly static CurlVersionInfoData s_curlVersionInfoData;
        private readonly static bool s_supportsAutomaticDecompression;
        private readonly static bool s_supportsSSL;

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

        private object LockObject { get { return _agent; } }

        #endregion        

        static CurlHandler()
        {
            // curl_global_init call handled by Interop.libcurl's cctor

            // Verify the version of curl we're using is new enough
            s_curlVersionInfoData = Marshal.PtrToStructure<CurlVersionInfoData>(Interop.libcurl.curl_version_info(CurlAge));
            if (s_curlVersionInfoData.age < MinCurlAge)
            {
                throw new InvalidOperationException(SR.net_http_unix_https_libcurl_too_old);
            }

            // Feature detection
            s_supportsSSL = (CurlFeatures.CURL_VERSION_SSL & s_curlVersionInfoData.features) != 0;
            s_supportsAutomaticDecompression = (CurlFeatures.CURL_VERSION_LIBZ & s_curlVersionInfoData.features) != 0;
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

            if ((request.RequestUri.Scheme != UriSchemeHttp) && (request.RequestUri.Scheme != UriSchemeHttps))
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_http_client_http_baseaddress_required);
            }

            if (request.RequestUri.Scheme == UriSchemeHttps && !s_supportsSSL)
            {
                throw new PlatformNotSupportedException(SR.net_http_unix_https_support_unavailable_libcurl);
            }

            if (request.Headers.TransferEncodingChunked.GetValueOrDefault() && (request.Content == null))
            {
                throw new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content);
            }

            if (_useCookie && _cookieContainer == null)
            {
                throw new InvalidOperationException(SR.net_http_invalid_cookiecontainer);
            }

            // TODO: Check that SendAsync is not being called again for same request object.
            //       Probably fix is needed in WinHttpHandler as well

            CheckDisposed();
            SetOperationStarted();

            // Do an initial cancellation check to avoid initiating the async operation if
            // cancellation has already been requested.  After this, we'll rely on CancellationToken.Register
            // to notify us of cancellation requests and shut down the operation if possible.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            // Create the easy request.  This associates the easy request with this handler and configures
            // it based on the settings configured for the handler.
            var easy = new EasyRequest(this, request, cancellationToken);

            // Submit the easy request to the multi agent.
            if (request.Content != null)
            {
                // If there is request content to be sent, preload the stream
                // and submit the request to the multi agent.  This is separated
                // out into a separate async method to avoid associated overheads
                // in the case where there is no request content stream.
                return QueueOperationWithRequestContentAsync(easy);
            }
            else
            {
                // Otherwise, just submit the request.
                ConfigureAndQueue(easy);
                return easy.Task;
            }
        }

        private void ConfigureAndQueue(EasyRequest easy)
        {
            try
            {
                easy.InitializeCurl();
                _agent.Queue(new MultiAgent.IncomingRequest { Easy = easy, Type = MultiAgent.IncomingRequestType.New });
            }
            catch (Exception exc)
            {
                easy.FailRequest(exc);
                easy.Cleanup(); // no active processing remains, so we can cleanup
            }
        }

        /// <summary>
        /// Loads the request's request content stream asynchronously and 
        /// then submits the request to the multi agent.
        /// </summary>
        private async Task<HttpResponseMessage> QueueOperationWithRequestContentAsync(EasyRequest easy)
        {
            Debug.Assert(easy._requestMessage.Content != null, "Expected request to have non-null request content");

            easy._requestContentStream = await easy._requestMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (easy._cancellationToken.IsCancellationRequested)
            {
                easy.FailRequest(new OperationCanceledException(easy._cancellationToken));
                easy.Cleanup(); // no active processing remains, so we can cleanup
            }
            else
            {
                ConfigureAndQueue(easy);
            }
            return await easy.Task.ConfigureAwait(false);
        }

        #region Private methods

        private void SetOperationStarted()
        {
            if (!_anyOperationStarted)
            {
                _anyOperationStarted = true;
            }
        }

        private NetworkCredential GetNetworkCredentials(ICredentials credentials, Uri requestUri)
        {
            if (_preAuthenticate)
            {
                NetworkCredential nc = null;
                lock (LockObject)
                {
                    Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                    nc = GetCredentials(_credentialCache, requestUri);
                }
                if (nc != null)
                {
                    return nc;
                }
            }

            return GetCredentials(credentials, requestUri);
        }

        private void AddCredentialToCache(Uri serverUri, ulong authAvail, NetworkCredential nc)
        {
            lock (LockObject)
            {
                for (int i=0; i < s_authSchemePriorityOrder.Length; i++)
                {
                    if ((authAvail & s_authSchemePriorityOrder[i]) != 0)
                    {
                        Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                        _credentialCache.Add(serverUri, s_authenticationSchemes[i], nc);
                    }
                }
            }
        }

        private void AddResponseCookies(Uri serverUri, HttpResponseMessage response)
        {
            if (!_useCookie)
            {
                return;
            }

            if (response.Headers.Contains(HttpKnownHeaderNames.SetCookie))
            {
                IEnumerable<string> cookieHeaders = response.Headers.GetValues(HttpKnownHeaderNames.SetCookie);
                foreach (var cookieHeader in cookieHeaders)
                {
                    try
                    {
                        _cookieContainer.SetCookies(serverUri, cookieHeader);
                    }
                    catch (CookieException e)
                    {
                        string msg = string.Format("Malformed cookie: SetCookies Failed with {0}, server: {1}, cookie:{2}",
                                                   e.Message,
                                                   serverUri.OriginalString,
                                                   cookieHeader);
                        VerboseTrace(msg);
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

            foreach (var authScheme in s_authenticationSchemes)
            {
                NetworkCredential networkCredential = credentials.GetCredential(requestUri, authScheme);
                if (networkCredential != null)
                {
                    return networkCredential;
                }
            }
            return null;
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

        private static void ThrowIfCURLEError(int error)
        {
            if (error != CURLcode.CURLE_OK)
            {
                var inner = new CurlException(error, isMulti: false);
                VerboseTrace(inner.Message);
                throw CreateHttpRequestException(inner);
            }
        }

        private static void ThrowIfCURLMError(int error)
        {
            if (error != CURLMcode.CURLM_OK)
            {
                string msg = CurlException.GetCurlErrorString(error, true);
                VerboseTrace(msg);
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
                        throw CreateHttpRequestException(new CurlException(error, msg));
                }
            }
        }

        [Conditional(VerboseDebuggingConditional)]
        private static void VerboseTrace(string text = null, [CallerMemberName] string memberName = null, EasyRequest easy = null, MultiAgent agent = null)
        {
            // If we weren't handed a multi agent, see if we can get one from the EasyRequest
            if (agent == null && easy != null && easy._associatedMultiAgent != null)
            {
                agent = easy._associatedMultiAgent;
            }
            int? agentId = agent != null ? agent.RunningWorkerId : null;

            // Get an ID string that provides info about which MultiAgent worker and which EasyRequest this trace is about
            string ids = "";
            if (agentId != null || easy != null)
            {
                ids = "(" +
                    (agentId != null ? "M#" + agentId : "") +
                    (agentId != null && easy != null ? ", " : "") +
                    (easy != null ? "E#" + easy.Task.Id : "") +
                    ")";
            }

            // Create the message and trace it out
            string msg = string.Format("[{0, -30}]{1, -16}: {2}", memberName, ids, text);
            Interop.Sys.PrintF("%s\n", msg);
        }

        [Conditional(VerboseDebuggingConditional)]
        private static void VerboseTraceIf(bool condition, string text = null, [CallerMemberName] string memberName = null, EasyRequest easy = null)
        {
            if (condition)
            {
                VerboseTrace(text, memberName, easy, agent: null);
            }
        }

        private static Exception CreateHttpRequestException(Exception inner = null)
        {
            return new HttpRequestException(SR.net_http_client_execution_error, inner);
        }

        private static bool TryParseStatusLine(HttpResponseMessage response, string responseHeader, EasyRequest state)
        {
            if (!responseHeader.StartsWith(HttpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Clear the header if status line is recieved again. This signifies that there are multiple response headers (like in redirection).
            response.Headers.Clear();

            response.Content.Headers.Clear();

            int responseHeaderLength = responseHeader.Length;

            // Check if line begins with HTTP/1.1 or HTTP/1.0
            int prefixLength = HttpPrefix.Length;
            int versionIndex = prefixLength + 2;

            if ((versionIndex < responseHeaderLength) && (responseHeader[prefixLength] == '1') && (responseHeader[prefixLength + 1] == '.'))
            {
                response.Version =
                    responseHeader[versionIndex] == '1' ? HttpVersion.Version11 :
                    responseHeader[versionIndex] == '0' ? HttpVersion.Version10 :
                    new Version(0, 0);
            }
            else
            {
                response.Version = new Version(0, 0);
            }

            // TODO: Parsing errors are treated as fatal. Find right behaviour

            int spaceIndex = responseHeader.IndexOf(SpaceChar);

            if (spaceIndex > -1)
            {
                int codeStartIndex = spaceIndex + 1;
                int statusCode = 0;

                // Parse first 3 characters after a space as status code
                if (TryParseStatusCode(responseHeader, codeStartIndex, out statusCode))
                {
                    response.StatusCode = (HttpStatusCode)statusCode;

                    int codeEndIndex = codeStartIndex + StatusCodeLength;

                    int reasonPhraseIndex = codeEndIndex + 1;

                    if (reasonPhraseIndex < responseHeaderLength && responseHeader[codeEndIndex] == SpaceChar)
                    {
                        int newLineCharIndex = responseHeader.IndexOfAny(s_newLineCharArray, reasonPhraseIndex);
                        int reasonPhraseEnd = newLineCharIndex >= 0 ? newLineCharIndex : responseHeaderLength;
                        response.ReasonPhrase = responseHeader.Substring(reasonPhraseIndex, reasonPhraseEnd - reasonPhraseIndex);
                    }
                    state._isRedirect = state._handler.AutomaticRedirection &&
                         (response.StatusCode == HttpStatusCode.Redirect ||
                         response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                         response.StatusCode == HttpStatusCode.RedirectMethod) ;
                }
            }

            return true;
        }

        private static bool TryParseStatusCode(string responseHeader, int statusCodeStartIndex, out int statusCode)
        {
            if (statusCodeStartIndex + StatusCodeLength > responseHeader.Length)
            {
                statusCode = 0;
                return false;
            }

            char c100 = responseHeader[statusCodeStartIndex];
            char c10 = responseHeader[statusCodeStartIndex + 1];
            char c1 = responseHeader[statusCodeStartIndex + 2];

            if (c100 < '0' || c100 > '9' ||
                c10 < '0' || c10 > '9' ||
                c1 < '0' || c1 > '9')
            {
                statusCode = 0;
                return false;
            }

            statusCode = (c100 - '0') * 100 + (c10 - '0') * 10 + (c1 - '0');

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
                NetworkCredential newCredential = GetCredentials(state._handler.Credentials as CredentialCache, forwardUri);
                state.SetCredentialsOptions(newCredential);

                // reset proxy - it is possible that the proxy has different credentials for the new URI
                state.SetProxyOptions(forwardUri);

                if (state._handler._useCookie)
                {
                    // reset cookies.
                    state.SetCurlOption(CURLoption.CURLOPT_COOKIE, IntPtr.Zero);

                    // set cookies again
                    state.SetCookieOption(forwardUri);
                }
            }
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

