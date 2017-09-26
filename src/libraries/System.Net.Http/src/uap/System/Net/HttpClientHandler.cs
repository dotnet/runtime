// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using RTApiInformation = Windows.Foundation.Metadata.ApiInformation;
using RTHttpBaseProtocolFilter = Windows.Web.Http.Filters.HttpBaseProtocolFilter;
using RTHttpCacheReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior;
using RTHttpCacheWriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior;
using RTHttpCookieUsageBehavior = Windows.Web.Http.Filters.HttpCookieUsageBehavior;
using RTHttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using RTPasswordCredential = Windows.Security.Credentials.PasswordCredential;
using RTCertificate = Windows.Security.Cryptography.Certificates.Certificate;
using RTChainValidationResult = Windows.Security.Cryptography.Certificates.ChainValidationResult;
using RTHttpServerCustomValidationRequestedEventArgs = Windows.Web.Http.Filters.HttpServerCustomValidationRequestedEventArgs;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private const string RequestMessageLookupKey = "System.Net.Http.HttpRequestMessage";
        private const string SavedExceptionDispatchInfoLookupKey = "System.Runtime.ExceptionServices.ExceptionDispatchInfo";
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";
        private static Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");
        private static readonly Lazy<bool> s_RTCookieUsageBehaviorSupported =
            new Lazy<bool>(InitRTCookieUsageBehaviorSupported);
        internal static bool RTCookieUsageBehaviorSupported => s_RTCookieUsageBehaviorSupported.Value;
        private static readonly Lazy<bool> s_RTNoCacheSupported =
            new Lazy<bool>(InitRTNoCacheSupported);
        private static bool RTNoCacheSupported => s_RTNoCacheSupported.Value;
        private static readonly Lazy<bool> s_RTServerCustomValidationRequestedSupported =
            new Lazy<bool>(InitRTServerCustomValidationRequestedSupported);
        private static bool RTServerCustomValidationRequestedSupported => s_RTServerCustomValidationRequestedSupported.Value;

        #region Fields

        private readonly HttpHandlerToFilter _handlerToFilter;
        private readonly HttpMessageHandler _diagnosticsPipeline;

        private RTHttpBaseProtocolFilter _rtFilter;
        private ClientCertificateOption _clientCertificateOptions;
        private CookieContainer _cookieContainer;
        private bool _useCookies;
        private DecompressionMethods _automaticDecompression;
        private bool _allowAutoRedirect;
        private int _maxAutomaticRedirections;
        private ICredentials _defaultProxyCredentials;
        private ICredentials _credentials;
        private IWebProxy _proxy;
        private X509Certificate2Collection _clientCertificates;
        private IDictionary<String, Object> _properties; // Only create dictionary when required.
        private Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _serverCertificateCustomValidationCallback;

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
            get { return false; }
            set
            {
                CheckDisposedOrStarted();
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                CheckDisposedOrStarted();
                if (value != null && value != CredentialCache.DefaultCredentials && !(value is NetworkCredential))
                {
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(Credentials)));
                }
                
                _credentials = value;
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
                if (value != null && value != CredentialCache.DefaultCredentials && !(value is NetworkCredential))
                {
                    throw new PlatformNotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SR.net_http_value_not_supported, value, nameof(DefaultProxyCredentials)));
                }
                
                _defaultProxyCredentials = value;;
            }
        }

        public bool AllowAutoRedirect
        {
            get { return _allowAutoRedirect; }
            set
            {
                CheckDisposedOrStarted();
                _allowAutoRedirect = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get { return _maxAutomaticRedirections; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
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

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificateOptions != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(
                        SR.net_http_invalid_enable_first,
                        nameof(ClientCertificateOptions),
                        nameof(ClientCertificateOption.Manual)));
                }

                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509Certificate2Collection();
                }

                return _clientCertificates;
            }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get
            {
                return _serverCertificateCustomValidationCallback;
            }
            set
            {
                CheckDisposedOrStarted();
                if (value != null)
                {
                    if (!RTServerCustomValidationRequestedSupported)
                    {
                        throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                            SR.net_http_feature_requires_Windows10Version1607));
                    }
                }

                _serverCertificateCustomValidationCallback = value;
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
            _rtFilter = CreateFilter();

            _handlerToFilter = new HttpHandlerToFilter(_rtFilter, this);
            _handlerToFilter.RequestMessageLookupKey = RequestMessageLookupKey;
            _handlerToFilter.SavedExceptionDispatchInfoLookupKey = SavedExceptionDispatchInfoLookupKey;
            _diagnosticsPipeline = new DiagnosticsHandler(_handlerToFilter);

            _clientCertificateOptions = ClientCertificateOption.Manual;

            _useCookies = true; // deal with cookies by default.
            _cookieContainer = new CookieContainer(); // default container used for dealing with auto-cookies.

            _allowAutoRedirect = true;
            _maxAutomaticRedirections = 50;

            _automaticDecompression = DecompressionMethods.None;
        }

        private RTHttpBaseProtocolFilter CreateFilter()
        {
            var filter = new RTHttpBaseProtocolFilter();

            // Always turn off WinRT cookie processing if the WinRT API supports turning it off.
            // Use .NET CookieContainer handling only.
            if (RTCookieUsageBehaviorSupported)
            {
                filter.CookieUsageBehavior = RTHttpCookieUsageBehavior.NoCookies;
            }

            // Handle redirections at the .NET layer so that we can see cookies on redirect responses
            // and have control of the number of redirections allowed.
            filter.AllowAutoRedirect = false;

            filter.AutomaticDecompression = false;

            // We don't support using the UI model in HttpBaseProtocolFilter() especially for auto-handling 401 responses.
            filter.AllowUI = false;

            // The .NET Desktop System.Net Http APIs (based on HttpWebRequest/HttpClient) uses no caching by default.
            // To preserve app-compat, we turn off caching in the WinRT HttpClient APIs.
            filter.CacheControl.ReadBehavior = RTNoCacheSupported ?
                RTHttpCacheReadBehavior.NoCache : RTHttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = RTHttpCacheWriteBehavior.NoCache;
            
            return filter;
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
            ApplyDecompressionSettings(request);
            
            await ApplyClientCertificateSettings().ConfigureAwait(false);
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
                if (_clientCertificates != null && _clientCertificates.Count > 0)
                {
                    X509Certificate2 clientCert = CertificateHelper.GetEligibleClientCertificate(_clientCertificates);
                    if (clientCert == null)
                    {
                        return;
                    }

                    RTCertificate rtClientCert = await CertificateHelper.ConvertDotNetClientCertToWinRtClientCertAsync(clientCert);
                    if (rtClientCert == null)
                    {
                        throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                            SR.net_http_feature_UWPClientCertSupportRequiresCertInPersonalCertificateStore));
                    }

                    _rtFilter.ClientCertificate = rtClientCert;
                }

                return;
            }
            else
            {
                X509Certificate2 clientCert = CertificateHelper.GetEligibleClientCertificate();
                if (clientCert == null)
                {
                    return;
                }

                // Unlike in the .Manual case above, the conversion to WinRT Certificate should always work;
                // so we just use an Assert. All the possible client certs were enumerated from that store and
                // filtered down to a single client cert.
                RTCertificate rtClientCert = await CertificateHelper.ConvertDotNetClientCertToWinRtClientCertAsync(clientCert);
                Debug.Assert(rtClientCert != null);
                _rtFilter.ClientCertificate = rtClientCert;
            }
        }

        private RTPasswordCredential RTPasswordCredentialFromICredentials(ICredentials creds)
        {
            // The WinRT PasswordCredential object does not have a special credentials value for "default credentials".
            // In general, the UWP HTTP platform automatically manages sending default credentials, if no explicit
            // credential was specified, based on if the app has EnterpriseAuthentication capability and if the endpoint
            // is listed in an intranet zone.
            //
            // A WinRT PasswordCredential object that is either null or created with the default constructor (i.e. with
            // empty values for username and password) indicates that there is no explicit credential. And that means
            // that the default logged-on credentials might be sent to the endpoint.
            //
            // There is currently no WinRT API to turn off sending default credentials other than the capability
            // and intranet zone checks described above. In general, the UWP HTTP model for specifying default
            // credentials is orthogonal to how the .NET System.Net APIs have been designed.
            if (creds == null || creds == CredentialCache.DefaultCredentials)
            {
                return null;
            }
            else
            {
                Debug.Assert(creds is NetworkCredential);

                NetworkCredential networkCred = (NetworkCredential)creds;

                // Creating a new WinRT PasswordCredential object with the default constructor ends up
                // with empty strings for username and password inside the object. However, one can't assign
                // empty strings to those properties; otherwise, it will throw an error.
                RTPasswordCredential rtCreds = new RTPasswordCredential();
                if (!string.IsNullOrEmpty(networkCred.UserName))
                {
                    if (!string.IsNullOrEmpty(networkCred.Domain))
                    {
                        rtCreds.UserName = networkCred.Domain + "\\" + networkCred.UserName;
                    }
                    else
                    {
                        rtCreds.UserName = networkCred.UserName;
                    }
                }

                if (!string.IsNullOrEmpty(networkCred.Password))
                {
                    rtCreds.Password = networkCred.Password;
                }

                return rtCreds;
            }
        }

        private void SetFilterProxyCredential()
        {
            // We don't support changing the proxy settings in the UAP version of HttpClient since it's layered on
            // WinRT HttpClient. But we do support passing in explicit proxy credentials, if specified, which we can
            // get from the specified or default proxy.
            ICredentials proxyCredentials = null;
            if (UseProxy)
            {
                if (_proxy != null)
                {
                    proxyCredentials = _proxy.Credentials;
                }
                else
                {
                    proxyCredentials = _defaultProxyCredentials;
                }
            }

            _rtFilter.ProxyCredential = RTPasswordCredentialFromICredentials(proxyCredentials);
        }

        private void SetFilterServerCredential()
        {
            _rtFilter.ServerCredential = RTPasswordCredentialFromICredentials(_credentials);
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
                if (string.Equals(request.Method.Method, HttpMethod.Trace.Method, StringComparison.OrdinalIgnoreCase))
                {
                    // https://github.com/dotnet/corefx/issues/22161
                    throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        SR.net_http_httpmethod_notsupported_error, request.Method.Method));
                }

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
                // Convert back to the expected exception type.
                throw new HttpRequestException(SR.net_http_client_execution_error, ex);
            }

            return response;
        }

        #endregion Request Execution

        #region Helpers

        private void SetOperationStarted()
        {
            if (!_operationStarted)
            {
                // Since this is the first operation, we set all the necessary WinRT filter properties.
                SetFilterProxyCredential();
                SetFilterServerCredential();
                if (_serverCertificateCustomValidationCallback != null)
                {
                    Debug.Assert(RTServerCustomValidationRequestedSupported);

                    // The WinRT layer uses a different model for the certificate callback. The callback is
                    // considered "extra" validation. We need to explicitly ignore errors so that the callback
                    // will get called.
                    //
                    // In addition, the WinRT layer restricts some errors so that they cannot be ignored, such 
                    // as "Revoked". This will result in behavior differences between UWP and other platforms.
                    // The following errors cannot be ignored right now in the WinRT layer:
                    //
                    //     ChainValidationResult.BasicConstraintsError
                    //     ChainValidationResult.InvalidCertificateAuthorityPolicy
                    //     ChainValidationResult.InvalidSignature
                    //     ChainValidationResult.OtherErrors
                    //     ChainValidationResult.Revoked
                    //     ChainValidationResult.UnknownCriticalExtension
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.Expired);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.IncompleteChain);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.InvalidName);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.RevocationFailure);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.RevocationInformationMissing);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.Untrusted);
                    _rtFilter.IgnorableServerCertificateErrors.Add(RTChainValidationResult.WrongUsage);
                    _rtFilter.ServerCustomValidationRequested += RTServerCertificateCallback;
                }

                _operationStarted = true;
            }
        }

        private static bool InitRTCookieUsageBehaviorSupported()
        {
            return RTApiInformation.IsPropertyPresent(
                "Windows.Web.Http.Filters.HttpBaseProtocolFilter",
                "CookieUsageBehavior");
        }

        private static bool InitRTNoCacheSupported()
        {
            return RTApiInformation.IsEnumNamedValuePresent(
                "Windows.Web.Http.Filters.HttpCacheReadBehavior",
                "NoCache");
        }

        private static bool InitRTServerCustomValidationRequestedSupported()
        {
            return RTApiInformation.IsEventPresent(
                "Windows.Web.Http.Filters.HttpBaseProtocolFilter",
                "ServerCustomValidationRequested");
        }

        internal void RTServerCertificateCallback(RTHttpBaseProtocolFilter sender, RTHttpServerCustomValidationRequestedEventArgs args)
        {
            bool success = RTServerCertificateCallbackHelper(
                args.RequestMessage,
                args.ServerCertificate,
                args.ServerIntermediateCertificates,
                args.ServerCertificateErrors);

            if (!success)
            {
                args.Reject();
            }
        }

        private bool RTServerCertificateCallbackHelper(
            RTHttpRequestMessage requestMessage,
            RTCertificate cert,
            IReadOnlyList<RTCertificate> intermediateCerts,
            IReadOnlyList<RTChainValidationResult> certErrors)
        {
            // Convert WinRT certificate to .NET certificate.
            X509Certificate2 serverCert = CertificateHelper.ConvertPublicKeyCertificate(cert);

            // Create .NET X509Chain from the WinRT information. We need to rebuild the chain since WinRT only
            // gives us an array of intermediate certificates and not a X509Chain object.
            var serverChain = new X509Chain();
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            foreach (RTCertificate intermediateCert in intermediateCerts)
            {
                serverChain.ChainPolicy.ExtraStore.Add(CertificateHelper.ConvertPublicKeyCertificate(cert));
            }
            serverChain.ChainPolicy.RevocationMode = X509RevocationMode.Online; // WinRT always checks revocation.
            serverChain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            // Authenticate the remote party: (e.g. when operating in client mode, authenticate the server).
            serverChain.ChainPolicy.ApplicationPolicy.Add(s_serverAuthOid);
            if (!serverChain.Build(serverCert))
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            // Determine name-mismatch error from the existing WinRT information since .NET X509Chain.Build does not
            // return that in the X509Chain.ChainStatus fields.
            foreach (RTChainValidationResult result in certErrors)
            {
                if (result == RTChainValidationResult.InvalidName)
                {
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                    break;
                }
            }

            // Get the .NET HttpRequestMessage we saved in the property bag of the WinRT HttpRequestMessage.
            HttpRequestMessage request = (HttpRequestMessage)requestMessage.Properties[RequestMessageLookupKey];

            // Call the .NET callback.
            bool success = false;
            try
            {
                success = _serverCertificateCustomValidationCallback(request, serverCert, serverChain, sslPolicyErrors);
            }
            catch (Exception ex)
            {
                // Save the exception info. We will return it later via the SendAsync response processing.
                requestMessage.Properties.Add(
                    SavedExceptionDispatchInfoLookupKey,
                    ExceptionDispatchInfo.Capture(ex));
            }
            finally
            {
                serverChain.Dispose();
                serverCert.Dispose();
            }

            return success;
        }

        #endregion Helpers
    }
}
