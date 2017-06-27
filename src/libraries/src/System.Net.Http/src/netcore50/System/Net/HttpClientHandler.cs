// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using RTApiInformation = Windows.Foundation.Metadata.ApiInformation;
using RTHttpBaseProtocolFilter = Windows.Web.Http.Filters.HttpBaseProtocolFilter;
using RTHttpCacheReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior;
using RTHttpCacheWriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior;
using RTHttpCookieUsageBehavior = Windows.Web.Http.Filters.HttpCookieUsageBehavior;
using RTPasswordCredential = Windows.Security.Credentials.PasswordCredential;
using RTCertificate = Windows.Security.Cryptography.Certificates.Certificate;
using RTCertificateQuery = Windows.Security.Cryptography.Certificates.CertificateQuery;
using RTCertificateStores = Windows.Security.Cryptography.Certificates.CertificateStores;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";
        private static readonly Lazy<bool> s_RTCookieUsageBehaviorSupported =
            new Lazy<bool>(InitRTCookieUsageBehaviorSupported);
        
        #region Fields

        private readonly RTHttpBaseProtocolFilter _rtFilter;
        private readonly HttpHandlerToFilter _handlerToFilter;
        private readonly HttpMessageHandler _diagnosticsPipeline;

        private volatile bool _operationStarted;
        private volatile bool _disposed;

        private ClientCertificateOption _clientCertificateOptions;
        private CookieContainer _cookieContainer;
        private bool _useCookies;
        private DecompressionMethods _automaticDecompression;
        private IWebProxy _proxy;
        private X509Certificate2Collection _clientCertificates;
        private IDictionary<String, Object> _properties; // Only create dictionary when required.

        #endregion Fields

        #region Properties

        public virtual bool SupportsAutomaticDecompression
        {
            get { return true; }
        }

        public virtual bool SupportsProxy
        {
            get { return false; }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { return false; }
        }

        public bool UseCookies
        {
            get { return _useCookies; }
            set
            {
                CheckDisposedOrStarted();
                _useCookies = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (!UseCookies)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_invalid_enable_first, nameof(UseCookies), "true"));
                }
                CheckDisposedOrStarted();
                _cookieContainer = value;
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { return _clientCertificateOptions; }
            set
            {
                if (value != ClientCertificateOption.Manual &&
                    value != ClientCertificateOption.Automatic)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                CheckDisposedOrStarted();
                _clientCertificateOptions = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { return _automaticDecompression; }
            set
            {
                CheckDisposedOrStarted();

                // Automatic decompression is implemented downstack.
                // HBPF will decompress both gzip and deflate, we will set
                // accept-encoding for one, the other, or both passed in here.
                _rtFilter.AutomaticDecompression = (value != DecompressionMethods.None);
                _automaticDecompression = value;
            }
        }

        public bool UseProxy
        {
            get { return _rtFilter.UseProxy; }
            set
            {
                CheckDisposedOrStarted();
                _rtFilter.UseProxy = value;
            }
        }

        public IWebProxy Proxy
        {
            // We don't actually support setting a different proxy because our Http stack in NETNative
            // layers on top of the WinRT HttpClient which uses Wininet.  And that API layer simply 
            // uses the proxy information in the registry (and the same that Internet Explorer uses).
            // However, we can't throw PlatformNotSupportedException because the .NET Desktop stack
            // does support this and doing so would break apps. So, we'll just let this get/set work
            // even though we ignore it.  The majority of apps actually use the default proxy anyways
            // so setting it here would be a no-op.
            get { return _proxy; }
            set
            {
                CheckDisposedOrStarted();
                _proxy = value;
                SetProxyCredential(_proxy);
            }
        }

        public bool PreAuthenticate
        {
            get { return true; }
            set
            {
                CheckDisposedOrStarted();
            }
        }

        public bool UseDefaultCredentials
        {
            get { return Credentials == null; }
            set
            {
                CheckDisposedOrStarted();
                if (value)
                {
                    // System managed
                    _rtFilter.ServerCredential = null;
                }
                else if (_rtFilter.ServerCredential == null)
                {
                    // The only way to disable default credentials is to provide credentials.
                    // Do not overwrite credentials if they were already assigned.
                    _rtFilter.ServerCredential = new RTPasswordCredential();
                }
            }
        }

        public ICredentials Credentials
        {
            get
            {
                RTPasswordCredential rtCreds = _rtFilter.ServerCredential;
                if (rtCreds == null)
                {
                    return null;
                }

                NetworkCredential creds = new NetworkCredential(rtCreds.UserName, rtCreds.Password);
                return creds;
            }
            set
            {
                if (value == null)
                {
                    CheckDisposedOrStarted();
                    _rtFilter.ServerCredential = null;
                }
                else if (value == CredentialCache.DefaultCredentials)
                {
                    CheckDisposedOrStarted();
                    // System managed
                    _rtFilter.ServerCredential = null;
                }
                else if (value is NetworkCredential)
                {
                    CheckDisposedOrStarted();
                    _rtFilter.ServerCredential = RTPasswordCredentialFromNetworkCredential((NetworkCredential)value);
                }
                else
                {
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(Credentials)));
                }
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get
            {
                RTPasswordCredential rtCreds = _rtFilter.ProxyCredential;
                if (rtCreds == null)
                {
                    return null;
                }

                NetworkCredential creds = new NetworkCredential(rtCreds.UserName, rtCreds.Password);
                return creds;
            }
            set
            {
                if (value == null)
                {
                    CheckDisposedOrStarted();
                    _rtFilter.ProxyCredential = null;
                }
                else if (value == CredentialCache.DefaultCredentials)
                {
                    CheckDisposedOrStarted();
                    // System managed
                    _rtFilter.ProxyCredential = null;
                }
                else if (value is NetworkCredential)
                {
                    CheckDisposedOrStarted();
                    _rtFilter.ProxyCredential = RTPasswordCredentialFromNetworkCredential((NetworkCredential)value);
                }
                else
                {
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(DefaultProxyCredentials)));
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get { return _rtFilter.AllowAutoRedirect; }
            set
            {
                CheckDisposedOrStarted();
                _rtFilter.AllowAutoRedirect = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get { return 10; } // WinRT Windows.Web.Http constant via use of native WinINet.
            set
            {
                CheckDisposedOrStarted();
            }
        }

        public int MaxConnectionsPerServer
        {
            get { return (int)_rtFilter.MaxConnectionsPerServer; }
            set
            {
                CheckDisposedOrStarted();
                _rtFilter.MaxConnectionsPerServer = (uint)value;
            }
        }
        
        public long MaxRequestContentBufferSize
        {
            get { return HttpContent.MaxBufferSize; }
            set
            {
                // .NET Native port note: We don't have an easy way to implement the MaxRequestContentBufferSize property. To maximize the chance of app compat,
                // we will "succeed" as long as the requested buffer size doesn't exceed the max. However, no actual
                // enforcement of the max buffer size occurs.
                if (value > MaxRequestContentBufferSize)
                {
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(MaxRequestContentBufferSize)));
                }
                CheckDisposedOrStarted();
            }
        }

        public int MaxResponseHeadersLength
        {
            // Windows.Web.Http is built on WinINet. There is no maximum limit (except for out of memory)
            // for received response headers. So, returning -1 (indicating no limit) is appropriate.
            get { return -1; }

            set
            {
                CheckDisposedOrStarted();
            }
        }

        private bool RTCookieUsageBehaviorSupported
        { 
            get
            {
                return s_RTCookieUsageBehaviorSupported.Value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            // TODO: Not yet implemented. Issue #7623.
            get
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509Certificate2Collection();
                }

                return _clientCertificates;
            }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            // TODO: Not yet implemented. Issue #7623.
            get{ return null; }
            set
            {
                CheckDisposedOrStarted();
                if (value != null)
                {
                    /* 
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(ServerCertificateCustomValidationCallback)));
                   */
                }
            }
        }

        public bool CheckCertificateRevocationList
        {
            // The WinRT API always checks for certificate revocation. If the revocation status is indeterminate
            // (such as revocation server is offline), then the WinRT API will indicate "success" and not fail
            // the request.
            get { return true; }
            set
            {
                CheckDisposedOrStarted();
            }
        }

        public SslProtocols SslProtocols
        {
            // The WinRT API does not expose a property to control this. It always uses the system default.
            get { return SslProtocols.None; }
            set
            {
                CheckDisposedOrStarted();
            }
        }

        public IDictionary<String, Object> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new Dictionary<String, object>();
                }

                return _properties;
            }
        }

        #endregion Properties

        #region De/Constructors

        public HttpClientHandler()
        {
            _rtFilter = new RTHttpBaseProtocolFilter();
            _handlerToFilter = new HttpHandlerToFilter(_rtFilter);
            _diagnosticsPipeline = new DiagnosticsHandler(_handlerToFilter);

            _clientCertificateOptions = ClientCertificateOption.Manual;

            InitRTCookieUsageBehavior();

            _useCookies = true; // deal with cookies by default.
            _cookieContainer = new CookieContainer(); // default container used for dealing with auto-cookies.

            // Managed at this layer for granularity, but uses the desktop default.
            _rtFilter.AutomaticDecompression = false;
            _automaticDecompression = DecompressionMethods.None;

            // Set initial proxy credentials based on default system proxy.
            SetProxyCredential(null);

            // We don't support using the UI model in HttpBaseProtocolFilter() especially for auto-handling 401 responses.
            _rtFilter.AllowUI = false;
            
            // The .NET Desktop System.Net Http APIs (based on HttpWebRequest/HttpClient) uses no caching by default.
            // To preserve app-compat, we turn off caching (as much as possible) in the WinRT HttpClient APIs.
            // TODO (#7877): use RTHttpCacheReadBehavior.NoCache when available in the next version of WinRT HttpClient API.
            _rtFilter.CacheControl.ReadBehavior = RTHttpCacheReadBehavior.MostRecent; 
            _rtFilter.CacheControl.WriteBehavior = RTHttpCacheWriteBehavior.NoCache;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                try
                {
                    _rtFilter.Dispose();
                }
                catch (InvalidComObjectException)
                {
                    // We'll ignore this error since it can happen when Dispose() is called from an object's finalizer
                    // and the WinRT object (rtFilter) has already been disposed by the .NET Native runtime.
                }
            }

            base.Dispose(disposing);
        }

        #endregion De/Constructors

        #region Request Setup

        private async Task ConfigureRequest(HttpRequestMessage request)
        {
            ApplyRequestCookies(request);

            ApplyDecompressionSettings(request);
            
            await ApplyClientCertificateSettings().ConfigureAwait(false);
        }

        // Taken from System.Net.CookieModule.OnSendingHeaders
        private void ApplyRequestCookies(HttpRequestMessage request)
        {
            if (UseCookies)
            {
                string cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    bool success = request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Cookie, cookieHeader);
                    System.Diagnostics.Debug.Assert(success);
                }
            }
        }

        private void ApplyDecompressionSettings(HttpRequestMessage request)
        {
            // Decompression: Add the Gzip and Deflate headers if not already present.
            ApplyDecompressionSetting(request, DecompressionMethods.GZip, "gzip");
            ApplyDecompressionSetting(request, DecompressionMethods.Deflate, "deflate");
        }

        private void ApplyDecompressionSetting(HttpRequestMessage request, DecompressionMethods method, string methodName)
        {
            if ((AutomaticDecompression & method) == method)
            {
                bool found = false;
                foreach (StringWithQualityHeaderValue encoding in request.Headers.AcceptEncoding)
                {
                    if (methodName.Equals(encoding.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(methodName));
                }
            }
        }

        private async Task ApplyClientCertificateSettings()
        {
            if (ClientCertificateOptions == ClientCertificateOption.Manual)
            {
                return;
            }

            // Get the certs that can be used for Client Authentication.
            var query = new RTCertificateQuery();
            var ekus = query.EnhancedKeyUsages;
            ekus.Add(ClientAuthenticationOID);
            var clientCertificates = await RTCertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);

            if (clientCertificates.Count > 0)
            {
                _rtFilter.ClientCertificate = clientCertificates[0];
            }
        }
        #endregion Request Setup

        #region Request Execution

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CheckDisposed();
            SetOperationStarted();

            HttpResponseMessage response;
            try
            {
                await ConfigureRequest(request).ConfigureAwait(false);

                Task<HttpResponseMessage> responseTask = DiagnosticsHandler.IsEnabled() ? 
                    _diagnosticsPipeline.SendAsync(request, cancellationToken) :
                    _handlerToFilter.SendAsync(request, cancellationToken);

                response = await responseTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Convert back to the expected exception type
                throw new HttpRequestException(SR.net_http_client_execution_error, ex);
            }

            ProcessResponse(response);
            return response;
        }

        #endregion Request Execution

        #region Response Processing

        private void ProcessResponse(HttpResponseMessage response)
        {
            ProcessResponseCookies(response);
        }

        // Taken from System.Net.CookieModule.OnReceivedHeaders
        private void ProcessResponseCookies(HttpResponseMessage response)
        {
            if (UseCookies)
            {
                IEnumerable<string> values;
                if (response.Headers.TryGetValues(HttpKnownHeaderNames.SetCookie, out values))
                {
                    foreach (string cookieString in values)
                    {
                        if (!string.IsNullOrWhiteSpace(cookieString))
                        {
                            try
                            {
                                // Parse the cookies so that we can filter some of them out
                                CookieContainer helper = new CookieContainer();
                                helper.SetCookies(response.RequestMessage.RequestUri, cookieString);
                                CookieCollection cookies = helper.GetCookies(response.RequestMessage.RequestUri);
                                foreach (Cookie cookie in cookies)
                                {
                                    // We don't want to put HttpOnly cookies in the CookieContainer if the system
                                    // doesn't support the RTHttpBaseProtocolFilter CookieUsageBehavior property.
                                    // Prior to supporting that, the WinRT HttpClient could not turn off cookie
                                    // processing. So, it would always be storing all cookies in its internal container.
                                    // Putting HttpOnly cookies in the .NET CookieContainer would cause problems later
                                    // when the .NET layer tried to add them on outgoing requests and conflicted with
                                    // the WinRT internal cookie processing.
                                    //
                                    // With support for WinRT CookieUsageBehavior, cookie processing is turned off
                                    // within the WinRT layer. This allows us to process cookies using only the .NET
                                    // layer. So, we need to add all applicable cookies that are received to the
                                    // CookieContainer.
                                    if (RTCookieUsageBehaviorSupported || !cookie.HttpOnly)
                                    {
                                        CookieContainer.Add(response.RequestMessage.RequestUri, cookie);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        #endregion Response Processing

        #region Helpers

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
                throw new ObjectDisposedException(GetType().ToString());
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

        private RTPasswordCredential RTPasswordCredentialFromNetworkCredential(NetworkCredential creds)
        {
            // RTPasswordCredential doesn't allow assigning string.Empty values, but those are the default values.
            RTPasswordCredential rtCreds = new RTPasswordCredential();
            if (!string.IsNullOrEmpty(creds.UserName))
            {
                if (!string.IsNullOrEmpty(creds.Domain))
                {
                    rtCreds.UserName = creds.Domain + "\\" + creds.UserName;
                }
                else
                {
                    rtCreds.UserName = creds.UserName;
                }
            }
            if (!string.IsNullOrEmpty(creds.Password))
            {
                rtCreds.Password = creds.Password;
            }

            return rtCreds;
        }
        
        private void SetProxyCredential(IWebProxy proxy)
        {
            // We don't support changing the proxy settings in the NETNative version of HttpClient since it's layered on
            // WinRT HttpClient. But we do support passing in explicit proxy credentials, if specified, which we can
            // get from the specified or default proxy.
            ICredentials proxyCredentials = null;
            if (_proxy != null)
            {
                proxyCredentials = _proxy.Credentials;
            }

            if (proxyCredentials != CredentialCache.DefaultCredentials && proxyCredentials is NetworkCredential)
            {
                _rtFilter.ProxyCredential = RTPasswordCredentialFromNetworkCredential((NetworkCredential)proxyCredentials);
            }
        }

        private static bool InitRTCookieUsageBehaviorSupported()
        {
            return RTApiInformation.IsPropertyPresent(
                "Windows.Web.Http.Filters.HttpBaseProtocolFilter",
                "CookieUsageBehavior");
        }

        // Regardless of whether we're running on a machine that supports this WinRT API, we still might not be able
        // to call the API. This is due to the calling app being compiled against an older Windows 10 Tools SDK. Since
        // this library was compiled against the newer SDK, having these new API calls in this class will cause JIT
        // failures in CoreCLR which generate a MissingMethodException before the code actually runs. So, we need
        // these helper methods and try/catch handling.

        private void InitRTCookieUsageBehavior()
        {
            try
            {
                InitRTCookieUsageBehaviorHelper();
            }
            catch (MissingMethodException)
            {
                Debug.WriteLine("HttpClientHandler.InitRTCookieUsageBehavior: MissingMethodException");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitRTCookieUsageBehaviorHelper()
        {
            // Always turn off WinRT cookie processing if the WinRT API supports turning it off.
            // Use .NET CookieContainer handling only.
            if (RTCookieUsageBehaviorSupported)
            {
                _rtFilter.CookieUsageBehavior = RTHttpCookieUsageBehavior.NoCookies;
            }
        }

        #endregion Helpers
    }
}
