// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public delegate void HttpContinueDelegate(int StatusCode, WebHeaderCollection httpHeaders);

    public class HttpWebRequest : WebRequest, ISerializable
    {
        private const int DefaultContinueTimeout = 350; // Current default value from .NET Desktop.
        private const int DefaultReadWriteTimeout = 5 * 60 * 1000; // 5 minutes

        private WebHeaderCollection _webHeaderCollection = new WebHeaderCollection();

        private readonly Uri _requestUri = null!;
        private string _originVerb = HttpMethod.Get.Method;

        private int _continueTimeout = DefaultContinueTimeout;

        private bool _allowReadStreamBuffering;
        private CookieContainer? _cookieContainer;
        private ICredentials? _credentials;
        private IWebProxy? _proxy = WebRequest.DefaultWebProxy;

        private Task<HttpResponseMessage>? _sendRequestTask;
        private HttpRequestMessage? _sendRequestMessage;

        private static int _defaultMaxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;

        private int _beginGetRequestStreamCalled;
        private int _beginGetResponseCalled;
        private int _endGetRequestStreamCalled;
        private int _endGetResponseCalled;

        private int _maximumAllowedRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private int _maximumResponseHeadersLen = _defaultMaxResponseHeadersLength;
        private ServicePoint? _servicePoint;
        private int _timeout = WebRequest.DefaultTimeoutMilliseconds;
        private int _readWriteTimeout = DefaultReadWriteTimeout;

        private HttpContinueDelegate? _continueDelegate;

        // stores the user provided Host header as Uri. If the user specified a default port explicitly we'll lose
        // that information when converting the host string to a Uri. _HostHasPort will store that information.
        private bool _hostHasPort;
        private Uri? _hostUri;

        private RequestStream? _requestStream;
        private TaskCompletionSource<Stream>? _requestStreamOperation;
        private TaskCompletionSource<WebResponse>? _responseOperation;
        private AsyncCallback? _requestStreamCallback;
        private AsyncCallback? _responseCallback;
        private int _abortCalled;
        private CancellationTokenSource? _sendRequestCts;
        private X509CertificateCollection? _clientCertificates;
        private Booleans _booleans = Booleans.Default;
        private bool _pipelined = true;
        private bool _preAuthenticate;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;

        private static readonly object s_syncRoot = new object();
        private static volatile HttpClient? s_cachedHttpClient;
        private static HttpClientParameters? s_cachedHttpClientParameters;
        private bool _disposeRequired;
        private HttpClient? _httpClient;

        //these should be safe.
        [Flags]
        private enum Booleans : uint
        {
            AllowAutoRedirect = 0x00000001,
            AllowWriteStreamBuffering = 0x00000002,
            ExpectContinue = 0x00000004,

            ProxySet = 0x00000010,

            UnsafeAuthenticatedConnectionSharing = 0x00000040,
            IsVersionHttp10 = 0x00000080,
            SendChunked = 0x00000100,
            EnableDecompression = 0x00000200,
            IsTunnelRequest = 0x00000400,
            IsWebSocketRequest = 0x00000800,
            Default = AllowAutoRedirect | AllowWriteStreamBuffering | ExpectContinue
        }

        private sealed class HttpClientParameters
        {
            public readonly bool Async;
            public readonly DecompressionMethods AutomaticDecompression;
            public readonly bool AllowAutoRedirect;
            public readonly int MaximumAutomaticRedirections;
            public readonly int MaximumResponseHeadersLength;
            public readonly bool PreAuthenticate;
            public readonly int ReadWriteTimeout;
            public readonly TimeSpan Timeout;
            public readonly SecurityProtocolType SslProtocols;
            public readonly bool CheckCertificateRevocationList;
            public readonly ICredentials? Credentials;
            public readonly IWebProxy? Proxy;
            public readonly RemoteCertificateValidationCallback? ServerCertificateValidationCallback;
            public readonly X509CertificateCollection? ClientCertificates;
            public readonly CookieContainer? CookieContainer;
            public readonly ServicePoint? ServicePoint;
            public readonly TimeSpan ContinueTimeout;

            public HttpClientParameters(HttpWebRequest webRequest, bool async)
            {
                Async = async;
                AutomaticDecompression = webRequest.AutomaticDecompression;
                AllowAutoRedirect = webRequest.AllowAutoRedirect;
                MaximumAutomaticRedirections = webRequest.MaximumAutomaticRedirections;
                MaximumResponseHeadersLength = webRequest.MaximumResponseHeadersLength;
                PreAuthenticate = webRequest.PreAuthenticate;
                ReadWriteTimeout = webRequest.ReadWriteTimeout;
                Timeout = webRequest.Timeout == Threading.Timeout.Infinite
                    ? Threading.Timeout.InfiniteTimeSpan
                    : TimeSpan.FromMilliseconds(webRequest.Timeout);
                SslProtocols = ServicePointManager.SecurityProtocol;
                CheckCertificateRevocationList = ServicePointManager.CheckCertificateRevocationList;
                Credentials = webRequest._credentials;
                Proxy = webRequest._proxy;
                ServerCertificateValidationCallback = webRequest.ServerCertificateValidationCallback ?? ServicePointManager.ServerCertificateValidationCallback;
                ClientCertificates = webRequest._clientCertificates;
                CookieContainer = webRequest._cookieContainer;
                ServicePoint = webRequest._servicePoint;
                ContinueTimeout = TimeSpan.FromMilliseconds(webRequest.ContinueTimeout);
            }

            public bool Matches(HttpClientParameters requestParameters)
            {
                return Async == requestParameters.Async
                    && AutomaticDecompression == requestParameters.AutomaticDecompression
                    && AllowAutoRedirect == requestParameters.AllowAutoRedirect
                    && MaximumAutomaticRedirections == requestParameters.MaximumAutomaticRedirections
                    && MaximumResponseHeadersLength == requestParameters.MaximumResponseHeadersLength
                    && PreAuthenticate == requestParameters.PreAuthenticate
                    && ReadWriteTimeout == requestParameters.ReadWriteTimeout
                    && Timeout == requestParameters.Timeout
                    && SslProtocols == requestParameters.SslProtocols
                    && CheckCertificateRevocationList == requestParameters.CheckCertificateRevocationList
                    && ContinueTimeout == requestParameters.ContinueTimeout
                    && ReferenceEquals(Credentials, requestParameters.Credentials)
                    && ReferenceEquals(Proxy, requestParameters.Proxy)
                    && ReferenceEquals(ServerCertificateValidationCallback, requestParameters.ServerCertificateValidationCallback)
                    && ReferenceEquals(ClientCertificates, requestParameters.ClientCertificates)
                    && ReferenceEquals(CookieContainer, requestParameters.CookieContainer)
                    && ReferenceEquals(ServicePoint, requestParameters.ServicePoint);
            }

            public bool AreParametersAcceptableForCaching()
            {
                return Credentials == null
                    && ReferenceEquals(Proxy, DefaultWebProxy)
                    && ServerCertificateValidationCallback == null
                    && ClientCertificates == null
                    && CookieContainer == null
                    && ServicePoint == null;
            }
        }

        private const string ContinueHeader = "100-continue";
        private const string ChunkedHeader = "chunked";

        [Obsolete(Obsoletions.WebRequestMessage, DiagnosticId = Obsoletions.WebRequestDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected HttpWebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for HttpWebRequest.")]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for HttpWebRequest.")]
        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal HttpWebRequest(Uri uri)
        {
            _requestUri = uri;
        }

        private void SetSpecialHeaders(string HeaderName, string? value)
        {
            _webHeaderCollection.Remove(HeaderName);
            if (!string.IsNullOrEmpty(value))
            {
                _webHeaderCollection[HeaderName] = value;
            }
        }

        public string? Accept
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Accept];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.Accept, value);
            }
        }

        public virtual bool AllowReadStreamBuffering
        {
            get
            {
                return _allowReadStreamBuffering;
            }
            set
            {
                _allowReadStreamBuffering = value;
            }
        }

        public int MaximumResponseHeadersLength
        {
            get => _maximumResponseHeadersLen;
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                ArgumentOutOfRangeException.ThrowIfLessThan(value, System.Threading.Timeout.Infinite);
                _maximumResponseHeadersLen = value;
            }
        }

        public int MaximumAutomaticRedirections
        {
            get
            {
                return _maximumAllowedRedirections;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                _maximumAllowedRedirections = value;
            }
        }

        public override string? ContentType
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.ContentType];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.ContentType, value);
            }
        }

        public int ContinueTimeout
        {
            get
            {
                return _continueTimeout;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                if ((value < 0) && (value != System.Threading.Timeout.Infinite))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_ge_zero);
                }
                _continueTimeout = value;
            }
        }

        public override int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_ge_zero);
                }

                _timeout = value;
            }
        }

        public override long ContentLength
        {
            get
            {
                long value;
                long.TryParse(_webHeaderCollection[HttpKnownHeaderNames.ContentLength], out value);
                return value;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                SetSpecialHeaders(HttpKnownHeaderNames.ContentLength, value.ToString());
            }
        }

        public Uri Address
        {
            get
            {
                return _requestUri;
            }
        }

        public string? UserAgent
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.UserAgent];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.UserAgent, value);
            }
        }

        public string Host
        {
            get
            {
                Uri hostUri = _hostUri ?? Address;
                return (_hostUri == null || !_hostHasPort) && Address.IsDefaultPort ?
                    hostUri.Host :
                    $"{hostUri.Host}:{hostUri.Port}";
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }
                ArgumentNullException.ThrowIfNull(value);

                Uri? hostUri;
                if ((value.Contains('/')) || (!TryGetHostUri(value, out hostUri)))
                {
                    throw new ArgumentException(SR.net_invalid_host, nameof(value));
                }

                _hostUri = hostUri;

                // Determine if the user provided string contains a port
                if (!_hostUri.IsDefaultPort)
                {
                    _hostHasPort = true;
                }
                else if (!value.Contains(':'))
                {
                    _hostHasPort = false;
                }
                else
                {
                    int endOfIPv6Address = value.IndexOf(']');
                    _hostHasPort = endOfIPv6Address == -1 || value.LastIndexOf(':') > endOfIPv6Address;
                }
            }
        }

        public bool Pipelined
        {
            get
            {
                return _pipelined;
            }
            set
            {
                _pipelined = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the value of the Referer header.
        ///    </para>
        /// </devdoc>
        public string? Referer
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Referer];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.Referer, value);
            }
        }

        /// <devdoc>
        ///    <para>Sets the media type header</para>
        /// </devdoc>
        public string? MediaType
        {
            get;
            set;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the value of the Transfer-Encoding header. Setting null clears it out.
        ///    </para>
        /// </devdoc>
        public string? TransferEncoding
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.TransferEncoding];
            }
            set
            {
                bool fChunked;
                //
                // on blank string, remove current header
                //
                if (string.IsNullOrWhiteSpace(value))
                {
                    //
                    // if the value is blank, then remove the header
                    //
                    _webHeaderCollection.Remove(HttpKnownHeaderNames.TransferEncoding);
                    return;
                }

                //
                // if not check if the user is trying to set chunked:
                //
                fChunked = (value.Contains(ChunkedHeader, StringComparison.OrdinalIgnoreCase));

                //
                // prevent them from adding chunked, or from adding an Encoding without
                // turning on chunked, the reason is due to the HTTP Spec which prevents
                // additional encoding types from being used without chunked
                //
                if (fChunked)
                {
                    throw new ArgumentException(SR.net_nochunked, nameof(value));
                }
                else if (!SendChunked)
                {
                    throw new InvalidOperationException(SR.net_needchunked);
                }
                else
                {
                    string checkedValue = HttpValidationHelpers.CheckBadHeaderValueChars(value);
                    _webHeaderCollection[HttpKnownHeaderNames.TransferEncoding] = checkedValue;
                }
            }
        }

        public bool KeepAlive { get; set; } = true;

        public bool UnsafeAuthenticatedConnectionSharing
        {
            get
            {
                return (_booleans & Booleans.UnsafeAuthenticatedConnectionSharing) != 0;
            }
            set
            {
                if (value)
                {
                    _booleans |= Booleans.UnsafeAuthenticatedConnectionSharing;
                }
                else
                {
                    _booleans &= ~Booleans.UnsafeAuthenticatedConnectionSharing;
                }
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
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }
                _automaticDecompression = value;
            }
        }

        public virtual bool AllowWriteStreamBuffering
        {
            get
            {
                return (_booleans & Booleans.AllowWriteStreamBuffering) != 0;
            }
            set
            {
                if (value)
                {
                    _booleans |= Booleans.AllowWriteStreamBuffering;
                }
                else
                {
                    _booleans &= ~Booleans.AllowWriteStreamBuffering;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Enables or disables automatically following redirection responses.
        ///    </para>
        /// </devdoc>
        public virtual bool AllowAutoRedirect
        {
            get
            {
                return (_booleans & Booleans.AllowAutoRedirect) != 0;
            }
            set
            {
                if (value)
                {
                    _booleans |= Booleans.AllowAutoRedirect;
                }
                else
                {
                    _booleans &= ~Booleans.AllowAutoRedirect;
                }
            }
        }

        public override string? ConnectionGroupName { get; set; }

        public override bool PreAuthenticate
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

        public string? Connection
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Connection];
            }
            set
            {
                bool fKeepAlive;
                bool fClose;

                //
                // on blank string, remove current header
                //
                if (string.IsNullOrWhiteSpace(value))
                {
                    _webHeaderCollection.Remove(HttpKnownHeaderNames.Connection);
                    return;
                }

                fKeepAlive = (value.Contains("keep-alive", StringComparison.OrdinalIgnoreCase));
                fClose = (value.Contains("close", StringComparison.OrdinalIgnoreCase));

                //
                // Prevent keep-alive and close from being added
                //

                if (fKeepAlive ||
                    fClose)
                {
                    throw new ArgumentException(SR.net_connarg, nameof(value));
                }
                else
                {
                    string checkedValue = HttpValidationHelpers.CheckBadHeaderValueChars(value);
                    _webHeaderCollection[HttpKnownHeaderNames.Connection] = checkedValue;
                }
            }
        }

        /*
            Accessor:   Expect

            The property that controls the Expect header

            Input:
                string Expect, null clears the Expect except for 100-continue value
            Returns: The value of the Expect on get.
        */

        public string? Expect
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Expect];
            }
            set
            {
                // only remove everything other than 100-cont
                bool fContinue100;

                //
                // on blank string, remove current header
                //

                if (string.IsNullOrWhiteSpace(value))
                {
                    _webHeaderCollection.Remove(HttpKnownHeaderNames.Expect);
                    return;
                }

                //
                // Prevent 100-continues from being added
                //

                fContinue100 = (value.Contains(ContinueHeader, StringComparison.OrdinalIgnoreCase));

                if (fContinue100)
                {
                    throw new ArgumentException(SR.net_no100, nameof(value));
                }
                else
                {
                    string checkedValue = HttpValidationHelpers.CheckBadHeaderValueChars(value);
                    _webHeaderCollection[HttpKnownHeaderNames.Expect] = checkedValue;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the default for the MaximumResponseHeadersLength property.
        ///    </para>
        ///    <remarks>
        ///       This value can be set in the config file, the default can be overridden using the MaximumResponseHeadersLength property.
        ///    </remarks>
        /// </devdoc>
        public static int DefaultMaximumResponseHeadersLength
        {
            get
            {
                return _defaultMaxResponseHeadersLength;
            }
            set
            {
                _defaultMaxResponseHeadersLength = value;
            }
        }

        // NOP
        public static int DefaultMaximumErrorResponseLength
        {
            get; set;
        }

        private static RequestCachePolicy? _defaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
        private static bool _isDefaultCachePolicySet;

        public static new RequestCachePolicy? DefaultCachePolicy
        {
            get
            {
                return _defaultCachePolicy;
            }
            set
            {
                _isDefaultCachePolicySet = true;
                _defaultCachePolicy = value;
            }
        }

        public DateTime IfModifiedSince
        {
            get
            {
                return GetDateHeaderHelper(HttpKnownHeaderNames.IfModifiedSince);
            }
            set
            {
                SetDateHeaderHelper(HttpKnownHeaderNames.IfModifiedSince, value);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the value of the Date header.
        ///    </para>
        /// </devdoc>
        public DateTime Date
        {
            get
            {
                return GetDateHeaderHelper(HttpKnownHeaderNames.Date);
            }
            set
            {
                SetDateHeaderHelper(HttpKnownHeaderNames.Date, value);
            }
        }

        public bool SendChunked
        {
            get
            {
                return (_booleans & Booleans.SendChunked) != 0;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }
                if (value)
                {
                    _booleans |= Booleans.SendChunked;
                }
                else
                {
                    _booleans &= ~Booleans.SendChunked;
                }
            }
        }

        public HttpContinueDelegate? ContinueDelegate
        {
            // Nop since the underlying API do not expose 100 continue.
            get
            {
                return _continueDelegate;
            }
            set
            {
                _continueDelegate = value;
            }
        }

        public ServicePoint ServicePoint => _servicePoint ??= ServicePointManager.FindServicePoint(Address, Proxy);

        public RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get; set; }

        //
        // ClientCertificates - sets our certs for our reqest,
        //  uses a hash of the collection to create a private connection
        //  group, to prevent us from using the same Connection as
        //  non-Client Authenticated requests.
        //
        public X509CertificateCollection ClientCertificates
        {
            get => _clientCertificates ??= new X509CertificateCollection();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _clientCertificates = value;
            }
        }

        // HTTP Version
        /// <devdoc>
        ///    <para>
        ///       Gets and sets
        ///       the HTTP protocol version used in this request.
        ///    </para>
        /// </devdoc>
        public Version ProtocolVersion
        {
            get
            {
                return IsVersionHttp10 ? HttpVersion.Version10 : HttpVersion.Version11;
            }
            set
            {
                if (value.Equals(HttpVersion.Version11))
                {
                    IsVersionHttp10 = false;
                }
                else if (value.Equals(HttpVersion.Version10))
                {
                    IsVersionHttp10 = true;
                }
                else
                {
                    throw new ArgumentException(SR.net_wrongversion, nameof(value));
                }
            }
        }

        public int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                }

                _readWriteTimeout = value;
            }
        }

        public virtual CookieContainer? CookieContainer
        {
            get
            {
                return _cookieContainer;
            }
            set
            {
                _cookieContainer = value;
            }
        }

        public override ICredentials? Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        public virtual bool HaveResponse
        {
            get
            {
                return (_sendRequestTask != null) && (_sendRequestTask.IsCompletedSuccessfully);
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return _webHeaderCollection;
            }
            set
            {
                // We can't change headers after they've already been sent.
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                WebHeaderCollection webHeaders = value;
                WebHeaderCollection newWebHeaders = new WebHeaderCollection();

                // Copy And Validate -
                // Handle the case where their object tries to change
                //  name, value pairs after they call set, so therefore,
                //  we need to clone their headers.
                foreach (string headerName in webHeaders.AllKeys)
                {
                    newWebHeaders[headerName] = webHeaders[headerName];
                }

                _webHeaderCollection = newWebHeaders;
            }
        }

        public override string Method
        {
            get
            {
                return _originVerb;
            }
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);

                if (HttpValidationHelpers.IsInvalidMethodOrHeaderString(value))
                {
                    throw new ArgumentException(SR.net_badmethod, nameof(value));
                }
                _originVerb = value;
            }
        }

        public override Uri RequestUri
        {
            get
            {
                return _requestUri;
            }
        }

        public virtual bool SupportsCookieContainer
        {
            get
            {
                return true;
            }
        }

        public override bool UseDefaultCredentials
        {
            get
            {
                return (_credentials == CredentialCache.DefaultCredentials);
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }

                // Match Desktop behavior.  Changing this property will also
                // change the .Credentials property as well.
                _credentials = value ? CredentialCache.DefaultCredentials : null;
            }
        }

        public override IWebProxy? Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                // We can't change the proxy while the request is already fired.
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                _proxy = value;
            }
        }

        public override void Abort()
        {
            if (Interlocked.Exchange(ref _abortCalled, 1) != 0)
            {
                return;
            }

            // .NET Desktop behavior requires us to invoke outstanding callbacks
            // before returning if used in either the BeginGetRequestStream or
            // BeginGetResponse methods.
            //
            // If we can transition the task to the canceled state, then we invoke
            // the callback. If we can't transition the task, it is because it is
            // already in the terminal state and the callback has already been invoked
            // via the async task continuation.

            if (_responseOperation != null)
            {
                if (_responseOperation.TrySetCanceled() && _responseCallback != null)
                {
                    _responseCallback(_responseOperation.Task);
                }
            }
            if (_requestStreamOperation != null)
            {
                if (_requestStreamOperation.TrySetCanceled() && _requestStreamCallback != null)
                {
                    _requestStreamCallback(_requestStreamOperation.Task);
                }

                // Cancel the underlying send operation.
                Debug.Assert(_sendRequestCts != null);
                _sendRequestCts.Cancel();
            }
        }

        // HTTP version of the request
        private bool IsVersionHttp10
        {
            get
            {
                return (_booleans & Booleans.IsVersionHttp10) != 0;
            }
            set
            {
                if (value)
                {
                    _booleans |= Booleans.IsVersionHttp10;
                }
                else
                {
                    _booleans &= ~Booleans.IsVersionHttp10;
                }
            }
        }

        public override WebResponse GetResponse()
        {
            try
            {
                return HandleResponse(async: false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw WebException.CreateCompatibleException(ex);
            }
        }

        public override Stream GetRequestStream()
        {
            return InternalGetRequestStream().Result;
        }

        private Task<Stream> InternalGetRequestStream()
        {
            CheckAbort();

            // Match Desktop behavior: prevent someone from getting a request stream
            // if the protocol verb/method doesn't support it. Note that this is not
            // entirely compliant RFC2616 for the aforementioned compatibility reasons.
            if (string.Equals(HttpMethod.Get.Method, _originVerb, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(HttpMethod.Head.Method, _originVerb, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("CONNECT", _originVerb, StringComparison.OrdinalIgnoreCase))
            {
                throw new ProtocolViolationException(SR.net_nouploadonget);
            }

            if (RequestSubmitted)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            // Create stream buffer for transferring data from RequestStream to the StreamContent.
            StreamBuffer streamBuffer = new StreamBuffer(maxBufferSize: AllowWriteStreamBuffering ? int.MaxValue : StreamBuffer.DefaultMaxBufferSize);

            // If we aren't buffering we need to open the connection right away.
            // Because we need to send the data as soon as possible when it's available from the RequestStream.
            // Making this allows us to keep the sync send request path for buffering cases.
            if (AllowWriteStreamBuffering is false)
            {
                // We're calling SendRequest with async, because we need to open the connection and send the request
                // Otherwise, sync path will block the current thread until the request is sent.
                _sendRequestTask = SendRequest(true, new RequestStreamContent(new HttpClientContentStream(streamBuffer)));
            }

            _requestStream = new RequestStream(streamBuffer);

            return Task.FromResult((Stream)_requestStream);
        }

        public Stream EndGetRequestStream(IAsyncResult asyncResult, out TransportContext? context)
        {
            context = null;
            return EndGetRequestStream(asyncResult);
        }

        public Stream GetRequestStream(out TransportContext? context)
        {
            context = null;
            return GetRequestStream();
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
        {
            CheckAbort();

            if (Interlocked.Exchange(ref _beginGetRequestStreamCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.net_repcall);
            }

            _requestStreamCallback = callback;
            _requestStreamOperation = InternalGetRequestStream().ToApm(callback, state);

            return _requestStreamOperation.Task;
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            CheckAbort();

            if (asyncResult == null || !(asyncResult is Task<Stream>))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _endGetRequestStreamCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetRequestStream"));
            }

            Stream stream;
            try
            {
                stream = ((Task<Stream>)asyncResult).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw WebException.CreateCompatibleException(ex);
            }

            return stream;
        }

        private Task<HttpResponseMessage> SendRequest(bool async, HttpContent? content = null)
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            _sendRequestMessage = new HttpRequestMessage(HttpMethod.Parse(_originVerb), _requestUri);
            _sendRequestCts = new CancellationTokenSource();
            _httpClient = GetCachedOrCreateHttpClient(async, out _disposeRequired);

            if (content is not null)
            {
                // Calculate Content-Length if we're buffering.
                if (AllowWriteStreamBuffering && _requestStream is not null)
                {
                    content.Headers.ContentLength = _requestStream.GetBuffer().ReadBytesAvailable;
                }

                _sendRequestMessage.Content = content;
            }

            if (_hostUri is not null)
            {
                _sendRequestMessage.Headers.Host = Host;
            }

            AddCacheControlHeaders(_sendRequestMessage);

            // Copy the HttpWebRequest request headers from the WebHeaderCollection into HttpRequestMessage.Headers and
            // HttpRequestMessage.Content.Headers.
            foreach (string headerName in _webHeaderCollection)
            {
                // The System.Net.Http APIs require HttpRequestMessage headers to be properly divided between the request headers
                // collection and the request content headers collection for all well-known header names.  And custom headers
                // are only allowed in the request headers collection and not in the request content headers collection.
                if (IsWellKnownContentHeader(headerName))
                {
                    _sendRequestMessage.Content ??= new ByteArrayContent(Array.Empty<byte>());
                    _sendRequestMessage.Content.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName!]);
                }
                else
                {
                    _sendRequestMessage.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName!]);
                }
            }

            if (_servicePoint?.Expect100Continue == true)
            {
                _sendRequestMessage.Headers.ExpectContinue = true;
            }

            _sendRequestMessage.Headers.TransferEncodingChunked = SendChunked;

            if (KeepAlive)
            {
                _sendRequestMessage.Headers.Connection.Add(HttpKnownHeaderNames.KeepAlive);
            }
            else
            {
                _sendRequestMessage.Headers.ConnectionClose = true;
            }

            _sendRequestMessage.Version = ProtocolVersion;
            HttpCompletionOption completionOption = _allowReadStreamBuffering ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
            // If we're not buffering, there is no way to open the connection and not send the request without async.
            // So we should use Async, if we're not buffering.
            _sendRequestTask = async || !AllowWriteStreamBuffering ?
                _httpClient.SendAsync(_sendRequestMessage, completionOption, _sendRequestCts.Token) :
                Task.FromResult(_httpClient.Send(_sendRequestMessage, completionOption, _sendRequestCts.Token));

            return _sendRequestTask!;
        }

        private async Task<WebResponse> HandleResponse(bool async)
        {
            // If user code used requestStream and didn't dispose it (end write on StreamBuffer)
            // We're ending it here.
            _requestStream?.EndWriteOnStreamBuffer();

            _sendRequestTask ??= _requestStream is not null ?
                SendRequest(async, new StreamContent(new HttpClientContentStream(_requestStream.GetBuffer()))) :
                SendRequest(async);

            try
            {
                HttpResponseMessage responseMessage = await _sendRequestTask.ConfigureAwait(false);
                HttpWebResponse response = new(responseMessage, _requestUri, _cookieContainer);

                int maxSuccessStatusCode = AllowAutoRedirect ? 299 : 399;
                if ((int)response.StatusCode > maxSuccessStatusCode || (int)response.StatusCode < 200)
                {
                    throw new WebException(
                        SR.Format(SR.net_servererror, (int)response.StatusCode, response.StatusDescription),
                        null,
                        WebExceptionStatus.ProtocolError,
                        response);
                }

                return response;
            }
            finally
            {
                _sendRequestMessage?.Dispose();

                if (_disposeRequired)
                {
                    _httpClient?.Dispose();
                }
            }
        }

        private void AddCacheControlHeaders(HttpRequestMessage request)
        {
            RequestCachePolicy? policy = GetApplicableCachePolicy();

            if (policy != null && policy.Level != RequestCacheLevel.BypassCache)
            {
                CacheControlHeaderValue? cacheControl = null;
                HttpHeaderValueCollection<NameValueHeaderValue> pragmaHeaders = request.Headers.Pragma;

                if (policy is HttpRequestCachePolicy httpRequestCachePolicy)
                {
                    switch (httpRequestCachePolicy.Level)
                    {
                        case HttpRequestCacheLevel.NoCacheNoStore:
                            cacheControl = new CacheControlHeaderValue
                            {
                                NoCache = true,
                                NoStore = true
                            };
                            pragmaHeaders.Add(new NameValueHeaderValue("no-cache"));
                            break;
                        case HttpRequestCacheLevel.Reload:
                            cacheControl = new CacheControlHeaderValue
                            {
                                NoCache = true
                            };
                            pragmaHeaders.Add(new NameValueHeaderValue("no-cache"));
                            break;
                        case HttpRequestCacheLevel.CacheOnly:
                            throw new WebException(SR.CacheEntryNotFound, WebExceptionStatus.CacheEntryNotFound);
                        case HttpRequestCacheLevel.CacheOrNextCacheOnly:
                            cacheControl = new CacheControlHeaderValue
                            {
                                OnlyIfCached = true
                            };
                            break;
                        case HttpRequestCacheLevel.Default:
                            cacheControl = new CacheControlHeaderValue();

                            if (httpRequestCachePolicy.MinFresh > TimeSpan.Zero)
                            {
                                cacheControl.MinFresh = httpRequestCachePolicy.MinFresh;
                            }

                            if (httpRequestCachePolicy.MaxAge != TimeSpan.MaxValue)
                            {
                                cacheControl.MaxAge = httpRequestCachePolicy.MaxAge;
                            }

                            if (httpRequestCachePolicy.MaxStale > TimeSpan.Zero)
                            {
                                cacheControl.MaxStale = true;
                                cacheControl.MaxStaleLimit = httpRequestCachePolicy.MaxStale;
                            }

                            break;
                        case HttpRequestCacheLevel.Refresh:
                            cacheControl = new CacheControlHeaderValue
                            {
                                MaxAge = TimeSpan.Zero
                            };
                            pragmaHeaders.Add(new NameValueHeaderValue("no-cache"));
                            break;
                    }
                }
                else
                {
                    switch (policy.Level)
                    {
                        case RequestCacheLevel.NoCacheNoStore:
                            cacheControl = new CacheControlHeaderValue
                            {
                                NoCache = true,
                                NoStore = true
                            };
                            pragmaHeaders.Add(new NameValueHeaderValue("no-cache"));
                            break;
                        case RequestCacheLevel.Reload:
                            cacheControl = new CacheControlHeaderValue
                            {
                                NoCache = true
                            };
                            pragmaHeaders.Add(new NameValueHeaderValue("no-cache"));
                            break;
                        case RequestCacheLevel.CacheOnly:
                            throw new WebException(SR.CacheEntryNotFound, WebExceptionStatus.CacheEntryNotFound);
                    }
                }

                if (cacheControl != null)
                {
                    request.Headers.CacheControl = cacheControl;
                }
            }
        }

        private RequestCachePolicy? GetApplicableCachePolicy()
        {
            if (CachePolicy != null)
            {
                return CachePolicy;
            }
            else if (_isDefaultCachePolicySet && DefaultCachePolicy != null)
            {
                return DefaultCachePolicy;
            }
            else
            {
                return WebRequest.DefaultCachePolicy;
            }
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
        {
            CheckAbort();

            if (Interlocked.Exchange(ref _beginGetResponseCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.net_repcall);
            }

            _responseCallback = callback;
            _responseOperation = HandleResponse(async: true).ToApm(callback, state);

            return _responseOperation.Task;
        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            CheckAbort();

            if (asyncResult == null || !(asyncResult is Task<WebResponse>))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _endGetResponseCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetResponse"));
            }

            WebResponse response;
            try
            {
                response = ((Task<WebResponse>)asyncResult).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw WebException.CreateCompatibleException(ex);
            }

            return response;
        }

        /// <devdoc>
        ///    <para>
        ///       Adds a range header to the request for a specified range.
        ///    </para>
        /// </devdoc>
        public void AddRange(int from, int to)
        {
            AddRange("bytes", (long)from, (long)to);
        }

        /// <devdoc>
        ///    <para>
        ///       Adds a range header to the request for a specified range.
        ///    </para>
        /// </devdoc>
        public void AddRange(long from, long to)
        {
            AddRange("bytes", from, to);
        }

        /// <devdoc>
        ///    <para>
        ///       Adds a range header to a request for a specific
        ///       range from the beginning or end
        ///       of the requested data.
        ///       To add the range from the end pass negative value
        ///       To add the range from the some offset to the end pass positive value
        ///    </para>
        /// </devdoc>
        public void AddRange(int range)
        {
            AddRange("bytes", (long)range);
        }

        /// <devdoc>
        ///    <para>
        ///       Adds a range header to a request for a specific
        ///       range from the beginning or end
        ///       of the requested data.
        ///       To add the range from the end pass negative value
        ///       To add the range from the some offset to the end pass positive value
        ///    </para>
        /// </devdoc>
        public void AddRange(long range)
        {
            AddRange("bytes", range);
        }

        public void AddRange(string rangeSpecifier, int from, int to)
        {
            AddRange(rangeSpecifier, (long)from, (long)to);
        }

        public void AddRange(string rangeSpecifier, long from, long to)
        {
            ArgumentNullException.ThrowIfNull(rangeSpecifier);

            if ((from < 0) || (to < 0))
            {
                throw new ArgumentOutOfRangeException(from < 0 ? nameof(from) : nameof(to), SR.net_rangetoosmall);
            }
            if (from > to)
            {
                throw new ArgumentOutOfRangeException(nameof(from), SR.net_fromto);
            }
            if (!HttpValidationHelpers.IsValidToken(rangeSpecifier))
            {
                throw new ArgumentException(SR.net_nottoken, nameof(rangeSpecifier));
            }
            if (!AddRange(rangeSpecifier, from.ToString(NumberFormatInfo.InvariantInfo), to.ToString(NumberFormatInfo.InvariantInfo)))
            {
                throw new InvalidOperationException(SR.net_rangetype);
            }
        }

        public void AddRange(string rangeSpecifier, int range)
        {
            AddRange(rangeSpecifier, (long)range);
        }

        public void AddRange(string rangeSpecifier, long range)
        {
            ArgumentNullException.ThrowIfNull(rangeSpecifier);

            if (!HttpValidationHelpers.IsValidToken(rangeSpecifier))
            {
                throw new ArgumentException(SR.net_nottoken, nameof(rangeSpecifier));
            }
            if (!AddRange(rangeSpecifier, range.ToString(NumberFormatInfo.InvariantInfo), (range >= 0) ? "" : null))
            {
                throw new InvalidOperationException(SR.net_rangetype);
            }
        }

        private bool AddRange(string rangeSpecifier, string from, string? to)
        {
            string? curRange = _webHeaderCollection[HttpKnownHeaderNames.Range];

            if ((curRange == null) || (curRange.Length == 0))
            {
                curRange = rangeSpecifier + "=";
            }
            else
            {
                if (!string.Equals(curRange.Substring(0, curRange.IndexOf('=')), rangeSpecifier, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                curRange = string.Empty;
            }
            curRange += from.ToString();
            if (to != null)
            {
                curRange += "-" + to;
            }
            _webHeaderCollection[HttpKnownHeaderNames.Range] = curRange;
            return true;
        }

        private bool RequestSubmitted
        {
            get
            {
                return _sendRequestTask != null;
            }
        }

        private void CheckAbort()
        {
            if (Volatile.Read(ref _abortCalled) == 1)
            {
                throw new WebException(SR.net_reqaborted, WebExceptionStatus.RequestCanceled);
            }
        }

        private static readonly string[] s_wellKnownContentHeaders = {
            HttpKnownHeaderNames.ContentDisposition,
            HttpKnownHeaderNames.ContentEncoding,
            HttpKnownHeaderNames.ContentLanguage,
            HttpKnownHeaderNames.ContentLength,
            HttpKnownHeaderNames.ContentLocation,
            HttpKnownHeaderNames.ContentMD5,
            HttpKnownHeaderNames.ContentRange,
            HttpKnownHeaderNames.ContentType,
            HttpKnownHeaderNames.Expires,
            HttpKnownHeaderNames.LastModified
        };

        private static bool IsWellKnownContentHeader(string header)
        {
            foreach (string contentHeaderName in s_wellKnownContentHeaders)
            {
                if (string.Equals(header, contentHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private DateTime GetDateHeaderHelper(string headerName)
        {
            string? headerValue = _webHeaderCollection[headerName];

            if (headerValue == null)
            {
                return DateTime.MinValue; // MinValue means header is not present
            }
            if (HttpDateParser.TryParse(headerValue, out DateTimeOffset dateTimeOffset))
            {
                return dateTimeOffset.LocalDateTime;
            }
            else
            {
                throw new ProtocolViolationException(SR.net_baddate);
            }
        }

        private void SetDateHeaderHelper(string headerName, DateTime dateTime)
        {
            SetSpecialHeaders(headerName, dateTime == DateTime.MinValue ?
                null : // remove header
                dateTime.ToUniversalTime().ToString("r"));
        }

        private bool TryGetHostUri(string hostName, [NotNullWhen(true)] out Uri? hostUri)
        {
            string s = Address.Scheme + "://" + hostName + Address.PathAndQuery;
            return Uri.TryCreate(s, UriKind.Absolute, out hostUri);
        }

        private HttpClient GetCachedOrCreateHttpClient(bool async, out bool disposeRequired)
        {
            var parameters = new HttpClientParameters(this, async);
            if (parameters.AreParametersAcceptableForCaching())
            {
                disposeRequired = false;
                if (s_cachedHttpClient == null)
                {
                    lock (s_syncRoot)
                    {
                        if (s_cachedHttpClient == null)
                        {
                            s_cachedHttpClientParameters = parameters;
                            s_cachedHttpClient = CreateHttpClient(parameters, null);
                            return s_cachedHttpClient;
                        }
                    }
                }

                if (s_cachedHttpClientParameters!.Matches(parameters))
                {
                    return s_cachedHttpClient;
                }
            }

            disposeRequired = true;
            return CreateHttpClient(parameters, this);
        }

        private static HttpClient CreateHttpClient(HttpClientParameters parameters, HttpWebRequest? request)
        {
            HttpClient? client = null;
            try
            {
                var handler = new SocketsHttpHandler();
                client = new HttpClient(handler);
                handler.AutomaticDecompression = parameters.AutomaticDecompression;
                handler.Credentials = parameters.Credentials;
                handler.AllowAutoRedirect = parameters.AllowAutoRedirect;
                handler.MaxAutomaticRedirections = parameters.MaximumAutomaticRedirections;
                handler.MaxResponseHeadersLength = parameters.MaximumResponseHeadersLength;
                handler.PreAuthenticate = parameters.PreAuthenticate;
                handler.Expect100ContinueTimeout = parameters.ContinueTimeout;
                client.Timeout = parameters.Timeout;

                if (parameters.CookieContainer != null)
                {
                    handler.CookieContainer = parameters.CookieContainer;
                    Debug.Assert(handler.UseCookies); // Default of handler.UseCookies is true.
                }
                else
                {
                    handler.UseCookies = false;
                }

                Debug.Assert(handler.UseProxy); // Default of handler.UseProxy is true.
                Debug.Assert(handler.Proxy == null); // Default of handler.Proxy is null.

                // HttpClientHandler default is to use a proxy which is the system proxy.
                // This is indicated by the properties 'UseProxy == true' and 'Proxy == null'.
                //
                // However, HttpWebRequest doesn't have a separate 'UseProxy' property. Instead,
                // the default of the 'Proxy' property is a non-null IWebProxy object which is the
                // system default proxy object. If the 'Proxy' property were actually null, then
                // that means don't use any proxy.
                //
                // So, we need to map the desired HttpWebRequest proxy settings to equivalent
                // HttpClientHandler settings.
                if (parameters.Proxy == null)
                {
                    handler.UseProxy = false;
                }
                else if (!object.ReferenceEquals(parameters.Proxy, WebRequest.GetSystemWebProxy()))
                {
                    handler.Proxy = parameters.Proxy;
                }
                else
                {
                    // Since this HttpWebRequest is using the default system proxy, we need to
                    // pass any proxy credentials that the developer might have set via the
                    // WebRequest.DefaultWebProxy.Credentials property.
                    handler.DefaultProxyCredentials = parameters.Proxy.Credentials;
                }

                if (parameters.ClientCertificates != null)
                {
                    handler.SslOptions.ClientCertificates = new X509CertificateCollection(parameters.ClientCertificates);
                }

                // Set relevant properties from ServicePointManager
                handler.SslOptions.EnabledSslProtocols = (SslProtocols)parameters.SslProtocols;
                handler.SslOptions.CertificateRevocationCheckMode = parameters.CheckCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                RemoteCertificateValidationCallback? rcvc = parameters.ServerCertificateValidationCallback;
                if (rcvc != null)
                {
                    handler.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, errors) => rcvc(request!, cert, chain, errors);
                }

                // Set up a ConnectCallback so that we can control Socket-specific settings, like ReadWriteTimeout => socket.Send/ReceiveTimeout.
                handler.ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        if (parameters.ServicePoint is { } servicePoint)
                        {
                            if (servicePoint.ReceiveBufferSize != -1)
                            {
                                socket.ReceiveBufferSize = servicePoint.ReceiveBufferSize;
                            }

                            if (servicePoint.KeepAlive is { } keepAlive)
                            {
                                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, keepAlive.Time);
                                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, keepAlive.Interval);
                            }
                        }

                        socket.NoDelay = true;

                        if (parameters.Async)
                        {
                            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            using (cancellationToken.UnsafeRegister(s => ((Socket)s!).Dispose(), socket))
                            {
                                socket.Connect(context.DnsEndPoint);
                            }

                            // Throw in case cancellation caused the socket to be disposed after the Connect completed
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        if (parameters.ReadWriteTimeout > 0) // default is 5 minutes, so this is generally going to be true
                        {
                            socket.SendTimeout = socket.ReceiveTimeout = parameters.ReadWriteTimeout;
                        }
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }

                    return new NetworkStream(socket, ownsSocket: true);
                };

                return client;
            }
            catch
            {
                client?.Dispose();
                throw;
            }
        }
    }
}
