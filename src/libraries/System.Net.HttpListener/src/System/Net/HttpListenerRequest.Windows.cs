// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerRequest
    {
        private Uri _requestUri;
        private ulong _requestId;
        internal ulong _connectionId;
        private SslStatus _sslStatus;
        private string _rawUrl;
        private string _cookedUrlHost;
        private string _cookedUrlPath;
        private string _cookedUrlQuery;
        private long _contentLength;
        private Stream _requestStream;
        private string _httpMethod;
        private bool? _keepAlive;
        private Version _version;
        private WebHeaderCollection _webHeaders;
        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;
        private BoundaryType _boundaryType;
        private ListenerClientCertState _clientCertState;
        private X509Certificate2 _clientCertificate;
        private int _clientCertificateError;
        private RequestContextBase _memoryBlob;
        private CookieCollection _cookies;
        private HttpListenerContext _httpContext;
        private bool _isDisposed = false;
        internal const uint CertBoblSize = 1500;
        private string _serviceName;
        private object _lock = new object();

        private enum SslStatus : byte
        {
            Insecure,
            NoClientCert,
            ClientCert
        }

        internal HttpListenerRequest(HttpListenerContext httpContext, RequestContextBase memoryBlob)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Info(this, $"httpContext:${httpContext} memoryBlob {((IntPtr)memoryBlob.RequestBlob)}");
                NetEventSource.Associate(this, httpContext);
            }
            _httpContext = httpContext;
            _memoryBlob = memoryBlob;
            _boundaryType = BoundaryType.None;

            // Set up some of these now to avoid refcounting on memory blob later.
            _requestId = memoryBlob.RequestBlob->RequestId;
            _connectionId = memoryBlob.RequestBlob->ConnectionId;
            _sslStatus = memoryBlob.RequestBlob->pSslInfo == null ? SslStatus.Insecure :
                memoryBlob.RequestBlob->pSslInfo->SslClientCertNegotiated == 0 ? SslStatus.NoClientCert :
                SslStatus.ClientCert;
            if (memoryBlob.RequestBlob->pRawUrl != null && memoryBlob.RequestBlob->RawUrlLength > 0)
            {
                _rawUrl = Marshal.PtrToStringAnsi((IntPtr)memoryBlob.RequestBlob->pRawUrl, memoryBlob.RequestBlob->RawUrlLength);
            }

            Interop.HttpApi.HTTP_COOKED_URL cookedUrl = memoryBlob.RequestBlob->CookedUrl;
            if (cookedUrl.pHost != null && cookedUrl.HostLength > 0)
            {
                _cookedUrlHost = Marshal.PtrToStringUni((IntPtr)cookedUrl.pHost, cookedUrl.HostLength / 2);
            }
            if (cookedUrl.pAbsPath != null && cookedUrl.AbsPathLength > 0)
            {
                _cookedUrlPath = Marshal.PtrToStringUni((IntPtr)cookedUrl.pAbsPath, cookedUrl.AbsPathLength / 2);
            }
            if (cookedUrl.pQueryString != null && cookedUrl.QueryStringLength > 0)
            {
                _cookedUrlQuery = Marshal.PtrToStringUni((IntPtr)cookedUrl.pQueryString, cookedUrl.QueryStringLength / 2);
            }
            _version = new Version(memoryBlob.RequestBlob->Version.MajorVersion, memoryBlob.RequestBlob->Version.MinorVersion);
            _clientCertState = ListenerClientCertState.NotInitialized;
            _keepAlive = null;
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Info(this, $"RequestId:{RequestId} ConnectionId:{_connectionId} RawConnectionId:{memoryBlob.RequestBlob->RawConnectionId} UrlContext:{memoryBlob.RequestBlob->UrlContext} RawUrl:{_rawUrl} Version:{_version} Secure:{_sslStatus}");
                NetEventSource.Info(this, $"httpContext:${httpContext} RequestUri:{RequestUri} Content-Length:{ContentLength64} HTTP Method:{HttpMethod}");
            }
            // Log headers
            if (NetEventSource.IsEnabled)
            {
                StringBuilder sb = new StringBuilder("HttpListenerRequest Headers:\n");
                for (int i = 0; i < Headers.Count; i++)
                {
                    sb.Append("\t");
                    sb.Append(Headers.GetKey(i));
                    sb.Append(" : ");
                    sb.Append(Headers.Get(i));
                    sb.Append("\n");
                }
                NetEventSource.Info(this, sb.ToString());
            }
        }

        internal HttpListenerContext HttpListenerContext
        {
            get
            {
                return _httpContext;
            }
        }

        // Note: RequestBuffer may get moved in memory. If you dereference a pointer from inside the RequestBuffer, 
        // you must use 'OriginalBlobAddress' below to adjust the location of the pointer to match the location of
        // RequestBuffer.
        internal byte[] RequestBuffer
        {
            get
            {
                CheckDisposed();
                return _memoryBlob.RequestBuffer;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                CheckDisposed();
                return _memoryBlob.OriginalBlobAddress;
            }
        }

        // Use this to save the blob from dispose if this object was never used (never given to a user) and is about to be
        // disposed.
        internal void DetachBlob(RequestContextBase memoryBlob)
        {
            if (memoryBlob != null && (object)memoryBlob == (object)_memoryBlob)
            {
                _memoryBlob = null;
            }
        }

        // Finalizes ownership of the memory blob.  DetachBlob can't be called after this.
        internal void ReleasePins()
        {
            _memoryBlob.ReleasePins();
        }

        internal ulong RequestId
        {
            get
            {
                return _requestId;
            }
        }

        public string[] AcceptTypes
        {
            get
            {
                return Helpers.ParseMultivalueHeader(GetKnownHeader(HttpRequestHeader.Accept));
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (UserAgent != null && CultureInfo.InvariantCulture.CompareInfo.IsPrefix(UserAgent, "UP"))
                {
                    string postDataCharset = Headers["x-up-devcap-post-charset"];
                    if (postDataCharset != null && postDataCharset.Length > 0)
                    {
                        try
                        {
                            return Encoding.GetEncoding(postDataCharset);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                if (HasEntityBody)
                {
                    if (ContentType != null)
                    {
                        string charSet = Helpers.GetAttributeFromHeader(ContentType, "charset");
                        if (charSet != null)
                        {
                            try
                            {
                                return Encoding.GetEncoding(charSet);
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                    }
                }
                return Encoding.Default;
            }
        }

        public long ContentLength64
        {
            get
            {
                if (_boundaryType == BoundaryType.None)
                {
                    if (GetKnownHeader(HttpRequestHeader.TransferEncoding).Equals("chunked", StringComparison.OrdinalIgnoreCase))
                    {
                        _boundaryType = BoundaryType.Chunked;
                        _contentLength = -1;
                    }
                    else
                    {
                        _contentLength = 0;
                        _boundaryType = BoundaryType.ContentLength;
                        string length = GetKnownHeader(HttpRequestHeader.ContentLength);
                        if (length != null)
                        {
                            bool success = long.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out _contentLength);
                            if (!success)
                            {
                                _contentLength = 0;
                                _boundaryType = BoundaryType.Invalid;
                            }
                        }
                    }
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_contentLength:{_contentLength} _boundaryType:{_boundaryType}");
                return _contentLength;
            }
        }

        public string ContentType
        {
            get
            {
                return GetKnownHeader(HttpRequestHeader.ContentType);
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                if (_webHeaders == null)
                {
                    _webHeaders = Interop.HttpApi.GetHeaders(RequestBuffer, OriginalBlobAddress);
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"webHeaders:{_webHeaders}");
                return _webHeaders;
            }
        }

        public string HttpMethod
        {
            get
            {
                if (_httpMethod == null)
                {
                    _httpMethod = Interop.HttpApi.GetVerb(RequestBuffer, OriginalBlobAddress);
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_httpMethod:{_httpMethod}");
                return _httpMethod;
            }
        }

        public Stream InputStream
        {
            get
            {
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
                if (_requestStream == null)
                {
                    _requestStream = HasEntityBody ? new HttpRequestStream(HttpListenerContext) : Stream.Null;
                }
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                return _requestStream;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                IPrincipal user = HttpListenerContext.User;
                return user != null && user.Identity != null && user.Identity.IsAuthenticated;
            }
        }

        public bool IsLocal
        {
            get
            {
                return LocalEndPoint.Address.Equals(RemoteEndPoint.Address);
            }
        }

        public bool IsSecureConnection
        {
            get
            {
                return _sslStatus != SslStatus.Insecure;
            }
        }

        public bool IsWebSocketRequest
        {
            get
            {
                if (!WebSocketProtocolComponent.IsSupported)
                {
                    return false;
                }

                bool foundConnectionUpgradeHeader = false;
                if (string.IsNullOrEmpty(this.Headers[HttpKnownHeaderNames.Connection]) || string.IsNullOrEmpty(this.Headers[HttpKnownHeaderNames.Upgrade]))
                {
                    return false;
                }

                foreach (string connection in this.Headers.GetValues(HttpKnownHeaderNames.Connection))
                {
                    if (string.Compare(connection, HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        foundConnectionUpgradeHeader = true;
                        break;
                    }
                }

                if (!foundConnectionUpgradeHeader)
                {
                    return false;
                }

                foreach (string upgrade in this.Headers.GetValues(HttpKnownHeaderNames.Upgrade))
                {
                    if (string.Equals(upgrade, WebSocketValidate.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                NameValueCollection queryString = new NameValueCollection();
                Helpers.FillFromString(queryString, Url.Query, true, ContentEncoding);
                return queryString;
            }
        }

        public string RawUrl
        {
            get
            {
                return _rawUrl;
            }
        }

        public string ServiceName
        {
            get { return _serviceName; }
            internal set { _serviceName = value; }
        }

        public Uri Url
        {
            get
            {
                return RequestUri;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                string referrer = GetKnownHeader(HttpRequestHeader.Referer);
                if (referrer == null)
                {
                    return null;
                }
                Uri urlReferrer;
                bool success = Uri.TryCreate(referrer, UriKind.RelativeOrAbsolute, out urlReferrer);
                return success ? urlReferrer : null;
            }
        }

        public string UserAgent
        {
            get
            {
                return GetKnownHeader(HttpRequestHeader.UserAgent);
            }
        }

        public string UserHostAddress
        {
            get
            {
                return LocalEndPoint.ToString();
            }
        }

        public string UserHostName
        {
            get
            {
                return GetKnownHeader(HttpRequestHeader.Host);
            }
        }

        public string[] UserLanguages
        {
            get
            {
                return Helpers.ParseMultivalueHeader(GetKnownHeader(HttpRequestHeader.AcceptLanguage));
            }
        }

        public int ClientCertificateError
        {
            get
            {
                if (_clientCertState == ListenerClientCertState.NotInitialized)
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "GetClientCertificate()/BeginGetClientCertificate()"));
                else if (_clientCertState == ListenerClientCertState.InProgress)
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcompletecall, "GetClientCertificate()/BeginGetClientCertificate()"));

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"ClientCertificateError:{_clientCertificateError}");
                return _clientCertificateError;
            }
        }

        internal X509Certificate2 ClientCertificate
        {
            set
            {
                _clientCertificate = value;
            }
        }

        internal ListenerClientCertState ClientCertState
        {
            set
            {
                _clientCertState = value;
            }
        }

        internal void SetClientCertificateError(int clientCertificateError)
        {
            _clientCertificateError = clientCertificateError;
        }

        public X509Certificate2 GetClientCertificate()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                ProcessClientCertificate();
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_clientCertificate:{_clientCertificate}");
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
            return _clientCertificate;
        }

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
            return AsyncProcessClientCertificate(requestCallback, state);
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            X509Certificate2 clientCertificate = null;
            try
            {
                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }
                ListenerClientCertAsyncResult clientCertAsyncResult = asyncResult as ListenerClientCertAsyncResult;
                if (clientCertAsyncResult == null || clientCertAsyncResult.AsyncObject != this)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
                }
                if (clientCertAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndGetClientCertificate)));
                }
                clientCertAsyncResult.EndCalled = true;
                clientCertificate = clientCertAsyncResult.InternalWaitForCompletion() as X509Certificate2;
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_clientCertificate:{_clientCertificate}");
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
            return clientCertificate;
        }

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task.Factory.FromAsync(
                (callback, state) => ((HttpListenerRequest)state).BeginGetClientCertificate(callback, state),
                iar => ((HttpListenerRequest)iar.AsyncState).EndGetClientCertificate(iar),
                null);
        }

        public TransportContext TransportContext
        {
            get
            {
                return new HttpListenerRequestContext(this);
            }
        }

        private CookieCollection ParseCookies(Uri uri, string setCookieHeader)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "uri:" + uri + " setCookieHeader:" + setCookieHeader);
            CookieContainer container = new CookieContainer();
            container.SetCookies(uri, setCookieHeader);
            return container.GetCookies(uri);
        }

        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                {
                    string cookieString = GetKnownHeader(HttpRequestHeader.Cookie);
                    if (cookieString != null && cookieString.Length > 0)
                    {
                        _cookies = ParseCookies(RequestUri, cookieString);
                    }
                    if (_cookies == null)
                    {
                        _cookies = new CookieCollection();
                    }
                }
                return _cookies;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return _version;
            }
        }

        public bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates m_BoundaryType
                return (ContentLength64 > 0 && _boundaryType == BoundaryType.ContentLength) ||
                    _boundaryType == BoundaryType.Chunked || _boundaryType == BoundaryType.Multipart;
            }
        }

        public bool KeepAlive
        {
            get
            {
                if (!_keepAlive.HasValue)
                {
                    string header = Headers[HttpKnownHeaderNames.ProxyConnection];
                    if (string.IsNullOrEmpty(header))
                    {
                        header = GetKnownHeader(HttpRequestHeader.Connection);
                    }
                    if (string.IsNullOrEmpty(header))
                    {
                        if (ProtocolVersion >= HttpVersion.Version11)
                        {
                            _keepAlive = true;
                        }
                        else
                        {
                            header = GetKnownHeader(HttpRequestHeader.KeepAlive);
                            _keepAlive = !string.IsNullOrEmpty(header);
                        }
                    }
                    else
                    {
                        header = header.ToLower(CultureInfo.InvariantCulture);
                        _keepAlive =
                            header.IndexOf("close", StringComparison.InvariantCultureIgnoreCase) < 0 ||
                            header.IndexOf("keep-alive", StringComparison.InvariantCultureIgnoreCase) >= 0;
                    }
                }

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "_keepAlive=" + _keepAlive);
                return _keepAlive.Value;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (_remoteEndPoint == null)
                {
                    _remoteEndPoint = Interop.HttpApi.GetRemoteEndPoint(RequestBuffer, OriginalBlobAddress);
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "_remoteEndPoint" + _remoteEndPoint);
                return _remoteEndPoint;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (_localEndPoint == null)
                {
                    _localEndPoint = Interop.HttpApi.GetLocalEndPoint(RequestBuffer, OriginalBlobAddress);
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_localEndPoint={_localEndPoint}");
                return _localEndPoint;
            }
        }

        //should only be called from httplistenercontext
        internal void Close()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            RequestContextBase memoryBlob = _memoryBlob;
            if (memoryBlob != null)
            {
                memoryBlob.Close();
                _memoryBlob = null;
            }
            _isDisposed = true;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        private ListenerClientCertAsyncResult AsyncProcessClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (_clientCertState == ListenerClientCertState.InProgress)
                throw new InvalidOperationException(SR.Format(SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
            _clientCertState = ListenerClientCertState.InProgress;

            ListenerClientCertAsyncResult asyncResult = null;
            //--------------------------------------------------------------------
            //When you configure the HTTP.SYS with a flag value 2
            //which means require client certificates, when the client makes the
            //initial SSL connection, server (HTTP.SYS) demands the client certificate
            //
            //Some apps may not want to demand the client cert at the beginning
            //perhaps server the default.htm. In this case the HTTP.SYS is configured
            //with a flag value other than 2, whcih means that the client certificate is
            //optional.So intially when SSL is established HTTP.SYS won't ask for client
            //certificate. This works fine for the default.htm in the case above
            //However, if the app wants to demand a client certficate at a later time
            //perhaps showing "YOUR ORDERS" page, then the server wans to demand
            //Client certs. this will inturn makes HTTP.SYS to do the
            //SEC_I_RENOGOTIATE through which the client cert demand is made
            //
            //THE BUG HERE IS THAT PRIOR TO QFE 4796, we call
            //GET Client certificate native API ONLY WHEN THE HTTP.SYS is configured with
            //flag = 2. Which means that apps using HTTPListener will not be able to
            //demand a client cert at a later point
            //
            //The fix here is to demand the client cert when the channel is NOT INSECURE
            //which means whether the client certs are requried at the beginning or not,
            //if this is an SSL connection, Call HttpReceiveClientCertificate, thus
            //starting the cert negotiation at that point
            //
            //NOTE: WHEN CALLING THE HttpReceiveClientCertificate, you can get
            //ERROR_NOT_FOUND - which means the client did not provide the cert
            //If this is important, the server should respond with 403 forbidden
            //HTTP.SYS will not do this for you automatically ***
            //--------------------------------------------------------------------
            if (_sslStatus != SslStatus.Insecure)
            {
                // at this point we know that DefaultFlags has the 2 bit set (Negotiate Client certificate)
                // the cert, though might or might not be there. try to retrieve it
                // this number is the same that IIS decided to use
                uint size = CertBoblSize;
                asyncResult = new ListenerClientCertAsyncResult(HttpListenerContext.RequestQueueBoundHandle, this, state, requestCallback, size);
                try
                {
                    while (true)
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
                        uint bytesReceived = 0;

                        uint statusCode =
                            Interop.HttpApi.HttpReceiveClientCertificate(
                                HttpListenerContext.RequestQueueHandle,
                                _connectionId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                                asyncResult.RequestBlob,
                                size,
                                &bytesReceived,
                                asyncResult.NativeOverlapped);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this,
                            "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
                        if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                        {
                            Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult.RequestBlob;
                            size = bytesReceived + pClientCertInfo->CertEncodedSize;
                            asyncResult.Reset(size);
                            continue;
                        }
                        if (statusCode != Interop.HttpApi.ERROR_SUCCESS &&
                            statusCode != Interop.HttpApi.ERROR_IO_PENDING)
                        {
                            // someother bad error, possible return values are:
                            // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                            // Also ERROR_BAD_DATA if we got it twice or it reported smaller size buffer required.
                            throw new HttpListenerException((int)statusCode);
                        }

                        if (statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                            HttpListener.SkipIOCPCallbackOnSuccess)
                        {
                            asyncResult.IOCompleted(statusCode, bytesReceived);
                        }
                        break;
                    }
                }
                catch
                {
                    asyncResult?.InternalCleanup();
                    throw;
                }
            }
            else
            {
                asyncResult = new ListenerClientCertAsyncResult(HttpListenerContext.RequestQueueBoundHandle, this, state, requestCallback, 0);
                asyncResult.InvokeCallback();
            }
            return asyncResult;
        }

        private void ProcessClientCertificate()
        {
            if (_clientCertState == ListenerClientCertState.InProgress)
                throw new InvalidOperationException(SR.Format(SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
            _clientCertState = ListenerClientCertState.InProgress;
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
            //--------------------------------------------------------------------
            //When you configure the HTTP.SYS with a flag value 2
            //which means require client certificates, when the client makes the
            //initial SSL connection, server (HTTP.SYS) demands the client certificate
            //
            //Some apps may not want to demand the client cert at the beginning
            //perhaps server the default.htm. In this case the HTTP.SYS is configured
            //with a flag value other than 2, whcih means that the client certificate is
            //optional.So intially when SSL is established HTTP.SYS won't ask for client
            //certificate. This works fine for the default.htm in the case above
            //However, if the app wants to demand a client certficate at a later time
            //perhaps showing "YOUR ORDERS" page, then the server wans to demand
            //Client certs. this will inturn makes HTTP.SYS to do the
            //SEC_I_RENOGOTIATE through which the client cert demand is made
            //
            //THE BUG HERE IS THAT PRIOR TO QFE 4796, we call
            //GET Client certificate native API ONLY WHEN THE HTTP.SYS is configured with
            //flag = 2. Which means that apps using HTTPListener will not be able to
            //demand a client cert at a later point
            //
            //The fix here is to demand the client cert when the channel is NOT INSECURE
            //which means whether the client certs are requried at the beginning or not,
            //if this is an SSL connection, Call HttpReceiveClientCertificate, thus
            //starting the cert negotiation at that point
            //
            //NOTE: WHEN CALLING THE HttpReceiveClientCertificate, you can get
            //ERROR_NOT_FOUND - which means the client did not provide the cert
            //If this is important, the server should respond with 403 forbidden
            //HTTP.SYS will not do this for you automatically ***
            //--------------------------------------------------------------------
            if (_sslStatus != SslStatus.Insecure)
            {
                // at this point we know that DefaultFlags has the 2 bit set (Negotiate Client certificate)
                // the cert, though might or might not be there. try to retrieve it
                // this number is the same that IIS decided to use
                uint size = CertBoblSize;
                while (true)
                {
                    byte[] clientCertInfoBlob = new byte[checked((int)size)];
                    fixed (byte* pClientCertInfoBlob = clientCertInfoBlob)
                    {
                        Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = (Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)pClientCertInfoBlob;

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
                        uint bytesReceived = 0;

                        uint statusCode =
                            Interop.HttpApi.HttpReceiveClientCertificate(
                                HttpListenerContext.RequestQueueHandle,
                                _connectionId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                                pClientCertInfo,
                                size,
                                &bytesReceived,
                                null);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this,
                            "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
                        if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                        {
                            size = bytesReceived + pClientCertInfo->CertEncodedSize;
                            continue;
                        }
                        else if (statusCode == Interop.HttpApi.ERROR_SUCCESS)
                        {
                            if (pClientCertInfo != null)
                            {
                                if (NetEventSource.IsEnabled) NetEventSource.Info(this,
                                    $"pClientCertInfo:{(IntPtr)pClientCertInfo} pClientCertInfo->CertFlags: {pClientCertInfo->CertFlags} pClientCertInfo->CertEncodedSize: {pClientCertInfo->CertEncodedSize} pClientCertInfo->pCertEncoded: {(IntPtr)pClientCertInfo->pCertEncoded} pClientCertInfo->Token: {(IntPtr)pClientCertInfo->Token} pClientCertInfo->CertDeniedByMapper: {pClientCertInfo->CertDeniedByMapper}");

                                if (pClientCertInfo->pCertEncoded != null)
                                {
                                    try
                                    {
                                        byte[] certEncoded = new byte[pClientCertInfo->CertEncodedSize];
                                        Marshal.Copy((IntPtr)pClientCertInfo->pCertEncoded, certEncoded, 0, certEncoded.Length);
                                        _clientCertificate = new X509Certificate2(certEncoded);
                                    }
                                    catch (CryptographicException exception)
                                    {
                                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"CryptographicException={exception}");
                                    }
                                    catch (SecurityException exception)
                                    {
                                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"SecurityException={exception}");
                                    }
                                }
                                _clientCertificateError = (int)pClientCertInfo->CertFlags;
                            }
                        }
                        else
                        {
                            Debug.Assert(statusCode == Interop.HttpApi.ERROR_NOT_FOUND,
                                $"Call to Interop.HttpApi.HttpReceiveClientCertificate() failed with statusCode {statusCode}.");
                        }
                    }
                    break;
                }
            }
            _clientCertState = ListenerClientCertState.Completed;
        }

        private string RequestScheme
        {
            get
            {
                return IsSecureConnection ? "https" : "http";
            }
        }

        private Uri RequestUri
        {
            get
            {
                if (_requestUri == null)
                {
                    _requestUri = HttpListenerRequestUriBuilder.GetRequestUri(
                        _rawUrl, RequestScheme, _cookedUrlHost, _cookedUrlPath, _cookedUrlQuery);
                }

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_requestUri:{_requestUri}");
                return _requestUri;
            }
        }

        private string GetKnownHeader(HttpRequestHeader header)
        {
            return Interop.HttpApi.GetKnownHeader(RequestBuffer, OriginalBlobAddress, (int)header);
        }

        internal ChannelBinding GetChannelBinding()
        {
            return HttpListenerContext.Listener.GetChannelBindingFromTls(_connectionId);
        }

        internal void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private static class Helpers
        {
            //
            // Get attribute off header value
            //
            internal static string GetAttributeFromHeader(string headerValue, string attrName)
            {
                if (headerValue == null)
                    return null;

                int l = headerValue.Length;
                int k = attrName.Length;

                // find properly separated attribute name
                int i = 1; // start searching from 1

                while (i < l)
                {
                    i = CultureInfo.InvariantCulture.CompareInfo.IndexOf(headerValue, attrName, i, CompareOptions.IgnoreCase);
                    if (i < 0)
                        break;
                    if (i + k >= l)
                        break;

                    char chPrev = headerValue[i - 1];
                    char chNext = headerValue[i + k];
                    if ((chPrev == ';' || chPrev == ',' || char.IsWhiteSpace(chPrev)) && (chNext == '=' || char.IsWhiteSpace(chNext)))
                        break;

                    i += k;
                }

                if (i < 0 || i >= l)
                    return null;

                // skip to '=' and the following whitespaces
                i += k;
                while (i < l && char.IsWhiteSpace(headerValue[i]))
                    i++;
                if (i >= l || headerValue[i] != '=')
                    return null;
                i++;
                while (i < l && char.IsWhiteSpace(headerValue[i]))
                    i++;
                if (i >= l)
                    return null;

                // parse the value
                string attrValue = null;

                int j;

                if (i < l && headerValue[i] == '"')
                {
                    if (i == l - 1)
                        return null;
                    j = headerValue.IndexOf('"', i + 1);
                    if (j < 0 || j == i + 1)
                        return null;

                    attrValue = headerValue.Substring(i + 1, j - i - 1).Trim();
                }
                else
                {
                    for (j = i; j < l; j++)
                    {
                        if (headerValue[j] == ' ' || headerValue[j] == ',')
                            break;
                    }

                    if (j == i)
                        return null;

                    attrValue = headerValue.Substring(i, j - i).Trim();
                }

                return attrValue;
            }

            internal static string[] ParseMultivalueHeader(string s)
            {
                if (s == null)
                    return null;

                int l = s.Length;

                // collect comma-separated values into list

                List<string> values = new List<string>();
                int i = 0;

                while (i < l)
                {
                    // find next ,
                    int ci = s.IndexOf(',', i);
                    if (ci < 0)
                        ci = l;

                    // append corresponding server value
                    values.Add(s.Substring(i, ci - i));

                    // move to next
                    i = ci + 1;

                    // skip leading space
                    if (i < l && s[i] == ' ')
                        i++;
                }

                // return list as array of strings

                int n = values.Count;
                string[] strings;

                // if n is 0 that means s was empty string

                if (n == 0)
                {
                    strings = new string[1];
                    strings[0] = string.Empty;
                }
                else
                {
                    strings = new string[n];
                    values.CopyTo(0, strings, 0, n);
                }
                return strings;
            }


            private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
            {
                int count = s.Length;
                UrlDecoder helper = new UrlDecoder(count, e);

                // go through the string's chars collapsing %XX and %uXXXX and
                // appending each char as char, with exception of %XX constructs
                // that are appended as bytes

                for (int pos = 0; pos < count; pos++)
                {
                    char ch = s[pos];

                    if (ch == '+')
                    {
                        ch = ' ';
                    }
                    else if (ch == '%' && pos < count - 2)
                    {
                        if (s[pos + 1] == 'u' && pos < count - 5)
                        {
                            int h1 = HexToInt(s[pos + 2]);
                            int h2 = HexToInt(s[pos + 3]);
                            int h3 = HexToInt(s[pos + 4]);
                            int h4 = HexToInt(s[pos + 5]);

                            if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0)
                            {   // valid 4 hex chars
                                ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                                pos += 5;

                                // only add as char
                                helper.AddChar(ch);
                                continue;
                            }
                        }
                        else
                        {
                            int h1 = HexToInt(s[pos + 1]);
                            int h2 = HexToInt(s[pos + 2]);

                            if (h1 >= 0 && h2 >= 0)
                            {     // valid 2 hex chars
                                byte b = (byte)((h1 << 4) | h2);
                                pos += 2;

                                // don't add as char
                                helper.AddByte(b);
                                continue;
                            }
                        }
                    }

                    if ((ch & 0xFF80) == 0)
                        helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                    else
                        helper.AddChar(ch);
                }

                return helper.GetString();
            }

            private static int HexToInt(char h)
            {
                return (h >= '0' && h <= '9') ? h - '0' :
                (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
                (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
                -1;
            }

            private class UrlDecoder
            {
                private int _bufferSize;

                // Accumulate characters in a special array
                private int _numChars;
                private char[] _charBuffer;

                // Accumulate bytes for decoding into characters in a special array
                private int _numBytes;
                private byte[] _byteBuffer;

                // Encoding to convert chars to bytes
                private Encoding _encoding;

                private void FlushBytes()
                {
                    if (_numBytes > 0)
                    {
                        _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                        _numBytes = 0;
                    }
                }

                internal UrlDecoder(int bufferSize, Encoding encoding)
                {
                    _bufferSize = bufferSize;
                    _encoding = encoding;

                    _charBuffer = new char[bufferSize];
                    // byte buffer created on demand
                }

                internal void AddChar(char ch)
                {
                    if (_numBytes > 0)
                        FlushBytes();

                    _charBuffer[_numChars++] = ch;
                }

                internal void AddByte(byte b)
                {
                    {
                        if (_byteBuffer == null)
                            _byteBuffer = new byte[_bufferSize];

                        _byteBuffer[_numBytes++] = b;
                    }
                }

                internal string GetString()
                {
                    if (_numBytes > 0)
                        FlushBytes();

                    if (_numChars > 0)
                        return new string(_charBuffer, 0, _numChars);
                    else
                        return string.Empty;
                }
            }


            internal static void FillFromString(NameValueCollection nvc, string s, bool urlencoded, Encoding encoding)
            {
                int l = (s != null) ? s.Length : 0;
                int i = (s.Length > 0 && s[0] == '?') ? 1 : 0;

                while (i < l)
                {
                    // find next & while noting first = on the way (and if there are more)

                    int si = i;
                    int ti = -1;

                    while (i < l)
                    {
                        char ch = s[i];

                        if (ch == '=')
                        {
                            if (ti < 0)
                                ti = i;
                        }
                        else if (ch == '&')
                        {
                            break;
                        }

                        i++;
                    }

                    // extract the name / value pair

                    string name = null;
                    string value = null;

                    if (ti >= 0)
                    {
                        name = s.Substring(si, ti - si);
                        value = s.Substring(ti + 1, i - ti - 1);
                    }
                    else
                    {
                        value = s.Substring(si, i - si);
                    }

                    // add name / value pair to the collection

                    if (urlencoded)
                        nvc.Add(
                           name == null ? null : UrlDecodeStringFromStringInternal(name, encoding),
                           UrlDecodeStringFromStringInternal(value, encoding));
                    else
                        nvc.Add(name, value);

                    // trailing '&'

                    if (i == l - 1 && s[i] == '&')
                        nvc.Add(null, "");

                    i++;
                }
            }
        }
    }
}
