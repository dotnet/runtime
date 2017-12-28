// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

        private Uri _requestUri;
        private string _originVerb = HttpMethod.Get.Method;

        // We allow getting and setting this (to preserve app-compat). But we don't do anything with it 
        // as the underlying System.Net.Http API doesn't support it.
        private int _continueTimeout = DefaultContinueTimeout;

        private bool _allowReadStreamBuffering = false;
        private CookieContainer _cookieContainer = null;
        private ICredentials _credentials = null;
        private IWebProxy _proxy = WebRequest.DefaultWebProxy;

        private Task<HttpResponseMessage> _sendRequestTask;

        private static int _defaultMaxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;

        private int _beginGetRequestStreamCalled = 0;
        private int _beginGetResponseCalled = 0;
        private int _endGetRequestStreamCalled = 0;
        private int _endGetResponseCalled = 0;

        private int _maximumAllowedRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private int _maximumResponseHeadersLen = _defaultMaxResponseHeadersLength;
        private ServicePoint _servicePoint;
        private int _timeout = WebRequest.DefaultTimeoutMilliseconds;
        private int _readWriteTimeout = DefaultReadWriteTimeout;

        private HttpContinueDelegate _continueDelegate;

        // stores the user provided Host header as Uri. If the user specified a default port explicitly we'll lose
        // that information when converting the host string to a Uri. _HostHasPort will store that information.
        private bool _hostHasPort;
        private Uri _hostUri;

        private RequestStream _requestStream;
        private TaskCompletionSource<Stream> _requestStreamOperation = null;
        private TaskCompletionSource<WebResponse> _responseOperation = null;
        private AsyncCallback _requestStreamCallback = null;
        private AsyncCallback _responseCallback = null;
        private int _abortCalled = 0;
        private CancellationTokenSource _sendRequestCts;
        private X509CertificateCollection _clientCertificates;
        private Booleans _booleans = Booleans.Default;
        private bool _pipelined = true;
        private bool _preAuthenticate;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;

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
        private const string ContinueHeader = "100-continue";
        private const string ChunkedHeader = "chunked";
        private const string GZipHeader = "gzip";
        private const string DeflateHeader = "deflate";

        public HttpWebRequest()
        {
        }

        [Obsolete("Serialization is obsoleted for this type.  http://go.microsoft.com/fwlink/?linkid=14202")]
        protected HttpWebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal HttpWebRequest(Uri uri)
        {
            _requestUri = uri;
        }

        private void SetSpecialHeaders(string HeaderName, string value)
        {
            _webHeaderCollection.Remove(HeaderName);
            if (!string.IsNullOrEmpty(value))
            {
                _webHeaderCollection[HeaderName] = value;
            }
        }

        public string Accept
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
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_toosmall);
                }
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
                if (value <= 0)
                {
                    throw new ArgumentException(SR.net_toosmall, nameof(value));
                }
                _maximumAllowedRedirections = value;
            }
        }

        public override String ContentType
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
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_clsmall);
                }
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

        public string UserAgent
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
                    hostUri.Host + ":" + hostUri.Port;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                Uri hostUri;
                if ((value.IndexOf('/') != -1) || (!TryGetHostUri(value, out hostUri)))
                {
                    throw new ArgumentException(SR.net_invalid_host, nameof(value));
                }

                _hostUri = hostUri;

                // Determine if the user provided string contains a port
                if (!_hostUri.IsDefaultPort)
                {
                    _hostHasPort = true;
                }
                else if (value.IndexOf(':') == -1)
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
        public string Referer
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
        public string MediaType
        {
            get;
            set;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the value of the Transfer-Encoding header. Setting null clears it out.
        ///    </para>
        /// </devdoc>
        public string TransferEncoding
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.TransferEncoding];
            }
            set
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
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
                    string newValue = value.ToLower();
                    fChunked = (newValue.IndexOf(ChunkedHeader) != -1);

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
#if DEBUG
                }
#endif
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

        public override string ConnectionGroupName { get; set; }

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

        public string Connection
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Connection];
            }
            set
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
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

                    string newValue = value.ToLower();

                    fKeepAlive = (newValue.IndexOf("keep-alive") != -1);
                    fClose = (newValue.IndexOf("close") != -1);

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
#if DEBUG
                }
