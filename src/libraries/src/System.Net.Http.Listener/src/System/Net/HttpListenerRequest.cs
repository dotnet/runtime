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
    public sealed unsafe class HttpListenerRequest
    {
        private Uri m_RequestUri;
        private ulong m_RequestId;
        internal ulong m_ConnectionId;
        private SslStatus m_SslStatus;
        private string m_RawUrl;
        private string m_CookedUrlHost;
        private string m_CookedUrlPath;
        private string m_CookedUrlQuery;
        private long m_ContentLength;
        private Stream m_RequestStream;
        private string m_HttpMethod;
        private bool? m_KeepAlive;
        private Version m_Version;
        private WebHeaderCollection m_WebHeaders;
        private IPEndPoint m_LocalEndPoint;
        private IPEndPoint m_RemoteEndPoint;
        private BoundaryType m_BoundaryType;
        private ListenerClientCertState m_ClientCertState;
        private X509Certificate2 m_ClientCertificate;
        private int m_ClientCertificateError;
        private RequestContextBase m_MemoryBlob;
        private CookieCollection m_Cookies;
        private HttpListenerContext m_HttpContext;
        private bool m_IsDisposed = false;
        internal const uint CertBoblSize = 1500;
        private string m_ServiceName;
        private object m_Lock = new object();

        private enum SslStatus : byte
        {
            Insecure,
            NoClientCert,
            ClientCert
        }

        internal HttpListenerRequest(HttpListenerContext httpContext, RequestContextBase memoryBlob)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", "httpContext#" + LoggingHash.HashString(httpContext) + " memoryBlob# " + LoggingHash.HashString((IntPtr)memoryBlob.RequestBlob));
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(NetEventSource.ComponentType.HttpListener, this, httpContext);
            m_HttpContext = httpContext;
            m_MemoryBlob = memoryBlob;
            m_BoundaryType = BoundaryType.None;

            // Set up some of these now to avoid refcounting on memory blob later.
            m_RequestId = memoryBlob.RequestBlob->RequestId;
            m_ConnectionId = memoryBlob.RequestBlob->ConnectionId;
            m_SslStatus = memoryBlob.RequestBlob->pSslInfo == null ? SslStatus.Insecure :
                memoryBlob.RequestBlob->pSslInfo->SslClientCertNegotiated == 0 ? SslStatus.NoClientCert :
                SslStatus.ClientCert;
            if (memoryBlob.RequestBlob->pRawUrl != null && memoryBlob.RequestBlob->RawUrlLength > 0)
            {
                m_RawUrl = Marshal.PtrToStringAnsi((IntPtr)memoryBlob.RequestBlob->pRawUrl, memoryBlob.RequestBlob->RawUrlLength);
            }

            Interop.HttpApi.HTTP_COOKED_URL cookedUrl = memoryBlob.RequestBlob->CookedUrl;
            if (cookedUrl.pHost != null && cookedUrl.HostLength > 0)
            {
                m_CookedUrlHost = Marshal.PtrToStringUni((IntPtr)cookedUrl.pHost, cookedUrl.HostLength / 2);
            }
            if (cookedUrl.pAbsPath != null && cookedUrl.AbsPathLength > 0)
            {
                m_CookedUrlPath = Marshal.PtrToStringUni((IntPtr)cookedUrl.pAbsPath, cookedUrl.AbsPathLength / 2);
            }
            if (cookedUrl.pQueryString != null && cookedUrl.QueryStringLength > 0)
            {
                m_CookedUrlQuery = Marshal.PtrToStringUni((IntPtr)cookedUrl.pQueryString, cookedUrl.QueryStringLength / 2);
            }
            m_Version = new Version(memoryBlob.RequestBlob->Version.MajorVersion, memoryBlob.RequestBlob->Version.MinorVersion);
            m_ClientCertState = ListenerClientCertState.NotInitialized;
            m_KeepAlive = null;
            GlobalLog.Print("HttpListenerContext#" + LoggingHash.HashString(this) + "::.ctor() RequestId:" + RequestId + " ConnectionId:" + m_ConnectionId + " RawConnectionId:" + memoryBlob.RequestBlob->RawConnectionId + " UrlContext:" + memoryBlob.RequestBlob->UrlContext + " RawUrl:" + m_RawUrl + " Version:" + m_Version.ToString() + " Secure:" + m_SslStatus.ToString());
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", "httpContext#" + LoggingHash.HashString(httpContext) + " RequestUri:" + LoggingHash.ObjectToString(RequestUri) + " Content-Length:" + LoggingHash.ObjectToString(ContentLength64) + " HTTP Method:" + LoggingHash.ObjectToString(HttpMethod));
            // Log headers
            if (NetEventSource.Log.IsEnabled())
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
                NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", sb.ToString());
            }
        }

        internal HttpListenerContext HttpListenerContext
        {
            get
            {
                return m_HttpContext;
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
                return m_MemoryBlob.RequestBuffer;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                CheckDisposed();
                return m_MemoryBlob.OriginalBlobAddress;
            }
        }

        // Use this to save the blob from dispose if this object was never used (never given to a user) and is about to be
        // disposed.
        internal void DetachBlob(RequestContextBase memoryBlob)
        {
            if (memoryBlob != null && (object)memoryBlob == (object)m_MemoryBlob)
            {
                m_MemoryBlob = null;
            }
        }

        // Finalizes ownership of the memory blob.  DetachBlob can't be called after this.
        internal void ReleasePins()
        {
            m_MemoryBlob.ReleasePins();
        }

        public Guid RequestTraceIdentifier
        {
            get
            {
                Guid guid = new Guid();
                *(1 + (ulong*)&guid) = RequestId;
                return guid;
            }
        }

        internal ulong RequestId
        {
            get
            {
                return m_RequestId;
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
                if (m_BoundaryType == BoundaryType.None)
                {
                    if (GetKnownHeader(HttpRequestHeader.TransferEncoding).Equals("chunked", StringComparison.OrdinalIgnoreCase))
                    {
                        m_BoundaryType = BoundaryType.Chunked;
                        m_ContentLength = -1;
                    }
                    else
                    {
                        m_ContentLength = 0;
                        m_BoundaryType = BoundaryType.ContentLength;
                        string length = GetKnownHeader(HttpRequestHeader.ContentLength);
                        if (length != null)
                        {
                            bool success = long.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out m_ContentLength);
                            if (!success)
                            {
                                m_ContentLength = 0;
                                m_BoundaryType = BoundaryType.Invalid;
                            }
                        }
                    }
                }
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ContentLength_get() returning m_ContentLength:" + m_ContentLength + " m_BoundaryType:" + m_BoundaryType);
                return m_ContentLength;
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
                if (m_WebHeaders == null)
                {
                    m_WebHeaders = Interop.HttpApi.GetHeaders(RequestBuffer, OriginalBlobAddress);
                }
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::Headers_get() returning#" + LoggingHash.HashString(m_WebHeaders));
                return m_WebHeaders;
            }
        }

        public string HttpMethod
        {
            get
            {
                if (m_HttpMethod == null)
                {
                    m_HttpMethod = Interop.HttpApi.GetVerb(RequestBuffer, OriginalBlobAddress);
                }
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::HttpMethod_get() returning m_HttpMethod:" + LoggingHash.ObjectToString(m_HttpMethod));
                return m_HttpMethod;
            }
        }

        public Stream InputStream
        {
            get
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "InputStream_get", "");
                if (m_RequestStream == null)
                {
                    m_RequestStream = HasEntityBody ? new HttpRequestStream(HttpListenerContext) : Stream.Null;
                }
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "InputStream_get", "");
                return m_RequestStream;
            }
        }

        // Requires ControlPrincipal permission if the request was authenticated with Negotiate, NTLM, or Digest.
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
                return m_SslStatus != SslStatus.Insecure;
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
                    if (string.Compare(upgrade, WebSocketHelpers.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase) == 0)
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
                return m_RawUrl;
            }
        }

        public string ServiceName
        {
            get { return m_ServiceName; }
            internal set { m_ServiceName = value; }
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
                if (m_ClientCertState == ListenerClientCertState.NotInitialized)
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "GetClientCertificate()/BeginGetClientCertificate()"));
                else if (m_ClientCertState == ListenerClientCertState.InProgress)
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcompletecall, "GetClientCertificate()/BeginGetClientCertificate()"));

                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ClientCertificateError_get() returning ClientCertificateError:" + LoggingHash.ObjectToString(m_ClientCertificateError));
                return m_ClientCertificateError;
            }
        }

        internal X509Certificate2 ClientCertificate
        {
            set
            {
                m_ClientCertificate = value;
            }
        }

        internal ListenerClientCertState ClientCertState
        {
            set
            {
                m_ClientCertState = value;
            }
        }

        internal void SetClientCertificateError(int clientCertificateError)
        {
            m_ClientCertificateError = clientCertificateError;
        }

        public X509Certificate2 GetClientCertificate()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "GetClientCertificate", "");
            try
            {
                ProcessClientCertificate();
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::GetClientCertificate() returning m_ClientCertificate:" + LoggingHash.ObjectToString(m_ClientCertificate));
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "GetClientCertificate", LoggingHash.ObjectToString(m_ClientCertificate));
            }
            return m_ClientCertificate;
        }

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "BeginGetClientCertificate", "");
            return AsyncProcessClientCertificate(requestCallback, state);
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "EndGetClientCertificate", "");
            X509Certificate2 clientCertificate = null;
            try
            {
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }
                ListenerClientCertAsyncResult clientCertAsyncResult = asyncResult as ListenerClientCertAsyncResult;
                if (clientCertAsyncResult == null || clientCertAsyncResult.AsyncObject != this)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, "asyncResult");
                }
                if (clientCertAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetClientCertificate"));
                }
                clientCertAsyncResult.EndCalled = true;
                clientCertificate = clientCertAsyncResult.InternalWaitForCompletion() as X509Certificate2;
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::EndGetClientCertificate() returning m_ClientCertificate:" + LoggingHash.ObjectToString(m_ClientCertificate));
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "EndGetClientCertificate", LoggingHash.HashString(clientCertificate));
            }
            return clientCertificate;
        }

        //************* Task-based async public methods *************************
        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
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
            GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ParseCookies() uri:" + uri + " setCookieHeader:" + setCookieHeader);
            CookieContainer container = new CookieContainer();
            container.SetCookies(uri, setCookieHeader);
            return container.GetCookies(uri);
        }

        public CookieCollection Cookies
        {
            get
            {
                if (m_Cookies == null)
                {
                    string cookieString = GetKnownHeader(HttpRequestHeader.Cookie);
                    if (cookieString != null && cookieString.Length > 0)
                    {
                        m_Cookies = ParseCookies(RequestUri, cookieString);
                    }
                    if (m_Cookies == null)
                    {
                        m_Cookies = new CookieCollection();
                    }
                }
                return m_Cookies;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return m_Version;
            }
        }

        public bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates m_BoundaryType
                return (ContentLength64 > 0 && m_BoundaryType == BoundaryType.ContentLength) ||
                    m_BoundaryType == BoundaryType.Chunked || m_BoundaryType == BoundaryType.Multipart;
            }
        }

        public bool KeepAlive
        {
            get
            {
                if (!m_KeepAlive.HasValue)
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
                            m_KeepAlive = true;
                        }
                        else
                        {
                            header = GetKnownHeader(HttpRequestHeader.KeepAlive);
                            m_KeepAlive = !string.IsNullOrEmpty(header);
                        }
                    }
                    else
                    {
                        header = header.ToLower(CultureInfo.InvariantCulture);
                        m_KeepAlive = header.IndexOf("close") < 0 || header.IndexOf("keep-alive") >= 0;
                    }
                }

                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::KeepAlive_get() returning:" + m_KeepAlive);
                return m_KeepAlive == true;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (m_RemoteEndPoint == null)
                {
                    m_RemoteEndPoint = Interop.HttpApi.GetRemoteEndPoint(RequestBuffer, OriginalBlobAddress);
                }
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::RemoteEndPoint_get() returning:" + m_RemoteEndPoint);
                return m_RemoteEndPoint;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (m_LocalEndPoint == null)
                {
                    m_LocalEndPoint = Interop.HttpApi.GetLocalEndPoint(RequestBuffer, OriginalBlobAddress);
                }
                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::LocalEndPoint_get() returning:" + m_LocalEndPoint);
                return m_LocalEndPoint;
            }
        }

        //should only be called from httplistenercontext
        internal void Close()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            RequestContextBase memoryBlob = m_MemoryBlob;
            if (memoryBlob != null)
            {
                memoryBlob.Close();
                m_MemoryBlob = null;
            }
            m_IsDisposed = true;
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
        }


        private ListenerClientCertAsyncResult AsyncProcessClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (m_ClientCertState == ListenerClientCertState.InProgress)
                throw new InvalidOperationException(SR.Format(SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
            m_ClientCertState = ListenerClientCertState.InProgress;
            HttpListenerContext.EnsureBoundHandle();

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
            //THE BUG HERE IS THAT PRIOR TO QFE 4796/DTS 609609, we call
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
            if (m_SslStatus != SslStatus.Insecure)
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
                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
                        uint bytesReceived = 0;

                        uint statusCode =
                            Interop.HttpApi.HttpReceiveClientCertificate(
                                HttpListenerContext.RequestQueueHandle,
                                m_ConnectionId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                                asyncResult.RequestBlob,
                                size,
                                &bytesReceived,
                                asyncResult.NativeOverlapped);

                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
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
                            // someother bad error, possible(?) return values are:
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
                    if (asyncResult != null)
                    {
                        asyncResult.InternalCleanup();
                    }
                    throw;
                }
            }
            else
            {
                asyncResult = new ListenerClientCertAsyncResult(this, state, requestCallback, 0);
                asyncResult.InvokeCallback();
            }
            return asyncResult;
        }

        private void ProcessClientCertificate()
        {
            if (m_ClientCertState == ListenerClientCertState.InProgress)
                throw new InvalidOperationException(SR.Format(SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
            m_ClientCertState = ListenerClientCertState.InProgress;
            GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate()");
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
            //THE BUG HERE IS THAT PRIOR TO QFE 4796/DTS 609609, we call
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
            if (m_SslStatus != SslStatus.Insecure)
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

                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() calling Interop.HttpApi.HttpReceiveClientCertificate size:" + size);
                        uint bytesReceived = 0;

                        uint statusCode =
                            Interop.HttpApi.HttpReceiveClientCertificate(
                                HttpListenerContext.RequestQueueHandle,
                                m_ConnectionId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                                pClientCertInfo,
                                size,
                                &bytesReceived,
                                null);

                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + statusCode + " bytesReceived:" + bytesReceived);
                        if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                        {
                            size = bytesReceived + pClientCertInfo->CertEncodedSize;
                            continue;
                        }
                        else if (statusCode == Interop.HttpApi.ERROR_SUCCESS)
                        {
                            if (pClientCertInfo != null)
                            {
                                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() pClientCertInfo:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo)
                                    + " pClientCertInfo->CertFlags:" + LoggingHash.ObjectToString(pClientCertInfo->CertFlags)
                                    + " pClientCertInfo->CertEncodedSize:" + LoggingHash.ObjectToString(pClientCertInfo->CertEncodedSize)
                                    + " pClientCertInfo->pCertEncoded:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo->pCertEncoded)
                                    + " pClientCertInfo->Token:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo->Token)
                                    + " pClientCertInfo->CertDeniedByMapper:" + LoggingHash.ObjectToString(pClientCertInfo->CertDeniedByMapper));
                                if (pClientCertInfo->pCertEncoded != null)
                                {
                                    try
                                    {
                                        byte[] certEncoded = new byte[pClientCertInfo->CertEncodedSize];
                                        Marshal.Copy((IntPtr)pClientCertInfo->pCertEncoded, certEncoded, 0, certEncoded.Length);
                                        m_ClientCertificate = new X509Certificate2(certEncoded);
                                    }
                                    catch (CryptographicException exception)
                                    {
                                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() caught CryptographicException in X509Certificate2..ctor():" + LoggingHash.ObjectToString(exception));
                                    }
                                    catch (SecurityException exception)
                                    {
                                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::ProcessClientCertificate() caught SecurityException in X509Certificate2..ctor():" + LoggingHash.ObjectToString(exception));
                                    }
                                }
                                m_ClientCertificateError = (int)pClientCertInfo->CertFlags;
                            }
                        }
                        else
                        {
                            Debug.Assert(statusCode == Interop.HttpApi.ERROR_NOT_FOUND, "HttpListenerRequest#{0}::ProcessClientCertificate()|Call to Interop.HttpApi.HttpReceiveClientCertificate() failed with statusCode {1}.", LoggingHash.HashString(this), statusCode);
                        }
                    }
                    break;
                }
            }
            m_ClientCertState = ListenerClientCertState.Completed;
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
                if (m_RequestUri == null)
                {

                    m_RequestUri = HttpListenerRequestUriBuilder.GetRequestUri(
                        m_RawUrl, RequestScheme, m_CookedUrlHost, m_CookedUrlPath, m_CookedUrlQuery);
                }

                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(this) + "::RequestUri_get() returning m_RequestUri:" + LoggingHash.ObjectToString(m_RequestUri));
                return m_RequestUri;
            }
        }

        private string GetKnownHeader(HttpRequestHeader header)
        {
            return Interop.HttpApi.GetKnownHeader(RequestBuffer, OriginalBlobAddress, (int)header);
        }

        internal ChannelBinding GetChannelBinding()
        {
            return HttpListenerContext.Listener.GetChannelBindingFromTls(m_ConnectionId);
        }

        internal void CheckDisposed()
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        // <BUGBUG>
        // THIS CODE IS STOLEN FROM System.Web!!!!
        // for the time being keep it here, but we need
        // to find a way to share this code before we ship!
        // </BUGBUG>
        static class Helpers
        {
            //
            // Get attribute off header value
            //
            internal static String GetAttributeFromHeader(String headerValue, String attrName)
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
                    if ((chPrev == ';' || chPrev == ',' || Char.IsWhiteSpace(chPrev)) && (chNext == '=' || Char.IsWhiteSpace(chNext)))
                        break;

                    i += k;
                }

                if (i < 0 || i >= l)
                    return null;

                // skip to '=' and the following whitespaces
                i += k;
                while (i < l && Char.IsWhiteSpace(headerValue[i]))
                    i++;
                if (i >= l || headerValue[i] != '=')
                    return null;
                i++;
                while (i < l && Char.IsWhiteSpace(headerValue[i]))
                    i++;
                if (i >= l)
                    return null;

                // parse the value
                String attrValue = null;

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

            internal static String[] ParseMultivalueHeader(String s)
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
                String[] strings;

                // if n is 0 that means s was empty string

                if (n == 0)
                {
                    strings = new String[1];
                    strings[0] = String.Empty;
                }
                else
                {
                    strings = new String[n];
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
                    // if there are no pending bytes treat 7 bit bytes as characters
                    // this optimization is temp disable as it doesn't work for some encodings
                    /*
                                    if (_numBytes == 0 && ((b & 0x80) == 0)) {
                                        AddChar((char)b);
                                    }
                                    else
                    */
                    {
                        if (_byteBuffer == null)
                            _byteBuffer = new byte[_bufferSize];

                        _byteBuffer[_numBytes++] = b;
                    }
                }

                internal String GetString()
                {
                    if (_numBytes > 0)
                        FlushBytes();

                    if (_numChars > 0)
                        return new String(_charBuffer, 0, _numChars);
                    else
                        return String.Empty;
                }
            }


            internal static void FillFromString(NameValueCollection nvc, String s, bool urlencoded, Encoding encoding)
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

                    String name = null;
                    String value = null;

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
