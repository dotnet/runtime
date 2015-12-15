// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private const string VerboseDebuggingConditional = "CURLHANDLER_VERBOSE";
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
        private readonly static CURLAUTH[] s_authSchemePriorityOrder = { CURLAUTH.Negotiate, CURLAUTH.Digest, CURLAUTH.Basic };

        private readonly static bool s_supportsAutomaticDecompression;
        private readonly static bool s_supportsSSL;
        private readonly static bool s_supportsHttp2Multiplexing;

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

        private KeyValuePair<NetworkCredential, CURLAUTH> GetNetworkCredentials(ICredentials credentials, Uri requestUri)
        {
            if (_preAuthenticate)
            {
                KeyValuePair<NetworkCredential, CURLAUTH> ncAndScheme;
                lock (LockObject)
                {
                    Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                    ncAndScheme = GetCredentials(_credentialCache, requestUri);
                }
                if (ncAndScheme.Key != null)
                {
                    return ncAndScheme;
                }
            }

            return GetCredentials(credentials, requestUri);
        }

        private void AddCredentialToCache(Uri serverUri, CURLAUTH authAvail, NetworkCredential nc)
        {
            lock (LockObject)
            {
                for (int i=0; i < s_authSchemePriorityOrder.Length; i++)
                {
                    if ((authAvail & s_authSchemePriorityOrder[i]) != 0)
                    {
                        Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                        try
                        {
                            _credentialCache.Add(serverUri, s_authenticationSchemes[i], nc);
                        }
                        catch(ArgumentException)
                        {
                            //Ignore the case of key already present
                        }
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

        private static KeyValuePair<NetworkCredential, CURLAUTH> GetCredentials(ICredentials credentials, Uri requestUri)
        {
            NetworkCredential nc = null;
            CURLAUTH curlAuthScheme = CURLAUTH.None;

            if (credentials != null)
            {
                // we collect all the schemes that are accepted by libcurl for which there is a non-null network credential.
                // But CurlHandler works under following assumption:
                //           for a given server, the credentials do not vary across authentication schemes.
                for (int i=0; i < s_authSchemePriorityOrder.Length; i++)
                {
                    NetworkCredential networkCredential = credentials.GetCredential(requestUri, s_authenticationSchemes[i]);
                    if (networkCredential != null)
                    {
                        curlAuthScheme |= s_authSchemePriorityOrder[i];
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

            VerboseTrace("curlAuthScheme = " + curlAuthScheme);
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
                VerboseTrace(inner.Message);
                throw inner;
            }
        }

        private static void ThrowIfCURLMError(CURLMcode error)
        {
            if (error != CURLMcode.CURLM_OK)
            {
                string msg = CurlException.GetCurlErrorString((int)error, true);
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
                KeyValuePair<NetworkCredential, CURLAUTH> ncAndScheme = GetCredentials(state._handler.Credentials as CredentialCache, forwardUri);
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
                state._requestMessage.RequestUri = forwardUri;
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

