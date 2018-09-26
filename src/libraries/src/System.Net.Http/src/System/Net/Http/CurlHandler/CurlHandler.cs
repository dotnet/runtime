// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using CURLAUTH = Interop.Http.CURLAUTH;
using CURLcode = Interop.Http.CURLcode;
using CURLMcode = Interop.Http.CURLMcode;
using CURLoption = Interop.Http.CURLoption;

namespace System.Net.Http
{
    // Object model:
    // -------------
    // CurlHandler provides an HttpMessageHandler implementation that wraps libcurl.  The core processing for CurlHandler
    // is handled via a CurlHandler.MultiAgent instance, where currently a CurlHandler instance stores and uses a single
    // MultiAgent for the lifetime of the handler (with the MultiAgent lazily initialized on first use, so that it can
    // be initialized with all of the configured options on the handler).  The MultiAgent is named as such because it wraps
    // a libcurl multi handle that's responsible for handling all requests on the instance.  When a request arrives, it's
    // queued to the MultiAgent, which ensures that a thread is running to continually loop and process all work associated
    // with the multi handle until no more work is required; at that point, the thread is retired until more work arrives,
    // at which point another thread will be spun up.  Any number of requests will have their handling multiplexed onto
    // this one event loop thread.  Each request is represented by a CurlHandler.EasyRequest, so named because it wraps
    // a libcurl easy handle, libcurl's representation of a request.  The EasyRequest stores all state associated with
    // the request, including the CurlHandler.CurlResponseMessage and CurlHandler.CurlResponseStream that are handed
    // back to the caller to provide access to the HTTP response information.
    //
    // Lifetime:
    // ---------
    // The MultiAgent is initialized on first use and is kept referenced by the CurlHandler for the remainder of the
    // handler's lifetime.  Both are disposable, and disposing of the CurlHandler will dispose of the MultiAgent.
    // However, libcurl is not thread safe in that two threads can't be using the same multi or easy handles concurrently,
    // so any interaction with the multi handle must happen on the MultiAgent's thread.  For this reason, the
    // SafeHandle storing the underlying multi handle has its ref count incremented when the MultiAgent worker is running
    // and decremented when it stops running, enabling any disposal requests to be delayed until the worker has quiesced.
    // To enable that to happen quickly when a dispose operation occurs, an "incoming request" (how all other threads
    // communicate with the MultiAgent worker) is queued to the worker to request a shutdown; upon receiving that request,
    // the worker will exit and allow the multi handle to be disposed of.
    //
    // An EasyRequest itself doesn't govern its own lifetime.  Since an easy handle is added to a multi handle for
    // the multi handle to process, the easy handle must not be destroyed until after it's been removed from the multi handle.
    // As such, once the SafeHandle for an easy handle is created, although its stored in the EasyRequest instance,
    // it's also stored into a dictionary on the MultiAgent and has its ref count incremented to prevent it from being
    // disposed of while it's in use by the multi handle.
    //
    // When a request is made to the CurlHandler, callbacks are registered with libcurl, including state that will
    // be passed back into managed code and used to identify the associated EasyRequest.  This means that the native
    // code needs to be able both to keep the EasyRequest alive and to refer to it using an IntPtr.  For this, we
    // use a GCHandle to the EasyRequest.  However, the native code needs to be able to refer to the EasyRequest for the
    // lifetime of the request, but we also need to avoid keeping the EasyRequest (and all state it references) alive artificially.
    // For the beginning phase of the request, the native code may be the only thing referencing the managed objects, since
    // when a caller invokes "Task<HttpResponseMessage> SendAsync(...)", there's nothing handed back to the caller that represents
    // the request until at least the HTTP response headers are received and the returned Task is completed with the response
    // message object.  However, after that point, if the caller drops the HttpResponseMessage, we also want to cancel and
    // dispose of the associated state, which means something needs to be finalizable and not kept rooted while at the same
    // time still allowing the native code to continue using its GCHandle and lookup the associated state as long as it's alive.
    // Yet then when an async read is made on the response message, we want to postpone such finalization and ensure that the async
    // read can be appropriately completed with control and reference ownership given back to the reader. As such, we do two things:
    // we make the response stream finalizable, and we make the GCHandle be to a wrapper object for the EasyRequest rather than to
    // the EasyRequest itself.  That wrapper object maintains a weak reference to the EasyRequest as well as sometimes maintaining
    // a strong reference.  When the request starts out, the GCHandle is created to the wrapper, which has a strong reference to
    // the EasyRequest (which also back references to the wrapper so that the wrapper can be accessed via it).  The GCHandle is
    // thus keeping the EasyRequest and all of the state it references alive, e.g. the CurlResponseStream, which itself has a reference
    // back to the EasyRequest.  Once the request progresses to the point of receiving HTTP response headers and the HttpResponseMessage
    // is handed back to the caller, the wrapper object drops its strong reference and maintains only a weak reference.  At this
    // point, if the caller were to drop its HttpResponseMessage object, that would also drop the only strong reference to the
    // CurlResponseStream; the CurlResponseStream would be available for collection and finalization, and its finalization would
    // request cancellation of the easy request to the multi agent.  The multi agent would then in response remove the easy handle
    // from the multi handle and decrement the ref count on the SafeHandle for the easy handle, allowing it to be finalized and
    // the underlying easy handle released.  If instead of dropping the HttpResponseMessage the caller makes a read request on the
    // response stream, the wrapper object is transitioned back to having a strong reference, so that even if the caller then drops
    // the HttpResponseMessage, the read Task returned from the read operation will still be completed eventually, at which point
    // the wrapper will transition back to being a weak reference.
    //
    // Even with that, of course, Dispose is still the recommended way of cleaning things up.  Disposing the CurlResponseMessage
    // will Dispose the CurlResponseStream, which will Dispose of the SafeHandle for the easy handle and request that the MultiAgent
    // cancel the operation.  Once canceled and removed, the SafeHandle will have its ref count decremented and the previous disposal
    // will proceed to release the underlying handle.