#endif
            }
        }


        /*
            Accessor:   Expect

            The property that controls the Expect header

            Input:
                string Expect, null clears the Expect except for 100-continue value
            Returns: The value of the Expect on get.
        */

        public string Expect
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Expect];
            }
            set
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
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

                    string newValue = value.ToLower();

                    fContinue100 = (newValue.IndexOf(ContinueHeader) != -1);

                    if (fContinue100)
                    {
                        throw new ArgumentException(SR.net_no100, nameof(value));
                    }
                    else
                    {
                        string checkedValue = HttpValidationHelpers.CheckBadHeaderValueChars(value);
                        _webHeaderCollection[HttpKnownHeaderNames.Expect] = checkedValue;
                    }
#if DEBUG
                }
#endif
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

        public static new RequestCachePolicy DefaultCachePolicy { get; set; } = new RequestCachePolicy(RequestCacheLevel.BypassCache);


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

        public HttpContinueDelegate ContinueDelegate
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

        public ServicePoint ServicePoint
        {
            get
            {
                if (_servicePoint == null)
                {
                    _servicePoint = ServicePointManager.FindServicePoint(Address, Proxy);
                }
                return _servicePoint;
            }
        }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        //
        // ClientCertificates - sets our certs for our reqest,
        //  uses a hash of the collection to create a private connection
        //  group, to prevent us from using the same Connection as
        //  non-Client Authenticated requests.
        //
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificates == null)
                    _clientCertificates = new X509CertificateCollection();
                return _clientCertificates;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
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

        public virtual CookieContainer CookieContainer
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

        public override ICredentials Credentials
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
                foreach (String headerName in webHeaders.AllKeys)
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
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SR.net_badmethod, nameof(value));
                }

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

        public override IWebProxy Proxy
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

                // Cancel the underlying send operation.
                Debug.Assert(_sendRequestCts != null);
                _sendRequestCts.Cancel();
            }
            else if (_requestStreamOperation != null)
            {
                if (_requestStreamOperation.TrySetCanceled() && _requestStreamCallback != null)
                {
                    _requestStreamCallback(_requestStreamOperation.Task);
                }
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
                _sendRequestCts = new CancellationTokenSource();
                return SendRequest().GetAwaiter().GetResult();
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

            _requestStream = new RequestStream();

            return Task.FromResult((Stream)_requestStream);
        }

        public Stream EndGetRequestStream(IAsyncResult asyncResult, out TransportContext context)
        {
            context = null;
            return EndGetRequestStream(asyncResult);
        }

        public Stream GetRequestStream(out TransportContext context)
        {
            context = null;
            return GetRequestStream();
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, Object state)
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

        private async Task<WebResponse> SendRequest()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            var handler = new HttpClientHandler();
            var request = new HttpRequestMessage(new HttpMethod(_originVerb), _requestUri);

            using (var client = new HttpClient(handler))
            {
                if (_requestStream != null)
                {
                    ArraySegment<byte> bytes = _requestStream.GetBuffer();
                    request.Content = new ByteArrayContent(bytes.Array, bytes.Offset, bytes.Count);
                }

                handler.AutomaticDecompression = AutomaticDecompression;
                handler.Credentials = _credentials;
                handler.AllowAutoRedirect = AllowAutoRedirect;
                handler.MaxAutomaticRedirections = MaximumAutomaticRedirections;
                handler.MaxResponseHeadersLength = MaximumResponseHeadersLength;
                handler.PreAuthenticate = PreAuthenticate;
                client.Timeout = Timeout == Threading.Timeout.Infinite ?
                    Threading.Timeout.InfiniteTimeSpan :
                    TimeSpan.FromMilliseconds(Timeout);

                if (_cookieContainer != null)
                {
                    handler.CookieContainer = _cookieContainer;
                    Debug.Assert(handler.UseCookies); // Default of handler.UseCookies is true.
                }
                else
                {
                    handler.UseCookies = false;
                }

                Debug.Assert(handler.UseProxy); // Default of handler.UseProxy is true.
                Debug.Assert(handler.Proxy == null); // Default of handler.Proxy is null.
                if (_proxy == null)
                {
                    handler.UseProxy = false;
                }
                else
                {
                    handler.Proxy = _proxy;
                }

                handler.ClientCertificates.AddRange(ClientCertificates);

                // Set relevant properties from ServicePointManager
                handler.SslProtocols = (SslProtocols)ServicePointManager.SecurityProtocol;
                handler.CheckCertificateRevocationList = ServicePointManager.CheckCertificateRevocationList;
                RemoteCertificateValidationCallback rcvc = ServerCertificateValidationCallback != null ?
                                                ServerCertificateValidationCallback :
                                                ServicePointManager.ServerCertificateValidationCallback;
                if (rcvc != null)
                {
                    RemoteCertificateValidationCallback localRcvc = rcvc;
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => localRcvc(this, cert, chain, errors);
                }

                if (_hostUri != null)
                {
                    request.Headers.Host = _hostUri.Host;
                }

                // Copy the HttpWebRequest request headers from the WebHeaderCollection into HttpRequestMessage.Headers and
                // HttpRequestMessage.Content.Headers.
                foreach (string headerName in _webHeaderCollection)
                {
                    // The System.Net.Http APIs require HttpRequestMessage headers to be properly divided between the request headers
                    // collection and the request content headers collection for all well-known header names.  And custom headers
                    // are only allowed in the request headers collection and not in the request content headers collection.
                    if (IsWellKnownContentHeader(headerName))
                    {
                        if (request.Content == null)
                        {
                            // Create empty content so that we can send the entity-body header.
                            request.Content = new ByteArrayContent(Array.Empty<byte>());
                        }

                        request.Content.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName]);
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName]);
                    }
                }

                request.Headers.TransferEncodingChunked = SendChunked;

                if (KeepAlive)
                {
                    request.Headers.Connection.Add(HttpKnownHeaderNames.KeepAlive);
                }
                else
                {
                    request.Headers.ConnectionClose = true;
                }

                request.Version = ProtocolVersion;

                _sendRequestTask = client.SendAsync(
                    request,
                    _allowReadStreamBuffering ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead,
                    _sendRequestCts.Token);
                HttpResponseMessage responseMessage = await _sendRequestTask.ConfigureAwait(false);

                HttpWebResponse response = new HttpWebResponse(responseMessage, _requestUri, _cookieContainer);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new WebException(
                        SR.Format(SR.net_servererror, (int)response.StatusCode, response.StatusDescription),
                        null,
                        WebExceptionStatus.ProtocolError,
                        response);
                }

                return response;
            }
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            CheckAbort();

            if (Interlocked.Exchange(ref _beginGetResponseCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.net_repcall);
            }

            _sendRequestCts = new CancellationTokenSource();
            _responseCallback = callback;
            _responseOperation = SendRequest().ToApm(callback, state);

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

            //
            // Do some range checking before assembling the header
            //

            if (rangeSpecifier == null)
            {
                throw new ArgumentNullException(nameof(rangeSpecifier));
            }
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
            if (rangeSpecifier == null)
            {
                throw new ArgumentNullException(nameof(rangeSpecifier));
            }
            if (!HttpValidationHelpers.IsValidToken(rangeSpecifier))
            {
                throw new ArgumentException(SR.net_nottoken, nameof(rangeSpecifier));
            }
            if (!AddRange(rangeSpecifier, range.ToString(NumberFormatInfo.InvariantInfo), (range >= 0) ? "" : null))
            {
                throw new InvalidOperationException(SR.net_rangetype);
            }
        }

        private bool AddRange(string rangeSpecifier, string from, string to)
        {

            string curRange = _webHeaderCollection[HttpKnownHeaderNames.Range];

            if ((curRange == null) || (curRange.Length == 0))
            {
                curRange = rangeSpecifier + "=";
            }
            else
            {
                if (String.Compare(curRange.Substring(0, curRange.IndexOf('=')), rangeSpecifier, StringComparison.OrdinalIgnoreCase) != 0)
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

        private bool IsWellKnownContentHeader(string header)
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
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                string headerValue = _webHeaderCollection[headerName];

                if (headerValue == null)
                {
                    return DateTime.MinValue; // MinValue means header is not present
                }
                return StringToDate(headerValue);
#if DEBUG
            }
#endif
        }

        private void SetDateHeaderHelper(string headerName, DateTime dateTime)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                if (dateTime == DateTime.MinValue)
                    SetSpecialHeaders(headerName, null); // remove header
                else
                    SetSpecialHeaders(headerName, DateToString(dateTime));
#if DEBUG
            }
#endif
        }

        // parse String to DateTime format.
        private static DateTime StringToDate(String S)
        {
            DateTime dtOut;
            if (HttpDateParse.ParseHttpDate(S, out dtOut))
            {
                return dtOut;
            }
            else
            {
                throw new ProtocolViolationException(SR.net_baddate);
            }
        }

        // convert Date to String using RFC 1123 pattern
        private static string DateToString(DateTime D)
        {
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", dateFormat);
        }

        private bool TryGetHostUri(string hostName, out Uri hostUri)
        {
            string s = Address.Scheme + "://" + hostName + Address.PathAndQuery;
            return Uri.TryCreate(s, UriKind.Absolute, out hostUri);
        }
    }
}
