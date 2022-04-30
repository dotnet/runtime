// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace System.Net
{
    public sealed unsafe partial class HttpListenerRequest
    {
        private readonly ulong _requestId;
        internal ulong _connectionId;
        private readonly SslStatus _sslStatus;
        private readonly string? _cookedUrlHost;
        private readonly string? _cookedUrlPath;
        private readonly string? _cookedUrlQuery;
        private long _contentLength;
        private Stream? _requestStream;
        private string? _httpMethod;
        private WebHeaderCollection? _webHeaders;
        private IPEndPoint? _localEndPoint;
        private IPEndPoint? _remoteEndPoint;
        private BoundaryType _boundaryType;
        private int _clientCertificateError;
        private RequestContextBase? _memoryBlob;
        private readonly HttpListenerContext _httpContext;
        private bool _isDisposed;
        internal const uint CertBoblSize = 1500;
        private string? _serviceName;

        private enum SslStatus : byte
        {
            Insecure,
            NoClientCert,
            ClientCert
        }

        internal HttpListenerRequest(HttpListenerContext httpContext, RequestContextBase memoryBlob)
        {
            if (NetEventSource.Log.IsEnabled())
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
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"RequestId:{RequestId} ConnectionId:{_connectionId} RawConnectionId:{memoryBlob.RequestBlob->RawConnectionId} UrlContext:{memoryBlob.RequestBlob->UrlContext} RawUrl:{_rawUrl} Version:{_version} Secure:{_sslStatus}");
                NetEventSource.Info(this, $"httpContext:${httpContext} RequestUri:{RequestUri} Content-Length:{ContentLength64} HTTP Method:{HttpMethod}");
            }
            // Log headers
            if (NetEventSource.Log.IsEnabled())
            {
                StringBuilder sb = new StringBuilder("HttpListenerRequest Headers:\n");
                for (int i = 0; i < Headers.Count; i++)
                {
                    sb.Append('\t');
                    sb.Append(Headers.GetKey(i));
                    sb.Append(" : ");
                    sb.Append(Headers.Get(i));
                    sb.Append('\n');
                }
                NetEventSource.Info(this, sb.ToString());
            }
        }

        internal HttpListenerContext HttpListenerContext => _httpContext;

        // Note: RequestBuffer may get moved in memory. If you dereference a pointer from inside the RequestBuffer,
        // you must use 'OriginalBlobAddress' below to adjust the location of the pointer to match the location of
        // RequestBuffer.
        internal IntPtr RequestBuffer
        {
            get
            {
                CheckDisposed();
                return _memoryBlob!.RequestBuffer;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                CheckDisposed();
                return _memoryBlob!.OriginalBlobAddress;
            }
        }

        // Use this to save the blob from dispose if this object was never used (never given to a user) and is about to be
        // disposed.
        internal void DetachBlob(RequestContextBase memoryBlob)
        {
            if (memoryBlob != null && (object)memoryBlob == (object)_memoryBlob!)
            {
                _memoryBlob = null;
            }
        }

        // Finalizes ownership of the memory blob.  DetachBlob can't be called after this.
        internal void ReleasePins()
        {
            _memoryBlob!.ReleasePins();
        }

        internal ulong RequestId => _requestId;

        public Guid RequestTraceIdentifier
        {
            get
            {
                Guid guid = default;
                *(1 + (ulong*)&guid) = RequestId;
                return guid;
            }
        }

        public long ContentLength64
        {
            get
            {
                if (_boundaryType == BoundaryType.None)
                {
                    string? transferEncodingHeader = Headers[HttpKnownHeaderNames.TransferEncoding];
                    if (transferEncodingHeader != null && transferEncodingHeader.Equals("chunked", StringComparison.OrdinalIgnoreCase))
                    {
                        _boundaryType = BoundaryType.Chunked;
                        _contentLength = -1;
                    }
                    else
                    {
                        _contentLength = 0;
                        _boundaryType = BoundaryType.ContentLength;
                        string? length = Headers[HttpKnownHeaderNames.ContentLength];
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_contentLength:{_contentLength} _boundaryType:{_boundaryType}");
                return _contentLength;
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"webHeaders:{_webHeaders}");
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_httpMethod:{_httpMethod}");
                return _httpMethod!;
            }
        }

        public Stream InputStream
        {
            get
            {
                if (_requestStream == null)
                {
                    _requestStream = HasEntityBody ? new HttpRequestStream(HttpListenerContext) : Stream.Null;
                }
                return _requestStream;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                IPrincipal? user = HttpListenerContext.User;
                return user != null && user.Identity != null && user.Identity.IsAuthenticated;
            }
        }

        public bool IsSecureConnection => _sslStatus != SslStatus.Insecure;

        public string? ServiceName
        {
            get => _serviceName;
            internal set => _serviceName = value;
        }

        private int GetClientCertificateErrorCore()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"ClientCertificateError:{_clientCertificateError}");
            return _clientCertificateError;
        }

        internal void SetClientCertificateError(int clientCertificateError)
        {
            _clientCertificateError = clientCertificateError;
        }

        public X509Certificate2? EndGetClientCertificate(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            X509Certificate2? clientCertificate = null;
            ListenerClientCertAsyncResult? clientCertAsyncResult = asyncResult as ListenerClientCertAsyncResult;
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_clientCertificate:{ClientCertificate}");

            return clientCertificate;
        }

        public TransportContext TransportContext => new HttpListenerRequestContext(this);

        public bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates m_BoundaryType
                return (ContentLength64 > 0 && _boundaryType == BoundaryType.ContentLength) ||
                    _boundaryType == BoundaryType.Chunked || _boundaryType == BoundaryType.Multipart;
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "_remoteEndPoint" + _remoteEndPoint);
                return _remoteEndPoint!;
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_localEndPoint={_localEndPoint}");
                return _localEndPoint!;
            }
        }

        //should only be called from httplistenercontext
        internal void Close()
        {
            RequestContextBase? memoryBlob = _memoryBlob;
            if (memoryBlob != null)
            {
                memoryBlob.Close();
                _memoryBlob = null;
            }
            _isDisposed = true;
        }

        private ListenerClientCertAsyncResult BeginGetClientCertificateCore(AsyncCallback? requestCallback, object? state)
        {
            ListenerClientCertAsyncResult? asyncResult = null;
            //--------------------------------------------------------------------
            //When you configure the HTTP.SYS with a flag value 2
            //which means require client certificates, when the client makes the
            //initial SSL connection, server (HTTP.SYS) demands the client certificate
            //
            //Some apps may not want to demand the client cert at the beginning
            //perhaps server the default.htm. In this case the HTTP.SYS is configured
            //with a flag value other than 2, whcih means that the client certificate is
            //optional.So initially when SSL is established HTTP.SYS won't ask for client
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
            //which means whether the client certs are required at the beginning or not,
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
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
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

                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
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

        private void GetClientCertificateCore()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);
            //--------------------------------------------------------------------
            //When you configure the HTTP.SYS with a flag value 2
            //which means require client certificates, when the client makes the
            //initial SSL connection, server (HTTP.SYS) demands the client certificate
            //
            //Some apps may not want to demand the client cert at the beginning
            //perhaps server the default.htm. In this case the HTTP.SYS is configured
            //with a flag value other than 2, whcih means that the client certificate is
            //optional.So initially when SSL is established HTTP.SYS won't ask for client
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
            //which means whether the client certs are required at the beginning or not,
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
                    fixed (byte* pClientCertInfoBlob = &clientCertInfoBlob[0])
                    {
                        Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = (Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)pClientCertInfoBlob;

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
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

                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
                        if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                        {
                            size = bytesReceived + pClientCertInfo->CertEncodedSize;
                            continue;
                        }
                        else if (statusCode == Interop.HttpApi.ERROR_SUCCESS)
                        {
                            if (pClientCertInfo != null)
                            {
                                if (NetEventSource.Log.IsEnabled())
                                    NetEventSource.Info(this, $"pClientCertInfo:{(IntPtr)pClientCertInfo} pClientCertInfo->CertFlags: {pClientCertInfo->CertFlags} pClientCertInfo->CertEncodedSize: {pClientCertInfo->CertEncodedSize} pClientCertInfo->pCertEncoded: {(IntPtr)pClientCertInfo->pCertEncoded} pClientCertInfo->Token: {(IntPtr)pClientCertInfo->Token} pClientCertInfo->CertDeniedByMapper: {pClientCertInfo->CertDeniedByMapper}");

                                if (pClientCertInfo->pCertEncoded != null)
                                {
                                    try
                                    {
                                        byte[] certEncoded = new byte[pClientCertInfo->CertEncodedSize];
                                        Marshal.Copy((IntPtr)pClientCertInfo->pCertEncoded, certEncoded, 0, certEncoded.Length);
                                        ClientCertificate = new X509Certificate2(certEncoded);
                                    }
                                    catch (CryptographicException exception)
                                    {
                                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"CryptographicException={exception}");
                                    }
                                    catch (SecurityException exception)
                                    {
                                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SecurityException={exception}");
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
        }

        private Uri RequestUri
        {
            get
            {
                if (_requestUri == null)
                {
                    _requestUri = HttpListenerRequestUriBuilder.GetRequestUri(
                        _rawUrl!, RequestScheme, _cookedUrlHost!, _cookedUrlPath!, _cookedUrlQuery!);
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_requestUri:{_requestUri}");
                return _requestUri;
            }
        }

        internal ChannelBinding? GetChannelBinding()
        {
            return HttpListener.GetChannelBindingFromTls(HttpListenerContext.ListenerSession, _connectionId);
        }

        internal void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        private static bool SupportsWebSockets => WebSocketProtocolComponent.IsSupported;
    }
}