    internal partial class CurlHandler : HttpMessageHandler
    {
        #region Constants

        private const char SpaceChar = ' ';
        private const int StatusCodeLength = 3;

        private const string UriSchemeHttp = "http";
        private const string UriSchemeHttps = "https";
        private const string EncodingNameGzip = "gzip";
        private const string EncodingNameDeflate = "deflate";

        private const int MaxRequestBufferSize = 16384; // Default used by libcurl
        private const string NoTransferEncoding = HttpKnownHeaderNames.TransferEncoding + ":";
        private const string NoContentType = HttpKnownHeaderNames.ContentType + ":";
        private const string NoExpect = HttpKnownHeaderNames.Expect + ":";
        private const int CurlAge = 5;
        private const int MinCurlAge = 3;

        #endregion

        #region Fields

        private static readonly KeyValuePair<string,CURLAUTH>[] s_orderedAuthTypes = new KeyValuePair<string, CURLAUTH>[] {
            new KeyValuePair<string,CURLAUTH>("Negotiate", CURLAUTH.Negotiate),
            new KeyValuePair<string,CURLAUTH>("NTLM", CURLAUTH.NTLM),
            new KeyValuePair<string,CURLAUTH>("Digest", CURLAUTH.Digest),
            new KeyValuePair<string,CURLAUTH>("Basic", CURLAUTH.Basic),
        };

        // Max timeout value used by WinHttp handler, so mapping to that here.
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private static readonly char[] s_newLineCharArray = new char[] { HttpRuleParser.CR, HttpRuleParser.LF };
        private static readonly bool s_supportsAutomaticDecompression;
        private static readonly bool s_supportsSSL;
        private static readonly bool s_supportsHttp2Multiplexing;
        private static string s_curlVersionDescription;
        private static string s_curlSslVersionDescription;

        private static readonly MultiAgent s_singletonSharedAgent;
        private readonly MultiAgent _agent;
        private volatile bool _anyOperationStarted;
        private volatile bool _disposed;

        private IWebProxy _proxy = null;
        private ICredentials _serverCredentials = null;
        private bool _useProxy = HttpHandlerDefaults.DefaultUseProxy;
        private ICredentials _defaultProxyCredentials = null;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private bool _preAuthenticate = HttpHandlerDefaults.DefaultPreAuthenticate;
        private CredentialCache _credentialCache = null; // protected by LockObject
        private bool _useDefaultCredentials = HttpHandlerDefaults.DefaultUseDefaultCredentials;
        private CookieContainer _cookieContainer = new CookieContainer();
        private bool _useCookies = HttpHandlerDefaults.DefaultUseCookies;
        private TimeSpan _connectTimeout = Timeout.InfiniteTimeSpan;
        private bool _automaticRedirection = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private int _maxConnectionsPerServer = HttpHandlerDefaults.DefaultMaxConnectionsPerServer;
        private int _maxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;
        private ClientCertificateOption _clientCertificateOption = HttpHandlerDefaults.DefaultClientCertificateOption;
        private X509Certificate2Collection _clientCertificates;
        private Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _serverCertificateValidationCallback;
        private bool _checkCertificateRevocationList = HttpHandlerDefaults.DefaultCheckCertificateRevocationList;
        private SslProtocols _sslProtocols = SslProtocols.None; // use default
        private IDictionary<string, object> _properties; // Only create dictionary when required.

        private object LockObject { get { return _agent; } }

        #endregion

        static CurlHandler()
        {
            // curl_global_init call handled by Interop.LibCurl's cctor

            Interop.Http.CurlFeatures features = Interop.Http.GetSupportedFeatures();
            s_supportsSSL = (features & Interop.Http.CurlFeatures.CURL_VERSION_SSL) != 0;
            s_supportsAutomaticDecompression = (features & Interop.Http.CurlFeatures.CURL_VERSION_LIBZ) != 0;
            s_supportsHttp2Multiplexing = (features & Interop.Http.CurlFeatures.CURL_VERSION_HTTP2) != 0 && Interop.Http.GetSupportsHttp2Multiplexing() && !UseSingletonMultiAgent;

            if (NetEventSource.IsEnabled)
            {
                EventSourceTrace($"libcurl: {CurlVersionDescription} {CurlSslVersionDescription} {features}");
            }

            // By default every CurlHandler gets its own MultiAgent.  But for some backends,
            // we need to restrict the number of threads involved in processing libcurl work,
            // so we create a single MultiAgent that's used by all handlers.
            if (UseSingletonMultiAgent)
            {
                s_singletonSharedAgent = new MultiAgent(null);
            }
        }

        public CurlHandler()
        {
            // If the shared MultiAgent was initialized, use it.
            // Otherwise, create a new MultiAgent for this handler.
            _agent = s_singletonSharedAgent ?? new MultiAgent(this);
        }

        #region Properties

        private static string CurlVersionDescription => s_curlVersionDescription ?? (s_curlVersionDescription = Interop.Http.GetVersionDescription() ?? string.Empty);
        private static string CurlSslVersionDescription => s_curlSslVersionDescription ?? (s_curlSslVersionDescription = Interop.Http.GetSslVersionDescription() ?? string.Empty);

        private static bool UseSingletonMultiAgent
        {
            get
            {
                // Some backends other than OpenSSL need locks initialized in order to use them in a
                // multithreaded context, which would happen with multiple HttpClients and thus multiple
                // MultiAgents. Since we don't currently have the ability to do so initialization, instead we
                // restrict all HttpClients to use the same MultiAgent instance in this case.  We know LibreSSL
                // is in this camp, so we currently special-case it.
                string curlSslVersion = Interop.Http.GetSslVersionDescription();
                return
                    !string.IsNullOrEmpty(curlSslVersion) &&
                    curlSslVersion.StartsWith(Interop.Http.LibreSslDescription, StringComparison.OrdinalIgnoreCase);
            }
        }

        internal bool AllowAutoRedirect
        {
            get { return _automaticRedirection; }
            set
            {
                CheckDisposedOrStarted();
                _automaticRedirection = value;
            }
        }

        internal bool UseProxy
        {
            get { return _useProxy; }
            set
            {
                CheckDisposedOrStarted();
                _useProxy = value;
            }
        }

        internal IWebProxy Proxy
        {
            get { return _proxy; }
            set
            {
                CheckDisposedOrStarted();
                _proxy = value;
            }
        }

        internal ICredentials DefaultProxyCredentials
        {
            get { return _defaultProxyCredentials; }
            set
            {
                CheckDisposedOrStarted();
                _defaultProxyCredentials = value;
            }
        }

        internal ICredentials Credentials
        {
            get { return _serverCredentials; }
            set { _serverCredentials = value; }
        }

        internal ClientCertificateOption ClientCertificateOptions
        {
            get { return _clientCertificateOption; }
            set
            {
                if (value != ClientCertificateOption.Manual &&
                    value != ClientCertificateOption.Automatic)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _clientCertificateOption = value;
            }
        }

        internal X509Certificate2Collection ClientCertificates
        {
            get
            {
                if (_clientCertificateOption != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                }

                return _clientCertificates ?? (_clientCertificates = new X509Certificate2Collection());
            }
        }

        internal Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get { return _serverCertificateValidationCallback; }
            set
            {
                CheckDisposedOrStarted();
                _serverCertificateValidationCallback = value;
            }
        }

        internal bool CheckCertificateRevocationList
        {
            get { return _checkCertificateRevocationList; }
            set
            {
                CheckDisposedOrStarted();
                _checkCertificateRevocationList = value;
            }
        }

        internal SslProtocols SslProtocols
        {
            get { return _sslProtocols; }
            set
            {
                CheckDisposedOrStarted();
                _sslProtocols = value;
            }
        }

        internal bool SupportsAutomaticDecompression => s_supportsAutomaticDecompression;

        internal DecompressionMethods AutomaticDecompression
        {
            get { return _automaticDecompression; }
            set
            {
                CheckDisposedOrStarted();
                _automaticDecompression = value;
            }
        }

        internal bool PreAuthenticate
        {
            get { return _preAuthenticate; }
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

        internal bool UseCookies
        {
            get { return _useCookies; }
            set
            {
                CheckDisposedOrStarted();
                _useCookies = value;
            }
        }

        internal CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set
            {
                CheckDisposedOrStarted();
                _cookieContainer = value;
            }
        }

        internal int MaxAutomaticRedirections
        {
            get { return _maxAutomaticRedirections; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxAutomaticRedirections = value;
            }
        }

        internal int MaxConnectionsPerServer
        {
            get { return _maxConnectionsPerServer; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxConnectionsPerServer = value;
            }
        }

        internal int MaxResponseHeadersLength
        {
            get { return _maxResponseHeadersLength; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxResponseHeadersLength = value;
            }
        }

        internal bool UseDefaultCredentials
        {
            get { return _useDefaultCredentials; }
            set
            {
                CheckDisposedOrStarted();
                _useDefaultCredentials = value;
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
            _disposed = true;
            if (disposing && _agent != s_singletonSharedAgent)
            {
                _agent.Dispose();
            }
            base.Dispose(disposing);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            if (request.RequestUri.Scheme == UriSchemeHttps)
            {
                if (!s_supportsSSL)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_http_unix_https_support_unavailable_libcurl, CurlVersionDescription));
                }
            }
            else
            {
                Debug.Assert(request.RequestUri.Scheme == UriSchemeHttp, "HttpClient expected to validate scheme as http or https.");
            }

            if (request.Headers.TransferEncodingChunked.GetValueOrDefault() && (request.Content == null))
            {
                return Task.FromException<HttpResponseMessage>(
                    new HttpRequestException(SR.net_http_client_execution_error,
                        new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content)));
            }

            if (_useCookies && _cookieContainer == null)
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
            var easy = new EasyRequest(this, _agent, request, cancellationToken);
            try
            {
                EventSourceTrace("{0}", request, easy: easy);
                _agent.Queue(new MultiAgent.IncomingRequest { Easy = easy, Type = MultiAgent.IncomingRequestType.New });
            }
            catch (Exception exc)
            {
                easy.CleanupAndFailRequest(exc);
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

        private KeyValuePair<NetworkCredential, CURLAUTH> GetCredentials(Uri requestUri)
        {
            // If preauthentication is enabled, we may have populated our internal credential cache,
            // so first check there to see if we have any credentials for this uri.
            if (_preAuthenticate)
            {
                KeyValuePair<NetworkCredential, CURLAUTH> ncAndScheme;
                lock (LockObject)
                {
                    Debug.Assert(_credentialCache != null, "Expected non-null credential cache");
                    ncAndScheme = GetCredentials(requestUri, _credentialCache, s_orderedAuthTypes);
                }
                if (ncAndScheme.Key != null)
                {
                    return ncAndScheme;
                }
            }

            // We either weren't preauthenticating or we didn't have any cached credentials
            // available, so check the credentials on the handler.
            return GetCredentials(requestUri, _serverCredentials, s_orderedAuthTypes);
        }

        private void TransferCredentialsToCache(Uri serverUri, CURLAUTH serverAuthAvail)
        {
            if (_serverCredentials == null)
            {
                // No credentials, nothing to put into the cache.
                return;
            }

            lock (LockObject)
            {
                // For each auth type we allow, check whether it's one supported by the server.
                KeyValuePair<string, CURLAUTH>[] validAuthTypes = s_orderedAuthTypes;
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

        private void AddResponseCookies(EasyRequest state, string cookieHeader)
        {
            if (!_useCookies)
            {
                return;
            }

            try
            {
                _cookieContainer.SetCookies(state._requestMessage.RequestUri, cookieHeader);
                state.SetCookieOption(state._requestMessage.RequestUri);
            }
            catch (CookieException e)
            {
                EventSourceTrace(
                    "Malformed cookie parsing failed: {0}, server: {1}, cookie: {2}",
                    e.Message, state._requestMessage.RequestUri, cookieHeader,
                    easy: state);
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
                            throw new PlatformNotSupportedException(SR.Format(SR.net_http_unix_invalid_credential, CurlVersionDescription));
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
            if (error != CURLcode.CURLE_OK) // success
            {
                string msg = CurlException.GetCurlErrorString((int)error, isMulti: false);
                EventSourceTrace(msg);
                switch (error)
                {
                    case CURLcode.CURLE_OPERATION_TIMEDOUT:
                        throw new OperationCanceledException(msg);

                    case CURLcode.CURLE_OUT_OF_MEMORY:
                        throw new OutOfMemoryException(msg);

                    case CURLcode.CURLE_SEND_FAIL_REWIND:
                        throw new InvalidOperationException(msg);

                    default:
                        throw new CurlException((int)error, msg);
                }
            }
        }

        private static void ThrowIfCURLMError(CURLMcode error)
        {
            if (error != CURLMcode.CURLM_OK && // success
                error != CURLMcode.CURLM_CALL_MULTI_PERFORM) // success + a hint to try curl_multi_perform again
            {
                string msg = CurlException.GetCurlErrorString((int)error, isMulti: true);
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

        // PERF NOTE:
        // These generic overloads of EventSourceTrace (and similar wrapper methods in some of the other CurlHandler
        // nested types) exist to allow call sites to call EventSourceTrace without boxing and without checking
        // NetEventSource.IsEnabled.  Do not remove these without fixing the call sites accordingly.

        private static void EventSourceTrace<TArg0>(
            string formatMessage, TArg0 arg0,
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (NetEventSource.IsEnabled)
            {
                EventSourceTraceCore(string.Format(formatMessage, arg0), agent, easy, memberName);
            }
        }

        private static void EventSourceTrace<TArg0, TArg1, TArg2>
            (string formatMessage, TArg0 arg0, TArg1 arg1, TArg2 arg2,
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (NetEventSource.IsEnabled)
            {
                EventSourceTraceCore(string.Format(formatMessage, arg0, arg1, arg2), agent, easy, memberName);
            }
        }

        private static void EventSourceTrace(
            string message,
            MultiAgent agent = null, EasyRequest easy = null, [CallerMemberName] string memberName = null)
        {
            if (NetEventSource.IsEnabled)
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

            NetEventSource.Log.HandlerMessage(
                agent?.GetHashCode() ?? 0,
                (agent?.RunningWorkerId).GetValueOrDefault(),
                easy?.Task.Id ?? 0,
                memberName,
                message);
        }

        private static HttpRequestException CreateHttpRequestException(Exception inner)
        {
            return new HttpRequestException(SR.net_http_client_execution_error, inner);
        }

        private static IOException MapToReadWriteIOException(Exception error, bool isRead)
        {
            return new IOException(
                isRead ? SR.net_http_io_read : SR.net_http_io_write,
                error is HttpRequestException && error.InnerException != null ? error.InnerException : error);
        }

        private static void SetChunkedModeForSend(HttpRequestMessage request)
        {
            bool chunkedMode = request.Headers.TransferEncodingChunked.GetValueOrDefault();
            HttpContent requestContent = request.Content;
            Debug.Assert(requestContent != null, "request is null");

            // Deal with conflict between 'Content-Length' vs. 'Transfer-Encoding: chunked' semantics.
            // libcurl adds a Transfer-Encoding header by default and the request fails if both are set.
            // ISSUE: 25163
            // Ideally we want to avoid modifying the users request message.
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
    }
}
